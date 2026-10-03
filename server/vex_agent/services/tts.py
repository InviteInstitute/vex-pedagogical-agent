"""Local text-to-speech for the Unity avatar: Kokoro-82M via ONNX Runtime, in process.

No separate service and no network hop: the model loads once (warmed at boot by
app.warm_up) and each reply is synthesized on the CPU. Measured on the dev VM
(8-core Haswell, AVX2): ~3s for a full 40-word reply. The fp32 model is used on
purpose; the int8 one ran ~6x slower there (no VNNI), fp16 was no faster.

Model files come from scripts/fetch_kokoro.py (into server/models/, git-ignored).
"""
import io
import logging
import os
import wave
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


def synthesize(text: str) -> bytes:
    samples, sample_rate = _kokoro().create(
        text, voice=TTS_VOICE, speed=TTS_SPEED, lang="en-us"
    )
    return to_wav(samples, sample_rate)


def warm() -> None:
    """Load the model and run one reply-length inference at boot, so ONNX Runtime's
    first-run setup and buffer growth don't land on the first student's request
    (a short warm-up text left ~1.3s of that cost behind)."""
    synthesize(
        "Nice work getting the robot to the crater. Which block could lower the arm "
        "so it can scoop up the sample before driving back to the lab?"
    )
