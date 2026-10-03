import { useEffect, useRef, useState } from "react";

// The student view's embodied tutor: the Unity WebGL avatar (webgl/index.html) in a
// transparent frame over VEXcode VR. It runs its own chat through the backend's Unity
// bridge; this component gives it the student id and forwards the proactive check-ins
// that arrive on the push stream, so the avatar says those too.

export const AVATAR_PAGE = "/webgl/";
// Present once a Unity WebGL build has been dropped into webgl/build_out/.
export const AVATAR_BUILD_PROBE =
  "/webgl/build_out/ai-conversation-agent/Build/ai-conversation-agent.loader.js";

export async function isAvatarBuildAvailable(fetchImpl = fetch) {
  try {
    const response = await fetchImpl(AVATAR_BUILD_PROBE, { method: "HEAD" });
    return response.ok;
  } catch {
    return false;
  }
}

export default function AvatarAgent({ studentId, checkIns, onResearchView }) {
  const frameRef = useRef(null);
  const sentIds = useRef(new Set());
  const [isReady, setIsReady] = useState(false);

  // The avatar page announces itself once Unity has loaded; messages sent earlier
  // would land before anything is listening.
  useEffect(() => {
    const onMessage = (event) => {
      if (
        event.origin === window.location.origin &&
        event.source === frameRef.current?.contentWindow &&
        event.data?.type === "avatar-ready"
      ) {
        setIsReady(true);
      }
    };
    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, []);

  useEffect(() => {
    const frame = frameRef.current?.contentWindow;
    if (!isReady || !frame) {
      return;
    }
    for (const checkIn of checkIns) {
      if (!sentIds.current.has(checkIn.id)) {
        sentIds.current.add(checkIn.id);
        frame.postMessage({ type: "proactive", text: checkIn.body }, window.location.origin);
      }
    }
  }, [isReady, checkIns]);

  const src = `${AVATAR_PAGE}?embed=1&audio=true&student_id=${encodeURIComponent(studentId)}`;

  return (
    <section className="avatar-agent" aria-label="INVITE Agent">
      <iframe ref={frameRef} src={src} title="INVITE Agent" allow="autoplay; microphone" />
      <button type="button" className="avatar-research-link" onClick={onResearchView}>
        Research view
      </button>
    </section>
  );
}
