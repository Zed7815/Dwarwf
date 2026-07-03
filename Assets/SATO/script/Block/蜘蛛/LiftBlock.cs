using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

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

    private bool isMoving = false;

    [Header("下り時のみ追加オフセット")]
    public float downYOffset = 0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Playモード以外は動かさない
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;

        // 2. 移動中なら無視
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
        float myHalfHeight = myCol != null ? myCol.bounds.extents.y : 0.5f;

        float startUpperY = transform.position.y + myHalfHeight + 0.05f;
        float startLowerY = transform.position.y - myHalfHeight - 0.05f;

        Vector3 leftUpper = new Vector3(lockX - 0.6f, startUpperY, 0);
        Vector3 rightUpper = new Vector3(lockX + 0.6f, startUpperY, 0);
        Vector3 leftLower = new Vector3(lockX - 0.6f, startLowerY, 0);
        Vector3 rightLower = new Vector3(lockX + 0.6f, startLowerY, 0);

        List<RaycastHit2D> allHits = new List<RaycastHit2D>();

        // 上方向のスキャン（上部の開始点から上へ発射）
        allHits.AddRange(Physics2D.RaycastAll(leftUpper, Vector2.up, maxSearchDistance, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightUpper, Vector2.up, maxSearchDistance, obstacleLayer));

        // 下方向のスキャン（下部の開始点から下へ発射）
        allHits.AddRange(Physics2D.RaycastAll(leftLower, Vector2.down, maxSearchDistance, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLower, Vector2.down, maxSearchDistance, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        foreach (var hit in allHits)
        {
            if (hit.collider == null) continue;

            // 自分自身、および「プレイヤー自身（Playerタグ）」をスキャン対象から完全に除外
            if (hit.collider.gameObject == gameObject || hit.collider.CompareTag("Player")) continue;

            // 他のリフトブロックも除外（お互いを床として誤認識するのを防ぐ）
            if (hit.collider.GetComponent<LiftBlock>() != null) continue;

            float targetY = hit.point.y;

            // Rayの発射方向（衝突点の高低）を正確に判定してオフセットを適用
            // 床埋まりや天井めり込みを正確に防止
            bool isGoingUp = targetY > transform.position.y;

            float sideOffset = isGoingUp ? -myHalfHeight : myHalfHeight;

            // 通常オフセット
            float candidateY = targetY + sideOffset + yOffset;

            // 下りの時だけ追加オフセット
            if (!isGoingUp)
            {
                candidateY += downYOffset;
            }

            float dist = Mathf.Abs(candidateY - transform.position.y);

            // 0.8マス以上離れている、最も近い有効な足場をターゲットにする
            if (dist > 0.8f && dist < closestDist)
            {
                closestDist = dist;
                // わずかな物理演算の誤差（0.01マス単位のズレ）を吸収するため、0.5マス単位に丸める（推奨）
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

        float startY = transform.position.y;
        pWalk.StateChange(0); // プレイヤーを一時停止

        float playerYOffset = pWalk.transform.position.y - startY;

        // プレイヤーのX軸をリフトに完全に合わせる
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

            if (pWalk != null)
            {
                pWalk.transform.position = new Vector3(lockX, transform.position.y + playerYOffset, lockZ);
            }

            yield return null;
        }

        transform.position = new Vector3(lockX, targetY, lockZ);
        yield return new WaitForSeconds(waitTime);

        if (pWalk != null) pWalk.StateChange(1); // プレイヤーの移動制限を解除
        isMoving = false;
    }
}