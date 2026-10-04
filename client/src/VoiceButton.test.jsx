import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import VoiceButton, { MAX_RECORDING_SECONDS, canRecordVoice } from "./VoiceButton.jsx";
import { installFakeMic } from "./test/fakeMic.js";

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

describe("VoiceButton", () => {
  let props;
  beforeEach(() => {
    props = { onRecordingStart: vi.fn(), onRecorded: vi.fn(), onError: vi.fn() };
  });

  it("records on the first tap and sends on the second, then frees the mic", async () => {
    const { tracks } = installFakeMic();
    const user = userEvent.setup();
    render(<VoiceButton {...props} />);

    await user.click(screen.getByRole("button", { name: "Ask out loud" }));
    expect(props.onRecordingStart).toHaveBeenCalled();
    const stop = screen.getByRole("button", { name: "Stop and send your question" });
    expect(stop).toHaveAttribute("aria-pressed", "true");

    await user.click(stop);
    expect(props.onRecorded).toHaveBeenCalledTimes(1);
    expect(props.onRecorded.mock.calls[0][0]).toBeInstanceOf(Blob);
    expect(tracks[0].stop).toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Ask out loud" })).toBeInTheDocument();
  });

  it(`stops by itself after ${MAX_RECORDING_SECONDS} seconds`, async () => {
    installFakeMic();
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(<VoiceButton {...props} />);
    await user.click(screen.getByRole("button", { name: "Ask out loud" }));
    act(() => {
      vi.advanceTimersByTime((MAX_RECORDING_SECONDS + 1) * 1000);
    });
    expect(props.onRecorded).toHaveBeenCalledTimes(1);
  });

  it("explains a blocked microphone instead of recording", async () => {
    installFakeMic({ blocked: true });
    const user = userEvent.setup();
    render(<VoiceButton {...props} />);
    await user.click(screen.getByRole("button", { name: "Ask out loud" }));
    expect(props.onError).toHaveBeenCalledWith(expect.stringMatching(/microphone is blocked/));
    expect(props.onRecorded).not.toHaveBeenCalled();
  });

  it("is offered only where the browser can record", () => {
    vi.stubGlobal("MediaRecorder", undefined);
    expect(canRecordVoice()).toBe(false);
    installFakeMic();
    expect(canRecordVoice()).toBe(true);
  });
});
