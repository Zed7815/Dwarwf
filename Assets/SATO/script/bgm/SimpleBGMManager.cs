using UnityEngine;

public class SimpleBGMManager : MonoBehaviour
{
    public static SimpleBGMManager instance;

    public AudioClip bgmClip;
    public float volume = 0.5f;
    private AudioSource bgmAudioSource; // BGM専用スピーカー

    void Awake()
    {
        instance = this;

        // ★BGM専用の独立したAudioSourceを作成
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;

        float savedVol = PlayerPrefs.GetFloat("SavedBGMVolume", volume);
        bgmAudioSource.volume = savedVol;
        bgmAudioSource.Play();
    }

    // ★BGMの音量だけを確実に変更する（他のSEには1ミリも干渉しない）
    public static void SetVolume(float vol)
    {
        if (instance != null && instance.bgmAudioSource != null)
        {
            instance.bgmAudioSource.volume = vol;
        }
    }

    // 他のスクリプトがBGMのスピーカーを誤認しないように公開
    public AudioSource GetBGMSource()
    {
        return bgmAudioSource;
    }
}