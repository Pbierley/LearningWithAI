using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Global Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource loopingSfxSource;

    [Header("Common SFX Clips")]
    [SerializeField] private AudioClip gestureTing;
    [SerializeField] private AudioClip gestureThump;
    [SerializeField] private AudioClip spellConfirm;
    [SerializeField] private AudioClip uiClick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persists across scenes
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip && sfxSource)
            sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayGestureTing()
    {
        PlayOneShot(gestureThump);
        PlayOneShot(gestureTing, 0.1f);
    } 

    public void PlaySpellConfirm() => PlayOneShot(spellConfirm);
    public void PlayUIClick() => PlayOneShot(uiClick);

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!musicSource || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
    {
        if (!musicSource || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    /// <summary>
    /// Returns true if the specified clip is currently playing as music.
    /// </summary>
    public bool IsMusicPlaying(AudioClip clip)
    {
        return musicSource != null && musicSource.isPlaying && musicSource.clip == clip;
    }

    public void PlayLoopingSFX(AudioClip clip, float startOffset = 0f)
    {
        if (!loopingSfxSource || clip == null) return;
        loopingSfxSource.clip = clip;
        loopingSfxSource.loop = true;
        loopingSfxSource.time = Mathf.Clamp(startOffset, 0f, clip.length - 0.1f);
        loopingSfxSource.Play();
    }

    public void StopLoopingSFX()
    {
        if (loopingSfxSource && loopingSfxSource.isPlaying)
            loopingSfxSource.Stop();
    }

    /// <summary>
    /// Returns the current playback time of the music in seconds.
    /// </summary>
    public float GetMusicTime()
    {
        return musicSource != null ? musicSource.time : 0f;
    }
}
