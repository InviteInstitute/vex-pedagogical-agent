# VEX Pedagogical Agent

This project is a pedagogical AI agent for **VEXcode VR**, the block-based tool that middle school
students use to drive a virtual robot. The agent watches how the code of a student changes during
work. It builds a grounded picture of what the student is actually doing. It gives short, kind,
specific feedback when a student asks. It also gives feedback on its own when a student looks stuck.

An embodied 3D tutor (a Unity avatar) can say that feedback out loud, with lip sync. The system
synthesizes the speech locally.

```mermaid
flowchart LR
    student["Student coding<br/>in VEXcode VR"] --> hub[("Invite Institute Hub<br/>VEX event logs")]
    hub --> ingest["Ingest + parse<br/>into Postgres"]
    ingest --> agent["Feedback pipeline<br/>situation model + one LLM pass"]
    agent --> msg[("chat.messages")]
    msg -. "Server-Sent Events" .-> student
    agent -- "text + /tts speech" --> avatar["Unity avatar<br/>(WebGL, lip-synced)"]
    avatar --> student
```

> The full documentation is at <https://inviteinstitute.github.io/vex-pedagogical-agent/>.
> To read it locally on port 4100, run `mkdocs serve`.

## Quick Start

You need **Docker** with the Compose v2 plugin, and an LLM that the agent can call. In development,
the LLM can be a local [Ollama](https://ollama.com), so you need no cloud credentials to see the
agent work.

```bash
cp .env.example .env       # set POSTGRES_PASSWORD and point OPENAI_* at an LLM
make dev                   # (or: docker compose up --build) API on :8001, Postgres on :5433
```

`make` (or `make help`) lists the shared commands: `dev`, `test`, `lint`, `format`, `build` and
`deploy`. See the
[Development](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/development/) docs.

Then apply the schema and load a bundled fixture. The agent then has real telemetry to use:

```bash
export DATABASE_URL=postgresql://vexagent:$POSTGRES_PASSWORD@127.0.0.1:5433/vexagent
for f in server/db/migrations/*.sql; do psql "$DATABASE_URL" -f "$f"; done
vex-parse-logs --input server/tests/fixtures/raw_logs/01_error_flagging_a.ndjson --insert
```

The [Quickstart](https://inviteinstitute.github.io/vex-pedagogical-agent/quickstart/) docs have the
full walkthrough: dev and prod, the LLM setup, and how to run one feedback tick.

## What You Get

The agent talks to a student in two ways. **Both ways run the same feedback code**, so the pedagogy
is identical on each path:

- **Reactive**: A student types a question or taps Help. The agent grounds the reply in the current
  program of the student and answers.
- **Proactive**: A background daemon watches the event stream and measures how each run differs
  from the last run. The daemon detects behaviors (wheel-spinning, resilience, exploring,
  step-by-step, going idle). When a behavior fires, the daemon sends a short note. The student does
  not need to ask.

The agent first sorts each student question into one of four **question types**: task/goal,
action/strategy, debugging or unclear. The scaffolding for that type steers what the reply says.
The learner-state feedback classes steer how the reply says it.

Both ways use one deterministic **situation model** and a single LLM pass. The situation model is a
plain-language read of the session. It comes from telemetry, and the model does not guess it.

The trigger engine comes from
[agent-lm-packages](https://github.com/InviteInstitute/agent-lm-packages), a git submodule under
`server/vendor/`. [lm-dashboard](https://github.com/InviteInstitute/lm-dashboard) uses the same
engine, so the read of a student by the agent stays comparable to the read by the researcher
dashboard.

## Layout

| Path | What |
|---|---|
| `server/vex_agent/` | the FastAPI backend, in layers (`api`, `services`, `domain`, `data`, `ingest`, `triggers`) |
| `server/db/migrations/` | the schema, as ordered idempotent SQL. This is the source of truth. |
| `client/` | the React (Vite) chat client |
| `unity/` | the embodied 3D tutor (Unity 2022.3.40f1): chat window, lip-sync, gestures |
| `webgl/` | the page that hosts the Unity WebGL build and points it at the backend |
| `docs/` | the Material for MkDocs site |

## Serving It Remotely

Production runs the same `compose.yml`: Postgres and the API, with the proactive daemon in-process.
The API binds to `127.0.0.1:8001`, so **nginx** sits in front of it. Nginx serves the built client
from `client/dist` and proxies `/v1`, `/admin` and `/healthz`.

`make deploy` is the whole rollout:

1. `scripts/deploy.sh` guards against a dirty tree.
2. The script pulls the code.
3. The script rolls the stack.
4. The script applies the migrations.
5. The script gates on `/healthz`.
6. The rollout rebuilds the client.

See the [Deployment](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/deployment/)
docs.

## Under the Hood

Ingestion pulls VEX logs from the Hub incrementally, with a cursor in Postgres and idempotent
inserts. Ingestion parses the logs into `parsed_events`. The proactive daemon assumes a **single
writer** and is the only owner of that cursor.

The full documentation covers the architecture, the feedback pipeline, proactive triggers, the data
model, configuration and the API. It is at
<https://inviteinstitute.github.io/vex-pedagogical-agent/>.

## The Avatar

`unity/` talks to the backend through a small bridge (`server/vex_agent/api/unity.py`):

- `POST /generate-feedback-from-text` answers like the chat does. The `-voice` form transcribes
  first.
- `GET /tts` speaks the reply.

The speech model is Paradee-8M, a small distillation of Kokoro-82M. It runs in the backend process
on the CPU, with no GPU or API key. A full reply takes about 0.2s. Fetch the model once with
`python server/scripts/fetch_tts_model.py`.

To build and serve the avatar, see [webgl/README.md](webgl/README.md) and the
[Unity avatar](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/unity-avatar/) docs.

## License

[GNU Affero General Public License v3](LICENSE).
