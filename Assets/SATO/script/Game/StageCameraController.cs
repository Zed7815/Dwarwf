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

    [Header("演出設定")]
    public nextscene nextSceneScript;  // ★追加：nextsceneスクリプトの参照
    public bool playIntro = true;
    public float introWaitTime = 1.0f;
    public float introSpeed = 10.0f;
    public float resetSpeed = 25.0f;

    private bool isEffectPlaying = false;

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
            ResetToStartSideInstant();
        }
    }

    // 初回イントロ演出
    IEnumerator IntroSequence()
    {
        isEffectPlaying = true;

       
        // --- ここまで ---

        // カメラを一旦ゴール端へワープ
        float goalPoint = (startSide == StartSide.MinSide) ? maxLimit : minLimit;
        SetCameraPos(goalPoint);

        // --- ★ここから追加：シーン開始時の演出 ---
        if (nextSceneScript != null)
        {
            // 黒い板をどかすアニメーションを実行
            yield return StartCoroutine(nextSceneScript.startKuro());
            // どかし終わった後、指示通り0.5秒待機
           // yield return new WaitForSecondsRealtime(0.2f);
        }

        yield return new WaitForSeconds(introWaitTime);

        // スタート端へ移動
        yield return StartCoroutine(MoveToStartSideRoutine(introSpeed));

        isEffectPlaying = false;
    }

    // ★リセットボタンから呼ばれる処理
    public void ResetCamera()
    {
        StopAllCoroutines();
        isDragging = false;
        currentVelocity = Vector3.zero;
        StartCoroutine(ResetSequence());
    }

    IEnumerator ResetSequence()
    {
        isEffectPlaying = true;
        yield return StartCoroutine(MoveToStartSideRoutine(resetSpeed));
        isEffectPlaying = false;
    }

    IEnumerator MoveToStartSideRoutine(float speed)
    {
        float targetVal = (startSide == StartSide.MinSide) ? minLimit : maxLimit;

        while (true)
        {
            float current = (stageType == StageType.Horizontal) ? transform.position.x : transform.position.y;
            float next = Mathf.MoveTowards(current, targetVal, speed * Time.deltaTime);

            SetCameraPos(next);

            if (Mathf.Abs(next - targetVal) < 0.01f) break;
            yield return null;
        }
    }

    void SetCameraPos(float value)
    {
        if (stageType == StageType.Horizontal)
            transform.position = new Vector3(value, initialPosition.y, transform.position.z);
        else
            transform.position = new Vector3(initialPosition.x, value, transform.position.z);
    }

    void ResetToStartSideInstant()
    {
        float targetVal = (startSide == StartSide.MinSide) ? minLimit : maxLimit;
        SetCameraPos(targetVal);
    }

    void LateUpdate()
    {
        if (gameManager == null || isEffectPlaying) return;

        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            HandleEditMode();
        }
        else if (gameManager.currentState == GameManager.GameState.Play)
        {
            HandlePlayMode();
        }
    }


    void HandleEditMode()
    {
        float scrollValue = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollValue) > 0.1f)
        {
            // 感度調整用の基本値
            float factor = 0.01f * dragSensitivity;
            Vector3 move = Vector3.zero;

            if (stageType == StageType.Horizontal)
            {
                // 横スクロール：下回しで右(+)へ進むようにマイナスを維持
                move = new Vector3(-scrollValue * factor * 10f, 0, 0);
            }
            else
            {
                // 縦スクロール：上回し(プラス)でカメラが上(+)へ動くように修正
                move = new Vector3(0, scrollValue * factor * 10f, 0);
            }

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
}