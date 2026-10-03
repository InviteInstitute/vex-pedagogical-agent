"""Question taxonomy ported from the study repo: classifier output parsing (falls back
to the safe General type) and the scaffolding reaching the main feedback prompt."""

from vex_agent.domain.context_builder import (
    NO_QUESTION_TYPE,
    build_feedback_prompt_from_classes,
)
from vex_agent.domain.feedback_policy import FeedbackClass
from vex_agent.domain.question_types import (
    QUESTION_TYPE_SPECS,
    QuestionType,
    build_classifier_prompt,
    parse_question_type,
)
from vex_agent.llm import client as ls


def test_parse_question_type():
    assert parse_question_type("2") is QuestionType.DEBUGGING_PROBLEM_DIAGNOSIS
    assert parse_question_type(" Type 0\n") is QuestionType.TASK_GOAL_UNDERSTANDING
    # unparseable / out of range / empty -> General (asks the student to narrow down)
    for raw in ("", None, "maybe", "7"):
        assert parse_question_type(raw) is QuestionType.GENERAL_UNCLEAR_HELP_SEEKING


def test_classifier_prompt_lists_every_type_and_the_message():
    prompt = build_classifier_prompt("why is it going sidways")
    assert "why is it going sidways" in prompt
    for qt, spec in QUESTION_TYPE_SPECS.items():
        assert f"{qt.value} = {spec.name}" in prompt


def test_classify_question_runs_classifier_prompt(monkeypatch):
    seen = {}

    def fake_execute(*, model, prompt, **kwargs):
        seen["prompt"] = prompt
        return "1"

    monkeypatch.setattr(ls, "execute_prompt", fake_execute)
    assert ls.classify_question("which block lifts the arm") is (
        QuestionType.ACTION_STRATEGY_SOLUTION_SUPPORT
    )
    assert "which block lifts the arm" in seen["prompt"]


def _prompt(question_type):
    return build_feedback_prompt_from_classes(
        task="t",
        student_message="m",
        available_blocks=["drive"],
        current_program="p",
        situation="s",
        recent_messages=[],
        feedback_classes={FeedbackClass.NUDGE},
        question_type=question_type,
    )


def test_feedback_prompt_carries_scaffolding_for_the_type():
    spec = QUESTION_TYPE_SPECS[QuestionType.DEBUGGING_PROBLEM_DIAGNOSIS]
    prompt = _prompt(QuestionType.DEBUGGING_PROBLEM_DIAGNOSIS)
    assert spec.name in prompt and spec.scaffolding in prompt
    # proactive (no question) still renders cleanly
    assert NO_QUESTION_TYPE in _prompt(None)


def test_scaffolding_is_game_agnostic():
    # the study prompts named Castle Crasher+; the playground comes from catalogs here
    for spec in QUESTION_TYPE_SPECS.values():
        assert "Castle" not in spec.scaffolding
