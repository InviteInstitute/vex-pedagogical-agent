"""Spoken questions (the chat's mic button): the recording is transcribed on the LLM
gateway behind the same bot gate and token budget as a reply. The gateway and the
budget's database lookups are stubbed."""

import pytest
from fastapi.testclient import TestClient

from vex_agent.api import students
from vex_agent.api.turnstile import COOKIE_NAME, sign_cookie
from vex_agent.llm import client as llm_client
from vex_agent.services import budget


@pytest.fixture
def api(monkeypatch):
    from vex_agent.app import app

    monkeypatch.setattr("vex_agent.app.warm_up", lambda: None)
    monkeypatch.setattr(budget, "get_llm_tokens_for_budget_key", lambda key: 0)
    monkeypatch.setattr(budget, "get_llm_tokens_last_day", lambda: 0)
    with TestClient(app) as client:
        client.cookies.set(COOKIE_NAME, sign_cookie())
        yield client


def post_audio(api, data=b"\x1aE\xdf\xa3webm"):
    return api.post(
        "/v1/students/s1/transcriptions",
        files={"audio": ("question.webm", data, "audio/webm")},
    )


def test_a_spoken_question_comes_back_as_text(api, monkeypatch):
    seen = {}

    def fake_transcribe(filename, audio):
        seen.update(filename=filename, audio=audio)
        return "why is my robot not moving"

    monkeypatch.setattr(students, "transcribe", fake_transcribe)
    response = post_audio(api)
    assert response.status_code == 200
    assert response.json() == {"text": "why is my robot not moving"}
    assert seen == {"filename": "question.webm", "audio": b"\x1aE\xdf\xa3webm"}


def test_empty_and_oversized_recordings_are_refused_before_the_gateway(api, monkeypatch):
    monkeypatch.setattr(students, "transcribe", lambda *a: pytest.fail("gateway called"))
    assert post_audio(api, b"").status_code == 400
    assert post_audio(api, b"x" * (students.MAX_AUDIO_BYTES + 1)).status_code == 413


def test_a_spent_budget_is_refused(api, monkeypatch):
    monkeypatch.setattr(budget, "get_llm_tokens_for_budget_key", lambda key: 10**9)
    monkeypatch.setattr(students, "transcribe", lambda *a: pytest.fail("gateway called"))
    assert post_audio(api).status_code == 429


def test_a_gateway_failure_reads_as_a_retry_hint(api, monkeypatch):
    def boom(*args):
        raise RuntimeError("gateway down")

    monkeypatch.setattr(students, "transcribe", boom)
    response = post_audio(api)
    assert response.status_code == 502
    assert "type your question" in response.json()["detail"]


def test_transcribe_calls_the_configured_model(monkeypatch):
    seen = {}

    class FakeTranscriptions:
        def create(self, *, model, file):
            seen.update(model=model, file=file)
            return type("R", (), {"text": "  hello there  "})()

    fake = type("C", (), {"audio": type("A", (), {"transcriptions": FakeTranscriptions()})()})()
    monkeypatch.setattr(llm_client, "get_openai_client", lambda: fake)
    monkeypatch.setenv("TRANSCRIBE_MODEL", "granite-speech-4.1-2b-plus")
    assert llm_client.transcribe("q.webm", b"abc") == "hello there"
    assert seen == {"model": "granite-speech-4.1-2b-plus", "file": ("q.webm", b"abc")}
