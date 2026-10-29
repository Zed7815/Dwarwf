using UnityEngine;

public class StageSelectPopupButton : MonoBehaviour
{
    [Header("UI演出参照")]
    [Tooltip("マウスを乗せた時に表示する赤い枠Sprite")]
    public GameObject hoverFrameObject;
    [Tooltip("クリア済みで星未獲得時に表示するビックリマーク")]
    public GameObject missingStarVisual;

    [Header("Animatorステート名（大文字小文字に注意）")]
    public string lockedStateName = "Locked";       // 挑戦不可時のidol
    public string availableStateName = "Available"; // 挑戦可能時のidol
    public string clearedStateName = "Cleared";     // クリア済み時のidol

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
    public void SetupButton(bool unlocked, bool isCleared, bool hasStar, bool hasStarFeatureInStage, string sceneName, GimmickType gimmick, StageSelectNest nest)
    {
        isUnlocked = unlocked;
        targetSceneName = sceneName;
        targetGimmick = gimmick;
        parentNest = nest;

        // ★3つのidolアニメーションの切り替え
        if (anim != null)
        {
            if (!isUnlocked)
            {
                // ① 挑戦不可
                anim.Play(lockedStateName, 0, 0f);
            }
            else if (isCleared)
            {
                // ③ クリア済みidol
                anim.Play(clearedStateName, 0, 0f);
            }
            else
            {
                // ② 挑戦可能idol
                anim.Play(availableStateName, 0, 0f);
            }
        }

        // ★ビックリマークの表示判定（星が存在するステージのみ）
        if (missingStarVisual != null)
        {
            // 「星が存在するステージ」かつ「クリア済み」かつ「星未獲得」のときだけ表示
            bool showWarning = hasStarFeatureInStage && isCleared && !hasStar;
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
                if (parentNest != null) parentNest.ClosePopup();
                SceneLoader.Load(targetSceneName, targetGimmick);
            }
        }
        else
        {
            if (deniedSE != null && audioSource != null) audioSource.PlayOneShot(deniedSE);
        }
    }
}