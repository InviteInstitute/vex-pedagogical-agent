"""Student question taxonomy (ported from VEX-Unity-Agent-Study-Integration).

A reactive student message is first classified into one of four question types; the
type's scaffolding guidance then steers WHAT the single grounded feedback call says,
while the feedback classes (feedback_policy) keep steering HOW it is said.

Scaffolding text is the study's, adapted to this pipeline: it names this prompt's input
sections, drops the game name (playground comes from catalogs), and drops length rules
(the main prompt's OUTPUT RULES own length).
"""
import re
from dataclasses import dataclass
from enum import Enum


class QuestionType(Enum):
    TASK_GOAL_UNDERSTANDING = 0
    ACTION_STRATEGY_SOLUTION_SUPPORT = 1
    DEBUGGING_PROBLEM_DIAGNOSIS = 2
    GENERAL_UNCLEAR_HELP_SEEKING = 3


@dataclass(frozen=True)
class QuestionTypeSpec:
    name: str
    definition: str
    inclusion: str
    exclusion: str
    scaffolding: str


_EVIDENCE = (
    "Use the Student message as the primary evidence. Use Recent chat, the student's "
    "current program, and What's happening now as supporting context. Treat any goal you "
    "infer as an estimate unless the student confirmed it. Do not invent missing information."
)

