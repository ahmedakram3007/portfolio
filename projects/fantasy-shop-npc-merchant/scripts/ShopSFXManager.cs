using UnityEngine;

public class ShopSFXManager : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioClip messageSentSFX;
    public AudioClip messageReceivedSFX;
    public AudioClip buttonClickSFX;
    public AudioClip panelOpenSFX;
    public AudioClip panelCloseSFX;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;
    
    private AudioSource sfxSource;
    
    [ContextMenu("Setup SFX Manager")]
    public void SetupSFX()
    {
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_AudioSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
        }
        
        #if UNITY_EDITOR
        if (messageSentSFX == null)
        {
            messageSentSFX = FindAudioClip("send");
        }
        if (messageReceivedSFX == null)
        {
            messageReceivedSFX = FindAudioClip("receive");
        }
        if (buttonClickSFX == null)
        {
            buttonClickSFX = FindAudioClip("click");
        }
        if (panelOpenSFX == null)
        {
            panelOpenSFX = FindAudioClip("open");
        }
        if (panelCloseSFX == null)
        {
            panelCloseSFX = FindAudioClip("close");
        }
        #endif
        
        Debug.Log("SFX Manager setup complete");
    }
    
    #if UNITY_EDITOR
    private AudioClip FindAudioClip(string searchTerm)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:AudioClip {searchTerm}", new[] { "Assets/Audio" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Debug.Log($"Auto-loaded SFX: {clip.name}");
            return clip;
        }
        return null;
    }
    #endif
    
    private void Start()
    {
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_AudioSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
        }
    }
    
    public void PlayMessageSent()
    {
        PlaySFX(messageSentSFX);
    }
    
    public void PlayMessageReceived()
    {
        PlaySFX(messageReceivedSFX);
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }
    
    public void PlayPanelOpen()
    {
        PlaySFX(panelOpenSFX);
    }
    
    public void PlayPanelClose()
    {
        PlaySFX(panelCloseSFX);
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
