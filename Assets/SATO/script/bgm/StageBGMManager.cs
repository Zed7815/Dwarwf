using UnityEngine;

public class StageBGMManager : MonoBehaviour
{
    [Header("BGM設定")]
    public AudioClip editModeBGM;  // 全ステージ共通の編集BGM
    public AudioClip playModeBGM;  // そのステージ固有の実行BGM
    public float volume = 0.5f;

    private AudioSource audioSource;
    private GameManager gameManager;
    private GameManager.GameState lastState;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = volume;

        gameManager = GameManager.instance;
        if (gameManager != null)
        {
            lastState = gameManager.currentState;
            SwitchBGM(lastState);
        }
    }

    void Update()
    {
        if (gameManager == null) return;

        // モードが切り替わった瞬間を検知
        if (gameManager.currentState != lastState)
        {
            lastState = gameManager.currentState;
            SwitchBGM(lastState);
        }
    }

    void SwitchBGM(GameManager.GameState state)
    {
        audioSource.Stop();

        if (state == GameManager.GameState.Edit)
        {
            audioSource.clip = editModeBGM;
        }
        else
        {
            audioSource.clip = playModeBGM;
        }

        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}