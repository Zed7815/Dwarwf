// =========================================================================
// SpiderLift.cs - クモの出現＆糸引き上げ上下・同期制御 (バグ修正＆演出完全版)
// =========================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class SpiderLift : MonoBehaviour
{
    [Header("リフト基本設定")]
    public float speed = 2f;
    public float waitTime = 0.5f; // 到着後の待ち時間（クモの踏ん張りタメ演出時間）
    public LayerMask obstacleLayer; // どのレイヤーを天井・隣の足場ブロックとみなすか

    [Header("クモ・糸のアニメーション設定")]
    public GameObject spiderPrefab;       // クモのプレハブ（丸と足で構成されたシンプルなものでOK）
    public LineRenderer threadRenderer;   // 蜘蛛の糸を描画するLineRenderer
    public float threadDeploySpeed = 4.0f; // 設置時にクモが天井へ登って糸を張る速度

    private Vector3 fixedXPos; // X座標を固定する変数
    private Vector3 targetPos;
    private bool isMoving = false;
    private bool isInitialized = false;

    // クモおよび演出用の内部変数
    private GameObject spawnedSpider;
    private Vector3 ceilingPos;
    private bool isThreadStretched = false;

    void Start()
    {
        if (!isInitialized) InitializeLift();

        // 🌟 設置された瞬間、クモが裏から出てきて天井まで糸を張るアニメーションを開始！
        StartCoroutine(DeploySpiderAndThread());
    }

    void InitializeLift()
    {
        fixedXPos = transform.position;
        isInitialized = true;
    }

    // クモがリフトから出現し、天井まで這い上がりながら糸を張るコルーチン
    IEnumerator DeploySpiderAndThread()
    {
        // 1. 真上の天井（obstacleLayer）を自動スキャンして高さを決定
        ceilingPos = transform.position + Vector3.up * 5.0f; // 見つからない場合のデフォルト（5マス上）
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, 20f, obstacleLayer);
        if (hit.collider != null)
        {
            ceilingPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        // 2. クモをリフトの初期位置（裏）に生成
        if (spiderPrefab != null && spawnedSpider == null)
        {
            spawnedSpider = Instantiate(spiderPrefab, transform.position, Quaternion.identity, transform);
            spawnedSpider.transform.localScale = Vector3.zero; // 最初は極小からぬるっと現れる
        }

        // 3. クモがぬるっと実体化（スケール変化）
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            if (spawnedSpider != null)
            {
                spawnedSpider.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 0.8f, elapsed / 0.5f);
            }
            yield return null;
        }

        // 4. クモが天井へ向かって這い上がる
        Vector3 spiderStart = transform.position;
        elapsed = 0f;
        float climbDuration = Vector3.Distance(spiderStart, ceilingPos) / threadDeploySpeed;

        // 糸（LineRenderer）の初期化
        if (threadRenderer != null)
        {
            threadRenderer.positionCount = 2;
            threadRenderer.SetPosition(0, transform.position); // 起点1：リフト足場
            threadRenderer.SetPosition(1, transform.position); // 起点2：這い上がるクモ自身
        }

        // クモを天井に固定するため、リフトの親子関係を解除
        if (spawnedSpider != null) spawnedSpider.transform.SetParent(null);

        while (elapsed < climbDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / climbDuration;
            Vector3 currentSpiderPos = Vector3.Lerp(spiderStart, ceilingPos, t);

            if (spawnedSpider != null)
            {
                spawnedSpider.transform.position = currentSpiderPos;
                // 天井を這い上がる際のもぞもぞとした伸縮演出
                float wiggle = 1.0f + Mathf.Sin(Time.time * 25f) * 0.12f;
                spawnedSpider.transform.localScale = new Vector3(0.8f, 0.8f * wiggle, 1.0f);
            }

            if (threadRenderer != null)
            {
                threadRenderer.SetPosition(1, currentSpiderPos); // 糸の先端をクモの現在地に更新
            }

            yield return null;
        }

        if (spawnedSpider != null) spawnedSpider.transform.position = ceilingPos;
        isThreadStretched = true;

        // 呼吸するような待機アニメーションループを裏で走らせる
        StartCoroutine(SpiderIdleLoop());
    }

    // クモの呼吸待機アニメ
    IEnumerator SpiderIdleLoop()
    {
        while (true)
        {
            if (!isMoving && spawnedSpider != null)
            {
                // もぞもぞと息をしているような微細な伸縮アニメ
                float breath = 1.0f + Mathf.Sin(Time.time * 3f) * 0.06f;
                spawnedSpider.transform.localScale = new Vector3(0.8f * breath, 0.8f, 1f);
            }
            yield return null;
        }
    }

    // 真上/真下の一番近いブロックをスキャンして目的地を設定
    bool FindNextDestination()
    {
        // 左右1マスずれた場所を起点
        Vector3 leftLine = transform.position + Vector3.left * 1.0f;
        Vector3 rightLine = transform.position + Vector3.right * 1.0f;
        List<RaycastHit2D> allHits = new List<RaycastHit2D>();

        // 左右それぞれの列で上と下をスキャン
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(leftLine, Vector2.down, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.up, 20f, obstacleLayer));
        allHits.AddRange(Physics2D.RaycastAll(rightLine, Vector2.down, 20f, obstacleLayer));

        float closestDist = float.MaxValue;
        bool found = false;

        foreach (var hit in allHits)
        {
            // 🌟 【正確な階層判定】コライダーの上面（物理的な床の高さ）を基準に判定
            float platformTopY = hit.collider.bounds.max.y;
            float currentTopY = GetComponent<Collider2D>().bounds.max.y;
            float distFromCurrentY = Mathf.Abs(platformTopY - currentTopY);

            if (distFromCurrentY > 0.8f) // 0.8マス以上離れている＝別の階層
            {
                if (distFromCurrentY < closestDist)
                {
                    closestDist = distFromCurrentY;

                    // 🌟 【バグ修正】足場の上面（bounds.max.y）にリフトの上面がぴったりフラットに揃うよう、リフト半幅分を引いてピタッと横並びに！
                    float liftHalfHeight = GetComponent<Collider2D>().bounds.extents.y;
                    targetPos = new Vector3(fixedXPos.x, platformTopY - liftHalfHeight, fixedXPos.z);
                    found = true;
                }
            }
        }
        return found;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // プレイヤーが乗り、かつ糸張り完了＆静止状態の時のみ作動
        if (collision.gameObject.CompareTag("Player") && !isMoving && isThreadStretched)
        {
            if (!isInitialized) InitializeLift();

            Player_walk pWalk = collision.gameObject.GetComponent<Player_walk>();

            if (FindNextDestination())
            {
                // プレイヤーを停止・真ん中に吸着
                pWalk.StateChange(0); // Idle
                pWalk.transform.position = new Vector3(transform.position.x, pWalk.transform.position.y, pWalk.transform.position.z);

                // 物理移動中の親子関係化
                collision.gameObject.transform.SetParent(transform);

                StartCoroutine(MoveRoutine(pWalk));
            }
        }
    }

    IEnumerator MoveRoutine(Player_walk pWalk)
    {
        isMoving = true;

        // ── 動き出す前のタメ：クモが糸を引っ張る準備でブルブル震える ──
        float elapsedPre = 0f;
        Vector3 spiderBasePos = ceilingPos;
        while (elapsedPre < waitTime)
        {
            elapsedPre += Time.deltaTime;
            if (spawnedSpider != null)
            {
                // 高速で震えて力を溜めている踏ん張り演出
                float shake = Mathf.Sin(Time.time * 40f) * 0.15f;
                spawnedSpider.transform.position = spiderBasePos + Vector3.up * shake;
                spawnedSpider.transform.localScale = new Vector3(0.9f, 0.7f, 1f); // 体を少し押し潰して踏ん張る
            }
            yield return null;
        }

        Vector3 currentStart = transform.position;
        Vector3 currentEnd = targetPos;
        currentEnd.x = fixedXPos.x; // X座標を完全固定

        float distance = Vector3.Distance(currentStart, currentEnd);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(currentStart, currentEnd, curve);

            // 糸の描画をリフトの動きに合わせてリアルタイム更新
            if (threadRenderer != null)
            {
                threadRenderer.SetPosition(0, transform.position);
            }

            // クモが一生懸命糸を巻き上げる「高速もぞもぞアニメ」
            if (spawnedSpider != null)
            {
                float pullWiggle = Mathf.Sin(Time.time * 30f) * 0.12f;
                spawnedSpider.transform.position = ceilingPos + Vector3.up * pullWiggle;
                float scaleWiggle = 0.8f + Mathf.Sin(Time.time * 30f) * 0.1f;
                spawnedSpider.transform.localScale = new Vector3(scaleWiggle, 0.8f, 1f);
            }

            yield return null;
        }

        // 終着点への完全な位置補正
        transform.position = currentEnd;
        if (threadRenderer != null) threadRenderer.SetPosition(0, currentEnd);
        if (spawnedSpider != null) spawnedSpider.transform.position = ceilingPos;

        isMoving = false;

        // 親子関係を解除し、プレイヤーの歩行を再開
        pWalk.transform.SetParent(null);
        pWalk.StateChange(1); // Walk/Straight
    }

    // 🌟 エディットのやり直しやリセット時に自動クリーニングする復元関数
    public void ResetSpider()
    {
        if (spawnedSpider != null)
        {
            Destroy(spawnedSpider);
            spawnedSpider = null;
        }
        isThreadStretched = false;
        if (threadRenderer != null) threadRenderer.positionCount = 0;
        Start();
    }
}
