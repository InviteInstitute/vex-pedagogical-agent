import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import App from "./App.jsx";

// With the avatar's WebGL build deployed, a signed-in student sees the avatar instead
// of the text chat; the research view keeps the text chat.
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

describe("avatar student view", () => {
  it("replaces the text chat with the avatar, and research view brings it back", async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText("Student ID"), "mars-042");
    await user.click(screen.getByRole("button", { name: "Start chat" }));

    const avatar = await screen.findByTitle("INVITE Agent");
    expect(avatar.getAttribute("src")).toContain("student_id=mars-042");
    expect(screen.queryByRole("region", { name: "Conversation" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Research view" }));
    expect(await screen.findByRole("region", { name: "Conversation" })).toBeInTheDocument();
    expect(screen.queryByTitle("INVITE Agent")).not.toBeInTheDocument();
  });
});
