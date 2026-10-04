import { vi } from "vitest";

// A stand-in for the browser's recorder: start, a chunk of audio, stop.
export function installFakeMic({ blocked = false } = {}) {
  const tracks = [{ stop: vi.fn() }];
  const getUserMedia = vi.fn(() =>
    blocked
      ? Promise.reject(new Error("NotAllowedError"))
      : Promise.resolve({ getTracks: () => tracks }),
  );
  vi.stubGlobal("navigator", { ...navigator, mediaDevices: { getUserMedia } });
  vi.stubGlobal(
    "MediaRecorder",
    class {
      constructor() {
        this.state = "inactive";
        this.mimeType = "audio/webm;codecs=opus";
      }
      start() {
        this.state = "recording";
      }
      stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["opus"], { type: this.mimeType }) });
        this.onstop?.();
      }
    },
  );
  return { tracks, getUserMedia };
}
