import { useEffect, useRef, useState } from "react";

// The tutor as a character attached to the chat panel: the Unity WebGL avatar
// (webgl/index.html ?mode=avatar) renders only the 3D character, transparent over
// VEXcode VR, and says each reply with lip-sync and gestures. The chat itself stays in
// the panel. Purely visual for assistive tech: everything it says is in the chat.

// Written by webgl/install_build.sh once a Unity WebGL build is installed.
export const AVATAR_BUILD_PROBE = "/webgl/build_out/ai-conversation-agent/build.json";

export async function isAvatarBuildAvailable(fetchImpl = fetch) {
  try {
    // Uncached at every layer: a CDN copy must not outlive a removed build.
    const response = await fetchImpl(`${AVATAR_BUILD_PROBE}?t=${Date.now()}`, {
      cache: "no-store",
    });
    return response.ok;
  } catch {
    return false;
  }
}

// The tutor rises from behind its chat: head to hips over the panel's top-left edge on
// a desktop, standing on the header; head and shoulders on a phone (the panel spans the
// screen) and over the "Open chat" button while the chat is closed. Only with no room
// above the panel does it stand beside it instead, whole.
export const FULL_BODY = 1;
export const UPPER_BODY = 0.55;
export const HEAD_AND_SHOULDERS = 0.42;
const PEEK_BREAKPOINT = 980;
const EDGE = 8;
// How much of the tutor's box sits behind the anchor's top edge (its cut line).
const TUCK = 44;
const MIN_RISE = 150;

// Where the tutor stands for a given anchor box (the panel or the launcher).
export function placeAvatar(anchor, viewport) {
  if (anchor.height < 120) {
    const width = 112;
    const height = 140;
    return {
      framing: HEAD_AND_SHOULDERS,
      style: { width, height, left: anchor.right - width - 8, top: anchor.top - height + 30 },
    };
  }
  if (viewport.width <= PEEK_BREAKPOINT) {
    const width = 120;
    const height = 150;
    return {
      framing: HEAD_AND_SHOULDERS,
      style: { width, height, left: anchor.right - width - 16, top: anchor.top - height + 30 },
    };
  }
  const rise = Math.min(240, anchor.top - EDGE + TUCK);
  if (rise >= MIN_RISE) {
    const width = Math.round(rise * 0.84);
    return {
      framing: UPPER_BODY,
      style: { width, height: rise, left: anchor.left + 14, top: anchor.top - rise + TUCK },
    };
  }
  const height = Math.round(Math.min(460, Math.max(280, anchor.height * 0.72)));
  const width = Math.round(height * 0.44);
  const tuck = Math.round(width * 0.12);
  const leftOfPanel = anchor.left - width + tuck;
  const left =
    leftOfPanel >= EDGE
      ? leftOfPanel
      : Math.min(anchor.right - tuck, viewport.width - width - EDGE);
  return { framing: FULL_BODY, style: { width, height, left, top: anchor.bottom - height } };
}

export default function AvatarCharacter({
  anchorRef,
  layoutKey,
  utterance,
  muted,
  onSpeakingChange,
}) {
  const frameRef = useRef(null);
  const spokenIds = useRef(new Set());
  const framingSent = useRef(null);
  const [isReady, setIsReady] = useState(false);
  const [placement, setPlacement] = useState(null);

  // Follow the anchor (the panel, or the launcher while the chat is closed): on
  // drag/resize and open/close (layoutKey), when its content changes size, and when
  // the window resizes. Measured after the commit, so the anchor's ref is attached even
  // when it renders after this component (reopening the chat used to find no panel
  // and leave the tutor hidden). With no anchor the tutor hides in place: shrinking
  // the frame to nothing would hand Unity a zero-size screen.
  useEffect(() => {
    const anchor = anchorRef.current;
    const measure = () => {
      const box = anchorRef.current?.getBoundingClientRect();
      setPlacement((previous) =>
        box
          ? placeAvatar(box, { width: window.innerWidth, height: window.innerHeight })
          : previous && { ...previous, hidden: true },
      );
    };
    measure();
    window.addEventListener("resize", measure);
    const observer = anchor && "ResizeObserver" in window ? new ResizeObserver(measure) : null;
    observer?.observe(anchor);
    return () => {
      window.removeEventListener("resize", measure);
      observer?.disconnect();
    };
  }, [anchorRef, layoutKey]);

  // The avatar page says "ready" once Unity has loaded, and reports speech start/stop.
  useEffect(() => {
    const onMessage = (event) => {
      if (
        event.origin !== window.location.origin ||
        event.source !== frameRef.current?.contentWindow
      ) {
        return;
      }
      if (event.data?.type === "avatar-ready") {
        framingSent.current = null;
        setIsReady(true);
      } else if (event.data?.type === "avatar-speaking") {
        onSpeakingChange?.(Boolean(event.data.speaking));
      }
    };
    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, [onSpeakingChange]);

  const post = (message) =>
    frameRef.current?.contentWindow?.postMessage(message, window.location.origin);

  useEffect(() => {
    if (isReady && placement && framingSent.current !== placement.framing) {
      framingSent.current = placement.framing;
      post({ type: "framing", framing: placement.framing });
    }
  }, [isReady, placement]);

  // Muting mid-sentence stops the voice at once, not at the next reply.
  useEffect(() => {
    if (isReady && muted) {
      post({ type: "hush" });
    }
  }, [isReady, muted]);

  // Say each new reply once. Muted, the character still gestures, silently.
  useEffect(() => {
    if (!isReady || !utterance || spokenIds.current.has(utterance.id)) {
      return;
    }
    spokenIds.current.add(utterance.id);
    post({ type: "speak", text: utterance.text, parts: utterance.parts, audio: !muted });
  }, [isReady, utterance, muted]);

  const style = placement?.style;
  return (
    <div
      className={`avatar-character ${isReady && style && !placement.hidden ? "is-shown" : ""}`}
      aria-hidden="true"
      style={
        style
          ? {
              left: `${style.left}px`,
              top: `${style.top}px`,
              width: `${style.width}px`,
              height: `${style.height}px`,
            }
          : undefined
      }
    >
      <iframe
        ref={frameRef}
        src={`/webgl/?embed=1&mode=avatar&framing=${FULL_BODY}`}
        title="INVITE Agent character"
        tabIndex={-1}
        allow="autoplay"
      />
    </div>
  );
}
