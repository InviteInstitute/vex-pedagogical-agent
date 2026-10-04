import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import App from "./App.jsx";

// With the avatar's WebGL build deployed, the chat panel stays the conversation and the
// character stands beside it: a voice toggle in the header, and the reply being spoken
// is marked.
beforeEach(() => {
  window.localStorage.clear();
  window.HTMLElement.prototype.scrollIntoView = () => {};
  vi.stubGlobal(
    "EventSource",
    class {
      addEventListener() {}
      close() {}
    },
  );
  vi.stubGlobal(
    "fetch",
    vi.fn(() =>
      Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ session_id: "session-1" }),
      }),
    ),
  );
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("avatar beside the chat", () => {
  it("keeps the chat, adds a voice toggle, and marks the reply being spoken", async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));

    expect(await screen.findByRole("region", { name: "Conversation" })).toBeInTheDocument();
    const voice = await screen.findByRole("button", { name: "Mute the tutor's voice" });
    const frame = container.querySelector('iframe[title="INVITE Agent character"]');
    expect(frame).not.toBeNull();

    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-speaking", speaking: true },
          origin: window.location.origin,
          source: frame.contentWindow,
        }),
      );
    });
    expect(container.querySelector(".turn.is-speaking .speaking")).not.toBeNull();

    await user.click(voice);
    expect(screen.getByRole("button", { name: "Turn the tutor's voice on" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(JSON.parse(window.localStorage.getItem("vex-agent:muted"))).toBe(true);
  });
});
