FROM python:3.12-slim
WORKDIR /app
ENV PYTHONUNBUFFERED=1 PIP_NO_CACHE_DIR=1
COPY . ./
# The trigger/learner-model engine ships as a git submodule under vendor/; install
# it editable first so vex_agent can import learner_models / log_parser_delta_engine.
# The editable install keeps package __file__ at /app/vex_agent, so the bind-mounted
# invite_hub_sync_state.json (compose) lands where fetch_invite_hub_logs reads it.
RUN pip install -e ./vendor/agent-lm-packages && pip install -e .
# Kokoro TTS model for the Unity avatar (~350MB), baked in so audio works offline.
RUN python scripts/fetch_kokoro.py
EXPOSE 8000
# Trust X-Forwarded-Proto/For from any peer: compose publishes the API on 127.0.0.1
# only, so nginx is the one peer, and inside docker it arrives from the bridge
# gateway rather than 127.0.0.1 (uvicorn's default). Without this, URLs the API
# builds (the avatar's /tts link) come out http:// behind an https site.
CMD ["uvicorn", "vex_agent.app:app", "--host", "0.0.0.0", "--port", "8000", "--forwarded-allow-ips", "*"]
