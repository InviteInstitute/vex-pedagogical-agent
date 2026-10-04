using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Random = System.Random;

[Serializable]
public class Output
{
    public string output;
}

public class ChatLLM : MonoBehaviour
{
    [SerializeField]
    private bool audioFeedback = false;

    private string serverurl = "http://localhost:5000";
    private string endpoint = "/generate-feedback-from-text"; // Default endpoint matching app.py

    // Identifies the student to the telemetry-grounded backend. Set in the
    // inspector for testing, or injected from JavaScript via ReceiveConfigJson.
    [SerializeField]
    private string studentId = "";

    [SerializeField]
    GameObject AgentRig;

    public AudioClip agentVoice;

    [SerializeField]
    GameObject AgentAudio;
    Animator animator;
    AudioSource voice;

    private Dictionary<string, AnimationClip> animationClips;
    private List<string> beatGestureNames;

    // Track Blockly workspace XML sent by Javascript
    private string currentXml = "";

    // Gets server information from javascript
    public void ReceiveConfigJson(string json)
    {
        try
        {
            var serverInfo = JsonUtility.FromJson<ServerInfo>(json);
            if (!string.IsNullOrEmpty(serverInfo.serverurl))
            {
                serverurl = serverInfo.serverurl;
            }
            if (!string.IsNullOrEmpty(serverInfo.endpoint))
            {
                endpoint = serverInfo.endpoint.StartsWith("/") ? serverInfo.endpoint : "/" + serverInfo.endpoint;
            }
            if (!string.IsNullOrEmpty(serverInfo.student_id))
            {
                studentId = serverInfo.student_id;
            }
            Debug.Log($"ReceiveConfigJson: Configured server to {serverurl}{endpoint} for student {studentId}");
            if (!string.IsNullOrEmpty(serverInfo.audioFeedback))
            {
                audioFeedback = bool.Parse(serverInfo.audioFeedback);
            }
            Debug.Log($"ReceiveConfigJson: Audio feedback set to {audioFeedback}");
            if (serverInfo.compact == "true" && ChatWindow.Instance != null)
            {
                ChatWindow.Instance.SetCompact(true);
            }
            // Avatar-only: the page owns the chat; this build only renders the character
            // and speaks what the page sends to Speak.
            if (serverInfo.mode == "avatar" && ChatWindow.Instance != null)
            {
                ChatWindow.Instance.SetAvatarOnly(ParseFraming(serverInfo.framing));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing config JSON from JS: " + e.Message);
        }
    }

    // Receives current Blockly workspace XML state from Javascript
    public void ReceiveXml(string xml)
    {
        currentXml = xml;
        Debug.Log("ReceiveXml: Updated Blockly workspace XML cache.");
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(serverurl))
        {
            Debug.LogError("Server URL endpoint is not configured.");
        }

        Debug.Log("Server Started");
        animator = AgentRig.GetComponent<Animator>();
        voice = AgentAudio.GetComponent<AudioSource>();
        LoadAnimationClips();
        beatGestureNames = new List<string>();
        beatGestureNames.Add("thinking");
        beatGestureNames.Add("keepGoing");
        beatGestureNames.Add("leanInHandOut");
        beatGestureNames.Add("scratchHead");
    }

    private void LoadAnimationClips()
    {
        animationClips = new Dictionary<string, AnimationClip>();
        AnimationClip[] loadedClips = Resources.LoadAll<AnimationClip>("gestures");
        foreach (AnimationClip clip in loadedClips)
        {
            animationClips[clip.name] = clip;
            Debug.Log(clip.name);
        }
    }

    public void SendConversation(Conversation conversation)
    {
        if (conversation == null)
        {
            Debug.LogWarning("SendConversation: conversation is null.");
            return;
        }
        StartCoroutine(SendConversationToLLM(conversation));
    }

    public void SendPrompt(string promptText)
    {
        if (string.IsNullOrEmpty(promptText))
        {
            Debug.LogWarning("Prompt text is empty.");
            return;
        }

        if (ChatWindow.Instance != null && ChatWindow.Instance.ActiveConversation != null)
        {
            SendConversation(ChatWindow.Instance.ActiveConversation);
            return;
        }

        Debug.Log("Sending query to LLM: " + promptText);
        StartCoroutine(SendRequestToLLM(promptText));
    }

