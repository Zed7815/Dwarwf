using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdCarrier : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 4.0f;
    public float waitTime = 0.5f;
    public LayerMask obstacleLayer;
    public float maxSearchDistance = 20.0f;
    public float heightOffset = 1.5f;
    public float grabOffsetY = -0.7f;
    public float landYOffset = 0.0f;

    [Header("判定（BoxCast）の設定")]
    [Tooltip("プレイヤーの体の大きさを想定した判定サイズ")]
    public Vector2 boxSize = new Vector2(0.5f, 0.8f);

    [Header("レイヤー設定")]
    public LayerMask landableLayer; // 地面（白くなる対象）
    public LayerMask blockingLayer; // 壁（赤くなる対象）

    [Header("ビジュアル・演出設定")]
    public SpriteRenderer birdSprite;
    public Animator animator;
    public string flyBoolParam = "isFlying";

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip grabSE;
    public AudioClip flyLoopSE;

    private Vector3 targetPos;
    private bool isMoving = false;

    void Start()
    {
        if (birdSprite == null) birdSprite = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // --- 1. 色検知ロジック (BoxCast を使用) ---
        if (GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.Edit)
        {
            UpdateVisualColor();
        }
        else if (birdSprite != null)
        {
            birdSprite.color = Color.white;
        }
    }

    // エディットモード中の色を更新する
    void UpdateVisualColor()
    {
        if (birdSprite == null) return;

        // ★右方向と左方向の両方をチェック
        bool canCarryRight = CheckDirection(Vector2.right);
        bool canCarryLeft = CheckDirection(Vector2.left);

        // どちらか一方向でも成功していれば白、両方ダメなら赤
        bool canCarry = canCarryRight || canCarryLeft;

        birdSprite.color = canCarry ? Color.white : new Color(1f, 0.3f, 0.3f, 0.8f);
    }

    // 指定した方向へのBoxCast判定をまとめた関数
    bool CheckDirection(Vector2 rayDir)
    {
        float estimatedPlayerFootY = transform.position.y + grabOffsetY;
        Vector3 boxStart = new Vector3(transform.position.x, estimatedPlayerFootY + (boxSize.y / 2f), transform.position.z);

        // BoxCastAllで自分自身を無視して取得
        RaycastHit2D[] hits = Physics2D.BoxCastAll(boxStart, boxSize, 0f, rayDir, maxSearchDistance, landableLayer | blockingLayer);

        float hitDist = maxSearchDistance;
        bool success = false;

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            hitDist = hit.distance;
            // 最初に当たったのが「着地可能レイヤー」なら成功
            if (((1 << hit.collider.gameObject.layer) & landableLayer) != 0)
            {
                success = true;
            }
            // 壁(blockingLayer)に当たった、または地面だった場合、この方向の探索は終了
            break;
        }

        // デバッグ用の箱を表示（成功なら緑、失敗なら赤）
        DrawBoxCastDebug(boxStart, boxSize, rayDir, hitDist, success ? Color.green : Color.red);

        return success;
    }

    // --- 2. 実際の動作ロジック (Raycast を使用) ---

    bool FindNextDestination(Player_walk pWalk)
    {
        int dir = pWalk.direction;
        Vector2 rayDir = dir > 0 ? Vector2.right : Vector2.left;
        float rayOffset = 1.2f;
        Vector3 rayStart = new Vector3(transform.position.x + (rayDir.x * rayOffset), pWalk.transform.position.y, transform.position.z);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDir, maxSearchDistance, landableLayer | blockingLayer);

        if (hit.collider != null)
        {
            if (((1 << hit.collider.gameObject.layer) & blockingLayer) != 0) return false;

            if (((1 << hit.collider.gameObject.layer) & landableLayer) != 0)
            {
                float targetX = hit.point.x + (dir * 0.8f);
                float targetY = pWalk.transform.position.y + landYOffset;
                targetPos = new Vector3(targetX, targetY, transform.position.z);
                return true;
            }
        }
        return false;
    }

    // --- 以下、既存の挙動 ---

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isMoving) return;
        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();
            if (p != null)
            {
                if (FindNextDestination(p)) StartCoroutine(CarrySequence(p));
                else GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    IEnumerator CarrySequence(Player_walk pWalk)
    {
        isMoving = true;
        Rigidbody2D rb = pWalk.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        pWalk.StateChange(0);
        int moveDir = pWalk.direction;
        SetBirdFacing(moveDir);
        yield return new WaitForSeconds(waitTime);
        if (audioSource != null && grabSE != null) audioSource.PlayOneShot(grabSE);

        Vector3 takeoffStartBird = transform.position;
        Vector3 takeoffEndBird = transform.position + new Vector3(0, heightOffset, 0);
        float takeoffDuration = 0.5f;
        float takeoffElapsed = 0f;
        Vector3 startPlayerPos = pWalk.transform.position;

        if (animator != null) animator.SetBool(flyBoolParam, true);
        while (takeoffElapsed < takeoffDuration)
        {
            takeoffElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, takeoffElapsed / takeoffDuration);
            transform.position = Vector3.Lerp(takeoffStartBird, takeoffEndBird, t);
            pWalk.transform.position = Vector3.Lerp(startPlayerPos, transform.position + new Vector3(0, grabOffsetY, 0), t);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        if (audioSource != null && flyLoopSE != null)
        {
            audioSource.clip = flyLoopSE;
            audioSource.loop = true;
            audioSource.Play();
        }

        Vector3 birdStartPos = transform.position;
        Vector3 birdEndPos = targetPos + new Vector3(0, heightOffset - grabOffsetY, 0);
        float distance = Vector3.Distance(birdStartPos, birdEndPos);
        float duration = distance / speed;
        float elapsed = 0f;
        pWalk.StateChange(4);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(birdStartPos, birdEndPos, curve);
            pWalk.transform.position = transform.position + new Vector3(0, grabOffsetY, 0);
            yield return null;
        }

        if (audioSource != null) audioSource.Stop();
        Vector3 landStartBird = transform.position;
        Vector3 landEndBird = targetPos;
        float landDuration = 0.6f;
        float landElapsed = 0f;
        bool isReleased = false;

        while (landElapsed < landDuration)
        {
            landElapsed += Time.deltaTime;
            float t = landElapsed / landDuration;
            float easedT = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector3.Lerp(landStartBird, landEndBird, easedT);

            if (!isReleased && t > 0.3f)
            {
                isReleased = true;
                pWalk.StateChange(3);
                Rigidbody2D pRb = pWalk.GetComponent<Rigidbody2D>();
                if (pRb != null) pRb.bodyType = RigidbodyType2D.Dynamic;
            }
            if (!isReleased) pWalk.transform.position = transform.position + new Vector3(0, grabOffsetY, 0);
            yield return null;
        }

        if (animator != null) animator.SetBool(flyBoolParam, false);
        yield return new WaitForSeconds(waitTime);
        SetBirdFacing(-moveDir);
        isMoving = false;
    }

    void SetBirdFacing(int dir)
    {
        if (birdSprite != null)
        {
            Vector3 scale = birdSprite.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * dir;
            birdSprite.transform.localScale = scale;
        }
    }

    void OnGimmickReset()
    {
        StopAllCoroutines();
        isMoving = false;
        GetComponent<Collider2D>().enabled = true;
        if (audioSource != null) audioSource.Stop();
        if (animator != null) animator.SetBool(flyBoolParam, false);
    }

    void DrawBoxCastDebug(Vector3 origin, Vector2 size, Vector2 direction, float distance, Color color)
    {
        Vector3 pos = origin + (Vector3)direction * distance;
        Vector3 halfSize = size / 2f;
        Debug.DrawLine(origin + new Vector3(-halfSize.x, halfSize.y), origin + new Vector3(halfSize.x, halfSize.y), color);
        Debug.DrawLine(origin + new Vector3(halfSize.x, halfSize.y), origin + new Vector3(halfSize.x, -halfSize.y), color);
        Debug.DrawLine(origin + new Vector3(halfSize.x, -halfSize.y), origin + new Vector3(-halfSize.x, -halfSize.y), color);
        Debug.DrawLine(origin + new Vector3(-halfSize.x, -halfSize.y), origin + new Vector3(-halfSize.x, halfSize.y), color);
        Debug.DrawLine(pos + new Vector3(-halfSize.x, halfSize.y), pos + new Vector3(halfSize.x, halfSize.y), color);
        Debug.DrawLine(pos + new Vector3(halfSize.x, halfSize.y), pos + new Vector3(halfSize.x, -halfSize.y), color);
        Debug.DrawLine(pos + new Vector3(halfSize.x, -halfSize.y), pos + new Vector3(-halfSize.x, -halfSize.y), color);
        Debug.DrawLine(pos + new Vector3(-halfSize.x, -halfSize.y), pos + new Vector3(-halfSize.x, halfSize.y), color);
        Debug.DrawLine(origin, pos, color);
    }
}