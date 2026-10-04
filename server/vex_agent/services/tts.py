"""Local text-to-speech for the Unity avatar: Kokoro-82M via ONNX Runtime, in process.

No separate service and no network hop: the model loads once (warmed at boot by
app.warm_up) and speech is synthesized on the CPU. Measured on the dev VM (8-core
Haswell, AVX2): ~0.75s for a 7-word sentence, ~2.5s for a 27-word reply; synthesis
time grows with length. The fp32 model is used on purpose; the int8 one ran ~6x
slower there (no VNNI), fp16 was no faster.

The avatar can't start a clip until it has all of it, so a reply is spoken a
sentence at a time (speech_chunks): the first sentence plays while the next is
prepared. Synthesis runs as cached jobs, so a reply prepared when it was generated
(prepare) or a line many students hear (the greeting) is ready when asked for.

Model files come from scripts/fetch_kokoro.py (into server/models/, git-ignored).
"""

import io
import logging
import os
import re
import threading
import wave
from collections import OrderedDict
from concurrent.futures import Future, ThreadPoolExecutor
from functools import lru_cache
from pathlib import Path

import numpy as np

# kokoro-onnx's phonemizer warns "words count mismatch" on most punctuated text; it's
# benign (the phonemes are fine) and would otherwise log on every reply.
logging.getLogger("phonemizer").setLevel(logging.ERROR)

MODELS_DIR = Path(__file__).resolve().parents[2] / "models"
KOKORO_MODEL = os.getenv("KOKORO_MODEL", str(MODELS_DIR / "kokoro-v1.0.onnx"))
KOKORO_VOICES = os.getenv("KOKORO_VOICES", str(MODELS_DIR / "voices-v1.0.bin"))
# ponytail: voice and pace are calibration knobs for the study, not code.
TTS_VOICE = os.getenv("TTS_VOICE", "af_heart")
TTS_SPEED = float(os.getenv("TTS_SPEED", "1.0"))


@lru_cache(maxsize=1)
def _kokoro():
    from kokoro_onnx import Kokoro  # deferred: onnxruntime import is slow, tests stub this

    return Kokoro(KOKORO_MODEL, KOKORO_VOICES)


def to_wav(samples: np.ndarray, sample_rate: int) -> bytes:
    """Float samples in [-1, 1] -> 16-bit mono PCM WAV with correct header sizes."""
    pcm = (np.clip(samples, -1.0, 1.0) * 32767).astype("<i2").tobytes()
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(sample_rate)
        wav.writeframes(pcm)
    return buffer.getvalue()


_BLOCK_PLACEHOLDER = re.compile(r"\s*\[[^\]]*\]")
_SENTENCE_END = re.compile(r"(?<=[.!?])\s+")


def spoken_text(text: str) -> str:
    """What the avatar should say: replies name blocks as `drive [forward/reverse]
    for [number] [mm/inches]`; speech drops the backticks and the [placeholders]."""
    return _BLOCK_PLACEHOLDER.sub("", text).replace("`", "")


def speech_chunks(text: str) -> list[str]:
    """The reply as the avatar says it, a sentence per clip (spoken form)."""
    spoken = " ".join(spoken_text(text).split())
    return [sentence for sentence in _SENTENCE_END.split(spoken) if sentence]


# ponytail: one in-process LRU of synthesis jobs (finished or running), keyed by the
# spoken text; a process restart drops it, a shared cache only if this ever runs as
# several workers.
_JOB_CACHE_SIZE = 64
_jobs: OrderedDict[str, Future] = OrderedDict()
_jobs_lock = threading.Lock()
# Two at a time: a second student's reply doesn't wait for the first's, and ONNX
# Runtime already spreads each synthesis over the cores.
_pool = ThreadPoolExecutor(max_workers=2, thread_name_prefix="tts")


def _job(text: str) -> Future:
    with _jobs_lock:
        job = _jobs.get(text)
        if job is None or (job.done() and job.exception() is not None):
            job = _pool.submit(synthesize, text)
            _jobs[text] = job
        _jobs.move_to_end(text)
        while len(_jobs) > _JOB_CACHE_SIZE:
            _jobs.popitem(last=False)
        return job


def prepare(texts: list[str]) -> None:
    """Start synthesizing now, in order, so the audio is ready (or nearly) by the time
    the avatar asks for it."""
    for text in texts:
        _job(text)


def audio_for(text: str) -> bytes:
    """WAV for one spoken line: from the cache, joining a synthesis already running,
    or synthesized now. A failed synthesis is retried on the next request."""
    return _job(text).result()


def clear_cache() -> None:
    """Test hook."""
    with _jobs_lock:
        _jobs.clear()


def synthesize(text: str) -> bytes:
    samples, sample_rate = _kokoro().create(text, voice=TTS_VOICE, speed=TTS_SPEED, lang="en-us")
    return to_wav(samples, sample_rate)


def warm() -> None:
    """Load the model and run one reply-length inference at boot, so ONNX Runtime's
    first-run setup and buffer growth don't land on the first student's request
    (a short warm-up text left ~1.3s of that cost behind)."""
    synthesize(
        "Nice work getting the robot to the crater. Which block could lower the arm "
        "so it can scoop up the sample before driving back to the lab?"
    )
