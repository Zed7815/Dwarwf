using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class StageCameraController : MonoBehaviour
{
    public enum StageType { Horizontal, Vertical }
    public enum DragButton { Left, Right, Middle }
    public enum StartSide { MinSide, MaxSide }

    [Header("ステージ設定")]
    public StageType stageType = StageType.Horizontal;
    public StartSide startSide = StartSide.MinSide;
    public DragButton dragButton = DragButton.Middle;
    public GameManager gameManager;
    private Transform playerTransform;

    [Header("移動制限")]
    public float minLimit;
    public float maxLimit;

    [Header("イントロ演出（ゴール→スタート）")]
    public bool playIntro = true;      // 演出をやるかどうか
    public float introWaitTime = 1.0f; // ゴール地点で待つ時間
    public float introSpeed = 10.0f;   // 戻ってくるスピード
    private bool isIntroPlaying = false;

    [Header("酔い対策：追従設定")]
    public float deadZone = 1.0f;
    public float smoothTime = 0.15f;
    private Vector3 currentVelocity;

    [Header("ドラッグ設定（Editモード）")]
    public float dragSensitivity = 1.0f;
    private Vector3 dragOrigin;
    private bool isDragging = false;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
        if (gameManager != null && gameManager.player != null)
        {
            playerTransform = gameManager.player.transform;
        }

        if (playIntro)
        {
            StartCoroutine(IntroSequence());
        }
        else
        {
            ResetCamera();
        }
    }

    // ★追加：イントロ演出コルーチン
    IEnumerator IntroSequence()
    {
        isIntroPlaying = true;

        // 1. ゴール地点（スタートの反対側）へ瞬時に移動
        float goalPoint = (startSide == StartSide.MinSide) ? maxLimit : minLimit;
        Vector3 startPos = transform.position;
        if (stageType == StageType.Horizontal) startPos.x = goalPoint;
        else startPos.y = goalPoint;
        transform.position = startPos;

        // 2. ゴールで少し待機（プレイヤーにゴールを見せる）
        yield return new WaitForSeconds(introWaitTime);

        // 3. スタート地点までスーッと移動
        float startPoint = (startSide == StartSide.MinSide) ? minLimit : maxLimit;
        while (true)
        {
            float current = (stageType == StageType.Horizontal) ? transform.position.x : transform.position.y;
            float next = Mathf.MoveTowards(current, startPoint, introSpeed * Time.deltaTime);

            if (stageType == StageType.Horizontal)
                transform.position = new Vector3(next, transform.position.y, transform.position.z);
            else
                transform.position = new Vector3(transform.position.x, next, transform.position.z);

            // 目的地に到達したら終了
            if (Mathf.Abs(next - startPoint) < 0.01f) break;
            yield return null;
        }

        isIntroPlaying = false;
    }

    void LateUpdate()
    {
        if (gameManager == null || isIntroPlaying) return; // イントロ中は通常の操作を無効化

        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            HandleEditMode();
        }
        else if (gameManager.currentState == GameManager.GameState.Play)
        {
            HandlePlayMode();
        }
    }

    // --- 以下、以前と同じメソッド ---
    bool IsDragButtonPressed()
    {
        switch (dragButton)
        {
            case DragButton.Left: return Mouse.current.leftButton.isPressed;
            case DragButton.Right: return Mouse.current.rightButton.isPressed;
            case DragButton.Middle: return Mouse.current.middleButton.isPressed;
            default: return false;
        }
    }

    void HandleEditMode()
    {
        if (!isDragging && IsDragButtonPressed())
        {
            isDragging = true;
            dragOrigin = Mouse.current.position.ReadValue();
        }
        if (isDragging && !IsDragButtonPressed()) isDragging = false;
        if (isDragging)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 difference = dragOrigin - currentMousePos;
            dragOrigin = currentMousePos;
            float factor = Camera.main.orthographicSize * 2.0f / Screen.height;
            Vector3 move = (stageType == StageType.Horizontal) ? new Vector3(difference.x * factor * dragSensitivity, 0, 0) : new Vector3(0, difference.y * factor * dragSensitivity, 0);
            transform.position = ClampPosition(transform.position + move);
        }
    }

    void HandlePlayMode()
    {
        if (playerTransform == null) return;
        Vector3 currentPos = transform.position;
        Vector3 targetPos = currentPos;
        if (stageType == StageType.Horizontal)
        {
            float deltaX = playerTransform.position.x - currentPos.x;
            if (Mathf.Abs(deltaX) > deadZone) targetPos.x = playerTransform.position.x - (Mathf.Sign(deltaX) * deadZone);
        }
        else
        {
            float deltaY = playerTransform.position.y - currentPos.y;
            if (Mathf.Abs(deltaY) > deadZone) targetPos.y = playerTransform.position.y - (Mathf.Sign(deltaY) * deadZone);
        }
        transform.position = Vector3.SmoothDamp(currentPos, ClampPosition(targetPos), ref currentVelocity, smoothTime);
    }

    Vector3 ClampPosition(Vector3 target)
    {
        float outX = stageType == StageType.Horizontal ? Mathf.Clamp(target.x, minLimit, maxLimit) : initialPosition.x;
        float outY = stageType == StageType.Vertical ? Mathf.Clamp(target.y, minLimit, maxLimit) : initialPosition.y;
        return new Vector3(outX, outY, transform.position.z);
    }

    public void ResetCamera()
    {
        isIntroPlaying = false;
        isDragging = false;
        currentVelocity = Vector3.zero;
        Vector3 resetPos = initialPosition;
        float startPoint = (startSide == StartSide.MinSide) ? minLimit : maxLimit;
        if (stageType == StageType.Horizontal) resetPos.x = startPoint;
        else resetPos.y = startPoint;
        transform.position = new Vector3(resetPos.x, resetPos.y, transform.position.z);
    }
}