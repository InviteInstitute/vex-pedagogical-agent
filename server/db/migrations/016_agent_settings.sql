BEGIN;

-- Agent settings a researcher chose on the research page, per student (canon_id).
-- Chat replies carry them on each request; the proactive daemon has no request, so
-- it reads them here and check-ins follow the same settings. No row = production.
CREATE TABLE IF NOT EXISTS chat.agent_settings (
    student_id TEXT PRIMARY KEY,
    settings JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMIT;
