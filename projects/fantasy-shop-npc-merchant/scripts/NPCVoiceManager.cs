using UnityEngine;
using System.Collections;

public class NPCVoiceManager : MonoBehaviour
{
    [Header("Voice Settings")]
    public AudioClip[] voiceBlips;
    
    [Range(0f, 1f)]
    public float voiceVolume = 0.4f;
    
    [Range(0.01f, 0.2f)]
    public float blipInterval = 0.05f;
    
    public bool playVoiceOnType = true;
    
    private AudioSource voiceSource;
    
    [ContextMenu("Setup NPC Voice")]
    public void SetupVoice()
    {
        if (voiceSource == null)
        {
            GameObject voiceObj = new GameObject("Voice_AudioSource");
            voiceObj.transform.SetParent(transform);
            voiceSource = voiceObj.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.volume = voiceVolume;
        }
        
        #if UNITY_EDITOR
        if (voiceBlips == null || voiceBlips.Length == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip blip", new[] { "Assets/Audio" });
            if (guids.Length > 0)
            {
                voiceBlips = new AudioClip[Mathf.Min(guids.Length, 5)];
                for (int i = 0; i < voiceBlips.Length; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    voiceBlips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
                Debug.Log($"Auto-loaded {voiceBlips.Length} voice blip clips");
            }
            else
            {
                Debug.LogWarning("No voice blip AudioClips found. Place blip sounds in Assets/Audio folder or assign manually.");
            }
        }
        #endif
        
        Debug.Log("NPC Voice Manager setup complete");
    }
    
    private void Start()
    {
        if (voiceSource == null)
        {
            GameObject voiceObj = new GameObject("Voice_AudioSource");
            voiceObj.transform.SetParent(transform);
            voiceSource = voiceObj.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.volume = voiceVolume;
        }
    }
    
    public void PlayVoiceBlip()
    {
        if (voiceSource != null && voiceBlips != null && voiceBlips.Length > 0)
        {
            AudioClip randomBlip = voiceBlips[Random.Range(0, voiceBlips.Length)];
            if (randomBlip != null)
            {
                voiceSource.pitch = Random.Range(0.95f, 1.05f);
                voiceSource.PlayOneShot(randomBlip, voiceVolume);
            }
        }
    }
    
    public IEnumerator PlayVoiceForText(string text)
    {
        if (!playVoiceOnType || string.IsNullOrEmpty(text))
        {
            yield break;
        }
        
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                PlayVoiceBlip();
            }
            yield return new WaitForSeconds(blipInterval);
        }
    }
}
