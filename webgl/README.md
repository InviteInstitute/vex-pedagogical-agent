# WebGL embed (Unity conversation agent)

Hosts the Unity avatar in a browser and injects the backend config into it, so
the avatar talks to the telemetry-grounded `vex_agent` backend.

## The WebGL build

The Unity project is `../unity/` (Editor **2022.3.40f1**). Build it either way:

- Headless:
  ```bash
  Unity -batchmode -nographics -quit -projectPath ../unity \
        -executeMethod BuildScript.BuildWebGL -logFile -
  ```
- Editor: open `../unity/` in Unity Hub, then **File ▸ Build Settings ▸ WebGL ▸ Build**
  into `webgl/build_out/ai-conversation-agent/`.

Either way, install the output with `./install_build.sh <build-dir>` (any build name;
it copies and renames the files into place). This must then exist (git-ignored):

```
webgl/build_out/ai-conversation-agent/Build/ai-conversation-agent.loader.js
webgl/build_out/ai-conversation-agent/StreamingAssets/
```

If your build name differs, set `BUILD_NAME` at the top of `index.html`.

## Run the MVP demo

1. Start the isolated DB and seed the fixture students (idempotent):
   ```bash
   cd ../server && bash scripts/mvp_db.sh
   ```
2. Start the backend on port 8010 against that DB:
   ```bash
   cd ../server
   DATABASE_URL="postgresql://postgres@127.0.0.1:5544/vexagent" TRIGGER_DAEMON_ENABLED=false \
     uvicorn vex_agent.app:app --port 8010
   ```
3. Serve this page and open it:
   ```bash
   cd ..   # repo root, so webgl/ is under the server root
   python3 -m http.server 8080
   # open http://localhost:8080/webgl/
   ```

`serverurl` defaults to the site serving the page (in production nginx proxies the
bridge routes to the API), so for this local demo pass it explicitly:
`http://localhost:8080/webgl/?serverurl=http://127.0.0.1:8010`. `student_id` defaults
to `fixture_01_1` (a seeded fixture), text only. Override per demo:
`http://localhost:8080/webgl/?student_id=SOME_ID&serverurl=https://...&audio=true`

## How a turn flows

1. The student types in the Unity chat window. `ChatLLM` POSTs the serialized
   conversation, `student_id` and `audioFeedback` to `/generate-feedback-from-text`.
2. The bridge (`server/vex_agent/api/unity.py`) stores the newest student turn,
   classifies it into a question type, and runs the grounded feedback pipeline.
3. The reply comes back with block names in `<b>` (TextMeshPro rich text). When
   audio is on, `response_audio` is a `/tts` URL. Unity plays that WAV with
   uLipSync visemes and beat gestures.

## Notes

- CORS: the serving origin must be in the backend's `BACKEND_CORS_ORIGINS`
  (add `http://localhost:8080` / `http://127.0.0.1:8080`).
- Audio is synthesized locally by Kokoro inside the backend. Download the model once:
  `python server/scripts/fetch_kokoro.py` (about 350MB into `server/models/`).
- The config is injected via `unityInstance.SendMessage("EventSystem",
  "ReceiveConfigJson", …)`. `EventSystem` is the GameObject holding `ChatLLM`.
- Voice input works in the backend (`/generate-feedback-from-voice`) but the
  Unity client doesn't record audio yet. Set `TRANSCRIBE_MODEL` before enabling it.
