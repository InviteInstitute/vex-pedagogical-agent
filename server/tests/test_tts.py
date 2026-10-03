"""Local Kokoro TTS: the WAV container Unity decodes, plus one real synthesis when
the model is downloaded (scripts/fetch_kokoro.py)."""
import io
import os
import wave

import numpy as np
import pytest

from vex_agent.services import tts


def test_to_wav_writes_a_complete_header():
    samples = np.array([0.0, 0.5, -0.5, 2.0], dtype=np.float32)  # 2.0 must clip
    with wave.open(io.BytesIO(tts.to_wav(samples, 24000))) as wav:
        assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, 24000)
        assert wav.getnframes() == 4  # header sizes match the data (Unity relies on it)
        pcm = np.frombuffer(wav.readframes(4), dtype="<i2")
    assert pcm.tolist() == [0, 16383, -16383, 32767]


@pytest.mark.skipif(not os.path.exists(tts.KOKORO_MODEL), reason="Kokoro model not downloaded")
def test_real_synthesis_produces_speech_length_audio():
    with wave.open(io.BytesIO(tts.synthesize("Which block moves the arm?"))) as wav:
        seconds = wav.getnframes() / wav.getframerate()
    assert 0.8 < seconds < 5
