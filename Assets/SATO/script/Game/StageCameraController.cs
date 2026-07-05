using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("酔い対策：追従設定")]
    [Tooltip("自機が中央からどれくらい離れたらカメラを動かすか（0.5〜1.5推奨）")]
    public float deadZone = 1.0f;
    [Tooltip("追従の速さ（小さいほどキビキビ、大きいほどぬるぬる。0.1〜0.2推奨）")]
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
        ResetCamera();
    }

    void LateUpdate()
    {
        if (gameManager == null) return;

        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            HandleEditMode();
        }
        else if (gameManager.currentState == GameManager.GameState.Play)
        {
            HandlePlayMode();
        }
    }

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

        if (isDragging && !IsDragButtonPressed())
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 difference = dragOrigin - currentMousePos;
            dragOrigin = currentMousePos;

            float factor = Camera.main.orthographicSize * 2.0f / Screen.height;
            Vector3 move = Vector3.zero;

            // 編集モードは酔い防止のためLerpを使わずダイレクトに動かす
            if (stageType == StageType.Horizontal)
            {
                move = new Vector3(difference.x * factor * dragSensitivity, 0, 0);
            }
            else
            {
                move = new Vector3(0, difference.y * factor * dragSensitivity, 0);
            }

            Vector3 nextPos = transform.position + move;
            transform.position = ClampPosition(nextPos);
        }
    }

    void HandlePlayMode()
    {
        if (playerTransform == null) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = currentPos;

        // ★酔い対策：デッドゾーン（遊び）の計算
        if (stageType == StageType.Horizontal)
        {
            float deltaX = playerTransform.position.x - currentPos.x;
            if (Mathf.Abs(deltaX) > deadZone)
            {
                // デッドゾーンを超えた分だけ目標地点をずらす
                targetPos.x = playerTransform.position.x - (Mathf.Sign(deltaX) * deadZone);
            }
        }
        else
        {
            float deltaY = playerTransform.position.y - currentPos.y;
            if (Mathf.Abs(deltaY) > deadZone)
            {
                targetPos.y = playerTransform.position.y - (Mathf.Sign(deltaY) * deadZone);
            }
        }

        // 制限範囲内に収める
        Vector3 clampedTarget = ClampPosition(targetPos);

        // 滑らかに追従
        transform.position = Vector3.SmoothDamp(currentPos, clampedTarget, ref currentVelocity, smoothTime);
    }

    Vector3 ClampPosition(Vector3 target)
    {
        float outX = stageType == StageType.Horizontal ? Mathf.Clamp(target.x, minLimit, maxLimit) : initialPosition.x;
        float outY = stageType == StageType.Vertical ? Mathf.Clamp(target.y, minLimit, maxLimit) : initialPosition.y;
        return new Vector3(outX, outY, transform.position.z);
    }

    public void ResetCamera()
    {
        isDragging = false;
        currentVelocity = Vector3.zero;
        Vector3 resetPos = initialPosition;
        float startPoint = (startSide == StartSide.MinSide) ? minLimit : maxLimit;

        if (stageType == StageType.Horizontal) resetPos.x = startPoint;
        else resetPos.y = startPoint;
        transform.position = new Vector3(resetPos.x, resetPos.y, transform.position.z);
    }
}