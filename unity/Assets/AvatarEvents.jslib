// Unity -> page events for the avatar (ChatLLM calls these). The page (webgl/index.html)
// listens on window and relays them to the chat client that embeds it.
mergeInto(LibraryManager.library, {
  // ChatLLM.Start has run: the audio source and animator exist, so Speak is safe.
  AvatarStarted: function () {
    window.dispatchEvent(new CustomEvent("avatar-started"));
  },
  AvatarSpeakingChanged: function (speaking) {
    window.dispatchEvent(new CustomEvent("avatar-speaking", { detail: speaking !== 0 }));
  },
});
