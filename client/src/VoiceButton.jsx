import { useEffect, useRef, useState } from "react";
import { Microphone, Stop } from "@phosphor-icons/react";

// The chat's mic: tap to ask out loud, tap again to send. Records with the browser's
// MediaRecorder (WebM/Opus, which the server's speech-to-text takes as is) and stops on
// its own at MAX_RECORDING_SECONDS.

export const MAX_RECORDING_SECONDS = 30;

export function canRecordVoice() {
  return Boolean(
    typeof window !== "undefined" && window.MediaRecorder && navigator.mediaDevices?.getUserMedia,
  );
}

const formatSeconds = (seconds) => `0:${String(seconds).padStart(2, "0")}`;

export default function VoiceButton({ disabled, onRecordingStart, onRecorded, onError }) {
  const [isRecording, setIsRecording] = useState(false);
  const [seconds, setSeconds] = useState(0);
  const recorderRef = useRef(null);
  const timersRef = useRef([]);
  const streamRef = useRef(null);

  const clearTimers = () => {
    timersRef.current.forEach((timer) => clearInterval(timer));
    timersRef.current = [];
  };

  const releaseMic = () => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
  };

  // Leaving the page (or the chat) mid-recording must free the microphone.
  useEffect(
    () => () => {
      clearTimers();
      if (recorderRef.current?.state === "recording") {
        recorderRef.current.onstop = null;
        recorderRef.current.stop();
      }
      releaseMic();
    },
    [],
  );

  const stop = () => {
    if (recorderRef.current?.state === "recording") {
      recorderRef.current.stop();
    }
  };

  const start = async () => {
    try {
      streamRef.current = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      onError(
        "The microphone is blocked. Allow it in your browser to ask out loud, or type your question.",
      );
      return;
    }
    const recorder = new MediaRecorder(streamRef.current);
    const chunks = [];
    recorder.ondataavailable = (event) => {
      if (event.data.size) {
        chunks.push(event.data);
      }
    };
    recorder.onstop = () => {
      clearTimers();
      releaseMic();
      setIsRecording(false);
      const recording = new Blob(chunks, { type: recorder.mimeType || "audio/webm" });
      if (recording.size) {
        onRecorded(recording);
      }
    };
    recorderRef.current = recorder;
    recorder.start();
    setSeconds(0);
    setIsRecording(true);
    onRecordingStart?.();

    const startedAt = Date.now();
    timersRef.current.push(
      setInterval(() => {
        const elapsed = Math.floor((Date.now() - startedAt) / 1000);
        setSeconds(elapsed);
        if (elapsed >= MAX_RECORDING_SECONDS) {
          stop();
        }
      }, 250),
    );
  };

  return (
    <button
      type="button"
      className={`voice-button ${isRecording ? "is-recording" : ""}`}
      onClick={isRecording ? stop : start}
      disabled={disabled && !isRecording}
      aria-pressed={isRecording}
      aria-label={isRecording ? "Stop and send your question" : "Ask out loud"}
      title={isRecording ? "Stop and send" : "Ask out loud"}
    >
      {isRecording ? (
        <>
          <Stop className="icon" weight="fill" aria-hidden="true" />
          <span className="voice-timer" aria-hidden="true">
            {formatSeconds(seconds)}
          </span>
        </>
      ) : (
        <Microphone className="icon" weight="bold" aria-hidden="true" />
      )}
    </button>
  );
}