    private IEnumerator SendConversationToLLM(Conversation conversation)
    {
        string conversationJson = JsonUtility.ToJson(conversation);
        string conversationId = conversation.id;
        Debug.Log($"Sending Conversation (ID: {conversationId}) to: {serverurl}{endpoint}\nPayload: {conversationJson}");

        // Build form data so Flask's request.form.get('input') receives the whole conversation JSON
        WWWForm form = new WWWForm();
        form.AddField("input", conversationJson);
        form.AddField("student_id", studentId);
        if (!string.IsNullOrEmpty(conversationId))
        {
            form.AddField("session_id", conversationId);
        }
        if (!string.IsNullOrEmpty(currentXml))
        {
            form.AddField("xml", currentXml);
        }
        form.AddField("audioFeedback", audioFeedback ? "true" : "false");

        UnityWebRequest request = UnityWebRequest.Post(serverurl + endpoint, form);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if ((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
        {
            Debug.LogError("Error contacting LLM: " + request.error + "\nResponse: " + request.downloadHandler?.text);
        }
        else
        {
            float timeout = 5000, timer = 0;

            while (!request.isDone)
            {
                timer += Time.deltaTime;
                if (timer >= timeout)
                {
                    Debug.Log("Timeout happened");
                    yield break;
                }
                yield return null;
            }

            if (string.IsNullOrEmpty(request.error))
            {
                string response_text = request.downloadHandler.text;
                Debug.Log("Raw Response text: " + response_text);

                string responseText = "";
                string responseAudio = "";
                bool parseSuccess = false;

                try
                {
                    var ai_response = JsonUtility.FromJson<APIResponse>(response_text);
                    responseText = ai_response.response_text;
                    responseAudio = ai_response.response_audio;
                    parseSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }

                if (parseSuccess)
                {
                    Debug.Log("Received text response: " + responseText);

                    // Add response message to the specific chat tab that initiated the request
                    if (ChatWindow.Instance != null)
                    {
                        ChatWindow.Instance.AddMessageToConversationById(conversationId, "ai", responseText, false);
                    }

                    // Run request to get audio file if configured and present in response
                    if (audioFeedback && !string.IsNullOrEmpty(responseAudio))
                    {
                        Debug.Log("Downloading audio: " + responseAudio);
                        UnityWebRequest audio_request = UnityWebRequestMultimedia.GetAudioClip(responseAudio, AudioType.WAV);
                        yield return audio_request.SendWebRequest();

                        if ((audio_request.result == UnityWebRequest.Result.ConnectionError) || (audio_request.result == UnityWebRequest.Result.ProtocolError))
                        {
                            Debug.LogWarning("Audio download failed: " + audio_request.error);
                            StartCoroutine(PlayAnimation());
                        }
                        else
                        {
                            while (!audio_request.isDone)
                            {
                                timer += Time.deltaTime;
                                if (timer >= timeout)
                                {
                                    Debug.Log("Timeout happened");
                                    yield break;
                                }
                                yield return null;
                            }

                            if (string.IsNullOrEmpty(audio_request.error))
                            {
                                AudioClip audioClip = ((DownloadHandlerAudioClip)audio_request.downloadHandler).audioClip;
                                voice.clip = audioClip;
                                voice.Play();
                                StartCoroutine(PlayAnimation());
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(PlayAnimation());
                    }
                }
            }
        }
    }

    private IEnumerator SendRequestToLLM(string userInput)
    {
        // Build JSON request payload matching server.py API specifications
        var requestData = new RequestData
        {
            session_id = "default_unity_session",
            student_id = studentId,
            input = userInput,
            xml = currentXml,
            audioFeedback = audioFeedback
        };
        string jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log("Sending JSON Request to: " + serverurl + endpoint + "\nPayload: " + jsonPayload);

        UnityWebRequest request = new UnityWebRequest(serverurl + endpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if ((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
        {
            Debug.LogError("Error contacting LLM: " + request.error + "\nResponse: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Form upload complete!");
            float timeout = 5000, timer = 0;

            while (!request.isDone)
            {
                timer += Time.deltaTime;
                if (timer >= timeout)
                {
                    Debug.Log("Timeout happened");
                    yield break;
                }
                yield return null;
            }

            if (string.IsNullOrEmpty(request.error))
            {
                string response_text = request.downloadHandler.text;
                Debug.Log("Raw Response text: " + response_text);

                string responseText = "";
                string responseAudio = "";
                bool parseSuccess = false;

                try
                {
                    var ai_response = JsonUtility.FromJson<APIResponse>(response_text);
                    responseText = ai_response.response_text;
                    responseAudio = ai_response.response_audio;
                    parseSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Error parsing JSON response: " + e.Message);
                }

                if (parseSuccess)
                {
                    Debug.Log("Received text response: " + responseText);

                    // Add response message to the UI conversation
                    if (ChatWindow.Instance != null)
                    {
                        ChatWindow.Instance.AddMessageToCurrentConversation("ai", responseText, false);
                    }

                    // Run request to get audio file if configured and present in response
                    if (audioFeedback && !string.IsNullOrEmpty(responseAudio))
                    {
                        Debug.Log("Downloading audio: " + responseAudio);
                        UnityWebRequest audio_request = UnityWebRequestMultimedia.GetAudioClip(responseAudio, AudioType.WAV);
                        yield return audio_request.SendWebRequest();

                        if ((audio_request.result == UnityWebRequest.Result.ConnectionError) || (audio_request.result == UnityWebRequest.Result.ProtocolError))
                        {
                            Debug.LogWarning("Audio download failed: " + audio_request.error);
                            StartCoroutine(PlayAnimation());
                        }
                        else
                        {
                            while (!audio_request.isDone)
                            {
                                timer += Time.deltaTime;
                                if (timer >= timeout)
                                {
                                    Debug.Log("Timeout happened");
                                    yield break;
                                }
                                yield return null;
                            }

                            if (string.IsNullOrEmpty(audio_request.error))
                            {
                                AudioClip audioClip = ((DownloadHandlerAudioClip)audio_request.downloadHandler).audioClip;
                                voice.clip = audioClip;
                                voice.Play();
                                StartCoroutine(PlayAnimation());
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(PlayAnimation());
                    }
                }
            }
        }
    }

    private IEnumerator PlayAnimation()
    {
        Random rnd = new Random();
        if (audioFeedback && voice.isPlaying)
        {
            while (voice.isPlaying)
            {
                int waitTime = rnd.Next(3, 8);
                int gestureNum = rnd.Next(0, 4);
                animator.Play(beatGestureNames[gestureNum]);
                yield return new WaitForSeconds(waitTime);
            }
        }
        else
        {
            int waitTime = rnd.Next(3, 8);
            int gestureNum = rnd.Next(0, 4);
            animator.Play(beatGestureNames[gestureNum]);
            yield return new WaitForSeconds(waitTime);
        }
        yield break;
    }

    // Proactive check-ins arrive from the embedding page (webgl/index.html relays the
    // backend's push stream) rather than as a reply to a request: show them in the open
    // chat and, with audio on, speak them like any reply.
    // Avatar-only mode: the page sends each reply to say, {text, audio}. A new line
    // interrupts the current one, like a person answering the latest question.
    public void Speak(string json)
    {
        SpeechLine line;
        try
        {
            line = JsonUtility.FromJson<SpeechLine>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing speech JSON from JS: " + e.Message);
            return;
        }
        if (line == null)
        {
            return;
        }
        voice.Stop();
        if (!string.IsNullOrEmpty(line.audio))
        {
            StartCoroutine(PlayProactiveAudio(line.audio));
        }
        else
        {
            StartCoroutine(PlayAnimation());
        }
    }

    // Stop talking now (the student muted the voice mid-sentence).
    public void Hush()
    {
        voice.Stop();
    }

    // How much of the character the camera shows, from the head down: 1 is head to
    // feet, ~0.45 head and shoulders. The page changes it with its layout (phone).
    public void SetFraming(string fraction)
    {
        if (ChatWindow.Instance != null)
        {
            ChatWindow.Instance.FrameAvatar(ParseFraming(fraction));
        }
    }

    private static float ParseFraming(string value)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float fraction) ? fraction : 1f;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AvatarSpeakingChanged(int speaking);
#else
    private static void AvatarSpeakingChanged(int speaking) { }
#endif

    private bool wasSpeaking = false;

    // Tell the page when speech starts and stops (AvatarEvents.jslib), so it can show
    // which reply is being spoken.
    private void Update()
    {
        bool speaking = voice != null && voice.isPlaying;
        if (speaking != wasSpeaking)
        {
            wasSpeaking = speaking;
            AvatarSpeakingChanged(speaking ? 1 : 0);
        }
    }

    public void ReceiveProactiveJson(string json)
    {
        ProactiveMessage message;
        try
        {
            message = JsonUtility.FromJson<ProactiveMessage>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing proactive JSON from JS: " + e.Message);
            return;
        }
        if (message == null || string.IsNullOrEmpty(message.text))
        {
            return;
        }

        if (ChatWindow.Instance != null)
        {
            ChatWindow.Instance.AddMessageToCurrentConversation("ai", message.text, false);
        }

        if (audioFeedback && !string.IsNullOrEmpty(message.audio))
        {
            StartCoroutine(PlayProactiveAudio(message.audio));
        }
        else
        {
            StartCoroutine(PlayAnimation());
        }
    }

    private IEnumerator PlayProactiveAudio(string url)
    {
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return audioRequest.SendWebRequest();
            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Proactive audio download failed: " + audioRequest.error);
                StartCoroutine(PlayAnimation());
                yield break;
            }
            voice.clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            voice.Play();
            StartCoroutine(PlayAnimation());
        }
    }

    [System.Serializable]
    private class SpeechLine
    {
        public string text;
        public string audio;
    }

    [System.Serializable]
    private class ProactiveMessage
    {
        public string text;
        public string audio;
    }

    [System.Serializable]
    private class RequestData
    {
        public string session_id;
        public string student_id;
        public string input;
        public string xml;
        public bool audioFeedback;
    }

    [System.Serializable]
    private class APIResponse
    {
        public string response_text;
        public string response_audio;
    }

    [System.Serializable]
    private class ServerInfo
    {
        public string serverurl;
        public string endpoint;
        public string audioFeedback;
        public string student_id;
        public string compact;
        public string mode;
        public string framing;
    }
}
