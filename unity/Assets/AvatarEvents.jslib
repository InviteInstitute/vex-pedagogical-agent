// Unity -> page events for the avatar (ChatLLM calls these). The page (webgl/index.html)
// listens on window and relays them to the chat client that embeds it.
mergeInto(LibraryManager.library, {
  AvatarSpeakingChanged: function (speaking) {
    window.dispatchEvent(new CustomEvent("avatar-speaking", { detail: speaking !== 0 }));
  },
});
