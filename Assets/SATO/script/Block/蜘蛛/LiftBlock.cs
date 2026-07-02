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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Playモード以外は動かさない
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;

        // 2. 移動中なら無視
        if (isMoving) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 🌟 踏んだ「その瞬間」のリフトの座標を、このインスタンス専用の軸として取得
            float myX = transform.position.x;
            float myZ = transform.position.z;

            Vector3 target;
            if (FindNextDestination(myX, out target))
            {
                Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();
                if (pWalk != null)
                {
                    // 🌟 コルーチンに「今このリフトがどこにいるか」を直接渡す
                    StartCoroutine(MoveRoutine(pWalk, myX, myZ, target.y));
                }
            }
        }
    }

    bool FindNextDestination(float lockX, out Vector3 foundTarget)
    {
        foundTarget = Vector3.zero;

        // lockX（今このリフトがあるX座標）を基準に上下を調べる
        Vector3 leftLine = new Vector3(lockX - 0.7f, transform.position.y, 0);
        Vector3 rightLine = new Vector3(lockX + 0.7f, transform.position.y, 0);

        List<RaycastHit2D> allHits = new List<RaycastHit2D>();
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.down, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.down, 20f, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        Collider2D myCol = GetComponent<Collider2D>();
        float myHalfHeight = myCol != null ? myCol.bounds.extents.y : 0.5f;

        foreach (var hit in allHits)
        {
            if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

            // 🌟 TileMapの個別タイルに対応するため、当たった点（hit.point）を利用
            float targetY = hit.point.y;
            
            // レイの方向を確認して着地位置を計算
            float sideOffset = (targetY > transform.position.y) ? -myHalfHeight : myHalfHeight;
            float candidateY = targetY + sideOffset + yOffset;

            float dist = Mathf.Abs(candidateY - transform.position.y);

            if (dist > 0.8f && dist < closestDist)
            {
                closestDist = dist;
                foundTarget = new Vector3(lockX, candidateY, 0);
                found = true;
            }
        }
        return found;
    }

    IEnumerator MoveRoutine(Player_walk pWalk, float lockX, float lockZ, float targetY)
    {
        isMoving = true;

        // 移動開始時点の情報を完全にロック
        float startY = transform.position.y;
        pWalk.StateChange(0); // プレイヤー停止
        
        // プレイヤーの初期位置との差分を記憶
        float playerYOffset = pWalk.transform.position.y - startY;

        // 🌟 ここでプレイヤーを「このリフトのX軸」に一瞬で合わせる（lockXを使う）
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

            // 🌟 常に「このリフト自身のlockX」を代入し続ける
            // これで他のリフトへ飛ぶことは物理的に不可能になります
            transform.position = new Vector3(lockX, currentY, lockZ);
            
            if (pWalk != null)
            {
                pWalk.transform.position = new Vector3(lockX, transform.position.y + playerYOffset, lockZ);
            }

            yield return null;
        }

        transform.position = new Vector3(lockX, targetY, lockZ);
        yield return new WaitForSeconds(waitTime);

        if (pWalk != null) pWalk.StateChange(1); 
        isMoving = false;
    }
}