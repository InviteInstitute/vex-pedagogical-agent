BEGIN;

-- Question type (study taxonomy) the classifier assigned to the student message this
-- assistant reply answers. NULL for proactive replies and pre-taxonomy rows.
ALTER TABLE chat.messages ADD COLUMN IF NOT EXISTS question_type TEXT;

COMMIT;
