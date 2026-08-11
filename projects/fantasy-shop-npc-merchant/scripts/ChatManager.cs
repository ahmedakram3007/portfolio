using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject chatPanel;
    public ScrollRect chatScrollView;
    public Transform chatContent;
    public TMP_InputField playerInputField;
    public Button sendButton;
    
    [Header("Message Prefabs")]
    public GameObject npcMessagePrefab;
    public GameObject playerMessagePrefab;
    public GameObject typingIndicatorPrefab;
    
    [Header("Settings")]
    public string npcName = "Merchant";
    public bool debugMode = false;
    
    private LLMAPIManager apiManager;
    private ShopSFXManager sfxManager;
    private NPCVoiceManager voiceManager;
    private GameObject activeTypingIndicator;

    private void Start()
    {
        apiManager = FindFirstObjectByType<LLMAPIManager>();
        sfxManager = FindFirstObjectByType<ShopSFXManager>();
        voiceManager = FindFirstObjectByType<NPCVoiceManager>();
        
        if (chatPanel != null) 
            chatPanel.SetActive(false);
            
        if (sendButton != null) 
            sendButton.onClick.AddListener(OnSendButtonClicked);
    }

    private void Update()
    {
        // Check if chat is open and keyboard is available
        if (chatPanel == null || !chatPanel.activeSelf || Keyboard.current == null) 
            return;
        
        // Check if player pressed Enter (but not Shift+Enter for new line)
        bool enterPressed = Keyboard.current.enterKey.wasPressedThisFrame || 
                          Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        bool shiftHeld = Keyboard.current.leftShiftKey.isPressed || 
                        Keyboard.current.rightShiftKey.isPressed;
        
        // Send message when Enter is pressed without Shift
        if (enterPressed && !shiftHeld)
            OnSendButtonClicked();
    }

    public void ToggleChatPanel()
    {
        if (chatPanel == null) return;
        
        if (!chatPanel.activeSelf)
        {
            chatPanel.SetActive(true);
            if (sfxManager != null) sfxManager.PlayPanelOpen();
            if (playerInputField != null) StartCoroutine(FocusInputField());
        }
    }
    
    private IEnumerator FocusInputField()
    {
        yield return new WaitForEndOfFrame();
        if (playerInputField != null)
        {
            playerInputField.ActivateInputField();
            playerInputField.Select();
        }
    }
    
    private bool HandleDebugCommands(string message)
    {
        string lower = message.ToLower();
        
        // Debug mode command - switches to generic LLM for testing
        if (lower == "/test" || lower == "/debug")
        {
            AddPlayerMessage(message);
            if (apiManager != null) apiManager.SetDebugMode(true);
            AddNPCMessage("[DEBUG MODE] Switched to generic LLM. Use /npc to return.");
            return true;
        }
        
        // NPC mode command - switches back to merchant character
        if (lower == "/npc" || lower == "/merchant")
        {
            AddPlayerMessage(message);
            if (apiManager != null) apiManager.SetDebugMode(false);
            AddNPCMessage("[NPC MODE] Switched to merchant roleplay. Use /test for generic LLM.");
            return true;
        }
        
        // Llama provider command - uses local Llama model
        if (lower == "/llama" || lower == "/ollama")
        {
            AddPlayerMessage(message);
            if (apiManager != null) apiManager.SetEndpoint("http://127.0.0.1:5000/chat", "Llama");
            AddNPCMessage("[LLAMA] Endpoint set to local Flask server (127.0.0.1:5000)");
            return true;
        }
        
        // Cohere provider command - uses Cohere API
        if (lower == "/cohere")
        {
            AddPlayerMessage(message);
            if (apiManager != null) apiManager.SetEndpoint("http://127.0.0.1:5000/chat", "Cohere");
            AddNPCMessage("[COHERE] Endpoint set to local Flask server (127.0.0.1:5000)");
            return true;
        }
        
        // Status command - shows current endpoint and provider
        if (lower == "/status")
        {
            AddPlayerMessage(message);
            if (apiManager != null)
            {
                AddNPCMessage($"[STATUS] Endpoint: {apiManager.flaskEndpoint}\\nProvider: {apiManager.lastUsedProvider}");
            }
            return true;
        }
        
        return false;
    }
    
    public void CloseChatPanel()
    {
        if (chatPanel != null && chatPanel.activeSelf)
        {
            chatPanel.SetActive(false);
            if (sfxManager != null) sfxManager.PlayPanelClose();
        }
    }

    public void OnSendButtonClicked()
    {
        // Make sure input field exists and has text
        if (playerInputField == null || string.IsNullOrWhiteSpace(playerInputField.text)) 
            return;

        string message = playerInputField.text.Trim();
        
        // Check if it's a slash command first
        if (HandleDebugCommands(message))
        {
            playerInputField.text = "";
            playerInputField.ActivateInputField();
            return;
        }
        
        // Handle goodbye messages with a special closing animation
        if (message.ToLower() == "goodbye" || message.ToLower() == "bye" || message.ToLower() == "exit")
        {
            AddPlayerMessage(message);
            if (sfxManager != null) sfxManager.PlayMessageSent();
            
            // Pick a random farewell message
            string[] farewells = {
                "Farewell, traveller. May the road rise to meet you!",
                "Safe travels, friend. Come back anytime!",
                "Until we meet again, adventurer!"
            };
            AddNPCMessage(farewells[Random.Range(0, farewells.Length)]);
            StartCoroutine(CloseAfterFarewell());
            return;
        }
        
        // Normal message - show it and send to API
        AddPlayerMessage(message);
        if (sfxManager != null) sfxManager.PlayMessageSent();
        
        // Clear input and refocus so player can type again
        playerInputField.text = "";
        playerInputField.ActivateInputField();
        
        // Send the message to the LLM API
        if (apiManager != null)
        {
            StartCoroutine(SendMessageToAPI(message));
        }
        else
        {
            AddNPCMessage("[ERROR] LLMAPIManager not found.");
        }
    }
    
    private IEnumerator CloseAfterFarewell()
    {
        yield return new WaitForSeconds(2f);
        if (chatPanel != null && chatPanel.activeSelf)
        {
            chatPanel.SetActive(false);
            if (sfxManager != null) sfxManager.PlayPanelClose();
        }
        playerInputField.text = "";
    }

    public void AddPlayerMessage(string message)
    {
        CreateMessageBubble($"<b>You:</b> {message}", false);
        ForceScrollToBottom();
    }

    public void AddNPCMessage(string message)
    {
        CreateMessageBubble($"<b>{npcName}:</b> {message}", true);
        if (sfxManager != null) sfxManager.PlayMessageReceived();
        if (voiceManager != null) StartCoroutine(voiceManager.PlayVoiceForText(message));
        ForceScrollToBottom();
    }

    private void CreateMessageBubble(string messageText, bool isNPC)
    {
        if (chatContent == null) return;

        GameObject messagePrefab = isNPC ? npcMessagePrefab : playerMessagePrefab;
        if (messagePrefab == null) return;
        
        GameObject messageObject = Instantiate(messagePrefab, chatContent);
        TextMeshProUGUI textComponent = messageObject.GetComponentInChildren<TextMeshProUGUI>();
        
        if (textComponent != null)
        {
            textComponent.text = messageText;
            StartCoroutine(FixMessageNextFrame(messageObject));
        }
    }

    private void ScrollToBottom()
    {
        if (chatScrollView != null) StartCoroutine(SmoothScrollToBottom());
    }
    
    private void ForceScrollToBottom()
    {
        if (chatScrollView != null)
        {
            StartCoroutine(ForceScrollToBottomCoroutine());
        }
    }
    
    private IEnumerator ForceScrollToBottomCoroutine()
    {
        yield return null;
        
        if (chatScrollView != null && chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
            Canvas.ForceUpdateCanvases();
            chatScrollView.verticalNormalizedPosition = 0f;
            
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            chatScrollView.verticalNormalizedPosition = 0f;
            
            yield return new WaitForEndOfFrame();
            chatScrollView.verticalNormalizedPosition = 0f;
        }
    }

    private IEnumerator SmoothScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (chatScrollView != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollView.verticalNormalizedPosition = 0f;
            
            yield return new WaitForEndOfFrame();
            chatScrollView.verticalNormalizedPosition = 0f;
        }
    }

    public IEnumerator SendMessageToAPI(string userMessage)
    {
        ShowTypingIndicator();
        
        bool responseReceived = false;
        string apiResponse = "";
        
        apiManager.SendMessage(userMessage, (response, success) =>
        {
            apiResponse = response;
            responseReceived = true;
        });
        
        float timeout = 0f;
        while (!responseReceived && timeout < 30f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }
        
        HideTypingIndicator();
        
        if (responseReceived)
        {
            AddNPCMessage(apiResponse);
        }
        else
        {
            AddNPCMessage("[ERROR] Request timed out. Check Flask server.");
        }
    }

    public void ShowTypingIndicator()
    {
        if (chatContent != null && activeTypingIndicator == null && npcMessagePrefab != null)
        {
            activeTypingIndicator = Instantiate(npcMessagePrefab, chatContent);
            TextMeshProUGUI textComponent = activeTypingIndicator.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.text = $"<b>{npcName}:</b> ...";
            StartCoroutine(FixMessageNextFrame(activeTypingIndicator));
            ForceScrollToBottom();
        }
    }

    public void HideTypingIndicator()
    {
        if (activeTypingIndicator != null)
        {
            Destroy(activeTypingIndicator);
            activeTypingIndicator = null;
        }
    }

    private IEnumerator FixMessageNextFrame(GameObject messageObject)
    {
        yield return null;
        ChatUILayoutFix.FixMessageObject(messageObject);
        ForceScrollToBottom();
    }
}
