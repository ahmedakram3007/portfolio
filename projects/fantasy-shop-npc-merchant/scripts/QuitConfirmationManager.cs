using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class QuitConfirmationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject quitPanel;
    public CanvasGroup quitCanvasGroup;
    public Button yesButton;
    public Button noButton;
    public TextMeshProUGUI messageText;
    
    [Header("Settings")]
    public Key quitKey = Key.Escape;
    public string confirmationMessage = "Are you sure you want to return to desktop?";
    
    [Header("Button Colors")]
    public Color yesButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color noButtonColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    
    private bool isQuitPanelActive = false;
    private float lastToggleTime = 0f;
    private const float TOGGLE_COOLDOWN = 0.3f;
    
    private void Start()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
        
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
            ColorBlock yesColors = yesButton.colors;
            yesColors.normalColor = yesButtonColor;
            yesColors.highlightedColor = yesButtonColor * 1.2f;
            yesColors.pressedColor = yesButtonColor * 0.8f;
            yesButton.colors = yesColors;
        }
        
        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
            ColorBlock noColors = noButton.colors;
            noColors.normalColor = noButtonColor;
            noColors.highlightedColor = noButtonColor * 1.2f;
            noColors.pressedColor = noButtonColor * 0.8f;
            noButton.colors = noColors;
        }
        
        if (messageText != null)
        {
            messageText.text = confirmationMessage;
        }
    }
    
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[quitKey].wasPressedThisFrame)
        {
            float timeSinceLastToggle = Time.unscaledTime - lastToggleTime;
            
            if (timeSinceLastToggle >= TOGGLE_COOLDOWN)
            {
                if (!isQuitPanelActive)
                {
                    ShowQuitConfirmation();
                }
                else
                {
                    HideQuitConfirmation();
                }
                
                lastToggleTime = Time.unscaledTime;
            }
        }
    }
    
    public void ShowQuitConfirmation()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(true);
            isQuitPanelActive = true;
            Time.timeScale = 0f;
        }
    }
    
    public void HideQuitConfirmation()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
            isQuitPanelActive = false;
            Time.timeScale = 1f;
        }
    }
    
    private void OnYesClicked()
    {
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void OnNoClicked()
    {
        HideQuitConfirmation();
    }
}
