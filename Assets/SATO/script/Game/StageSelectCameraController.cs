using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StageSelectCameraController : MonoBehaviour
{
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

    [Tooltip("マウスを止めてからアニメーションを終了するまでの猶予時間")]
    public float walkStopDelay = 0.15f;
    private float stopTimer;

    [Header("岩の明るさ設定")]
    public Image[] rockImages;
    public Color brightColor = Color.white;
    public Color darkColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public AnimationCurve darknessCurve = AnimationCurve.Linear(0, 0, 1, 1);

    void Update()
    {
        HandleCameraDrag();
        UpdateRockBrightness();
        UpdateAnimationState(); // アニメーションの状態更新を分離
    }

    void HandleCameraDrag()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragOrigin = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 difference = dragOrigin - currentMousePos;
            dragOrigin = currentMousePos;

            // ★修正点：ごくわずかな移動でも検知し、タイマーをリセット
            if (Mathf.Abs(difference.x) > 0.01f)
            {
                stopTimer = walkStopDelay; // タイマーを最大値にリセット

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

    // ★追加：アニメーションのON/OFFをタイマーで制御
    void UpdateAnimationState()
    {
        if (playerAnimator == null) return;

        if (isDragging && stopTimer > 0)
        {
            // タイマーが残っている間は歩き続ける
            playerAnimator.SetBool(animBoolName, true);
            stopTimer -= Time.deltaTime;
        }
        else
        {
            // ドラッグしていない、またはマウスが止まって一定時間経ったら停止
            playerAnimator.SetBool(animBoolName, false);
        }
    }

    void UpdateRockBrightness()
    {
        if (rockImages == null || rockImages.Length == 0) return;
        float t = Mathf.InverseLerp(minX, maxX, transform.position.x);
        float curveValue = darknessCurve.Evaluate(t);
        Color targetColor = Color.Lerp(darkColor, brightColor, curveValue);

        foreach (Image rock in rockImages)
        {
            if (rock != null) rock.color = targetColor;
        }
    }
}