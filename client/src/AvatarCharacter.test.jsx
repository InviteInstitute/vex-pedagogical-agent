import { describe, it, expect, vi } from "vitest";
import { act, render } from "@testing-library/react";
import { createRef } from "react";

import AvatarCharacter, {
  AVATAR_BUILD_PROBE,
  FULL_BODY,
  HEAD_AND_SHOULDERS,
  isAvatarBuildAvailable,
  placeAvatar,
} from "./AvatarCharacter.jsx";

const box = (left, top, width, height) => ({
  left,
  top,
  width,
  height,
  right: left + width,
  bottom: top + height,
});

describe("isAvatarBuildAvailable", () => {
  it("is true only when the WebGL build is deployed", async () => {
    const fetchImpl = vi.fn((url) =>
      Promise.resolve({
        ok: url.startsWith(AVATAR_BUILD_PROBE),
        json: () => Promise.resolve({ version: "b44b9c21" }),
      }),
    );
    expect(await isAvatarBuildAvailable(fetchImpl)).toBe(true);
    // A single-page app's index.html fallback, served for the missing build.json.
    const spaFallback = () =>
      Promise.resolve({ ok: true, json: () => Promise.reject(new SyntaxError("<!doctype")) });
    expect(await isAvatarBuildAvailable(spaFallback)).toBe(false);
    expect(await isAvatarBuildAvailable(() => Promise.resolve({ ok: false }))).toBe(false);
    expect(await isAvatarBuildAvailable(() => Promise.reject(new Error("offline")))).toBe(false);
  });
});

describe("placeAvatar", () => {
  const desktop = { width: 1280, height: 800 };

  it("stands the whole character at the panel's left edge, feet on its bottom line", () => {
    const { framing, style } = placeAvatar(box(816, 136, 440, 640), desktop);
    expect(framing).toBe(FULL_BODY);
    expect(style.top + style.height).toBe(136 + 640);
    expect(style.left + style.width).toBeGreaterThan(816); // tucked behind the edge
    expect(style.left).toBeLessThan(816);
  });

  it("moves to the right side when the panel is against the left edge", () => {
    const { style } = placeAvatar(box(12, 136, 440, 640), desktop);
    expect(style.left).toBeGreaterThan(12 + 440 - 40);
    expect(style.left + style.width).toBeLessThanOrEqual(desktop.width);
  });

  it("peeks head and shoulders over the panel's top edge on a phone", () => {
    const { framing, style } = placeAvatar(box(12, 132, 366, 700), { width: 390, height: 844 });
    expect(framing).toBe(HEAD_AND_SHOULDERS);
    expect(style.top).toBeGreaterThanOrEqual(0);
    expect(style.top + style.height).toBeGreaterThan(132); // shoulders tuck behind the panel
    expect(style.left + style.width).toBeLessThanOrEqual(12 + 366);
  });
});

describe("AvatarCharacter", () => {
  function setup(props) {
    const panelRef = createRef();
    const panel = document.createElement("section");
    panel.getBoundingClientRect = () => box(816, 136, 440, 640);
    document.body.appendChild(panel);
    panelRef.current = panel;
    const view = render(
      <AvatarCharacter panelRef={panelRef} layoutKey="k" muted={false} {...props} />,
    );
    const frame = view.container.querySelector("iframe");
    const postMessage = vi.spyOn(frame.contentWindow, "postMessage");
    const fromFrame = (data) =>
      act(() => {
        window.dispatchEvent(
          new MessageEvent("message", {
            data,
            origin: window.location.origin,
            source: frame.contentWindow,
          }),
        );
      });
    const sent = (type) => postMessage.mock.calls.map(([m]) => m).filter((m) => m.type === type);
    return { ...view, panelRef, frame, postMessage, fromFrame, sent };
  }

  it("loads the character-only avatar page, hidden from assistive tech", () => {
    const { frame, container } = setup({ utterance: null });
    expect(frame.getAttribute("src")).toBe("/webgl/?embed=1&mode=avatar&framing=1");
    expect(container.firstChild).toHaveAttribute("aria-hidden", "true");
  });

  it("says each reply once, after the avatar is ready", () => {
    const reply = {
      id: "student:a1",
      text: "Nice start! Look at your `drive` block.",
      parts: ["Nice start!", "Look at your drive block."],
    };
    const { fromFrame, sent, rerender, panelRef } = setup({ utterance: reply });
    expect(sent("speak")).toHaveLength(0);

    fromFrame({ type: "avatar-ready" });
    expect(sent("speak")).toEqual([
      {
        type: "speak",
        text: "Nice start! Look at your `drive` block.",
        parts: ["Nice start!", "Look at your drive block."],
        audio: true,
      },
    ]);

    rerender(<AvatarCharacter panelRef={panelRef} layoutKey="k" muted={false} utterance={reply} />);
    expect(sent("speak")).toHaveLength(1);

    const next = { id: "student:a2", text: "Nice progress!", parts: ["Nice progress!"] };
    rerender(<AvatarCharacter panelRef={panelRef} layoutKey="k" muted={false} utterance={next} />);
    expect(sent("speak").at(-1)).toEqual({
      type: "speak",
      text: "Nice progress!",
      parts: ["Nice progress!"],
      audio: true,
    });
  });

  it("muted, hushes at once and keeps gesturing without sound", () => {
    const reply = { id: "student:a1", text: "Hi" };
    const { fromFrame, sent, rerender, panelRef } = setup({ utterance: reply });
    fromFrame({ type: "avatar-ready" });

    rerender(<AvatarCharacter panelRef={panelRef} layoutKey="k" muted utterance={reply} />);
    expect(sent("hush")).toHaveLength(1);

    const next = { id: "student:a2", text: "Try one change.", parts: ["Try one change."] };
    rerender(<AvatarCharacter panelRef={panelRef} layoutKey="k" muted utterance={next} />);
    expect(sent("speak").at(-1)).toEqual({
      type: "speak",
      text: "Try one change.",
      parts: ["Try one change."],
      audio: false,
    });
  });

  it("reports speaking, and ignores messages from anywhere but its frame", () => {
    const onSpeakingChange = vi.fn();
    const { fromFrame } = setup({ utterance: null, onSpeakingChange });
    fromFrame({ type: "avatar-speaking", speaking: true });
    expect(onSpeakingChange).toHaveBeenLastCalledWith(true);

    act(() => {
      window.dispatchEvent(
        new MessageEvent("message", {
          data: { type: "avatar-speaking", speaking: false },
          origin: "https://evil.example",
        }),
      );
    });
    expect(onSpeakingChange).toHaveBeenCalledTimes(1);
  });
});
