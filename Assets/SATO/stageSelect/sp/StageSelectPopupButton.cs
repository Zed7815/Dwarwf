using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectPopupButton : MonoBehaviour
{
    [Header("UI演出参照")]
    [Tooltip("マウスを乗せた時に表示する赤い枠Sprite")]
    public GameObject hoverFrameObject;
    [Tooltip("クリア済みで星未獲得時に表示するビックリマーク")]
    public GameObject missingStarVisual;

    [Header("アニメーター設定")]
    [Tooltip("挑戦可能な時にTRUEになるBoolパラメータ名")]
    public string unlockedBoolName = "isUnlocked";

    [Header("SE設定")]
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip deniedSE;

    private Animator anim;
    private AudioSource audioSource;

    private bool isUnlocked = false;
    private string targetSceneName;
    private GimmickType targetGimmick;
    private StageSelectNest parentNest;

    void Awake()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (hoverFrameObject != null) hoverFrameObject.SetActive(false);
    }

    /// <summary>
    /// 巣スクリプトからボタンの状態をセットアップ
    /// </summary>
    public void SetupButton(bool unlocked, bool isCleared, bool hasStar, string sceneName, GimmickType gimmick, StageSelectNest nest)
    {
        isUnlocked = unlocked;
        targetSceneName = sceneName;
        targetGimmick = gimmick;
        parentNest = nest;

        // アニメーションの切り替え（挑戦可能か不可か）
        if (anim != null)
        {
            anim.SetBool(unlockedBoolName, isUnlocked);
        }

        // ビックリマークの表示（クリア済み かつ 星未獲得）
        if (missingStarVisual != null)
        {
            bool showWarning = isCleared && !hasStar;
            missingStarVisual.SetActive(showWarning);
        }

        if (hoverFrameObject != null) hoverFrameObject.SetActive(false);
    }

    void OnMouseEnter()
    {
        // 挑戦可能な場合のみ、ふちが赤い枠を表示しSEを鳴らす
        if (isUnlocked)
        {
            if (hoverFrameObject != null) hoverFrameObject.SetActive(true);
            if (hoverSE != null && audioSource != null) audioSource.PlayOneShot(hoverSE);
        }
    }

    void OnMouseExit()
    {
        if (hoverFrameObject != null) hoverFrameObject.SetActive(false);
    }

    void OnMouseDown()
    {
        if (isUnlocked)
        {
            if (clickSE != null && audioSource != null) audioSource.PlayOneShot(clickSE);

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                // ポップアップを閉じてシーン遷移
                if (parentNest != null) parentNest.ClosePopup();
                SceneLoader.Load(targetSceneName, targetGimmick);
            }
        }
        else
        {
            // ロックされている場合は拒否音を鳴らす
            if (deniedSE != null && audioSource != null) audioSource.PlayOneShot(deniedSE);
        }
    }
}