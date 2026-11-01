"""Local Paradee TTS: the WAV container Unity decodes, the spoken sentences and the
cached synthesis jobs, plus one real synthesis when the model is downloaded
(scripts/fetch_tts_model.py)."""

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


@pytest.mark.skipif(not os.path.exists(tts.PARADEE_MODEL), reason="Paradee model not downloaded")
def test_real_synthesis_produces_speech_length_audio():
    with wave.open(io.BytesIO(tts.synthesize("Which block moves the arm?"))) as wav:
        seconds = wav.getnframes() / wav.getframerate()
    assert 0.8 < seconds < 5


def test_trim_silence_keeps_a_short_pause_around_speech():
    sr = 1000
    speech = np.full(500, 0.3, dtype=np.float32)
    clip = np.concatenate([np.zeros(300), speech, np.zeros(500)]).astype(np.float32)
    trimmed = tts.trim_silence(clip, sr)
    assert len(trimmed) == 50 + 500 + 120  # _KEEP_BEFORE / _KEEP_AFTER at 1 kHz
    assert np.abs(trimmed[:50]).max() == 0 and trimmed[50] == 0.3
    assert tts.trim_silence(np.zeros(10, dtype=np.float32), sr).size == 10  # all silent: untouched


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
