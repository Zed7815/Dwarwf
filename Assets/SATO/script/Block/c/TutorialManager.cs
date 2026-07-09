using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;
    public RectTransform startPointA; // A: UIのアイコンなど
    public Transform targetFrameB;   // B: ステージ上の枠
    public GameObject startButton;

    [Header("ガイドオブジェクト (UI/Canvas内を推奨)")]
    public RectTransform placementHand; // 動く指のアイコン
    public GameObject scrollIcon;      // マウスホイール説明
    public GameObject startArrow;      // スタートボタン用矢印

    [Header("アニメーション演出設定")]
    public float moveSpeed = 2.0f;
    public float startDelay = 0.5f;
    public float endDelay = 0.8f;

    private bool isPlacementDone = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        // 初期表示設定
        if (placementHand) placementHand.gameObject.SetActive(true);
        if (scrollIcon) scrollIcon.SetActive(true);
        if (startArrow) startArrow.SetActive(false);

        // スクロール説明は5秒後に消す
        StartCoroutine(HideScrollIconAfterDelay(5f));

        // 設置アニメーション開始
        StartCoroutine(PlacementAnimationRoutine());
    }

    void Update()
    {
        if (gameManager == null || gameManager.blockManager == null) return;

        // 【ステップ1：設置ガイドの表示判定】
        if (!isPlacementDone)
        {
            // 指定した枠(B)にブロックが置かれたかチェック
            if (IsBlockInTargetFrame())
            {
                isPlacementDone = true;
                if (placementHand) placementHand.gameObject.SetActive(false);
            }
        }

        // 【ステップ2：スタートボタンへの誘導判定】
        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            // すべて置き終わったら矢印を出す
            bool allPlaced = gameManager.blockManager.IsAllBlocksPlaced();
            if (startArrow) startArrow.SetActive(allPlaced);
        }
        else
        {
            // プレイ中（実行モード）はガイドをすべて消す
            HideAllGuides();
        }
    }

    // 指アイコンが A（UI）から B（枠）へ移動するループ
    IEnumerator PlacementAnimationRoutine()
    {
        while (!isPlacementDone)
        {
            if (startPointA == null || targetFrameB == null || placementHand == null) yield break;

            // 1. A地点に移動して少し待機
            placementHand.position = startPointA.position;
            yield return new WaitForSeconds(startDelay);

            // 2. AからBへスーッと移動
            float t = 0;
            Vector3 startPos = startPointA.position;

            while (t < 1.0f)
            {
                if (isPlacementDone) yield break;

                // ステージ上の枠(B)の現在地を、画面上の座標にリアルタイム変換
                // これにより、カメラが動いても正確に枠を指し示します
                Vector3 endPos = mainCam.WorldToScreenPoint(targetFrameB.position);

                t += Time.deltaTime * moveSpeed;
                placementHand.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // 3. B地点で少し待機
            yield return new WaitForSeconds(endDelay);
        }
    }

    void HideAllGuides()
    {
        if (startArrow) startArrow.SetActive(false);
        if (placementHand) placementHand.gameObject.SetActive(false);
        if (scrollIcon) scrollIcon.SetActive(false);
    }

    IEnumerator HideScrollIconAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (scrollIcon) scrollIcon.SetActive(false);
    }

    // 物理判定で枠(B)の中に「PlacedBlock」タグのオブジェクトがあるか確認
    bool IsBlockInTargetFrame()
    {
        if (targetFrameB == null) return false;
        Collider2D hit = Physics2D.OverlapPoint(targetFrameB.position);
        return hit != null && hit.CompareTag("PlacedBlock");
    }

    // リセットボタンが押されたとき（やり直し）
    void OnGimmickReset()
    {
        StopAllCoroutines();
        isPlacementDone = false;
        if (placementHand) placementHand.gameObject.SetActive(true);
        if (scrollIcon) scrollIcon.SetActive(true);
        StartCoroutine(HideScrollIconAfterDelay(5f));
        StartCoroutine(PlacementAnimationRoutine());
    }
}