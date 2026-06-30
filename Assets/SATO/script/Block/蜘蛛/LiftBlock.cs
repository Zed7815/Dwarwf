using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiftBlock : MonoBehaviour
{

    [Header("設定")]
    public float speed = 2f;
    public float waitTime = 0.5f; // 到着後の待ち時間
    public LayerMask obstacleLayer; // どのレイヤーを天井とみなすか

    private Vector3 fixedXPos; // X座標を固定するための変数
    private Vector3 targetPos;
    private bool isMoving = false;
    private bool toTarget = true; // 今から目的地へ向かうのか、戻るのか
    private bool isInitialized = false; // 初期化フラグ

    void Start()
    {
    }

    void InitializeLift()
    {
        fixedXPos = transform.position;
        isInitialized = true;
    }

    // 真上にある一番近いブロックを探し、それを目的地に設定
    bool FindNextDestination()
    {
        // 左右１マスずれた場所を起点
        Vector3 leftLine = transform.position + Vector3.left * 1.0f;
        Vector3 rightLine = transform.position + Vector3.right * 1.0f;

        // 全ての衝突情報を集めるためのリスト
        List<RaycastHit2D> allHits = new List<RaycastHit2D>();

        // 左右それぞれの列で、上と下をスキャン
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.down, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.down, 20f, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        foreach (var hit in allHits)
        {
            // 今の高さ(transform.position.y)とほぼ同じ高さにあるブロックは「現在の階」なので無視する
            float distFromCurrentY = Mathf.Abs(hit.collider.transform.position.y - transform.position.y);

            if (distFromCurrentY > 0.8f) // 0.8マス以上離れている＝別の階層
            {
                // その中で、一番近い距離にあるものを探す
                if (distFromCurrentY < closestDist)
                {
                    closestDist = distFromCurrentY;
                    // Xはリフトの固定位置、Yは検知したブロックの高さ
                    targetPos = new Vector3(fixedXPos.x, hit.collider.transform.position.y, fixedXPos.z);
                    found = true;
                }
            }
        }
        return found;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isInitialized)InitializeLift();

            // プレイヤーをリフトを親子関係に
            Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();

            // まだ動いていなければ移動開始
            if (FindNextDestination())
            {
                // プレイヤーを止める
                pWalk.StateChange(0); // idol
                // プレイヤーをリフトの真ん中に吸着
                pWalk.transform.position = new Vector3(transform.position.x, pWalk.transform.position.y, pWalk.transform.position.z);


                // 親子関係に
                collision.gameObject.transform.SetParent(transform);

               StartCoroutine(MoveRoutine(pWalk));
            }
        }
    }

    IEnumerator MoveRoutine(Player_walk pWalk)
    {
        isMoving = true;
        yield return new WaitForSeconds(waitTime); // 動き出す前のタメ
        
        Vector3 currentStart = transform.position;
        Vector3 currentEnd = targetPos;

        // X座標を固定
        currentEnd.x = fixedXPos.x;

        float distance = Vector3.Distance(currentStart, currentEnd);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = Mathf.SmoothStep(0,1,elapsed / duration);

            // SmoothStepで動き出しと止まる前をなめらかに
            transform.position = Vector3.Lerp(currentStart, currentEnd, curve);

            yield return null;
        }

        transform.position = currentEnd;
        toTarget = !toTarget; // 逆方向にリセット
        isMoving = false;

        // 親子関係の解除、再歩
        pWalk.transform.SetParent(null);
        pWalk.StateChange(1); 
    }
}
