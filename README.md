# VEX Pedagogical Agent

A pedagogical AI agent for **VEXcode VR**, the block-based tool middle schoolers use to
drive a virtual robot. It watches how a student's code changes as they work, builds a
grounded picture of what they are actually doing, and gives short, kind, specific
feedback - both when a student asks and, on its own, when a student looks stuck. An
embodied 3D tutor (a Unity avatar) can deliver that feedback out loud, lip-synced, with
speech synthesized locally.

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

> Full documentation is published at <https://inviteinstitute.github.io/vex-pedagogical-agent/>
> (or run `mkdocs serve` to read it locally on port 4100).

## Quick Start

All you need is **Docker** with the Compose v2 plugin, and an LLM the agent can call -
in development that can be a local [Ollama](https://ollama.com), so no cloud credentials
are needed to see it work.

```bash
cp .env.example .env       # set POSTGRES_PASSWORD and point OPENAI_* at an LLM
make dev                   # (or: docker compose up --build) API on :8001, Postgres on :5433
```

`make` (or `make help`) lists the shared command vocabulary - `dev`, `test`, `lint`,
`format`, `build`, `deploy`. See the [Development](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/development/) docs.

Then apply the schema and load a bundled fixture so there's real telemetry to ground on:

```bash
export DATABASE_URL=postgresql://vexagent:$POSTGRES_PASSWORD@127.0.0.1:5433/vexagent
for f in server/db/migrations/*.sql; do psql "$DATABASE_URL" -f "$f"; done
vex-parse-logs --input server/tests/fixtures/raw_logs/01_error_flagging_a.ndjson --insert
```

The full walkthrough (dev vs prod, the LLM setup, running one feedback tick) is in the
[Quickstart](https://inviteinstitute.github.io/vex-pedagogical-agent/quickstart/) docs.

## What You Get

The agent talks to a student in two ways, and **both run the same feedback code**, so the
pedagogy is identical on either path:

- **Reactive** - a student types a question or taps Help. The agent grounds the reply in
  their current program and answers.
- **Proactive** - a background daemon watches the event stream, measures how each run
  differs from the last, and detects behaviors (wheel-spinning, resilience, exploring,
  step-by-step, going idle). When one fires it pushes a short note without being asked.

Every student question is first sorted into one of four **question types** (task/goal,
action/strategy, debugging, unclear), and that type's scaffolding steers what the reply
says, while the learner-state feedback classes steer how it says it.

Under both is one deterministic **situation model** - a plain-language read of the session
built from telemetry, not guessed by the model - plus a single LLM pass. The trigger
engine is vendored from [lm-dashboard](https://github.com/InviteInstitute/lm-dashboard) so
the agent's read of a student stays comparable to the researcher dashboard's.

## Layout

| Path | What |
|---|---|
| `server/vex_agent/` | the FastAPI backend, in layers (`api`, `services`, `domain`, `data`, `ingest`, `triggers`) |
| `server/db/migrations/` | the schema, as ordered idempotent SQL - the source of truth |
| `client/` | the React (Vite) chat client |
| `unity/` | the embodied 3D tutor (Unity 2022.3.40f1): chat window, lip-sync, gestures |
| `webgl/` | the page that hosts the Unity WebGL build and points it at the backend |
| `docs/` | the Material for MkDocs site |

## Serving It Remotely

Production runs the same `compose.yml` (Postgres + the API, with the proactive daemon
in-process). The API binds to `127.0.0.1:8001` so **nginx** sits in front of it, serving
the built client from `client/dist` and proxying `/v1`, `/admin`, and `/healthz`.
`make deploy` is the whole rollout: `scripts/deploy.sh` guards a dirty tree, pulls, rolls
the stack, applies the migrations, and gates on `/healthz`, then the client is rebuilt.
See the
[Deployment](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/deployment/)
docs.

## Under the Hood

Ingestion pulls VEX logs from the Hub incrementally (a cursor in Postgres, idempotent
inserts) and parses them into `parsed_events`. The proactive daemon assumes a **single
writer** and is the sole owner of that cursor. The full write-up - architecture, the
feedback pipeline, proactive triggers, the data model, configuration, and the API - lives
at <https://inviteinstitute.github.io/vex-pedagogical-agent/>.

## The Avatar

`unity/` talks to the backend through a small bridge (`server/vex_agent/api/unity.py`):
`POST /generate-feedback-from-text` (and `-voice`, which transcribes first) answers like
the chat does, and `GET /tts` speaks the reply. Speech is Kokoro-82M running in the
backend process on the CPU (about 3s for a full reply, no GPU or API key); fetch the model
once with `python server/scripts/fetch_kokoro.py`. Building and serving the avatar is in
[webgl/README.md](webgl/README.md) and the
[Unity avatar](https://inviteinstitute.github.io/vex-pedagogical-agent/guides/unity-avatar/) docs.

## License

[GNU Affero General Public License v3](LICENSE).
