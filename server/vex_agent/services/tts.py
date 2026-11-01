"""Local text-to-speech for the Unity avatar: Paradee-8M (an 8M-parameter distillation
of Kokoro-82M) via ONNX Runtime, in process.

No separate service and no network hop: the model loads once (warmed at boot by
app.warm_up) and speech is synthesized on the CPU. Measured on the dev VM (8-core
Haswell, AVX2, 4 threads): ~0.07s for an 8-word sentence, ~0.2s for a 27-word reply,
about 10x faster than Kokoro-82M was there. The int8 file runs as fast as the fp32
one on that CPU, so the 8.5 MB int8 file is used.

The avatar can't start a clip until it has all of it, so a reply is spoken a
sentence at a time (speech_chunks): the first sentence plays while the next is
prepared. Synthesis runs as cached jobs, so a reply prepared when it was generated
(prepare) or a line many students hear (the greeting) is ready when asked for.

Model files come from scripts/fetch_tts_model.py (into server/models/, git-ignored).
"""

import io
import os
import re
import threading
import wave
from collections import OrderedDict
from concurrent.futures import Future, ThreadPoolExecutor
from functools import lru_cache
from pathlib import Path

import numpy as np

MODELS_DIR = Path(__file__).resolve().parents[2] / "models"
PARADEE_MODEL = os.getenv("PARADEE_MODEL", str(MODELS_DIR / "paradee_int8.onnx"))
PARADEE_CONFIG = os.getenv("PARADEE_CONFIG", str(MODELS_DIR / "paradee_config.json"))
# ponytail: voice and pace are calibration knobs for the study, not code. Paradee 1.2
# voices: af_heart, am_puck (American), bf_isabella, bm_fable (British).
TTS_VOICE = os.getenv("TTS_VOICE", "af_heart")
TTS_SPEED = float(os.getenv("TTS_SPEED", "1.0"))
# Paradee pads each clip with ~0.3s of silence before and ~0.5s after; the avatar plays
# a reply a sentence per clip, so that would put ~0.8s gaps between sentences. Clips are
# trimmed to Kokoro's padding (~0.05s / ~0.12s).
_SILENCE = 0.01  # |sample| below this counts as silence
_KEEP_BEFORE, _KEEP_AFTER = 0.05, 0.12  # seconds of silence kept around the speech


@lru_cache(maxsize=1)
def _paradee():
    from paradee import Paradee  # deferred: onnxruntime/spaCy import is slow, tests stub this

    return Paradee(model_path=PARADEE_MODEL, config_path=PARADEE_CONFIG, threads=4)


def trim_silence(samples: np.ndarray, sample_rate: int) -> np.ndarray:
    """Cut the model's long lead-in and tail down to a short natural pause."""
    loud = np.flatnonzero(np.abs(samples) > _SILENCE)
    if not loud.size:
        return samples
    start = max(0, loud[0] - int(_KEEP_BEFORE * sample_rate))
    return samples[start : loud[-1] + 1 + int(_KEEP_AFTER * sample_rate)]


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
# One at a time: Paradee's G2P (spaCy, espeak) isn't thread-safe, and a reply takes
# ~0.2s, so a second student's reply waits at most that long behind the first's.
# ponytail: one worker; a pool of Paradee instances if a full class queues up.
_pool = ThreadPoolExecutor(max_workers=1, thread_name_prefix="tts")


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
    from paradee import SAMPLE_RATE

    samples = _paradee()(text, speed=TTS_SPEED, voice=TTS_VOICE)
    return to_wav(trim_silence(samples, SAMPLE_RATE), SAMPLE_RATE)


def warm() -> None:
    """Load the model and run one reply-length inference at boot, so ONNX Runtime's
    first-run setup and buffer growth don't land on the first student's request
    (a short warm-up text leaves part of that cost behind)."""
    synthesize(
        "Nice work getting the robot to the crater. Which block could lower the arm "
        "so it can scoop up the sample before driving back to the lab?"
    )
