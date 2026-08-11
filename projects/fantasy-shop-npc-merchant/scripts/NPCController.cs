using UnityEngine;
using UnityEngine.InputSystem;

public class NPCController : MonoBehaviour
{
    [Header("References")]
    public ChatManager chatManager;
    
    [Header("Interaction Settings")]
    public Key interactionKey = Key.E;
    public bool showInteractionPrompt = false;
    
    [Header("Animation Settings (Optional)")]
    public Animator npcAnimator;
    public string idleAnimationName = "Idle";
    
    private Keyboard keyboard;

    private void Start()
    {
        keyboard = Keyboard.current;
        
        if (chatManager == null)
        {
            chatManager = FindFirstObjectByType<ChatManager>();
        }
        
        if (npcAnimator == null)
        {
            npcAnimator = GetComponent<Animator>();
        }
        
        if (npcAnimator != null && npcAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(idleAnimationName))
        {
            npcAnimator.Play(idleAnimationName);
        }
    }

    private void Update()
    {
        if (keyboard != null && keyboard[interactionKey].wasPressedThisFrame)
        {
            if (chatManager != null && !IsChatOpen())
            {
                chatManager.ToggleChatPanel();
            }
        }
    }

    private void OnGUI()
    {
        if (showInteractionPrompt && !IsChatOpen())
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(0.95f, 0.9f, 0.75f, 1f);
            style.alignment = TextAnchor.MiddleCenter;
            
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0.1f, 0.05f, 0f, 0.8f);
            
            string promptText = $"Press [{interactionKey}] to talk to the merchant";
            
            float width = 400;
            float height = 50;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - 150;
            
            Rect shadowRect = new Rect(x + 2, y + 2, width, height);
            GUI.Label(shadowRect, promptText, shadowStyle);
            
            Rect rect = new Rect(x, y, width, height);
            GUI.Label(rect, promptText, style);
        }
    }

    private bool IsChatOpen()
    {
        return chatManager != null && chatManager.chatPanel != null && chatManager.chatPanel.activeSelf;
    }
}