QUESTION_TYPE_SPECS: dict[QuestionType, QuestionTypeSpec] = {
    QuestionType.TASK_GOAL_UNDERSTANDING: QuestionTypeSpec(
        name="Task / Goal Understanding",
        definition="The student is asking about the meaning of the task or playground, rather than how to code something: what they are supposed to accomplish, what an object/location means, what the rules are, how scoring works, or what counts as success.",
        inclusion="Questions about the task goal, mission, scoring, rules, objects, or locations. For example: “what is my goal”; “what’s the main mission?”; “where is the lab”; “is the lab the green area i started in?”; “How many samples can you hold at once”; “Do you get more points if you place the craters on the right color?”",
        exclusion="How to code an action, choose a block, improve a strategy, or debug a failure. For example: “how do you tell the bot to lower its arms” → Action / Strategy / Solution Support; “why is my vehicle picking up the object but dropping right away” → Debugging / Problem Diagnosis; “Help” → General / Unclear Help-Seeking.",
        scaffolding=f"""{_EVIDENCE}
Move through these steps across turns:
1. Directly clarify the task fact, rule, object, location, requirement, or success condition the student asked about.
2. Connect the clarification to one immediate goal the student can work on. If that goal is unclear, ask one short goal question instead of assuming it.
3. Identify one observable sign that would show the immediate goal was achieved.
4. Ask one focused question that connects the clarified information to the student's next action.
Do not provide a complete program.""",
    ),
    QuestionType.ACTION_STRATEGY_SOLUTION_SUPPORT: QuestionTypeSpec(
        name="Action / Strategy / Solution Support",
        definition="The student is asking how to make the robot do something or how to build/improve a solution: which block to use, how to perform an action, what a block/code segment does, how to make the robot faster or more efficient, or asking for code.",
        inclusion="Which block to use, how to perform an action, what code/block does, how to improve speed/score/efficiency, or code/solution requests. For example: “Which block lets me raise the arms and lower the arms”; “How do i use a sensor block?”; “If i have collected my sample how do i deposit it at the lab”; “what the spin armMotor do”; “how do i make it faster”; “can you give me the code to do it.”",
        exclusion="A specific failure or unexpected behavior: “why is it going sidways” → Debugging / Problem Diagnosis. Vague help requests: “Help”; “idk” → General / Unclear Help-Seeking. Task/location/rule questions: “where is the lab” → Task / Goal Understanding.",
        scaffolding=f"""{_EVIDENCE}
First identify what the student is trying to accomplish. If the immediate goal is unclear, ask one short goal question and stop.
Then choose the least specific support that fits the evidence:
1. FIRST RELATED REQUEST: Give a conceptual cue. Explain the programming idea without giving a block sequence, and ask what the next block or pattern must accomplish.
2. AFTER ONE UNSUCCESSFUL RELEVANT ATTEMPT: Name the relevant block family or programming structure, explain why it fits, and give one local test.
3. AFTER REPEATED UNSUCCESSFUL RELEVANT ATTEMPTS: Give a partial sequence, a contrast between two choices, or a small incomplete structure. Leave a meaningful decision to the student.
Use Recent chat and What's happening now to decide whether an attempt occurred; if they don't show one, do not assume an unsuccessful attempt.
After progress, reduce support and ask the student to identify the next step. Never provide the complete program.""",
    ),
    QuestionType.DEBUGGING_PROBLEM_DIAGNOSIS: QuestionTypeSpec(
        name="Debugging / Problem Diagnosis",
        definition="The student reports that their robot, code, or current attempt is not working as expected: a bug, failure, unexpected behavior, or mismatch between intended and actual robot behavior.",
        inclusion="A specific problem, bug, failed attempt, unexpected behavior, or mismatch. For example: “That code does not work”; “that still does not work what can i do”; “why is my vehicle picking up the object but dropping right away”; “why is it going sidways”; “how come im getting stuck?”; “What if it is not releasing when spinning the armotor down”; “why does it do that.”",
        exclusion="Vague help without describing what is wrong: “Help”; “Idk” → General / Unclear Help-Seeking. Block/action requests without a failure: “which block should i use” → Action / Strategy / Solution Support. Task/goal/location questions: “where is the lab” → Task / Goal Understanding.",
        scaffolding="""Use the Student message and Recent chat as the primary evidence, and the student's current program and What's happening now as measured evidence. Never invent a goal, expected behavior, observed behavior, block, or log event.
Move through these steps across turns, skipping any the recent chat already settled:
1. GOAL: Identify what the student is trying to make the robot accomplish. If unclear, ask one short goal question and stop.
2. EXPECTED: Identify what the student expected at the point of failure.
3. ACTUAL: Restate what actually happened using only available evidence.
4. LOCALIZE: Focus on the first block, transition, condition, loop iteration, movement, or event where actual behavior differs from expected.
5. HYPOTHESIS: Offer one possible explanation supported by the evidence.
6. TEST: Ask the student to change or isolate one thing and observe one result.
7. FOLLOW-UP: Use the next reported result to support or reject the hypothesis before proposing another change.
Do not list many possible bugs or change several values at once. After repeated unsuccessful tests, narrow the code region or give one small incorrect-versus-correct contrast. When the problem is fixed, ask the student to explain why the change worked. Do not provide the complete program.""",
    ),
    QuestionType.GENERAL_UNCLEAR_HELP_SEEKING: QuestionTypeSpec(
        name="General / Unclear Help-Seeking",
        definition="Last resort: the message is too vague, short, off-task, or non-substantive to determine a specific need (generic help, “I don't know”, yes/no, unrelated answers, pushing the task back to the agent) with no goal, object, block, action, strategy, or failure named.",
        inclusion="Generic help requests, vague uncertainty, or non-substantive/off-task comments. For example: “Help”; “idk”; “I dont know”; “sure”; “Yes”; “2”; “orange”; “that’s your job buddy”; “figure it out”; “hello”; “what did you eat for dinner.”",
        exclusion="Any specific goal, object, location, rule, block, action, strategy, solution request, or failure. For example: “where is the lab” → Task / Goal Understanding; “how do i make it faster” → Action / Strategy / Solution Support; “why is it going sidways” → Debugging / Problem Diagnosis.",
        scaffolding="""There is not enough information to tell whether the student needs task clarification, action support, or debugging. Do not state an inferred goal as though the student confirmed it. Do not invent missing information.
Respond calmly and ask one bounded choice question that helps the student say whether they need: help understanding the task or goal; help deciding or coding the next action; or help fixing something that behaved differently than expected.
Do not combine several kinds of scaffolding. The next student message will be classified again.""",
    ),
}

DEFAULT_QUESTION_TYPE = QuestionType.GENERAL_UNCLEAR_HELP_SEEKING

CLASSIFIER_PROMPT_TEMPLATE = """Classify a middle school student's message to a VEXcode VR tutor into exactly one question type.

Question types:
{question_types}

Student message:
{student_message}

Output only the number of the question type (0, 1, 2, or 3). No other text."""


def build_classifier_prompt(student_message: str) -> str:
    question_types = "\n".join(
        f"{qt.value} = {spec.name}\n"
        f"  Definition: {spec.definition}\n"
        f"  Include: {spec.inclusion}\n"
        f"  Exclude: {spec.exclusion}"
        for qt, spec in QUESTION_TYPE_SPECS.items()
    )
    return CLASSIFIER_PROMPT_TEMPLATE.format(
        question_types=question_types, student_message=student_message
    )


_TYPE_DIGIT = re.compile(r"\b([0-3])\b")


def parse_question_type(raw_output: str | None) -> QuestionType:
    """First standalone 0-3 in the classifier output; the safe General type otherwise."""
    match = _TYPE_DIGIT.search(raw_output or "")
    return QuestionType(int(match.group(1))) if match else DEFAULT_QUESTION_TYPE
