using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class StageSelectCameraController : MonoBehaviour
{
    public static bool isComingFromTitle = true;

    [Header("移動制限")]
    public float minX;
    public float maxX;

    [Header("ホイールスクロール設定")]
    [Tooltip("ホイールを回した時の移動速度")]
    public float scrollSensitivity = 0.05f;

    [Header("プレイヤー演出設定")]
    public Animator playerAnimator;
    public SpriteRenderer playerSR;
    public string animBoolName = "isWalking";
    public float walkStopDelay = 0.15f;
    private float stopTimer;

    [Header("イントロ歩き演出設定")]
    public bool playIntroWalk = true;
    public float introStartDelay = 0.8f;
    public float introWalkSpeed = 3.0f;
    public Vector3 introLocalStartPos = new Vector3(-10f, -2.5f, 10f);
    public Vector3 introLocalEndPos = new Vector3(-5f, -2.5f, 10f);
    private bool isIntroPlaying = false;

    [Header("岩の明るさ設定")]
    public Image[] rockImages;
    public Color brightColor = Color.white;
    public Color darkColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public AnimationCurve darknessCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("ステージから戻った時の設定（巣穴連動）")]
    [Tooltip("手動で巣穴を登録したい場合（空でも自動検索されます）")]
    public StageSelectNest[] stageNests;
    [Tooltip("巣穴の上に自機が立つ高さ（Y軸オフセット）")]
    public float playerYOffset = 0.8f;

    void Start()
    {
        if (isComingFromTitle)
        {
            if (playIntroWalk) StartCoroutine(IntroWalkRoutine());
        }
        else
        {
            SetPlayerAtClearedStage();
        }
        isComingFromTitle = false;
    }

    void Update()
    {
        if (InGameMenuController.IsMenuOpen) return;

        if (isIntroPlaying) return;

        HandleCameraScroll();
        UpdateRockBrightness();
        UpdateAnimationState();
    }

    void HandleCameraScroll()
    {
        if (Camera.main == null) return;

        float scrollValue = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : Input.mouseScrollDelta.y;

        if (Mathf.Abs(scrollValue) > 0.1f)
        {
            float moveX = -scrollValue * scrollSensitivity;
            Vector3 nextPos = transform.position + new Vector3(moveX, 0, 0);
            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
            transform.position = nextPos;

            stopTimer = walkStopDelay;
            if (playerSR != null)
            {
                if (moveX > 0) playerSR.flipX = false;
                else if (moveX < 0) playerSR.flipX = true;
            }
        }
    }

    void SetPlayerAtClearedStage()
    {
        int lastNestIndex = PlayerPrefs.GetInt("LastSelectedNest", 1);

        Vector3 targetNestPos = Vector3.zero;
        bool found = false;

        // 1. 自動登録されている巣穴リストから検索
        StageSelectNest targetNest = StageSelectNest.allNests.Find(n => n != null && n.nestIndex == lastNestIndex);
        if (targetNest != null)
        {
            targetNestPos = targetNest.transform.position;
            found = true;
        }
        // 2. Inspectorで手動登録されていた場合のフォールバック
        else if (stageNests != null && stageNests.Length > 0)
        {
            int idx = Mathf.Clamp(lastNestIndex - 1, 0, stageNests.Length - 1);
            if (stageNests[idx] != null)
            {
                targetNestPos = stageNests[idx].transform.position;
                found = true;
            }
        }

        if (found)
        {
            float camX = Mathf.Clamp(targetNestPos.x, minX, maxX);
            transform.position = new Vector3(camX, transform.position.y, transform.position.z);

            UpdateRockBrightness();

            if (playerAnimator != null)
            {
                playerAnimator.transform.position = new Vector3(
                    targetNestPos.x,
                    targetNestPos.y + playerYOffset,
                    playerAnimator.transform.position.z
                );

                playerAnimator.SetBool(animBoolName, false);
            }

            Debug.Log($"<color=green>【自回復帰】巣穴 {lastNestIndex} の上に自機を配置しました</color>");
        }
    }

    IEnumerator IntroWalkRoutine()
    {
        isIntroPlaying = true;
        playerAnimator.transform.localPosition = introLocalStartPos;
        playerAnimator.SetBool(animBoolName, false);
        if (playerSR != null) playerSR.flipX = false;
        yield return new WaitForSeconds(introStartDelay);
        playerAnimator.SetBool(animBoolName, true);
        float t = 0;
        Vector3 startPos = playerAnimator.transform.localPosition;
        float distance = Vector3.Distance(startPos, introLocalEndPos);
        float duration = distance / introWalkSpeed;
        while (t < 1.0f)
        {
            t += Time.deltaTime / duration;
            playerAnimator.transform.localPosition = Vector3.Lerp(startPos, introLocalEndPos, t);
            yield return null;
        }
        playerAnimator.transform.localPosition = introLocalEndPos;
        playerAnimator.SetBool(animBoolName, false);
        isIntroPlaying = false;
    }

    void UpdateAnimationState()
    {
        if (playerAnimator == null) return;
        playerAnimator.SetBool(animBoolName, stopTimer > 0);
        if (stopTimer > 0) stopTimer -= Time.deltaTime;
    }

    void UpdateRockBrightness()
    {
        if (rockImages == null || rockImages.Length == 0) return;
        float t = Mathf.InverseLerp(minX, maxX, transform.position.x);
        float curveValue = darknessCurve.Evaluate(t);
        Color targetColor = Color.Lerp(darkColor, brightColor, curveValue);
        foreach (Image rock in rockImages) if (rock != null) rock.color = targetColor;
    }
}