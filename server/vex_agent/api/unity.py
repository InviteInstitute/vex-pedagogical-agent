"""Unity conversation-agent bridge.

Adapts the Unity client's flat form contract (`input` + `student_id` +
`audioFeedback` -> `{response_text, response_audio}`) onto the telemetry-grounded
`/v1` feedback pipeline, so the 3D avatar gets the same grounded feedback as the
React client, and speaks it via `/tts` when audio is on.

`input` is the Unity ChatWindow's serialized Conversation
(`{"id", "title", "messages": [{"speaker", "text", "isUser"}]}`); plain text is
accepted too. Only the newest student turn is used: the backend keeps its own
chat history per session.

ponytail: unauthenticated, same as the original Flask `app.py` bridge it
replaces. The `/v1/*` routes are Turnstile-gated; these are not. Put behind
auth / a gateway before exposing publicly.
"""

import json
import logging
import os
import re
from typing import Annotated

from fastapi import APIRouter, File, Form, HTTPException, Query, Request, UploadFile
from fastapi.responses import Response

from vex_agent.api.schemas import MessageRequest, StudentResponseRequest
from vex_agent.api.students import create_message, create_response
from vex_agent.llm.client import get_openai_client
from vex_agent.services import tts as tts_service

router = APIRouter(tags=["unity"])
logger = logging.getLogger(__name__)

# ponytail: the transcription model is a calibration knob; each LLM provider
# exposes its own (e.g. granite-speech-4.1-2b-plus on NCSA Lumen).
TRANSCRIBE_MODEL = os.getenv("TRANSCRIBE_MODEL", "gpt-4o-transcribe")
# Replies are capped at 40 words by the LLM client; this only bounds abuse of the open endpoint.
TTS_MAX_CHARS = 500
_BLOCK_PLACEHOLDER = re.compile(r"\s*\[[^\]]*\]")
_BACKTICKED = re.compile(r"`([^`]+)`")


def display_text(text: str) -> str:
    """Replies wrap block names in backticks (the React client renders them as
    chips); the Unity bubble is TextMeshPro rich text, so bold them instead."""
    return _BACKTICKED.sub(r"<b>\1</b>", text)


def spoken_text(text: str) -> str:
    """What the avatar should say: replies name blocks as `drive [forward/reverse]
    for [number] [mm/inches]`; speech drops the backticks and the [placeholders]."""
    return _BLOCK_PLACEHOLDER.sub("", text).replace("`", "")


def latest_student_text(conversation_input: str) -> str:
    """Newest student turn from a serialized Unity Conversation, else the raw text."""
    try:
        conversation = json.loads(conversation_input)
    except json.JSONDecodeError:
        return conversation_input.strip()
    if not isinstance(conversation, dict):
        return conversation_input.strip()
    for message in reversed(conversation.get("messages") or []):
        if message.get("isUser"):
            return (message.get("text") or "").strip()
    return ""


def _feedback(request: Request, student_id: str, text: str, audio_feedback: bool) -> dict:
    """Record the student turn, run the grounded pipeline, shape the reply for Unity."""
    if not text:
        raise HTTPException(status_code=400, detail="No student message in input.")
    # Store the student turn first, as the React client does via /v1/messages, so
    # the next reply's recent chat (and scaffolding progression) sees it.
    message = create_message(student_id=student_id, payload=MessageRequest(message=text))
    result = create_response(
        student_id=student_id,
        request=request,
        payload=StudentResponseRequest(
            message_id=message.message_id,
            session_id=message.session_id,
            playground=message.playground,
            student_message=text,
        ),
    )
    response_audio = ""
    if audio_feedback and result.response_text:
        response_audio = str(request.url_for("tts").include_query_params(text=result.response_text))
    return {
        "response_text": display_text(result.response_text),
        "response_audio": response_audio,
        "question_type": result.question_type,
    }


@router.post("/generate-feedback-from-text")
def generate_feedback_from_text(
    request: Request,
    input: Annotated[str, Form()],
    student_id: Annotated[str, Form()],
    audioFeedback: Annotated[bool, Form()] = False,
) -> dict:
    return _feedback(request, student_id, latest_student_text(input), audioFeedback)


@router.post("/generate-feedback-from-voice")
def generate_feedback_from_voice(
    request: Request,
    student_id: Annotated[str, Form()],
    audiofile: Annotated[UploadFile, File()],
    audioFeedback: Annotated[bool, Form()] = False,
) -> dict:
    transcript = get_openai_client().audio.transcriptions.create(
        model=TRANSCRIBE_MODEL,
        file=(audiofile.filename or "audio.wav", audiofile.file.read()),
    )
    return _feedback(request, student_id, transcript.text.strip(), audioFeedback)


@router.get("/tts", name="tts")
def tts(text: Annotated[str, Query(min_length=1, max_length=TTS_MAX_CHARS)]) -> Response:
    """WAV speech for one reply, synthesized locally by Kokoro. Unity loads it with
    AudioType.WAV and lip-syncs to it. Whole-clip rather than streamed: Unity plays
    a clip only once fully downloaded, and a failure surfaces as a 500, not a
    truncated clip."""
    try:
        audio = tts_service.synthesize(spoken_text(text))
    except Exception as error:
        logger.exception("TTS generation failed")
        raise HTTPException(status_code=500, detail=f"TTS generation failed: {error}") from error
    return Response(content=audio, media_type="audio/wav")
