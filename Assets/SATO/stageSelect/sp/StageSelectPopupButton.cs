using UnityEngine;
using System.Collections;

public class StageSelectPopupButton : MonoBehaviour
{
    // ★追加：画面全体の「遷移中ロック」フラグ（どこをクリックしても無視させる）
    public static bool isAnyTransitioning = false;

    [Header("ホバー枠演出参照")]
    public GameObject hoverFrameObject;
    public SpriteRenderer hoverFrameRenderer;

    [Header("ホバー枠の切り替え設定（画像 ＆ 色）")]
    public Sprite normalHoverSprite;
    public Color normalHoverColor = Color.white;
    public Sprite clearedHoverSprite;
    public Color clearedHoverColor = Color.yellow;

    [Header("UI演出参照")]
    public GameObject missingStarVisual;

    [Header("演出スクリプト参照")]
    public nextscene fadeOutScript;

    [Header("Animatorステート名")]
    public string lockedStateName = "Locked";
    public string availableStateName = "Available";
    public string clearedStateName = "Cleared";

    [Header("SE設定")]
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip deniedSE;

    private Animator anim;
    private AudioSource audioSource;

    private bool isUnlocked = false;
    private bool isStageCleared = false;
    private string targetSceneName;
    private GimmickType targetGimmick;
    private StageSelectNest parentNest;
    private string currentStateName;

    void Awake()
    {
        isAnyTransitioning = false; // シーン開始時にロック解除
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (hoverFrameObject != null)
        {
            if (hoverFrameRenderer == null)
            {
                hoverFrameRenderer = hoverFrameObject.GetComponent<SpriteRenderer>()
                                   ?? hoverFrameObject.GetComponentInChildren<SpriteRenderer>();
            }
            hoverFrameObject.SetActive(false);
        }

        if (fadeOutScript == null) fadeOutScript = FindObjectOfType<nextscene>();
    }

    void OnEnable()
    {
        PlayCurrentAnimation();
    }

    public void SetupButton(bool unlocked, bool isCleared, bool hasStar, bool hasStarFeatureInStage, string sceneName, GimmickType gimmick, StageSelectNest nest)
    {
        isUnlocked = unlocked;
        isStageCleared = isCleared;
        targetSceneName = sceneName;
        targetGimmick = gimmick;
        parentNest = nest;

        UpdateHoverFrameVisual();

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

    void UpdateHoverFrameVisual()
    {
        if (hoverFrameRenderer == null && hoverFrameObject != null)
        {
            hoverFrameRenderer = hoverFrameObject.GetComponent<SpriteRenderer>()
                               ?? hoverFrameObject.GetComponentInChildren<SpriteRenderer>();
        }

        if (hoverFrameRenderer != null)
        {
            if (isStageCleared)
            {
                if (clearedHoverSprite != null) hoverFrameRenderer.sprite = clearedHoverSprite;
                hoverFrameRenderer.color = clearedHoverColor;
            }
            else
            {
                if (normalHoverSprite != null) hoverFrameRenderer.sprite = normalHoverSprite;
                hoverFrameRenderer.color = normalHoverColor;
            }
        }
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
        if (isUnlocked && !isAnyTransitioning)
        {
            UpdateHoverFrameVisual();
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
        // 遷移中なら連打を完全無視
        if (isAnyTransitioning) return;

        if (isUnlocked)
        {
            StartCoroutine(LoadSequence());
        }
        else
        {
            if (deniedSE != null && audioSource != null) audioSource.PlayOneShot(deniedSE);
        }
    }

    IEnumerator LoadSequence()
    {
        isAnyTransitioning = true;

        // ★追加：遊んだ巣穴の番号を保存する！
        if (parentNest != null)
        {
            PlayerPrefs.SetInt("LastSelectedNest", parentNest.nestIndex);
            PlayerPrefs.Save();
        }

        if (clickSE != null && audioSource != null) audioSource.PlayOneShot(clickSE);

        if (fadeOutScript != null)
        {
            yield return fadeOutScript.StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.3f);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneLoader.Load(targetSceneName, targetGimmick);
        }
    }
}