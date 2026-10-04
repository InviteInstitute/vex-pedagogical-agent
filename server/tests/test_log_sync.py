"""Covers the incremental-sync cursor logic (the lm-dashboard dateFrom port): the
fetch sends dateFrom, sync state round-trips the timestamp cursor, and the timestamp
parser tolerates prod's misspelling. Pure -- request_json is monkeypatched, no network."""

from vex_agent.ingest import fetch_invite_hub_logs as fh


def test_incremental_fetch_passes_datefrom(monkeypatch):
    seen = {}

    def fake_request_json(url, token=None):
        seen["url"] = url
        return {"results": []}  # empty -> loop returns immediately

    monkeypatch.setattr(fh, "request_json", fake_request_json)
    fh.fetch_vex_logs_incremental(
        "http://h",
        "t",
        "",
        page_size=500,
        last_source_log_id=10,
        date_from="2026-07-21T00:00:00+00:00",
    )
    assert "dateFrom=" in seen["url"]


def test_incremental_fetch_omits_datefrom_when_none(monkeypatch):
    seen = {}
    monkeypatch.setattr(
        fh, "request_json", lambda url, token=None: seen.update(url=url) or {"results": []}
    )
    fh.fetch_vex_logs_incremental("http://h", "t", "", page_size=500, last_source_log_id=None)
    assert "dateFrom=" not in seen["url"]


def test_sync_state_roundtrips_event_time(tmp_path):
    p = tmp_path / "state.json"
    fh.write_sync_state(p, 42, "2026-07-21T18:00:00+00:00")
    state = fh.read_sync_state(p)
    assert state["last_source_log_id"] == 42
    assert state["last_event_time"] == "2026-07-21T18:00:00+00:00"


def test_parse_event_time_tolerates_misspelling_and_z():
    assert fh.parse_event_time({"recieved_at": "2026-07-21T18:00:00Z"}).tzinfo is not None
    assert fh.parse_event_time({"received_at": "2026-07-21T18:00:00+00:00"}) is not None
    assert fh.parse_event_time({}) is None
    assert fh.parse_event_time({"recieved_at": "not-a-date"}) is None


def test_fresh_database_syncs_only_the_recency_window(monkeypatch):
    """No cursor yet: the full sync starts at the daemon's recency window instead of
    draining the Hub's whole history, which never finished between restarts."""
    from datetime import UTC, datetime, timedelta

    from vex_agent.services import logsync

    seen = {}

    def fake_fetch(base_url, token, query_string, **kwargs):
        seen.update(kwargs)
        return []

    monkeypatch.setenv("TRIGGER_STUDENT_RECENCY_HOURS", "6")
    monkeypatch.setattr(logsync, "get_auth_token", lambda base_url: "t")
    monkeypatch.setattr(logsync, "get_ingest_cursor", lambda: {})
    monkeypatch.setattr(logsync, "fetch_vex_logs_incremental", fake_fetch)

    logsync.sync_invite_hub_logs()
    start = datetime.fromisoformat(seen["date_from"])
    assert abs(start - (datetime.now(UTC) - timedelta(hours=6))) < timedelta(minutes=1)

    # A per-student fetch still reaches that student's whole history.
    logsync.sync_invite_hub_logs(student_id="mars-042", advance_cursor=False)
    assert seen["date_from"] is None
