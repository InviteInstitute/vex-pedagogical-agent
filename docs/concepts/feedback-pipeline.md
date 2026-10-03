---
description: How one situation model, a question-type classifier, and a single feedback pass turn a session into one grounded reply, on both the reactive and proactive lanes.
---

# Feedback Pipeline

Every reply the agent gives runs through one function, `generate_feedback` in
`services/feedback.py`. A student who types a question and a daemon that fires a trigger
both land here, so the pedagogy is identical either way. This page walks the steps and
then shows what changes between the two lanes.

## The Steps

```mermaid
flowchart LR
    a["Resolve task<br/>+ blocks"] --> b["Fetch session<br/>events"]
    b --> c["Build situation<br/>model"]
    c --> d["Build current<br/>program"]
    d --> q["Classify the<br/>question"]
    q --> e["One feedback<br/>LLM pass"]
    e --> f["Sanitize and<br/>trim"]
```

1. **Resolve the task and blocks.** Given the playground the student is in, look up the
   task description and the list of blocks available for it (`domain/catalogs.py`).
   Unknown playgrounds fall back to a default.
2. **Fetch the session events.** Read the parsed VEX events for this student and session.
   The reactive lane already has them and passes them in to skip a second read.
3. **Build the situation model.** Turn the telemetry into a plain-language read of what
   is happening, computed deterministically in `domain/context_builder.py`. This is the
   grounding, and it is measured, not guessed.
4. **Build the current program.** Render the student's live workspace as readable
   pseudo-code, marking which blocks are `[Active]` and which are `[Orphaned]`
   (`log_parser_delta_engine/smart_delta.py` in the `agent-lm-packages` submodule). This
   replaced dumping raw logs at the model, which was the source of early hallucinations.
5. **Classify the question.** When there is a student message, a small LLM call sorts
   it into one of four question types (see [Question types](#question-types) below).
   Proactive turns have no message and skip this.
6. **Run one feedback pass.** Hand the model the task, the available blocks, the current
   program, the situation model, the recent chat, the question type with its scaffolding,
   and the feedback classes, and get back
   one short reply (`llm/client.py`). Some models reason before they answer and spend
   the whole answer budget doing it (an empty reply, a cut-off one, or reasoning left in
   the text). When that happens the call is retried once with `reasoning_effort: low` and
   a separate reasoning budget, and the model is remembered so later calls ask that way
   first. Models that answer directly never see the retry.
7. **Sanitize and trim.** Strip reasoning blocks and the label and quote leaks that small
   local models tend to emit (`llm/sanitizer.py`), then trim to bite size: the first
   sentence, plus a second when both fit 40 words, so "Nice start! Try X." keeps its
   hint. A single sentence up to 40 words stays whole; a longer one is cut at a clause
   break rather than mid-phrase.

## One Feedback Call, Grounded On Facts

The important choice here is what the pipeline does **not** do. It does not make a first
call to ask the model what the robot did and then a second call to write feedback about
it. That two-step shape is where a model invents a tidy story that never happened. Here
the grounding is a deterministic situation model over real events, and there is exactly
one feedback call on top of it. No raw logs, no paraphrase in the loop, nothing that can
quietly drift.

The question classifier is a different kind of call: it only labels the student's own
message with one digit and never describes the session, so it adds nothing the feedback
call could be grounded on.

## Question Types

The four types come from the VEX-Unity-Agent-Study-Integration study and live in
`domain/question_types.py`:

| # | Type | The student is asking |
|---|---|---|
| 0 | Task / Goal Understanding | what the task, a rule, an object, or success means |
| 1 | Action / Strategy / Solution Support | which block or approach does something |
| 2 | Debugging / Problem Diagnosis | why their robot or code misbehaves |
| 3 | General / Unclear Help-Seeking | something too vague to tell (also the fallback) |

Each type carries scaffolding guidance, for example debugging walks goal, expected,
actual, localize, hypothesis, test across turns. The scaffolding decides **what** the
reply says next, and the feedback classes decide **how** it says it. Since a reply is at
most two short sentences, the model delivers only the next step the recent chat hasn't
covered. The classifier runs on the same model as the reply (a researcher's pick
included) and its tokens count toward the [session budget](../guides/configuration.md#token-budgets).
The type is stored in `chat.messages.question_type` and returned as `question_type`.

## What Differs Between The Lanes

The steps are the same. Only the inputs change.

| Input | Reactive | Proactive |
|---|---|---|
| `student_message` | the student's real message | empty, there is no student turn |
| `behavior_fact` | none | a neutral fact about the behavior that fired |
| `feedback_classes` | decided from the session snapshot | decided from the trigger |

The `behavior_fact` is worth calling out. When a trigger fires, the pipeline appends a
**neutral, measured statement** of what happened to the situation model, never the
internal trigger label. The model is told something like the student re-ran the same code
several times, not the word wheel-spinning. A label-only prompt hallucinated in early
testing, so the design feeds facts and lets the reply stay grounded.

## Where It Goes

The generated reply is saved to `chat.messages` and delivered to the browser over
Server-Sent Events. Proactive replies are saved with `origin = 'proactive'` so they can
be told apart from answers to a typed question, though the text itself came through the
same pipeline. For the behaviors that start a proactive reply, see
[proactive triggers](proactive-triggers.md).
