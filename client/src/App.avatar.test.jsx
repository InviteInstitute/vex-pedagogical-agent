import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import App from "./App.jsx";
import { installFakeMic } from "./test/fakeMic.js";

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
  it("keeps the chat and adds a voice toggle, without a speaking indicator", async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));

    expect(await screen.findByRole("region", { name: "Conversation" })).toBeInTheDocument();
    const voice = await screen.findByRole("button", { name: "Mute the tutor's voice" });
    const frame = container.querySelector('iframe[title="INVITE Agent character"]');
    expect(frame).not.toBeNull();

    // Speaking shows no indicator on the reply: the tutor's voice is the signal.
    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-speaking", speaking: true },
          origin: window.location.origin,
          source: frame.contentWindow,
        }),
      );
    });
    expect(container.querySelector(".speaking")).toBeNull();

    await user.click(voice);
    expect(screen.getByRole("button", { name: "Turn the tutor's voice on" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(JSON.parse(window.localStorage.getItem("vex-agent:muted"))).toBe(true);
  });

  it("asks the server to prepare speech only while the voice is on", async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    const ask = async (text) => {
      await user.type(await screen.findByLabelText("Message"), text);
      await user.click(screen.getByRole("button", { name: /Send/ }));
      const calls = fetch.mock.calls.filter(([url]) => String(url).endsWith("/responses"));
      return JSON.parse(calls.at(-1)[1].body);
    };

    expect((await ask("why won't it turn?")).speak).toBe(true);
    await user.click(await screen.findByRole("button", { name: "Mute the tutor's voice" }));
    expect((await ask("and now?")).speak).toBe(false);
  });

  it("hides the tutor on the sign-in card, and brings it back after reopening the chat", async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);
    const tutor = () => container.querySelector(".avatar-character");
    const frame = await waitFor(() => {
      const element = container.querySelector('iframe[title="INVITE Agent character"]');
      expect(element).not.toBeNull();
      return element;
    });
    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-ready" },
          origin: window.location.origin,
          source: frame.contentWindow,
        }),
      );
    });

    // Ready, but not shown while the student enters their ID.
    expect(tutor()).not.toHaveClass("is-shown");

    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    await waitFor(() => expect(tutor()).toHaveClass("is-shown"));

    await user.click(screen.getByRole("button", { name: "Collapse chat" }));
    await waitFor(() => expect(tutor()).not.toHaveClass("is-shown"));
    await user.click(screen.getByRole("button", { name: /Open chat/ }));
    await waitFor(() => expect(tutor()).toHaveClass("is-shown"));
  });

  it("says a reply once, however often the student switches views", async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    const frame = container.querySelector('iframe[title="INVITE Agent character"]');
    const postToTutor = vi.spyOn(frame.contentWindow, "postMessage");
    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-ready" },
          origin: window.location.origin,
          source: frame.contentWindow,
        }),
      );
    });
    const spoken = () => postToTutor.mock.calls.filter(([message]) => message.type === "speak");
    await waitFor(() => expect(spoken()).toHaveLength(1));

    for (const view of ["Research", "Student", "Research", "Student"]) {
      await user.click(screen.getByRole("button", { name: view }));
    }
    expect(spoken()).toHaveLength(1);
  });

  it("sends a spoken question as what the tutor heard, and hushes it while recording", async () => {
    installFakeMic();
    const posted = [];
    fetch.mockImplementation((url, options = {}) => {
      posted.push([String(url), options.body]);
      const body = String(url).endsWith("/transcriptions")
        ? { text: "why won't my robot turn" }
        : { session_id: "session-1" };
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(body) });
    });
    const user = userEvent.setup();
    const { container } = render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    const frame = container.querySelector('iframe[title="INVITE Agent character"]');
    const postToTutor = vi.spyOn(frame.contentWindow, "postMessage");
    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-ready" },
          origin: window.location.origin,
          source: frame.contentWindow,
        }),
      );
    });

    await user.click(await screen.findByRole("button", { name: "Ask out loud" }));
    expect(postToTutor).toHaveBeenCalledWith({ type: "hush" }, window.location.origin);
    await user.click(screen.getByRole("button", { name: "Stop and send your question" }));

    expect(await screen.findByText("why won't my robot turn")).toBeInTheDocument();
    const upload = posted.find(([url]) => url.endsWith("/students/mars-042/transcriptions"));
    expect(upload[1]).toBeInstanceOf(FormData);
    const asked = posted.filter(([url]) => url.endsWith("/responses")).at(-1);
    expect(JSON.parse(asked[1]).student_message).toBe("why won't my robot turn");
  });

  it("asks again when nothing was heard", async () => {
    installFakeMic();
    fetch.mockImplementation((url) =>
      Promise.resolve({
        ok: true,
        status: 200,
        json: () =>
          Promise.resolve(
            String(url).endsWith("/transcriptions") ? { text: "  " } : { session_id: "s" },
          ),
      }),
    );
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    await user.click(await screen.findByRole("button", { name: "Ask out loud" }));
    await user.click(screen.getByRole("button", { name: "Stop and send your question" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("I didn't catch that");
  });
});
