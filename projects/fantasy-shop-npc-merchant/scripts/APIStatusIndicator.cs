using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class APIStatusIndicator : MonoBehaviour
{
    public enum ConnectionStatus
    {
        Disconnected,
        Checking,
        Connected,
        Error,
        Fallback
    }

    public TextMeshProUGUI statusText;
    public Image statusDot;
    public TextMeshProUGUI providerText;

    public Color disconnectedColor = new Color(0.5f, 0.5f, 0.5f);
    public Color checkingColor = new Color(1f, 0.8f, 0f);
    public Color connectedColor = new Color(0f, 1f, 0f);
    public Color errorColor = new Color(1f, 0f, 0f);
    public Color fallbackColor = new Color(1f, 0.5f, 0f);

    private ConnectionStatus currentStatus = ConnectionStatus.Disconnected;

    private void Start()
    {
        if (statusText == null)
            statusText = transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        
        if (statusDot == null)
            statusDot = transform.Find("StatusDot")?.GetComponent<Image>();
        
        if (providerText == null)
            providerText = transform.Find("ProviderText")?.GetComponent<TextMeshProUGUI>();

        if (providerText != null)
            providerText.gameObject.SetActive(false);

        SetStatus(ConnectionStatus.Checking, "Ready");
    }

    public void SetStatus(ConnectionStatus status, string message)
    {
        currentStatus = status;
        
        if (statusText != null)
            statusText.text = message;

        if (statusDot != null)
        {
            switch (status)
            {
                case ConnectionStatus.Disconnected:
                    statusDot.color = disconnectedColor;
                    break;
                case ConnectionStatus.Checking:
                    statusDot.color = checkingColor;
                    break;
                case ConnectionStatus.Connected:
                    statusDot.color = connectedColor;
                    break;
                case ConnectionStatus.Error:
                    statusDot.color = errorColor;
                    break;
                case ConnectionStatus.Fallback:
                    statusDot.color = fallbackColor;
                    break;
            }
        }
    }

    public void OnAPISuccess(string provider)
    {
        string cleanProvider = CleanProviderName(provider);
        SetStatus(ConnectionStatus.Connected, cleanProvider);
    }

    public void OnAPIFallback(string provider)
    {
        string cleanProvider = CleanProviderName(provider);
        SetStatus(ConnectionStatus.Fallback, $"{cleanProvider} (Local)");
    }

    public void OnAPIError(string errorMessage)
    {
        SetStatus(ConnectionStatus.Error, errorMessage);
    }

    private string CleanProviderName(string provider)
    {
        if (string.IsNullOrEmpty(provider))
            return "Connected";

        string clean = provider.Trim();
        
        if (clean.ToLower().Contains("cohere"))
            return "Cohere";
        else if (clean.ToLower().Contains("ollama"))
            return "Ollama";
        else if (clean.ToLower().Contains("llama"))
            return "Llama";
        else if (clean.ToLower().Contains("gpt"))
            return "GPT";
        else
            return clean;
    }
}
