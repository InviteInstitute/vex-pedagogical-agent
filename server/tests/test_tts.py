"""Local Kokoro TTS: the WAV container Unity decodes, the spoken sentences and the
cached synthesis jobs, plus one real synthesis when the model is downloaded
(scripts/fetch_kokoro.py)."""

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


def test_spoken_text_drops_block_markup():
    assert (
        tts.spoken_text(
            "Connect `drive [forward/reverse] for [number] [mm/inches]` under `when started`."
        )
        == "Connect drive for under when started."
    )


def test_speech_chunks_are_spoken_sentences():
    assert tts.speech_chunks(
        "Nice start!  Look at your `drive [forward/reverse]` block. Does it run?"
    ) == ["Nice start!", "Look at your drive block.", "Does it run?"]
    assert tts.speech_chunks("One line without an end") == ["One line without an end"]


def test_jobs_are_shared_cached_and_retried(monkeypatch):
    calls = []
    outcomes = iter([RuntimeError("model busy"), b"RIFF-a", b"RIFF-b"])

    def fake_synthesize(text):
        calls.append(text)
        outcome = next(outcomes)
        if isinstance(outcome, Exception):
            raise outcome
        return outcome

    monkeypatch.setattr(tts, "synthesize", fake_synthesize)

    # A failed synthesis surfaces, then the next request retries it.
    with pytest.raises(RuntimeError):
        tts.audio_for("Hello.")
    assert tts.audio_for("Hello.") == b"RIFF-a"

    # Prepared once, served from the cache after: no second synthesis.
    tts.prepare(["Try it."])
    assert tts.audio_for("Try it.") == b"RIFF-b"
    assert tts.audio_for("Try it.") == b"RIFF-b"
    assert calls == ["Hello.", "Hello.", "Try it."]
