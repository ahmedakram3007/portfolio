using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Security.Cryptography.X509Certificates;

public class AcceptAllCertificates : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}

public class LLMAPIManager : MonoBehaviour
{
    public string flaskEndpoint = "http://127.0.0.1:5000/chat";
    public float requestTimeout = 60f;
    
    [TextArea(3, 10)]
    public string systemPrompt = "You are a friendly medieval merchant in a fantasy shop. You sell various items like potions, weapons, and magical artifacts. Respond in character, keeping your answers concise and engaging (2-3 sentences max).";
    
    public bool debugMode = false;
    private string debugSystemPrompt = "You are a helpful AI assistant. Respond accurately and concisely.";
    
    private APIStatusIndicator statusIndicator;
    public string lastUsedProvider = "Not connected";
    private string preferredProvider = "";
    
    private void Start()
    {
        statusIndicator = FindFirstObjectByType<APIStatusIndicator>();
        if (statusIndicator != null)
        {
            statusIndicator.SetStatus(APIStatusIndicator.ConnectionStatus.Checking, "Ready");
        }
    }
    
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }
    
    public void SetEndpoint(string endpoint, string providerName = "")
    {
        flaskEndpoint = endpoint;
        Debug.Log($"[LLM] Endpoint changed to: {endpoint}");
        
        // Save the provider preference if one was given
        if (!string.IsNullOrEmpty(providerName))
        {
            lastUsedProvider = providerName;
            preferredProvider = providerName;
            Debug.Log($"[LLM] Preferred provider set to: {providerName}");
        }
        
        // Update the status indicator to show we're switching providers
        if (statusIndicator != null)
        {
            string displayText = string.IsNullOrEmpty(providerName) ? "Endpoint changed" : providerName;
            statusIndicator.SetStatus(APIStatusIndicator.ConnectionStatus.Checking, displayText);
        }
    }
    
    public void TestAPIConnection()
    {
        SendMessage("Hello", (response, success) =>
        {
            Debug.Log(success ? $"Connection test successful: {response}" : $"Connection test failed: {response}");
        });
    }
    
    public void SendMessage(string userMessage, Action<string, bool> callback)
    {
        StartCoroutine(SendChatRequest(userMessage, callback));
    }
    
    private IEnumerator SendChatRequest(string userMessage, Action<string, bool> callback)
    {
        // Choose which system prompt to use (debug or merchant)
        string activePrompt = debugMode ? debugSystemPrompt : systemPrompt;
        
        // Create the request data with message, prompt, and provider preference
        FlaskRequest requestData = new FlaskRequest 
        { 
            message = userMessage, 
            system_prompt = activePrompt,
            preferred_provider = preferredProvider
        };
        string requestBody = JsonUtility.ToJson(requestData);
        
        Debug.Log($"[LLM] Sending request to: {flaskEndpoint}");
        Debug.Log($"[LLM] Request body: {requestBody}");
        
        // Set up the web request
        UnityWebRequest request = new UnityWebRequest(flaskEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)requestTimeout;
        
        // Accept all SSL certificates (needed for local testing)
        request.certificateHandler = new AcceptAllCertificates();
        request.disposeCertificateHandlerOnDispose = true;
        
        Debug.Log("[LLM] Sending web request...");
        yield return request.SendWebRequest();
        
        // Log the response for debugging
        Debug.Log($"[LLM] Response code: {request.responseCode}");
        Debug.Log($"[LLM] Result: {request.result}");
        Debug.Log($"[LLM] Error: {request.error}");
        
        // Check if request was successful
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[LLM] Raw response: {request.downloadHandler.text}");
            string extractedMessage = ParseFlaskResponse(request.downloadHandler.text);
            
            if (!string.IsNullOrEmpty(extractedMessage))
            {
                Debug.Log($"[LLM] Extracted message: {extractedMessage}");
                Debug.Log($"[LLM] Provider: {lastUsedProvider}");
                
                // Update status indicator based on which provider was used
                if (statusIndicator != null)
                {
                    if (lastUsedProvider.ToLower().Contains("cohere"))
                        statusIndicator.OnAPISuccess(lastUsedProvider);
                    else if (lastUsedProvider.ToLower().Contains("llama") || lastUsedProvider.ToLower().Contains("ollama"))
                        statusIndicator.OnAPIFallback(lastUsedProvider);
                    else
                        statusIndicator.OnAPISuccess(lastUsedProvider);
                }
                callback?.Invoke(extractedMessage, true);
            }
            else
            {
                Debug.LogError("[LLM] Flask returned empty response!");
                if (statusIndicator != null) 
                    statusIndicator.OnAPIError("No Response");
                callback?.Invoke("[ERROR] Flask returned empty response.", false);
            }
        }
        else
        {
            // Request failed - log error details
            Debug.LogError($"[LLM] Request failed: {request.error}");
            Debug.LogError($"[LLM] Response code: {request.responseCode}");
            Debug.LogError($"[LLM] Download handler text: {request.downloadHandler?.text}");
            
            if (statusIndicator != null)
            {
                statusIndicator.OnAPIError(request.responseCode == 0 ? "Disconnected" : $"Error {request.responseCode}");
            }
            
            // Create a user-friendly error message
            string errorMsg = request.responseCode == 0 
                ? "[ERROR] Cannot connect to Flask server. Start: python flask_server.py"
                : $"[ERROR] HTTP {request.responseCode}: {request.error}";
            
            callback?.Invoke(errorMsg, false);
        }
        
        // Clean up the request
        request.Dispose();
    }
    
    private string ParseFlaskResponse(string jsonResponse)
    {
        try
        {
            Debug.Log($"[LLM] Parsing JSON: {jsonResponse}");
            FlaskResponse response = JsonUtility.FromJson<FlaskResponse>(jsonResponse);
            
            if (response != null && !string.IsNullOrEmpty(response.response))
            {
                // Check if Flask told us which provider it used
                if (!string.IsNullOrEmpty(response.provider))
                {
                    string providerFromResponse = !string.IsNullOrEmpty(response.model) ? response.model : response.provider;
                    
                    // Only update provider if we don't have a preference set
                    // This keeps the UI showing what the user chose
                    if (string.IsNullOrEmpty(preferredProvider))
                    {
                        lastUsedProvider = providerFromResponse;
                        Debug.Log($"[LLM] Provider set to: {lastUsedProvider}");
                    }
                    else
                    {
                        Debug.Log($"[LLM] Keeping preferred provider: {preferredProvider} (Flask said: {providerFromResponse})");
                    }
                }
                return response.response.Trim();
            }
            Debug.LogWarning("[LLM] Response object is null or empty, returning raw JSON");
            return jsonResponse;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLM] JSON parsing failed: {e.Message}");
            return jsonResponse;
        }
    }
    
    [Serializable]
    private class FlaskRequest
    {
        public string message;
        public string system_prompt;
        public string preferred_provider;
    }
    
    [Serializable]
    private class FlaskResponse
    {
        public string response;
        public string provider;
        public string model;
    }
}
