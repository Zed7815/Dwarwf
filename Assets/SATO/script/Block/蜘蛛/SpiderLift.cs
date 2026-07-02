using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderLift : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("リフトが上下に移動する速度")]
    public float speed = 2f;

    [Tooltip("目的地に到着した後の待機時間（秒）")]
    public float waitTime = 0.5f;

    [Tooltip("どのレイヤーを天井・足場（ obstacle ）としてみなすか（通常は Ground や Obstacle など）")]
    public LayerMask obstacleLayer;

    [Header("位置の微調整")]
    [Tooltip("リフトが止まる位置の微調整用オフセット。少し下で止まる場合は「0.1」や「0.2」などプラスの値を設定してピッタリ合わせてください。")]
    public float yOffset = 0f;

    private Vector3 fixedXPos; // 垂直移動を保証するため、初期のX座標を完全に固定する変数
    private Vector3 targetPos; // 次の移動目的地
    private bool isMoving = false;
    private bool isInitialized = false;

    private void Awake()
    {
        // 🌟【重要バグ修正】ゲーム開始時の「最初の1フレーム」で、リフトが1ミリも動いていない状態の初期位置を確実に記憶します。
        // これにより、物理衝突や乗車時の衝撃で左右にブレても、移動軸とRayの照射軸が完全に垂直に保たれます。
        InitializeLift();
    }

    void Start()
    {
        if (!isInitialized)
        {
            InitializeLift();
        }
    }

    void InitializeLift()
    {
        fixedXPos = transform.position;
        isInitialized = true;
    }

    // 真上、または真下にある一番近いブロック（またはTilemap）を探し、目的地を算出する
    bool FindNextDestination()
    {
        // 照射点のX座標は初期の完全固定位置(fixedXPos.x)にロックし、Y座標は現在の高さから上下に照射します。
        Vector3 leftLine = new Vector3(fixedXPos.x - 1.0f, transform.position.y, fixedXPos.z);
        Vector3 rightLine = new Vector3(fixedXPos.x + 1.0f, transform.position.y, fixedXPos.z);

        List<RaycastHit2D> allHits = new List<RaycastHit2D>();

        // 左右それぞれの列で、上方向と下方向を徹底的にスキャン
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.down, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.down, 20f, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        Collider2D myCollider = GetComponent<Collider2D>();
        float myHalfHeight = myCollider != null ? myCollider.bounds.extents.y : 0.5f;

        // リフトの現在の上面の高さ
        float myTopY = myCollider != null ? myCollider.bounds.max.y : transform.position.y + myHalfHeight;

        foreach (var hit in allHits)
        {
            // 🌟【最重要バグ修正：自分自身と他のリフトを除外】
            // 自分自身のコライダー、および「他の動いているリフト(LiftBlock)」はお互いに目的地から除外します。
            // これにより、複数リフトを置いたときに引き合って一箇所に固まるバグを100%防ぎます。
            if (myCollider != null && (hit.collider == myCollider || hit.collider.gameObject == gameObject)) continue;
            if (hit.collider.GetComponent<LiftBlock>() != null) continue;

            float targetTopY = 0f;

            // 🌟【Tilemap & 個別コライダー完全対応の二刀流ロジック】
            UnityEngine.Tilemaps.Tilemap tilemap = hit.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap != null)
            {
                // 対象がTilemapの場合：衝突点（hit.point）から、Rayの進行方向に少し進んだ「タイルの内側」をサンプルして、グリッドセルを特定します。
                Vector2 rayDir = (hit.point.y > transform.position.y) ? Vector2.up : Vector2.down;
                Vector2 cellSamplePos = hit.point + (rayDir * 0.1f);
                Vector3Int cell = tilemap.WorldToCell(cellSamplePos);
                Vector3 cellWorld = tilemap.CellToWorld(cell);

                // タイル上面の高さ = セルの下端Y座標 + タイルの高さ（通常は1.0f）
                targetTopY = cellWorld.y + tilemap.cellSize.y;
            }
            else
            {
                // 対象が通常オブジェクトの場合：コライダーの上面の最大Y座標をそのまま採用します。
                targetTopY = hit.collider.bounds.max.y;
            }

            // 🌟【グリッドスナップ処理】
            // 小数点第何位といった極小の物理演算のズレや、タイルの隙間のノイズを完全に吸収するために0.5f刻みに綺麗に四捨五入して丸めます。
            targetTopY = Mathf.Round(targetTopY * 2.0f) / 2.0f;

            // 現在の高さ（上面）とほぼ同じ高さにあるものは「現在の階（自分自身が今いる床）」なので無視する
            float distFromCurrentY = Mathf.Abs(targetTopY - myTopY);

            if (distFromCurrentY > 0.8f) // 0.8マス以上離れている＝別の階層
            {
                if (distFromCurrentY < closestDist)
                {
                    closestDist = distFromCurrentY;

                    // 🌟【ピッタリ合わせるターゲット座標の計算】
                    // リフトの上面を目的地の上面(targetTopY)にぴったりと揃えるため、
                    // リフトの中心Y座標 =「目的地の上面 - リフトの厚みの半分 + 微調整用のyOffset」となります。
                    targetPos = new Vector3(fixedXPos.x, targetTopY - myHalfHeight + yOffset, fixedXPos.z);
                    found = true;
                }
            }
        }
        return found;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // プレイヤーがリフトに飛び乗った場合
        if (collision.gameObject.CompareTag("Player") && !isMoving)
        {
            if (!isInitialized) InitializeLift();

            // プレイヤーの歩行コントロールコンポーネントを取得
            Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();

            // 目的地となる天井か床が見つかった場合、移動を開始
            if (FindNextDestination())
            {
                if (pWalk != null)
                {
                    // 1. プレイヤーを立ち止まらせる (Idle状態など)
                    pWalk.StateChange(0);

                    // 2. プレイヤーをリフトの水平中心にピッタリと吸着
                    pWalk.transform.position = new Vector3(transform.position.x, pWalk.transform.position.y, pWalk.transform.position.z);
                }

                // 3. プレイヤーをリフトの子オブジェクトにして、移動時に一緒に滑らかに動くようにする
                collision.gameObject.transform.SetParent(transform);

                // 4. 移動処理を開始
                StartCoroutine(MoveRoutine(pWalk));
            }
        }
    }

    IEnumerator MoveRoutine(Player_walk pWalk)
    {
        isMoving = true;

        // 到着後の待ち時間（タメ）
        yield return new WaitForSeconds(waitTime);

        // 🌟【バグ修正：物理ノイズの除去】
        // 移動開始時と終了時のX座標を、最初の1フレームでロックした fixedXPos.x に強制リセットし、絶対に左右にズレないようにします。
        Vector3 currentStart = transform.position;
        currentStart.x = fixedXPos.x;
        Vector3 currentEnd = targetPos;
        currentEnd.x = fixedXPos.x;

        float distance = Vector3.Distance(currentStart, currentEnd);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // イージング（滑らかな加減速）をかけてスムーズに移動
            float curve = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(currentStart, currentEnd, curve);
            yield return null;
        }

        // 最終地点に完全に吸着
        transform.position = currentEnd;

        // 到着後の待機時間
        yield return new WaitForSeconds(waitTime);

        // プレイヤーの親子関係を解除
        if (pWalk != null && pWalk.transform.parent == transform)
        {
            pWalk.transform.SetParent(null);
        }

        isMoving = false;
    }
}