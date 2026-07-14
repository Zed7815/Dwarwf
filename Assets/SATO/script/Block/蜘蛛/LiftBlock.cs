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
    [Tooltip("上方向のブロックを検知する最小距離")]
    public float minMoveDistance = 0f;
    // ★追加：下方向専用の制限距離
    [Tooltip("下方向のブロックを検知する最小距離")]
    public float minDownMoveDistance = 2f;

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

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip startSE;
    public AudioClip stopSE;

    private float myHalfHeight;

    private bool isPlayerTouching = false;

    private void Start()
    {
        SetupReferences();
        UpdateDimensions();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (spiderObject == null || threadTransform == null)
        {
            SetupReferences();
        }
        UpdateThreadPosition();
    }

    void SetupReferences()
    {
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
            if (spiderObject == null)
            {
                Transform s = transform.Find("Spider");
                if (s != null) spiderObject = s.gameObject;
            }
            if (threadTransform == null) threadTransform = transform.Find("Thread");
        }

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



    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Application.isPlaying && GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;
        if (isMoving) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();
            if (pWalk != null)
            {
                float myX = transform.position.x;
                float myZ = transform.position.z;

                Vector3 target;
                // 移動先を探す
                if (FindNextDestination(myX, out target))
                {
                    // 移動先が見つかった場合：通常通り移動開始
                    pWalk.ForceStopAbilities();
                    isPlayerTouching = true;
                    pWalk.transform.position = new Vector3(myX, pWalk.transform.position.y, myZ);
                    StartCoroutine(MoveRoutine(pWalk, myX, myZ, target.y));
                }
                else
                {
                    // ★追加：移動先が見つからない場合
                    // リフトの当たり判定を消して、プレイヤーがすり抜けて（または落ちて）進めるようにする
                    GetComponent<Collider2D>().enabled = false;
                    Debug.Log("[LiftBlock] 移動先がないため、当たり判定を無効化しました");
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerTouching = false; // 離れた！
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
            if (hit.collider.GetComponentInParent<LiftIgnore>() != null) continue;

            float targetY = hit.point.y;
            bool isGoingUp = targetY > transform.position.y;
            float sideOffset = isGoingUp ? -currentHalfHeight : currentHalfHeight;
            float candidateY = targetY + sideOffset + yOffset;
            if (!isGoingUp) candidateY += downYOffset;
            float dist = Mathf.Abs(candidateY - transform.position.y);

            // ★修正ポイント：上方向か下方向かで、比較する距離(threshold)を切り替える
            float threshold = isGoingUp ? minMoveDistance : minDownMoveDistance;

            if (dist > threshold && dist < closestDist)
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

        // ★リフトが反応した瞬間の演出（任意：音を鳴らすなど）
        // if (audioSource != null && startSE != null) audioSource.PlayOneShot(startSE);

        // 出発前のタメ時間（プレイヤーはすでに中央で止まっている）
        yield return new WaitForSeconds(waitTime);

        // 万が一、待機中にリセットされたりしてプレイヤーが消えた場合の安全策
        if (pWalk == null) { isMoving = false; yield break; }

        // --- 移動開始の演出 ---
        if (audioSource != null && startSE != null) audioSource.PlayOneShot(startSE);
        if (spiderAnimator != null) spiderAnimator.SetBool(animBoolName, true);

        float startY = transform.position.y;
        float playerYOffset = playerYOffsetOnLift;

        // 移動処理
        float distance = Mathf.Abs(startY - targetY);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            float currentY = Mathf.Lerp(startY, targetY, t);

            transform.position = new Vector3(lockX, currentY, lockZ);

            // プレイヤーをリフトの高さに合わせて運ぶ
            if (pWalk != null)
                pWalk.transform.position = new Vector3(lockX, transform.position.y + playerYOffset, lockZ);

            yield return null;
        }

        // 到着処理
        transform.position = new Vector3(lockX, targetY, lockZ);
        if (audioSource != null && stopSE != null) audioSource.PlayOneShot(stopSE);

        // 到着後の余韻
        yield return new WaitForSeconds(waitTime);

        if (spiderAnimator != null) spiderAnimator.SetBool(animBoolName, false);

        // プレイヤーを歩行再開
        if (pWalk != null) pWalk.StateChange(1);

        isMoving = false;
        isPlayerTouching = false;
    }
    void OnGimmickReset()
    {
        StopAllCoroutines();
        isMoving = false;
        GetComponent<Collider2D>().enabled = true;
        if (audioSource != null) audioSource.Stop();
    }
}