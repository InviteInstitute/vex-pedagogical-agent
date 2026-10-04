import { describe, it, expect, vi } from "vitest";
import { act, render } from "@testing-library/react";
import { createRef } from "react";

import AvatarCharacter, {
  AVATAR_BUILD_PROBE,
  FULL_BODY,
  HEAD_AND_SHOULDERS,
  UPPER_BODY,
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
    const fetchImpl = vi.fn((url) => Promise.resolve({ ok: url.startsWith(AVATAR_BUILD_PROBE) }));
    expect(await isAvatarBuildAvailable(fetchImpl)).toBe(true);
    expect(await isAvatarBuildAvailable(() => Promise.resolve({ ok: false }))).toBe(false);
    expect(await isAvatarBuildAvailable(() => Promise.reject(new Error("offline")))).toBe(false);
  });
});

describe("placeAvatar", () => {
  const desktop = { width: 1280, height: 800 };

  it("rises head to hips from behind the panel's top-left edge", () => {
    const { framing, style } = placeAvatar(box(816, 216, 440, 560), desktop);
    expect(framing).toBe(UPPER_BODY);
    expect(style.left).toBeGreaterThanOrEqual(816); // over the panel, not beside it
    expect(style.top).toBeGreaterThanOrEqual(0);
    expect(style.top + style.height).toBeGreaterThan(216); // hips tuck behind the panel
  });

  it("stands beside the panel, whole, when there is no room above it", () => {
    const { framing, style } = placeAvatar(box(816, 40, 440, 736), desktop);
    expect(framing).toBe(FULL_BODY);
    expect(style.top + style.height).toBe(40 + 736); // feet on the panel's bottom line
    expect(style.left).toBeLessThan(816);
  });

  it("moves to the right side when the panel is against the left edge", () => {
    const { style } = placeAvatar(box(12, 40, 440, 736), desktop);
    expect(style.left).toBeGreaterThan(12 + 440 - 40);
    expect(style.left + style.width).toBeLessThanOrEqual(desktop.width);
  });

  it("peeks head and shoulders over the panel's top edge on a phone", () => {
    const { framing, style } = placeAvatar(box(12, 132, 366, 700), { width: 390, height: 844 });
    expect(framing).toBe(HEAD_AND_SHOULDERS);
    expect(style.top).toBeGreaterThanOrEqual(0);
    expect(style.top + style.height).toBeGreaterThan(132);
    expect(style.left + style.width).toBeLessThanOrEqual(12 + 366);
  });

  it("peeks over the Open chat button while the chat is closed", () => {
    const { framing, style } = placeAvatar(box(1100, 732, 156, 44), desktop);
    expect(framing).toBe(HEAD_AND_SHOULDERS);
    expect(style.top + style.height).toBeGreaterThan(732);
    expect(style.left + style.width).toBeLessThanOrEqual(1100 + 156 + 8);
  });
});

describe("AvatarCharacter", () => {
  function setup(props) {
    const anchorRef = createRef();
    const panel = document.createElement("section");
    panel.getBoundingClientRect = () => box(816, 136, 440, 640);
    document.body.appendChild(panel);
    anchorRef.current = panel;
    const view = render(
      <AvatarCharacter anchorRef={anchorRef} layoutKey="k" muted={false} {...props} />,
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
    return { ...view, anchorRef, frame, postMessage, fromFrame, sent };
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
    const { fromFrame, sent, rerender, anchorRef } = setup({ utterance: reply });
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

    rerender(
      <AvatarCharacter anchorRef={anchorRef} layoutKey="k" muted={false} utterance={reply} />,
    );
    expect(sent("speak")).toHaveLength(1);

    const next = { id: "student:a2", text: "Nice progress!", parts: ["Nice progress!"] };
    rerender(
      <AvatarCharacter anchorRef={anchorRef} layoutKey="k" muted={false} utterance={next} />,
    );
    expect(sent("speak").at(-1)).toEqual({
      type: "speak",
      text: "Nice progress!",
      parts: ["Nice progress!"],
      audio: true,
    });
  });

  it("muted, hushes at once and keeps gesturing without sound", () => {
    const reply = { id: "student:a1", text: "Hi" };
    const { fromFrame, sent, rerender, anchorRef } = setup({ utterance: reply });
    fromFrame({ type: "avatar-ready" });

    rerender(<AvatarCharacter anchorRef={anchorRef} layoutKey="k" muted utterance={reply} />);
    expect(sent("hush")).toHaveLength(1);

    const next = { id: "student:a2", text: "Try one change.", parts: ["Try one change."] };
    rerender(<AvatarCharacter anchorRef={anchorRef} layoutKey="k" muted utterance={next} />);
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
