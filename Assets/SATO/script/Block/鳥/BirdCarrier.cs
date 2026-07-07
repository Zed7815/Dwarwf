
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdCarrier : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("運ぶスピード")]
    public float speed = 4.0f;
    [Tooltip("つかむ前・到着後のタメ（待ち時間）")]
    public float waitTime = 0.5f;
    [Tooltip("障害物や着地させる足場のレイヤー")]
    public LayerMask obstacleLayer;
    [Tooltip("横方向の索敵最大距離")]
    public float maxSearchDistance = 20.0f;
    [Tooltip("飛び上がる高さ（高度）")]
    public float heightOffset = 1.5f;
    [Tooltip("運んでいる最中のプレイヤーの上下位置（鳥に対する相対Y座標。頭付近なら正数、お腹下なら負数）")]
    public float grabOffsetY = -0.7f;
    [Tooltip("着地時のY座標オフセット調整（出発時の高さに対する微調整値）")]
    public float landYOffset = 0.0f;

    [Header("ビジュアル・演出設定")]
    [Tooltip("鳥のグラフィック用SpriteRenderer（左右反転に使用。未設定時は自身から取得）")]
    public SpriteRenderer birdSprite;
    [Tooltip("鳥のアニメーション用Animator（未設定時は自身から取得）")]
    public Animator animator;
    [Tooltip("飛行アニメーションを制御するAnimatorのBoolパラメータ名")]
    public string flyBoolParam = "isFlying";

    private Vector3 targetPos;
    private bool isMoving = false;

    void Start()
    {
        if (birdSprite == null)
        {
            birdSprite = GetComponent<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // プレイヤーが向いている方向に向かって一番近い足場を探す
    bool FindNextDestination(Player_walk pWalk)
    {
        // プレイヤーの進行方向（1: 右, -1: 左）
        int dir = pWalk.direction;
        Vector2 rayDir = dir > 0 ? Vector2.right : Vector2.left;

        float rayOffset = 1.5f;
        Vector3 rayStart = new Vector3(transform.position.x + (rayDir.x * rayOffset), pWalk.transform.position.y, transform.position.z);

        //UnityのSceneビューでRay（索敵レーザー）を赤い線で表示（10秒間表示）
        Debug.DrawRay(rayStart, rayDir * maxSearchDistance, Color.red, 10.0f);

        // レイキャストを飛ばして足場（ブロック）を探索
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDir, maxSearchDistance, obstacleLayer);

        if (hit.collider != null)
        {
            float targetX = hit.point.x + (dir * 0.8f);

            float targetY = pWalk.transform.position.y + landYOffset;

            targetPos = new Vector3(targetX, targetY, transform.position.z);

            Debug.Log($"[BirdCarrier] 足場を発見！ 衝突点: {hit.point} -> 補正後の目的地: {targetPos}");
            return true;
        }

        Debug.LogWarning($"[BirdCarrier] 索敵失敗。Rayの開始位置: {rayStart}, 方向: {rayDir}。障害物レイヤー(ObstacleLayer)の設定やコライダーを確認してください。");
        return false;
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isMoving) return;

        // 接触時、コンソールに必ず出力
        Debug.Log($"[BirdCarrier] 接触オブジェクト: {trigger.gameObject.name}、タグ: {trigger.gameObject.tag}");

        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();

            if (p != null)
            {
                bool found = FindNextDestination(p);
                Debug.Log($"[BirdCarrier] 足場の探索結果: {found}");

                if (found)
                {
                    StartCoroutine(CarrySequence(p));
                }
            }
        }
    }

    IEnumerator CarrySequence(Player_walk pWalk)
    {
        isMoving = true;

        // --- 1.プレイヤーの移動を止めてその場で待機 ---
        Rigidbody2D rb = pWalk.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        pWalk.StateChange(0); // プレイヤーを一時的に待機(idol)状態にする

        // 鳥の向きを、移動する方向（プレイヤーの向き）に合わせる
        int moveDir = pWalk.direction;
        SetBirdFacing(moveDir);

        yield return new WaitForSeconds(waitTime); // 飛び立ち前のタメ

        // --- 2.鳥が上に飛び上がると同時に、プレイヤーを滑らかに引き上げる ---
        Vector3 takeoffStartBird = transform.position;
        Vector3 takeoffEndBird = transform.position + new Vector3(0, heightOffset, 0);
        float takeoffDuration = 0.5f;
        float takeoffElapsed = 0f;

        Vector3 startPlayerPos = pWalk.transform.position; // 掴み上げ開始時のプレイヤーの元の位置

        // 飛行開始（羽ばたきループ）
        if (animator != null && !string.IsNullOrEmpty(flyBoolParam))
        {
            animator.SetBool(flyBoolParam, true);
        }

        while (takeoffElapsed < takeoffDuration)
        {
            takeoffElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, takeoffElapsed / takeoffDuration);

            // 鳥を上昇させる
            transform.position = Vector3.Lerp(takeoffStartBird, takeoffEndBird, t);

            // プレイヤーを「元の位置」から「現在の鳥のぶら下がり位置（grabOffsetY）」へ滑らかに引き上げる！
            Vector3 targetGrabPos = transform.position + new Vector3(0, grabOffsetY, 0);
            pWalk.transform.position = Vector3.Lerp(startPlayerPos, targetGrabPos, t);

            yield return null;
        }
        transform.position = takeoffEndBird;
        pWalk.transform.position = transform.position + new Vector3(0, grabOffsetY, 0);

        yield return new WaitForSeconds(0.2f); // 上昇しきった後の短いタメ

        // --- 3.高度を維持したまま、目標足場の上空まで等速・滑らかに移動 ---
        Vector3 birdStartPos = transform.position;
        // 目的地の上空（地面から heightOffset 分高い位置）を目標にする
        Vector3 birdEndPos = targetPos + new Vector3(0, heightOffset - grabOffsetY, 0);

        float distance = Vector3.Distance(birdStartPos, birdEndPos);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(birdStartPos, birdEndPos, curve);

            // プレイヤーも同期させて移動（カメラ追従を自然に保つ）
            pWalk.transform.position = transform.position + new Vector3(0, grabOffsetY, 0);

            yield return null;
        }
        transform.position = birdEndPos;
        pWalk.transform.position = birdEndPos + new Vector3(0, grabOffsetY, 0);

        yield return new WaitForSeconds(0.1f); // 移動完了後の短いタメ

       
        // プレイヤーを歩行（自由行動）状態に戻
        pWalk.StateChange(1);

        // --- 4.に向けてゆっくり降下し、着地する ---
        Vector3 landStartBird = transform.position;
        Vector3 landEndBird = targetPos; // 鳥自身の着地座標（targetPos＝ブロックの上）
        float landDuration = 0.5f; // 降下にかける時間
        float landElapsed = 0f;

        while (landElapsed < landDuration)
        {
            landElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, landElapsed / landDuration);
            transform.position = Vector3.Lerp(landStartBird, landEndBird, t);

            // 💡 プレイヤーの同期処理は行わない（もう離したため自由に行動できます）
            yield return null;
        }
        transform.position = landEndBird;

        // 着地完了（待機Idolへ戻る）
        if (animator != null && !string.IsNullOrEmpty(flyBoolParam))
        {
            animator.SetBool(flyBoolParam, false);
        }

        // 到着後の溜め（着陸の余韻）
        yield return new WaitForSeconds(waitTime);

        // --- 5.鳥の見た目を反転して終了 ---
        SetBirdFacing(-moveDir);

        // 連続発動防止用のインターバル
        yield return new WaitForSeconds(0.1f);
        isMoving = false;
    }

    // 鳥のグラフィックの向きを左右反転させるヘルパーメソッド
    void SetBirdFacing(int dir)
    {
        if (birdSprite != null)
        {
            Vector3 scale = birdSprite.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * dir; // 絶対値に方向を掛けて左右反転
            birdSprite.transform.localScale = scale;
        }
    }

    // スクリプト内のどこでも良いので追記
    void OnGimmickReset()
    {
        StopAllCoroutines();

        isMoving = false;
    }

}