using UnityEngine;

public class ShopBGMManager : MonoBehaviour
{
    [Header("BGM Settings")]
    public AudioClip medievalBGM;
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    public bool playOnStart = true;
    
    private AudioSource bgmSource;
    
    private void Start()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
        bgmSource.clip = medievalBGM;
        
        if (playOnStart && medievalBGM != null) bgmSource.Play();
    }
    
    public void Play() => bgmSource?.Play();
    public void Stop() => bgmSource?.Stop();
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }
    
    public void SetupBGM()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.playOnAwake = false;
            bgmSource.clip = medievalBGM;
        }
        if (playOnStart && medievalBGM != null) bgmSource.Play();
    }
}
