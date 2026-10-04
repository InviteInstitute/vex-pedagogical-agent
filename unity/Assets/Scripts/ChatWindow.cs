using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

[System.Serializable]
public class ChatMessage
{
    public string speaker;
    public string text;
    public bool isUser;
}

[System.Serializable]
public class Conversation
{
    public string id;
    public string title;
    public List<ChatMessage> messages = new List<ChatMessage>();
}

public class ChatWindow : MonoBehaviour
{
    public static ChatWindow Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] public ChatLLM chatLLM;
    [SerializeField] public GameObject agentObject;

    [Header("UI References (Optional - Auto-generated if null)")]
    [SerializeField] public Canvas mainCanvas;
    [SerializeField] public RectTransform sidebarPanel;
    [SerializeField] public Transform sidebarContent;
    [SerializeField] public Button plusButton;
    [SerializeField] public Button toggleSidebarButton;
    [SerializeField] public RectTransform rightPanel;
    [SerializeField] public TMP_InputField inputField;
    [SerializeField] public Button sendButton;
    [SerializeField] public GameObject chatContent;
    [SerializeField] public ScrollRect chatScrollRect;
    [SerializeField] public RectTransform agentContainer;

    [Header("Styling")]
    [SerializeField] public TMP_FontAsset fontAsset;
    [SerializeField] public Sprite backgroundChatBubbleImage;

    // VEXcode VR Light Theme Color Palette (matching VEXcode VR block style)
    private readonly Color vexBlueHeader = new Color(0.369f, 0.58f, 0.886f, 1f);   // #5E94E2 (VEX Header Blue)
    private readonly Color vexWhiteBg = new Color(1f, 1f, 1f, 1f);                  // #FFFFFF (Main Workspace Bg)
    private readonly Color vexSidebarBg = new Color(0.941f, 0.949f, 0.961f, 1f);    // #F0F2F5 (Sidebar Bg)
    private readonly Color vexSidebarHover = new Color(0.839f, 0.894f, 1f, 1f);     // #D6E4FF (Sidebar Selection Blue)
    private readonly Color vexBubbleUser = new Color(0.298f, 0.592f, 1.0f, 1f);     // #4C97FF (User message bubble: Drivetrain Blue block)
    private readonly Color vexBubbleAI = new Color(0.173f, 0.647f, 0.886f, 1f);      // #2CA5E2 (AI message bubble: Sensing Cyan block)
    private readonly Color vexInputBg = new Color(1f, 1f, 1f, 1f);                  // #FFFFFF (Input Bg)
    private readonly Color vexGold = new Color(1f, 0.72f, 0f, 1f);                  // #FFB800 (Plus Button: Gold "when started" block)
    private readonly Color vexBorder = new Color(0.369f, 0.58f, 0.886f, 1f);        // #5E94E2 (Outline border blue)

    // State
    private List<Conversation> conversations = new List<Conversation>();
    private Conversation activeConversation;
    private bool sidebarExpanded = true;
    // Set by SetCompact; read in Start too, since the config can arrive either side of it.
    private bool isCompact = false;
    private float sidebarWidth = 320f;

    // Agent Centering State
    private Vector3 agentInitialPosition;
    private Quaternion agentInitialRotation;
    private Vector3 agentShiftVector;
    private bool agentPositionInitialized = false;
    private Transform headBoneTransform;
    private bool foundHeadBone = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure DOTween is initialized
        DOTween.Init();
    }

    private void Start()
    {
        // Set the rect transformation position to x = 0, y = 0, z = 50
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition3D = new Vector3(0f, 0f, 50f);
        }

        // Adjust HistoryScroll to stretch fully to the edges of the sidebar at runtime
        if (sidebarContent != null)
        {
            ScrollRect sRectComp = sidebarContent.GetComponentInParent<ScrollRect>();
            if (sRectComp != null)
            {
                RectTransform sRect = sRectComp.GetComponent<RectTransform>();
                if (sRect != null)
                {
                    sRect.anchorMin = new Vector2(0, 0);
                    sRect.anchorMax = new Vector2(1, 1);
                    sRect.offsetMin = new Vector2(0, 10);
                    sRect.offsetMax = new Vector2(0, -10);
                }

                RectTransform vRect = sRectComp.viewport;
                if (vRect != null)
                {
                    vRect.anchorMin = Vector2.zero;
                    vRect.anchorMax = Vector2.one;
                    vRect.offsetMin = Vector2.zero;
                    vRect.offsetMax = Vector2.zero;
                }

                RectTransform cRect = sRectComp.content;
                if (cRect != null)
                {
                    cRect.anchorMin = new Vector2(0, 1);
                    cRect.anchorMax = new Vector2(1, 1);
                    cRect.pivot = new Vector2(0.5f, 1);
                    cRect.sizeDelta = new Vector2(0, cRect.sizeDelta.y);
                }
            }
        }

        // If ChatLLM is not assigned, try to find it
        if (chatLLM == null)
        {
            chatLLM = FindObjectOfType<ChatLLM>();
        }

        // Setup button listeners
        if (plusButton != null)
        {
            plusButton.onClick.AddListener(StartNewConversation);
        }

        if (toggleSidebarButton != null)
        {
            toggleSidebarButton.onClick.AddListener(ToggleSidebar);
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessageFromInput);
        }

        if (inputField != null)
        {
            inputField.onSubmit.AddListener((val) => SendMessageFromInput());
        }

        // Ensure agent container background matches the blue header color (clear when
        // compact, so the page shows through behind the avatar)
        if (agentContainer != null)
        {
            Image agentBg = agentContainer.GetComponent<Image>();
            if (agentBg != null)
            {
                agentBg.color = isCompact ? Color.clear : vexBlueHeader;
            }
        }

        // Initialize sidebar and right panel offsets based on initial state
        if (sidebarPanel != null)
        {
            sidebarPanel.sizeDelta = new Vector2(sidebarExpanded ? sidebarWidth : 0f, sidebarPanel.sizeDelta.y);
        }
        if (rightPanel != null)
        {
            rightPanel.offsetMin = new Vector2(sidebarExpanded ? sidebarWidth : 0f, rightPanel.offsetMin.y);
            
            // Adjust chatScrollRect to stretch horizontally to rightPanel
            if (chatScrollRect != null)
            {
                RectTransform csRect = chatScrollRect.GetComponent<RectTransform>();
                if (csRect != null)
                {
                    csRect.anchorMin = new Vector2(0, csRect.anchorMin.y);
                    csRect.anchorMax = new Vector2(1, csRect.anchorMax.y);
                    csRect.offsetMin = new Vector2(0, csRect.offsetMin.y);
                    csRect.offsetMax = new Vector2(0, csRect.offsetMax.y);
                }
            }
        }

        if (chatContent != null)
        {
            RectTransform ccRect = chatContent.GetComponent<RectTransform>();
            if (ccRect != null)
            {
                ccRect.anchorMin = new Vector2(0, 1);
                ccRect.anchorMax = new Vector2(1, 1);
                ccRect.pivot = new Vector2(0.5f, 1);
                ccRect.sizeDelta = new Vector2(0, ccRect.sizeDelta.y);
                ccRect.anchoredPosition = new Vector2(0, ccRect.anchoredPosition.y);
            }
        }

        // Start with a clean conversation, unless a message already opened one (a
        // proactive check-in relayed by the page can arrive before Start runs; a second
        // conversation here would hide it in the sidebar)
        if (activeConversation == null)
        {
            StartNewConversation();
        }
    }

    private void Update()
    {
        // Fallback: Send message on Enter key if inputField is focused
        if (inputField != null && inputField.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SendMessageFromInput();
        }
    }

    private void LateUpdate()
    {
        UpdateAgentPositionAndRotation();
    }

    private void UpdateAgentPositionAndRotation()
    {
        if (agentObject == null) return;

        // Verify if we need to initialize the initial position and shift vector
        if (!agentPositionInitialized)
        {
            if (Camera.main != null && sidebarPanel != null)
            {
                InitializeAgentOffsets();
            }
            return;
        }

        RectTransform agentRt = agentObject.GetComponent<RectTransform>();
        if (agentRt != null)
        {
            // If it's a UI element, just parent it and center it
            if (agentRt.parent != agentContainer)
            {
                agentRt.SetParent(agentContainer, false);
                agentRt.anchorMin = new Vector2(0.5f, 0.5f);
                agentRt.anchorMax = new Vector2(0.5f, 0.5f);
                agentRt.pivot = new Vector2(0.5f, 0.5f);
                agentRt.anchoredPosition3D = Vector3.zero;
            }
        }
        else if (sidebarPanel != null)
        {
            // If it's a 3D object, smoothly shift it along the calculated shift vector based on sidebar expansion state
            float currentWidth = sidebarPanel.sizeDelta.x;
            float t = Mathf.Clamp01(currentWidth / sidebarWidth); // 1 = fully expanded (sidebar at 320), 0 = fully retracted (sidebar at 0)
            
            // Interpolate position relative to initial position: when t=1 (expanded), agent stays at agentInitialPosition.
            // When t=0 (retracted), agent shifts by agentShiftVector.
            agentObject.transform.position = agentInitialPosition + (1f - t) * agentShiftVector;

            // Keep the rest of his body in the same orientation as defined in the editor
            agentObject.transform.rotation = agentInitialRotation;

            // Make only his head look at the camera using a relative delta rotation (avoids bone coordinate snapping)
            if (foundHeadBone && headBoneTransform != null && Camera.main != null)
            {
                // The default direction the character's face points in the rig (negative local Z-axis)
                Vector3 defaultFaceDir = agentInitialRotation * Vector3.back;
                Vector3 faceTargetDir = (Camera.main.transform.position - headBoneTransform.position).normalized;

                // Calculate the rotation from the default face direction to the camera target direction
                Quaternion deltaRotation = Quaternion.FromToRotation(defaultFaceDir, faceTargetDir);

                // Apply this relative offset on top of the animator's current frame head rotation
                headBoneTransform.rotation = deltaRotation * headBoneTransform.rotation;
            }
        }
    }

    private void InitializeAgentOffsets()
    {
        if (agentObject == null || Camera.main == null || sidebarPanel == null) return;

        agentInitialPosition = agentObject.transform.position;
        agentInitialRotation = agentObject.transform.rotation;

        // Try to retrieve the head bone from the Animator component
        Animator animator = agentObject.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            headBoneTransform = animator.GetBoneTransform(HumanBodyBones.Head);
        }
        if (headBoneTransform == null)
        {
            headBoneTransform = FindHeadBone(agentObject.transform);
        }
        foundHeadBone = (headBoneTransform != null);

        agentShiftVector = ComputeAgentShift();
        agentPositionInitialized = true;
    }

    // The world-space move the avatar makes as the sidebar collapses. Wide layout: half
    // the sidebar width on screen (320f / 2f = 160f pixels), keeping it under the chat.
    // Compact: whatever lands it in the middle of the frame.
    private Vector3 ComputeAgentShift()
    {
        // Project initial position to screen, subtract horizontal pixel shift, and project back to world space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(agentInitialPosition);
        float shiftPixels = isCompact ? screenPos.x - Screen.width * 0.5f : sidebarWidth / 2f;
        Vector3 shiftedScreenPos = screenPos - new Vector3(shiftPixels, 0f, 0f);
        Vector3 shiftedWorldPos = Camera.main.ScreenToWorldPoint(shiftedScreenPos);
        return shiftedWorldPos - agentInitialPosition;
    }

    private Transform FindHeadBone(Transform parent)
    {
        if (parent.name.ToLower().Contains("head"))
        {
            return parent;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindHeadBone(parent.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    public void StartNewConversation()
    {
        Conversation newConv = new Conversation
        {
            id = Guid.NewGuid().ToString(),
            title = "Conversation " + (conversations.Count + 1),
            messages = new List<ChatMessage>()
        };

        conversations.Add(newConv);
        LoadConversation(newConv);
    }

    public void LoadConversation(Conversation conversation)
    {
        activeConversation = conversation;

        // Force sidebar and right panel offsets to match current sidebarExpanded state
        if (sidebarPanel != null)
        {
            sidebarPanel.sizeDelta = new Vector2(sidebarExpanded ? sidebarWidth : 0f, sidebarPanel.sizeDelta.y);
        }
        if (rightPanel != null)
        {
            rightPanel.offsetMin = new Vector2(sidebarExpanded ? sidebarWidth : 0f, rightPanel.offsetMin.y);
        }

        // Clear existing bubble GameObjects
        if (chatContent != null)
        {
            foreach (Transform child in chatContent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Spawn bubbles for the selected conversation
        foreach (var msg in conversation.messages)
        {
            createTextBubble(msg.speaker, msg.text, msg.isUser);
        }

        // Rebuild layouts immediately to reflect offset and content changes
        if (chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        }
        if (chatScrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.GetComponent<RectTransform>());
        }

        // Scroll to the bottom
        StartCoroutine(ScrollToBottom());

        // Refresh sidebar lists to reflect active highlight
        RefreshSidebarList();
    }

    public Conversation ActiveConversation => activeConversation;
    public List<Conversation> Conversations => conversations;

    public Conversation GetConversationById(string id)
    {
        if (string.IsNullOrEmpty(id) || conversations == null)
            return null;
        return conversations.Find(c => c.id == id);
    }

    public void AddMessageToConversationById(string conversationId, string speaker, string text, bool isUser)
    {
        Conversation targetConv = GetConversationById(conversationId);
        if (targetConv == null)
        {
            targetConv = activeConversation;
        }

        if (targetConv == null)
        {
            StartNewConversation();
            targetConv = activeConversation;
        }

        ChatMessage newMsg = new ChatMessage
        {
            speaker = speaker,
            text = text,
            isUser = isUser
        };
        targetConv.messages.Add(newMsg);

        // If the target conversation is currently displayed in the chat window, render the bubble
        if (targetConv == activeConversation)
        {
            createTextBubble(speaker, text, isUser);
            StartCoroutine(ScrollToBottom());
        }
    }

    public void AddMessageToCurrentConversation(string speaker, string text, bool isUser)
    {
        if (activeConversation == null)
        {
            StartNewConversation();
        }

        ChatMessage newMsg = new ChatMessage
        {
            speaker = speaker,
            text = text,
            isUser = isUser
        };
        activeConversation.messages.Add(newMsg);

        createTextBubble(speaker, text, isUser);
        StartCoroutine(ScrollToBottom());
    }

    private void SendMessageFromInput()
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text))
            return;

        string prompt = inputField.text;
        inputField.text = "";

        // Add user message to UI and history of this specific active tab
        AddMessageToCurrentConversation("you", prompt, true);

        // Send the complete conversation history from this specific chat tab to the LLM (aligning with app.py)
        if (chatLLM != null)
        {
            chatLLM.SendConversation(activeConversation);
        }
        else
        {
            Debug.LogError("ChatLLM dependency is missing on ChatWindow.");
        }

        // Re-focus the input field and select it so typing can continue immediately
        inputField.ActivateInputField();
        inputField.Select();
    }

    public string GetActiveConversationHistory()
    {
        if (activeConversation == null || activeConversation.messages == null)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var msg in activeConversation.messages)
        {
            string label = msg.isUser ? "User" : "AI";
            sb.AppendLine($"{label}: {msg.text}");
        }
        return sb.ToString().TrimEnd();
    }

    // Compact mode for when the page embeds the avatar in a corner over VEXcode VR
    // (webgl/index.html ?embed=1): no conversation sidebar, and nothing painted behind
    // the avatar, so the playground shows through (TransparentBackground.jslib keeps
    // the canvas alpha).
    public void SetCompact(bool compact)
    {
        if (!compact)
        {
            return;
        }
        isCompact = true;
        // In a 440px frame a 320px sidebar would squeeze the chat to a sliver when opened.
        sidebarWidth = 220f;

        // The interface is a world-space board sized for a wide screen; in a tall corner
        // frame it runs off both sides. Draw it in screen space at the same depth, scaled
        // so one UI unit is one pixel of a 440px-wide frame (the 800px reference would
        // shrink 16px text to ~9px there).
        Canvas canvas = mainCanvas != null ? mainCanvas : GetComponent<Canvas>();
        Camera cam = Camera.main;
        if (canvas != null && cam != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            float depth = Vector3.Dot(canvas.transform.position - cam.transform.position, cam.transform.forward);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = depth > cam.nearClipPlane ? depth : canvas.planeDistance;
        }
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(440f, 720f);
            scaler.matchWidthOrHeight = 0f;
        }

        // Re-aim the avatar's collapse move at the frame's centre (if it was already
        // measured; otherwise InitializeAgentOffsets will use isCompact).
        if (agentPositionInitialized && cam != null)
        {
            agentShiftVector = ComputeAgentShift();
        }

        if (sidebarExpanded)
        {
            ToggleSidebar();
        }
        if (agentContainer != null)
        {
            Image agentBg = agentContainer.GetComponent<Image>();
            if (agentBg != null)
            {
                agentBg.color = Color.clear;
            }
        }
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.clear;
        }
    }

    public void ToggleSidebar()
    {
        sidebarExpanded = !sidebarExpanded;
        float targetWidth = sidebarExpanded ? sidebarWidth : 0f;

        // Smoothly animate sidebar width
        if (sidebarPanel != null)
        {
            DOTween.To(() => sidebarPanel.sizeDelta, x => sidebarPanel.sizeDelta = x, new Vector2(targetWidth, sidebarPanel.sizeDelta.y), 0.3f)
                .SetEase(Ease.OutQuad);
        }

        // Smoothly animate right panel expansion
        if (rightPanel != null)
        {
            DOTween.To(() => rightPanel.offsetMin, x => rightPanel.offsetMin = x, new Vector2(targetWidth, rightPanel.offsetMin.y), 0.3f)
                .SetEase(Ease.OutQuad);
        }
    }

    public void createTextBubble(string speaker, string text, bool isUser)
    {
        if (chatContent == null) return;

        // 1. Create Row container
        GameObject row = new GameObject("BubbleRow", typeof(RectTransform));
        row.transform.SetParent(chatContent.transform, false);
        
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot = new Vector2(0.5f, 1);
        rowRect.offsetMin = new Vector2(0, rowRect.offsetMin.y);
        rowRect.offsetMax = new Vector2(0, rowRect.offsetMax.y);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.padding = new RectOffset(16, 16, 4, 4);
        rowLayout.childAlignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

        // 2. Create the Text Bubble Card
        GameObject textBubble = new GameObject("TextBubble", typeof(RectTransform));
        textBubble.transform.SetParent(row.transform, false);
        
        Image textImage = textBubble.AddComponent<Image>();
        if (backgroundChatBubbleImage != null)
        {
            textImage.sprite = backgroundChatBubbleImage;
            textImage.type = Image.Type.Sliced;
        }

        // Color bubbles using VEXcode VR scheme
        textImage.color = isUser ? vexBubbleUser : vexBubbleAI;

        // Set layout rules for the bubble card: at most 600px, and never wider than the
        // message list allows (a fixed 600 clipped text in a narrow window).
        LayoutElement layoutElement = textBubble.AddComponent<LayoutElement>();
        float listWidth = chatContent.GetComponent<RectTransform>().rect.width - rowLayout.padding.horizontal;
        layoutElement.preferredWidth = listWidth > 0f ? Mathf.Min(600f, listWidth * 0.85f) : 600f;

        VerticalLayoutGroup bubbleLayout = textBubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.childControlHeight = true;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childForceExpandHeight = true;
        bubbleLayout.childForceExpandWidth = true;
        bubbleLayout.padding = new RectOffset(14, 14, 8, 8);

        // 3. Create the Text inside the card
        GameObject textBox = new GameObject("TextBox", typeof(RectTransform));
        textBox.transform.SetParent(textBubble.transform, false);
        
        TextMeshProUGUI textComponent = textBox.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            textComponent.font = fontAsset;
        }
        
        textComponent.fontSize = 16;
        textComponent.color = Color.white;
        textComponent.text = text; // Clean, no speaker name prefix
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.enableWordWrapping = true;

        ContentSizeFitter bubbleFitter = textBubble.AddComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Scale-in animation with DOTween
        textBubble.transform.localScale = Vector3.zero;
        textBubble.SetActive(true);
        StartCoroutine(AnimateBubble(textBubble));
    }

    private IEnumerator AnimateBubble(GameObject textBubble)
    {
        yield return new WaitForEndOfFrame();
        textBubble.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        }
        if (chatScrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.GetComponent<RectTransform>());
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void RefreshSidebarList()
    {
        if (sidebarContent == null) return;

        // Clear existing conversation entries
        foreach (Transform child in sidebarContent)
        {
            Destroy(child.gameObject);
        }

        // Render each conversation button
        foreach (var conv in conversations)
        {
            GameObject btnGO = new GameObject("ConvButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(sidebarContent, false);

            RectTransform btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(320, 52);

            Image img = btnGO.GetComponent<Image>();
            if (backgroundChatBubbleImage != null)
            {
                img.sprite = backgroundChatBubbleImage;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;
            img.raycastTarget = true;

            bool isActive = (conv == activeConversation);
            
            Button btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.normalColor = isActive ? vexSidebarHover : new Color(0.98f, 0.98f, 0.98f, 1.0f); // Crisp white background for inactive cards
            cb.highlightedColor = vexSidebarHover;
            cb.pressedColor = new Color(0.78f, 0.85f, 0.96f, 1.0f);
            cb.selectedColor = isActive ? vexSidebarHover : new Color(0.98f, 0.98f, 0.98f, 1.0f);
            btn.colors = cb;

            // Highlight bar on the left for the active item
            if (isActive)
            {
                GameObject accent = new GameObject("ActiveAccent", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(btnGO.transform, false);
                RectTransform accRect = accent.GetComponent<RectTransform>();
                accRect.anchorMin = new Vector2(0, 0);
                accRect.anchorMax = new Vector2(0, 1);
                accRect.pivot = new Vector2(0, 0.5f);
                accRect.sizeDelta = new Vector2(4, 0);
                accRect.anchoredPosition = Vector2.zero;

                Image accImg = accent.GetComponent<Image>();
                accImg.color = vexGold; // Gold active selection bar
                accImg.raycastTarget = false; // Accent bar should not intercept raycasts
            }

            // Create Text
            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(isActive ? 20 : 14, 0);
            textRect.offsetMax = new Vector2(sidebarWidth, 0);
            textRect.localScale = Vector3.one;

            TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                if (fontAsset != null)
                {
                    textComp.font = fontAsset;
                }
                textComp.text = conv.title;
                textComp.fontSize = 16; // Matches the font size of the message window bubbles!
                textComp.color = new Color(0.12f, 0.12f, 0.13f, 1f); // Dark text on light sidebar
                textComp.alignment = TextAlignmentOptions.Left;
                textComp.enableWordWrapping = false;
                textComp.overflowMode = TextOverflowModes.Ellipsis;
                textComp.raycastTarget = true; // Enable raycast target on text so clicks bubble up and trigger the parent Button!
            }

            // Add click callback
            Conversation localConv = conv;
            btn.onClick.AddListener(() => LoadConversation(localConv));
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate UI in Editor")]
    public void GenerateUIInEditor()
    {
        // Set the rect transformation position to x = 0, y = 0, z = 50
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition3D = new Vector3(0f, 0f, 50f);
            UnityEditor.EditorUtility.SetDirty(rt);
        }

        // Find canvas if null
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }

        // 1. Destroy old programmatic UI panels under the canvas to avoid duplicates
        if (mainCanvas != null)
        {
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in mainCanvas.transform)
            {
                if (child.name == "SidebarPanel" || child.name == "RightPanel")
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in childrenToDestroy)
            {
                DestroyImmediate(go);
            }
        }

        // 2. Clear serialized references to trigger a clean regeneration
        sidebarPanel = null;
        sidebarContent = null;
        plusButton = null;
        toggleSidebarButton = null;
        rightPanel = null;
        inputField = null;
        sendButton = null;
        chatContent = null;
        chatScrollRect = null;
        agentContainer = null;


        // 4. Mark dirty to ensure Unity saves the scene/prefab modifications
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        
        Debug.Log("Successfully generated ChatGPT UI GameObjects in the Editor! Save the scene/prefab to persist.");
    }
#endif
}
