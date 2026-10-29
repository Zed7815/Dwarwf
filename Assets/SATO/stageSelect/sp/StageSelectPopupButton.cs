using UnityEngine;
using System.Collections;

public class StageSelectPopupButton : MonoBehaviour
{
    [Header("UI演出参照")]
    [Tooltip("マウスを乗せた時に表示する赤い枠Sprite")]
    public GameObject hoverFrameObject;
    [Tooltip("クリア済みで星未獲得時に表示するビックリマーク")]
    public GameObject missingStarVisual;

    [Header("★演出スクリプト参照")]
    [Tooltip("黒い板で暗転させるnextsceneスクリプト（空なら自動でシーン内から探します）")]
    public nextscene fadeOutScript;

    [Header("Animatorステート名")]
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
    private string currentStateName;
    private bool isTransitioning = false; // 二重クリック防止フラグ

    void Awake()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (hoverFrameObject != null) hoverFrameObject.SetActive(false);
        if (fadeOutScript == null) fadeOutScript = FindObjectOfType<nextscene>();
    }

    void OnEnable()
    {
        isTransitioning = false;
        PlayCurrentAnimation();
    }

    public void SetupButton(bool unlocked, bool isCleared, bool hasStar, bool hasStarFeatureInStage, string sceneName, GimmickType gimmick, StageSelectNest nest)
    {
        isUnlocked = unlocked;
        targetSceneName = sceneName;
        targetGimmick = gimmick;
        parentNest = nest;

        if (!isUnlocked) currentStateName = lockedStateName;
        else if (isCleared) currentStateName = clearedStateName;
        else currentStateName = availableStateName;

        PlayCurrentAnimation();

        if (missingStarVisual != null)
        {
            bool showWarning = hasStarFeatureInStage && isCleared && !hasStar;
            missingStarVisual.SetActive(showWarning);
        }

        if (hoverFrameObject != null) hoverFrameObject.SetActive(false);
    }

    public void PlayCurrentAnimation()
    {
        if (anim == null) anim = GetComponent<Animator>();

        if (anim != null && !string.IsNullOrEmpty(currentStateName) && gameObject.activeInHierarchy)
        {
            anim.Play(currentStateName, 0, 0f);
            anim.Update(0f);
        }
    }

    void OnMouseEnter()
    {
        if (isUnlocked && !isTransitioning)
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
        if (isTransitioning) return;

        if (isUnlocked)
        {
            StartCoroutine(LoadSequence());
        }
        else
        {
            if (deniedSE != null && audioSource != null) audioSource.PlayOneShot(deniedSE);
        }
    }

    // ★フェード演出を挟んでからシーンを読み込む処理
    IEnumerator LoadSequence()
    {
        isTransitioning = true;

        if (clickSE != null && audioSource != null) audioSource.PlayOneShot(clickSE);

        // ★修正点1：ここで ClosePopup() を呼ぶと自分自身が非アクティブになってエラーになるため、呼ばない！
        // （黒幕が降りて画面全体が隠れるため、閉じる必要はありません）

        // 黒幕が降りてくるアニメーションを実行
        if (fadeOutScript != null)
        {
            // ★修正点2：fadeOutScript 自身にコルーチンを実行させる（最も安全）
            yield return fadeOutScript.StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.3f);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneLoader.Load(targetSceneName, targetGimmick);
        }
    }
}