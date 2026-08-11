using UnityEngine;
using TMPro;

public class ChatUILayoutFix : MonoBehaviour
{
    private void Start()
    {
        FixChatContentOnStart();
    }

    private void FixChatContentOnStart()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Transform viewport = canvas.transform.Find("ChatPanel/ChatScrollView/Viewport");
            if (viewport != null)
            {
                var mask = viewport.GetComponent<UnityEngine.UI.Mask>();
                if (mask != null)
                {
                    Object.Destroy(mask);
                }
                
                var rectMask = viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                if (rectMask == null)
                {
                    viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                }
            }
            
            Transform chatContent = canvas.transform.Find("ChatPanel/ChatScrollView/Viewport/ChatContent");
            if (chatContent != null)
            {
                var rectTransform = chatContent.GetComponent<RectTransform>();
                
                var layout = chatContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                }
                
                var contentSizeFitter = chatContent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitter.enabled = false;
                }
                
                rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
            }
        }
    }

    [ContextMenu("Fix Chat Layout NOW")]
    public void FixChatLayout()
    {
        FixChatContent();
        FixMessagePrefabs();
    }

    private void FixChatContent()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        Transform chatContent = canvas.transform.Find("ChatPanel/ChatScrollView/Viewport/ChatContent");
        if (chatContent != null)
        {
            var rectTransform = chatContent.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
            
            var layout = chatContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 10, 10);
            }
        }
    }

    private void FixMessagePrefabs()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        Transform chatContent = canvas.transform.Find("ChatPanel/ChatScrollView/Viewport/ChatContent");
        if (chatContent == null) return;

        foreach (Transform child in chatContent)
        {
            if (child.name.Contains("Line"))
            {
                FixMessageObject(child.gameObject);
            }
        }
    }

    public static void FixMessageObject(GameObject messageObject)
    {
        var sizeFitter = messageObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.enabled = false;
        }
        
        var rectTransform = messageObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(0, 50);
            
            var textMesh = messageObject.GetComponent<TextMeshProUGUI>();
            if (textMesh != null)
            {
                textMesh.overflowMode = TextOverflowModes.Overflow;
                textMesh.textWrappingMode = TextWrappingModes.Normal;
                textMesh.alignment = TextAlignmentOptions.TopLeft;
                textMesh.margin = new Vector4(10, 5, 10, 5);
                textMesh.fontSize = 20;
            }
        }
    }
}
