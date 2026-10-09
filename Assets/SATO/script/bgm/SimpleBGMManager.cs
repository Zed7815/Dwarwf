using UnityEngine;

public class SimpleBGMManager : MonoBehaviour
{
    public AudioClip bgmClip;
    public float volume = 0.5f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;

        // セーブされた音量があればそれを使い、なければデフォルトのvolumeを使う
        float savedVol = PlayerPrefs.GetFloat("SavedBGMVolume", volume);
        audioSource.volume = savedVol;

        audioSource.playOnAwake = false;
        audioSource.Play();
    }
}
