# WebGL embed (Unity conversation agent)

This page hosts the Unity avatar in a browser and injects the backend config into the avatar. The
avatar then talks to the telemetry-grounded `vex_agent` backend.

## The WebGL build

The Unity project is `../unity/` (Editor **2022.3.40f1**). You can build it in two ways:

- Headless (output in `../unity/Builds/WebGL/`):
  ```bash
  Unity -batchmode -nographics -quit -projectPath ../unity -buildTarget WebGL \
        -executeMethod BuildScript.BuildWebGL -logFile -
  ```
- Editor: Open `../unity/` in Unity Hub. Then select **File ▸ Build Settings ▸ WebGL ▸ Build** and
  choose any folder.

For either way, install the output with `./install_build.sh <build-dir>`. Any build name is
accepted, because the script copies and renames the files into place. These paths must then exist
(git-ignored):

```
webgl/build_out/ai-conversation-agent/Build/ai-conversation-agent.loader.js
webgl/build_out/ai-conversation-agent/StreamingAssets/
```

## Run the MVP demo

1. Start the isolated DB and seed the fixture students. This step is idempotent:
   ```bash
   cd ../server && bash scripts/mvp_db.sh
   ```
2. Start the backend on port 8010 with that DB:
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

By default, `serverurl` is the site that serves the page. In production, nginx proxies the bridge
routes to the API. For this local demo, set `serverurl` explicitly:
`http://localhost:8080/webgl/?serverurl=http://127.0.0.1:8010`.

By default, `student_id` is `fixture_01_1` (a seeded fixture), and the demo is text only. To
override these values for a demo, use this URL:
`http://localhost:8080/webgl/?student_id=SOME_ID&serverurl=https://...&audio=true`

## How a turn flows

1. The student types in the Unity chat window. `ChatLLM` POSTs the serialized conversation,
   `student_id` and `audioFeedback` to `/generate-feedback-from-text`.
2. The bridge (`server/vex_agent/api/unity.py`) stores the newest student turn and classifies it
   into a question type. The bridge then runs the grounded feedback pipeline.
3. The reply has block names in `<b>` (TextMeshPro rich text). When audio is on, `response_audio`
   is a `/tts` URL. Unity plays that WAV with uLipSync visemes and beat gestures.

## Notes

- CORS: The serving origin must be in `BACKEND_CORS_ORIGINS` on the backend. Add
  `http://localhost:8080` and `http://127.0.0.1:8080`.
- Paradee-8M synthesizes the audio locally, inside the backend. Download the model once with
  `python server/scripts/fetch_tts_model.py` (into `server/models/`).
- The page injects the config with `unityInstance.SendMessage("EventSystem", "ReceiveConfigJson", …)`.
  `EventSystem` is the GameObject that holds `ChatLLM`.
- Voice input works in the backend (`/generate-feedback-from-voice`), but the Unity client does not
  record audio yet. Set `TRANSCRIBE_MODEL` before you enable voice input.
