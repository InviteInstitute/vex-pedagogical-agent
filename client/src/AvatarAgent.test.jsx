import { describe, it, expect, vi } from "vitest";
import { act, render, screen } from "@testing-library/react";

import AvatarAgent, { AVATAR_BUILD_PROBE, isAvatarBuildAvailable } from "./AvatarAgent.jsx";

describe("isAvatarBuildAvailable", () => {
  it("is true only when the WebGL build is deployed", async () => {
    const fetchImpl = vi.fn((url) => Promise.resolve({ ok: url === AVATAR_BUILD_PROBE }));
    expect(await isAvatarBuildAvailable(fetchImpl)).toBe(true);
    expect(await isAvatarBuildAvailable(() => Promise.resolve({ ok: false }))).toBe(false);
    expect(await isAvatarBuildAvailable(() => Promise.reject(new Error("offline")))).toBe(false);
  });
});

describe("AvatarAgent", () => {
  const checkIn = (id, body) => ({ id, body, role: "assistant", proactive: true });

  function setup(checkIns) {
    const view = render(
      <AvatarAgent studentId="mars 042" checkIns={checkIns} onResearchView={() => {}} />,
    );
    const frame = screen.getByTitle("INVITE Agent");
    const postMessage = vi.spyOn(frame.contentWindow, "postMessage");
    const announceReady = () =>
      act(() => {
        window.dispatchEvent(
          new MessageEvent("message", {
            data: { type: "avatar-ready" },
            origin: window.location.origin,
            source: frame.contentWindow,
          }),
        );
      });
    return { ...view, frame, postMessage, announceReady };
  }

  it("loads the avatar page for this student, with speech on", () => {
    const { frame } = setup([]);
    expect(frame.getAttribute("src")).toBe("/webgl/?embed=1&audio=true&student_id=mars%20042");
  });

  it("holds check-ins until the avatar is ready, then sends each once", () => {
    const first = [checkIn("p-1", "Try one change at a time.")];
    const { postMessage, announceReady, rerender } = setup(first);
    expect(postMessage).not.toHaveBeenCalled();

    announceReady();
    expect(postMessage).toHaveBeenCalledTimes(1);
    expect(postMessage).toHaveBeenCalledWith(
      { type: "proactive", text: "Try one change at a time." },
      window.location.origin,
    );

    rerender(
      <AvatarAgent
        studentId="mars 042"
        checkIns={[...first, checkIn("p-2", "Nice progress!")]}
        onResearchView={() => {}}
      />,
    );
    expect(postMessage).toHaveBeenCalledTimes(2);
    expect(postMessage).toHaveBeenLastCalledWith(
      { type: "proactive", text: "Nice progress!" },
      window.location.origin,
    );
  });

  it("ignores a ready message from anywhere but its own frame", () => {
    const { postMessage } = setup([checkIn("p-1", "hello")]);
    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-ready" },
          origin: "https://evil.example",
        }),
      );
    });
    expect(postMessage).not.toHaveBeenCalled();
  });
});
