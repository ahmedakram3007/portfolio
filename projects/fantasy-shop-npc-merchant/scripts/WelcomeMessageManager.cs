using UnityEngine;
using System.Collections;

public class WelcomeMessageManager : MonoBehaviour
{
    [Header("Welcome Settings")]
    public string welcomeMessage = "Greetings and welcome, traveller. How may I be of service?";
    
    [Range(0f, 5f)]
    public float delayBeforeWelcome = 1f;
    
    public bool showWelcomeOnStart = true;
    
    private ChatManager chatManager;
    private bool welcomeShown = false;
    
    private void Start()
    {
        if (showWelcomeOnStart)
        {
            StartCoroutine(ShowWelcomeMessage());
        }
    }
    
    [ContextMenu("Show Welcome Message")]
    public void TriggerWelcomeMessage()
    {
        if (!welcomeShown)
        {
            StartCoroutine(ShowWelcomeMessage());
        }
    }
    
    private IEnumerator ShowWelcomeMessage()
    {
        yield return new WaitForSeconds(delayBeforeWelcome);
        
        if (chatManager == null)
        {
            chatManager = FindFirstObjectByType<ChatManager>();
        }
        
        if (chatManager != null)
        {
            bool chatWasClosed = chatManager.chatPanel != null && !chatManager.chatPanel.activeSelf;
            
            if (chatWasClosed)
            {
                chatManager.ToggleChatPanel();
            }
            
            yield return new WaitForSeconds(0.2f);
            
            NPCVoiceManager voiceManager = FindFirstObjectByType<NPCVoiceManager>();
            if (voiceManager != null)
            {
                StartCoroutine(voiceManager.PlayVoiceForText(welcomeMessage));
            }
            
            chatManager.AddNPCMessage(welcomeMessage);
            welcomeShown = true;
            
            if (chatManager.playerInputField != null)
            {
                yield return new WaitForEndOfFrame();
                chatManager.playerInputField.ActivateInputField();
                chatManager.playerInputField.Select();
            }
            
            Debug.Log("Welcome message displayed");
        }
        else
        {
            Debug.LogWarning("ChatManager not found. Cannot show welcome message.");
        }
    }
}
