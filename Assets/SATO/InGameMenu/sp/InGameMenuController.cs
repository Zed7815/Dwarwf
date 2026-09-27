using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InGameMenuController : MonoBehaviour
{
    public static InGameMenuController instance;

    [Header("UIパネル参照")]
    public GameObject menuModalPanel;   // メニューのポップアップ親オブジェクト
    public GameObject guideBookPanel;   // ギミック図鑑のサブパネル（任意）

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip openSE;
    public AudioClip closeSE;
    public AudioClip clickSE;

    private float previousTimeScale = 1f;
    private bool isMenuOpen = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
    }

    // --- メニューの開閉 ---

    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        if (audioSource != null && openSE != null) audioSource.PlayOneShot(openSE);

        // 現在のゲーム速度を記憶して一時停止
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (menuModalPanel != null) menuModalPanel.SetActive(true);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;

        if (audioSource != null && closeSE != null) audioSource.PlayOneShot(closeSE);

        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);

        // 時間を元に戻す
        Time.timeScale = previousTimeScale;
    }

    // --- ボタン1：最初からやり直す（全リセット） ---

    public void OnFullResetClicked()
    {
        PlayClickSE();

        // 1. チェックポイントを完全に消去（旗を未通過色に戻す）
        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.ClearCheckpoint();
        }

        // 2. メニューを閉じて時間を戻す
        CloseMenu();

        // 3. 通常のリセットを実行（これでステージ初期位置・初期状態でリスタート！）
        if (GameManager.instance != null)
        {
            GameManager.instance.ResetGame();
        }

        Debug.Log("<color=yellow>【最初からやり直す（全リセット実行）】</color>");
    }

    // --- ボタン2：ギミック図鑑を開く ---

    public void OnOpenGuideClicked()
    {
        PlayClickSE();
        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(true);
    }

    public void OnCloseGuideClicked()
    {
        PlayClickSE();
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
        if (menuModalPanel != null) menuModalPanel.SetActive(true);
    }

    // --- ボタン3：ステージセレクトへ戻る ---

    public void OnReturnToStageSelectClicked()
    {
        PlayClickSE();
        Time.timeScale = 1f; // 時間を確実に戻す
        StartCoroutine(ReturnSequence());
    }

    IEnumerator ReturnSequence()
    {
        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene("StageSelect");
    }

    void PlayClickSE()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }
}