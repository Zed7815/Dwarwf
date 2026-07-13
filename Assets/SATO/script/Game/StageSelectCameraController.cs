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

    [Header("ステージから戻った時の設定")]
    public Button[] stageButtons;
    public float playerYOffset = 1.2f;

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
        if (isIntroPlaying) return;

        HandleCameraScroll(); // ドラッグからスクロールに変更
        UpdateRockBrightness();
        UpdateAnimationState();
    }

    // ★修正：ホイールをコロコロして移動する処理
    void HandleCameraScroll()
    {
        if (Camera.main == null) return;

        // ホイールの回転量を取得
        float scrollValue = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollValue) > 0.1f)
        {
            // 移動量を計算（下回しで右へ、上回しで左へ）
            float moveX = -scrollValue * scrollSensitivity;
            Vector3 nextPos = transform.position + new Vector3(moveX, 0, 0);
            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
            transform.position = nextPos;

            // プレイヤーの演出用タイマーと向き
            stopTimer = walkStopDelay;
            if (playerSR != null)
            {
                if (moveX > 0) playerSR.flipX = false; // 右へ
                else if (moveX < 0) playerSR.flipX = true; // 左へ
            }
        }
    }

    // --- 以下、既存の演出ロジック（変更なし） ---
    void SetPlayerAtClearedStage()
    {
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        int targetIndex = Mathf.Max(0, clearedStage - 1);
        if (stageButtons != null && targetIndex < stageButtons.Length && stageButtons[targetIndex] != null)
        {
            Vector3 buttonWorldPos = stageButtons[targetIndex].transform.position;
            float camX = Mathf.Clamp(buttonWorldPos.x, minX, maxX);
            transform.position = new Vector3(camX, transform.position.y, transform.position.z);
            UpdateRockBrightness();
            playerAnimator.transform.position = new Vector3(buttonWorldPos.x, buttonWorldPos.y + playerYOffset, playerAnimator.transform.position.z);
            playerAnimator.SetBool(animBoolName, false);
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