import { useEffect, useRef, useState } from "react";

// The tutor as a character beside the chat panel: the Unity WebGL avatar
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
    // A site without the build may answer any path with its own index.html (200).
    return response.ok && Boolean(await response.json());
  } catch {
    return false;
  }
}

// Beside the panel the whole character shows; on a phone the panel spans the screen,
// so head and shoulders peek over its top edge instead.
export const FULL_BODY = 1;
export const HEAD_AND_SHOULDERS = 0.42;
const PEEK_BREAKPOINT = 980;
const EDGE = 8;

// Where the character stands for a given panel box and viewport.
export function placeAvatar(panel, viewport) {
  if (viewport.width <= PEEK_BREAKPOINT) {
    const width = 120;
    const height = 150;
    return {
      framing: HEAD_AND_SHOULDERS,
      style: {
        width,
        height,
        left: panel.right - width - 16,
        top: panel.top - height + 30, // the shoulders tuck behind the panel
      },
    };
  }
  const height = Math.round(Math.min(460, Math.max(280, panel.height * 0.72)));
  const width = Math.round(height * 0.44);
  // Tuck a little behind the panel edge, so the character reads as standing at it.
  const tuck = Math.round(width * 0.12);
  const top = panel.bottom - height;
  const leftOfPanel = panel.left - width + tuck;
  const left =
    leftOfPanel >= EDGE ? leftOfPanel : Math.min(panel.right - tuck, viewport.width - width - EDGE);
  return { framing: FULL_BODY, style: { width, height, left, top } };
}

export default function AvatarCharacter({
  panelRef,
  visible = true,
  hushSignal = 0,
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

  // Follow the panel: on drag/resize (layoutKey), when its content changes size, and
  // when the window resizes. No panel (chat collapsed) hides the character in place:
  // shrinking the frame to nothing would hand Unity a zero-size screen.
  // Measured after the commit, not in a layout effect: this component renders before
  // the panel, so a layout effect ran before a reopened panel's ref was attached,
  // found no panel and left the tutor hidden.
  useEffect(() => {
    const panel = panelRef.current;
    const measure = () => {
      const box = panelRef.current?.getBoundingClientRect();
      setPlacement((previous) =>
        box
          ? placeAvatar(box, { width: window.innerWidth, height: window.innerHeight })
          : previous && { ...previous, hidden: true },
      );
    };
    measure();
    window.addEventListener("resize", measure);
    const observer = panel && "ResizeObserver" in window ? new ResizeObserver(measure) : null;
    observer?.observe(panel);
    return () => {
      window.removeEventListener("resize", measure);
      observer?.disconnect();
    };
  }, [panelRef, layoutKey]);

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

  // The student started speaking: stop talking, or the mic would hear the tutor.
  useEffect(() => {
    if (isReady && hushSignal) {
      post({ type: "hush" });
    }
  }, [isReady, hushSignal]);

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
      className={`avatar-character ${
        visible && isReady && style && !placement.hidden ? "is-shown" : ""
      }`}
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
