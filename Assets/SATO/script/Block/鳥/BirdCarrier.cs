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

    bool FindNextDestination(Player_walk pWalk)
    {
        int dir = pWalk.direction;
        Vector2 rayDir = dir > 0 ? Vector2.right : Vector2.left;
        float rayOffset = 1.5f;
        Vector3 rayStart = new Vector3(transform.position.x + (rayDir.x * rayOffset), pWalk.transform.position.y, transform.position.z);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDir, maxSearchDistance, obstacleLayer);

        if (hit.collider != null)
        {
            float targetX = hit.point.x + (dir * 0.8f);
            float targetY = pWalk.transform.position.y + landYOffset;
            targetPos = new Vector3(targetX, targetY, transform.position.z);
            return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isMoving) return;
        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();
            if (p != null)
            {
                bool found = FindNextDestination(p);
                if (found) StartCoroutine(CarrySequence(p));
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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(birdStartPos, birdEndPos, curve);
            pWalk.transform.position = transform.position + new Vector3(0, grabOffsetY, 0);
            yield return null;
        }

        if (audioSource != null) audioSource.Stop();

        pWalk.StateChange(1);

        Vector3 landStartBird = transform.position;
        Vector3 landEndBird = targetPos;
        float landDuration = 0.5f;
        float landElapsed = 0f;

        while (landElapsed < landDuration)
        {
            landElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, landElapsed / landDuration);
            transform.position = Vector3.Lerp(landStartBird, landEndBird, t);
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

    // ★リセットボタンが押されたときに呼ばれる
    void OnGimmickReset()
    {
        // 1. 実行中の運搬コルーチンを強制停止（これが重要！）
        StopAllCoroutines();

        // 2. フラグを初期化
        isMoving = false;

        // 3. 音を止める
        if (audioSource != null) audioSource.Stop();

        // 4. アニメーションを待機状態に戻す
        if (animator != null) animator.SetBool(flyBoolParam, false);
    }
}