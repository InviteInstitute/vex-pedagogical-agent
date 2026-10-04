import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import App from "./App.jsx";

// Student view hides why a proactive message fired; research view shows it.
let streamListeners;

beforeEach(() => {
  streamListeners = {};
  window.localStorage.clear();
  window.HTMLElement.prototype.scrollIntoView = () => {};
  vi.stubGlobal(
    "EventSource",
    class {
      addEventListener(type, listener) {
        streamListeners[type] = listener;
      }
      close() {}
    },
  );
  vi.stubGlobal(
    "fetch",
    vi.fn((url) =>
      Promise.resolve({
        // No avatar build deployed: students get the text chat these tests drive.
        ok: !String(url).startsWith("/webgl/"),
        status: String(url).startsWith("/webgl/") ? 404 : 200,
        json: () => Promise.resolve({ session_id: "session-1" }),
      }),
    ),
  );
});

afterEach(() => {
  vi.unstubAllGlobals();
});

async function startChatWithCheckIn(user) {
  render(<App />);
  await user.type(screen.getByLabelText("Student ID"), "mars-042");
  await user.click(screen.getByRole("button", { name: "Start chat" }));
  await screen.findByRole("region", { name: "Conversation" });
  act(() => {
    streamListeners.assistant_message({
      data: JSON.stringify({
        message_id: 7,
        message: "What do you expect the robot to do next time?",
        trigger_type: "wheel_spinning",
        trigger_why: "3 runs with no code change",
      }),
    });
  });
}

describe("the chat and the research settings page", () => {
  it("shows students a plain check-in label without the trigger", async () => {
    const user = userEvent.setup();
    await startChatWithCheckIn(user);

    expect(screen.getByText("INVITE Agent is checking in")).toBeInTheDocument();
    expect(screen.queryByText("wheel_spinning")).not.toBeInTheDocument();
    expect(screen.queryByText("session-1")).not.toBeInTheDocument();
  });

  it("makes the research view the agent settings page, with no second chat", async () => {
    const user = userEvent.setup();
    await startChatWithCheckIn(user);

    await user.click(screen.getByRole("button", { name: "Research" }));

    expect(screen.getByRole("region", { name: "Agent settings" })).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: "Conversation" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Message")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Student" }));
    expect(screen.getByText("What do you expect the robot to do next time?")).toBeInTheDocument();
  });

  it("starts every visit in the student view, with the toggle in the same place", async () => {
    const user = userEvent.setup();
    const first = render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    await screen.findByRole("region", { name: "Conversation" });
    const footer = () => screen.getByRole("group", { name: "View" }).parentElement;
    const studentFooter = footer();
    await user.click(screen.getByRole("button", { name: "Research" }));
    // The same footer element holds the toggle on the settings page.
    expect(footer()).toBe(studentFooter);
    expect(footer()).toHaveClass("panel-foot");
    first.unmount();

    render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    expect(await screen.findByRole("region", { name: "Conversation" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Student" })).toHaveAttribute("aria-pressed", "true");
  });

  it("counts replies that arrive while the chat is collapsed", async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));
    await screen.findByRole("region", { name: "Conversation" });

    await user.click(screen.getByRole("button", { name: "Collapse chat" }));
    act(() => {
      streamListeners.assistant_message({
        data: JSON.stringify({ message_id: 9, message: "Still there?" }),
      });
    });

    expect(screen.getByRole("button", { name: /Open chat\s*1 new/ })).toBeInTheDocument();
  });
});
