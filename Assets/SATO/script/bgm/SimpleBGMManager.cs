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
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        audioSource.Play();
    }
}
