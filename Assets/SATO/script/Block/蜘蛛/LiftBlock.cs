using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

[ExecuteAlways]
public class LiftBlock : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 2.5f;
    public float waitTime = 0.5f;
    public LayerMask obstacleLayer;

    [Header("高さの微調整")]
    public float yOffset = 0f;

    [Header("Tilemap")]
    public Tilemap tilemap;

    public int maxSearchDistance = 20;

    [Header("検知設定")]
    public float minMoveDistance = 0.8f;

    private bool isMoving = false;

    [Header("下り時のみ追加オフセット")]
    public float downYOffset = 0f;

    [Header("プレイヤーの高さ調整")]
    public float playerYOffsetOnLift = 1.0f;

    [Header("クモ・糸の演出設定 (自動取得されます)")]
    public GameObject spiderObject;
    public Transform threadTransform;
    public Animator spiderAnimator;

    [Header("クモ・糸の配置設定")]
    public float spiderYPosition = 8.0f;
    public string animBoolName = "isMoving";
    public float threadSpriteUnitSize = 1.0f;

    private float myHalfHeight;

    private void Start()
    {
        SetupReferences();
        UpdateDimensions();
    }

    private void Update()
    {
        // 参照が消えていたら自動で再取得を試みる（プレハブ対策）
        if (spiderObject == null || threadTransform == null)
        {
            SetupReferences();
        }

        UpdateThreadPosition();
    }

    // ★追加：子オブジェクトから自動でクモと糸を探す機能
    void SetupReferences()
    {
        // 親オブジェクト（LiftSetなど）がいる場合、その中から名前で探す
        if (transform.parent != null)
        {
            if (spiderObject == null)
            {
                Transform s = transform.parent.Find("Spider");
                if (s != null) spiderObject = s.gameObject;
            }
            if (threadTransform == null) threadTransform = transform.parent.Find("Thread");
        }
        else
        {
            // 親がいない場合は自分の子要素から探す
            if (spiderObject == null)
            {
                Transform s = transform.Find("Spider");
                if (s != null) spiderObject = s.gameObject;
            }
            if (threadTransform == null) threadTransform = transform.Find("Thread");
        }

        // アニメーターも自動取得
        if (spiderObject != null && spiderAnimator == null)
        {
            spiderAnimator = spiderObject.GetComponent<Animator>();
        }
    }


    void UpdateDimensions()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myHalfHeight = myCol.bounds.extents.y;
        else myHalfHeight = 0.5f;
    }

    void UpdateThreadPosition()
    {
        if (myHalfHeight == 0) UpdateDimensions();

        if (spiderObject != null)
        {
            spiderObject.transform.position = new Vector3(transform.position.x, spiderYPosition, transform.position.z);
        }

        if (threadTransform != null && spiderObject != null)
        {
            float liftTopY = transform.position.y + myHalfHeight;
            float distance = Mathf.Abs(spiderYPosition - liftTopY);

            Vector3 newScale = threadTransform.localScale;
            newScale.y = distance / threadSpriteUnitSize;
            threadTransform.localScale = newScale;

            threadTransform.position = new Vector3(transform.position.x, (spiderYPosition + liftTopY) / 2.0f, transform.position.z);
        }
    }

    // --- 以下、元の挙動ロジック（変更なし） ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Application.isPlaying && GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;
        if (isMoving) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            float myX = transform.position.x;
            float myZ = transform.position.z;

            Vector3 target;
            if (FindNextDestination(myX, out target))
            {
                Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();
                if (pWalk != null)
                {
                    StartCoroutine(MoveRoutine(pWalk, myX, myZ, target.y));
                }
            }
        }
    }

    bool FindNextDestination(float lockX, out Vector3 foundTarget)
    {
        foundTarget = Vector3.zero;
        Collider2D myCol = GetComponent<Collider2D>();
        float currentHalfHeight = myCol != null ? myCol.bounds.extents.y : 0.5f;

        float startUpperY = transform.position.y + currentHalfHeight + 0.05f;
        float startLowerY = transform.position.y - currentHalfHeight - 0.05f;

        Vector3 leftUpper = new Vector3(lockX - 0.6f, startUpperY, 0);
        Vector3 rightUpper = new Vector3(lockX + 0.6f, startUpperY, 0);
        Vector3 leftLower = new Vector3(lockX - 0.6f, startLowerY, 0);
        Vector3 rightLower = new Vector3(lockX + 0.6f, startLowerY, 0);

        List<RaycastHit2D> allHits = new List<RaycastHit2D>();
        allHits.AddRange(Physics2D.RaycastAll(leftUpper, Vector2.up, maxSearchDistance, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightUpper, Vector2.up, maxSearchDistance, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(leftLower, Vector2.down, maxSearchDistance, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLower, Vector2.down, maxSearchDistance, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        foreach (var hit in allHits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.gameObject == gameObject || hit.collider.CompareTag("Player")) continue;
            if (hit.collider.GetComponent<LiftBlock>() != null) continue;

            float targetY = hit.point.y;
            bool isGoingUp = targetY > transform.position.y;
            float sideOffset = isGoingUp ? -currentHalfHeight : currentHalfHeight;
            float candidateY = targetY + sideOffset + yOffset;
            if (!isGoingUp) candidateY += downYOffset;
            float dist = Mathf.Abs(candidateY - transform.position.y);

            if (dist > minMoveDistance && dist < closestDist)
            {
                closestDist = dist;
                float snappedY = Mathf.Round(candidateY * 2.0f) / 2.0f;
                foundTarget = new Vector3(lockX, snappedY, 0);
                found = true;
            }
        }
        return found;
    }

    IEnumerator MoveRoutine(Player_walk pWalk, float lockX, float lockZ, float targetY)
    {
        isMoving = true;
        if (spiderAnimator != null) spiderAnimator.SetBool(animBoolName, true);

        float startY = transform.position.y;
        pWalk.StateChange(0);
        float playerYOffset = playerYOffsetOnLift;
        pWalk.transform.position = new Vector3(lockX, pWalk.transform.position.y, lockZ);

        yield return new WaitForSeconds(waitTime);

        float distance = Mathf.Abs(startY - targetY);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            float currentY = Mathf.Lerp(startY, targetY, t);

            transform.position = new Vector3(lockX, currentY, lockZ);
            if (pWalk != null) pWalk.transform.position = new Vector3(lockX, transform.position.y + playerYOffset, lockZ);

            yield return null;
        }

        transform.position = new Vector3(lockX, targetY, lockZ);
        yield return new WaitForSeconds(waitTime);

        if (spiderAnimator != null) spiderAnimator.SetBool(animBoolName, false);
        if (pWalk != null) pWalk.StateChange(1);
        isMoving = false;
    }

    void OnGimmickReset()
    {
        StopAllCoroutines();

        isMoving = false;
    }

}