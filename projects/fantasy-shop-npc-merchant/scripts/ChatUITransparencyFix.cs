using UnityEngine;
using UnityEngine.UI;

public class ChatUITransparencyFix : MonoBehaviour
{
    [Header("Transparency Settings")]
    [Range(0f, 1f)]
    public float panelAlpha = 0.4f;
    
    [Range(0f, 1f)]
    public float headerAlpha = 0.5f;
    
    [Range(0f, 1f)]
    public float inputAlpha = 0.5f;
    
    [ContextMenu("Apply Transparency")]
    public void ApplyTransparency()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        Transform chatPanelTransform = canvas.transform.Find("ChatPanel");
        if (chatPanelTransform == null) return;
        
        GameObject chatPanel = chatPanelTransform.gameObject;
        
        Image panelImage = chatPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            Color color = panelImage.color;
            color.a = panelAlpha;
            panelImage.color = color;
        }
        
        Transform header = chatPanel.transform.Find("ChatHeader");
        if (header != null)
        {
            Image headerImage = header.GetComponent<Image>();
            if (headerImage != null)
            {
                Color color = headerImage.color;
                color.a = headerAlpha;
                headerImage.color = color;
            }
        }
        
        Transform inputField = chatPanel.transform.Find("InputArea/PlayerInput");
        if (inputField != null)
        {
            Image inputImage = inputField.GetComponent<Image>();
            if (inputImage != null)
            {
                Color color = inputImage.color;
                color.a = inputAlpha;
                inputImage.color = color;
            }
        }
    }
    
    private void Start()
    {
        Invoke(nameof(ApplyTransparency), 0.5f);
    }
    
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            Invoke(nameof(ApplyTransparency), 0.5f);
        }
    }
}
