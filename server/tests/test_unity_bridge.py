"""Unity bridge contract: the flat form endpoints take the ChatWindow's serialized
Conversation, record the student turn, route it through the grounded pipeline, and
hand back a /tts URL only when audio is on. Driven through the real FastAPI app; the
pipeline, DB, and speech clients are stubbed."""

import json
from types import SimpleNamespace

from fastapi.testclient import TestClient

from vex_agent.api import unity
from vex_agent.app import app

CONVERSATION = json.dumps(
    {
        "id": "tab-1",
        "title": "Chat 1",
        "messages": [
            {"speaker": "you", "text": "how do i lift the arm", "isUser": True},
            {"speaker": "ai", "text": "Which block moves the arm?", "isUser": False},
            {"speaker": "you", "text": "why won't it move?", "isUser": True},
        ],
    }
)


def _stub_pipeline(monkeypatch, seen):
    def fake_create_message(*, student_id, payload):
        seen["stored"] = (student_id, payload.message)
        return SimpleNamespace(message_id="m1", session_id="sess-1", playground="GO-Mars")

    def fake_create_response(*, student_id, payload, request):
        seen["pipeline"] = (student_id, payload.student_message, payload.session_id)
        return SimpleNamespace(
            response_text="Which block runs first?",
            question_type="Debugging / Problem Diagnosis",
        )

    monkeypatch.setattr(unity, "create_message", fake_create_message)
    monkeypatch.setattr(unity, "create_response", fake_create_response)


def test_latest_student_text():
    assert unity.latest_student_text(CONVERSATION) == "why won't it move?"
    assert unity.latest_student_text("  plain text  ") == "plain text"
    assert unity.latest_student_text('{"messages": []}') == ""


def test_text_endpoint_without_audio(monkeypatch):
    seen = {}
    _stub_pipeline(monkeypatch, seen)
    reply = (
        TestClient(app)
        .post(
            "/generate-feedback-from-text",
            data={"input": CONVERSATION, "student_id": "s1", "audioFeedback": "false"},
        )
        .json()
    )

    # only the newest student turn is stored and answered, in the resolved session
    assert seen["stored"] == ("s1", "why won't it move?")
    assert seen["pipeline"] == ("s1", "why won't it move?", "sess-1")
    assert reply == {
        "response_text": "Which block runs first?",
        "response_audio": "",
        "question_type": "Debugging / Problem Diagnosis",
    }


def test_text_endpoint_with_audio_links_tts(monkeypatch):
    _stub_pipeline(monkeypatch, {})
    reply = (
        TestClient(app)
        .post(
            "/generate-feedback-from-text",
            data={"input": CONVERSATION, "student_id": "s1", "audioFeedback": "true"},
        )
        .json()
    )
    assert reply["response_audio"] == ("http://testserver/tts?text=Which+block+runs+first%3F")


def test_text_endpoint_rejects_missing_student_turn(monkeypatch):
    _stub_pipeline(monkeypatch, {})
    response = TestClient(app).post(
        "/generate-feedback-from-text",
        data={"input": '{"messages": []}', "student_id": "s1"},
    )
    assert response.status_code == 400


def test_voice_endpoint_transcribes_then_feeds_pipeline(monkeypatch):
    seen = {}
    _stub_pipeline(monkeypatch, seen)
    monkeypatch.setattr(unity, "transcribe", lambda filename, audio: "spoken words")
    reply = (
        TestClient(app)
        .post(
            "/generate-feedback-from-voice",
            data={"student_id": "s1"},
            files={"audiofile": ("a.wav", b"RIFF", "audio/wav")},
        )
        .json()
    )
    assert seen["pipeline"][1] == "spoken words"
    assert reply["response_audio"] == ""


def test_tts_speaks_the_spoken_form(monkeypatch):
    seen = {}

    def fake_synthesize(text):
        seen["text"] = text
        return b"RIFFwav"

    monkeypatch.setattr(unity.tts_service, "synthesize", fake_synthesize)
    response = TestClient(app).get("/tts", params={"text": "Use `turn [right/left]`."})
    assert response.status_code == 200
    assert response.headers["content-type"] == "audio/wav"
    assert response.content == b"RIFFwav"
    assert seen["text"] == "Use turn."


def test_tts_failure_is_500_and_text_is_bounded(monkeypatch):
    def boom(text):
        raise RuntimeError("model missing")

    monkeypatch.setattr(unity.tts_service, "synthesize", boom)
    client = TestClient(app)
    assert client.get("/tts", params={"text": "Hi"}).status_code == 500
    assert client.get("/tts", params={"text": "x" * 501}).status_code == 422


def test_display_text_bolds_block_names_for_unity():
    assert (
        unity.display_text("Use `turn [right/left]` next.") == "Use <b>turn [right/left]</b> next."
    )
