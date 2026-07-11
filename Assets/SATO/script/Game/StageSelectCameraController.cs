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

    [Header("ドラッグ設定")]
    public float dragSensitivity = 1.0f;
    private Vector3 dragOrigin;
    private bool isDragging = false;

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
    [Tooltip("ボタンの何ユニット上に立たせるか")]
    public float playerYOffset = 0f;

    void Start()
    {
        if (isComingFromTitle)
        {
            if (playIntroWalk) StartCoroutine(IntroWalkRoutine());
        }
        else
        {
            // ★ズレを防止するため、1フレーム待たずに即時実行
            SetPlayerAtClearedStage();
        }

        isComingFromTitle = false;
    }

    void SetPlayerAtClearedStage()
    {
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

        // 通常のステージボタンのインデックス
        int targetIndex = clearedStage - 1;

        // 1. まずカメラを目的の場所に移動させる
        Vector3 targetWorldPos = Vector3.zero;
        bool found = false;

        // もしクリアしたのが最終ステージなら、FinalStageLockの場所を探す
        if (targetIndex >= stageButtons.Length)
        {
            FinalStageLock finalLock = FindObjectOfType<FinalStageLock>();
            if (finalLock != null)
            {
                targetWorldPos = finalLock.transform.position;
                found = true;
            }
        }
        // 通常ステージの場合
        else if (targetIndex >= 0 && targetIndex < stageButtons.Length && stageButtons[targetIndex] != null)
        {
            targetWorldPos = stageButtons[targetIndex].transform.position;
            found = true;
        }

        if (found)
        {
            // カメラを移動（制限範囲内）
            float camX = Mathf.Clamp(targetWorldPos.x, minX, maxX);
            transform.position = new Vector3(camX, transform.position.y, transform.position.z);
            UpdateRockBrightness();

            // プレイヤーを配置
            playerAnimator.transform.position = new Vector3(targetWorldPos.x, targetWorldPos.y + playerYOffset, playerAnimator.transform.position.z);
            playerAnimator.SetBool(animBoolName, false);
        }
    }

    IEnumerator IntroWalkRoutine()
    {
        isIntroPlaying = true;

        // プレイヤーの向きと初期位置をセット
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

    // --- 以下、Updateなどのドラッグ処理は以前のまま ---
    void Update()
    {
        if (isIntroPlaying) return;
        HandleCameraDrag();
        UpdateRockBrightness();
        UpdateAnimationState();
    }

    void HandleCameraDrag()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragOrigin = Mouse.current.position.ReadValue();
        }
        if (Mouse.current.middleButton.wasReleasedThisFrame) isDragging = false;

        if (isDragging)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 difference = dragOrigin - currentMousePos;
            dragOrigin = currentMousePos;

            if (Mathf.Abs(difference.x) > 0.01f)
            {
                stopTimer = walkStopDelay;
                if (playerSR != null)
                {
                    if (difference.x > 0) playerSR.flipX = false;
                    else if (difference.x < 0) playerSR.flipX = true;
                }
            }

            float factor = Camera.main.orthographicSize * 2.0f / Screen.height;
            float moveX = difference.x * factor * dragSensitivity;
            Vector3 nextPos = transform.position + new Vector3(moveX, 0, 0);
            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
            transform.position = nextPos;
        }
    }

    void UpdateAnimationState()
    {
        if (playerAnimator == null) return;
        playerAnimator.SetBool(animBoolName, isDragging && stopTimer > 0);
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