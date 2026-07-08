using UnityEngine;
using System.Collections.Generic;

public class JumpTrajectoryGuide : MonoBehaviour
{
    [Header("ライン設定")]
    public LineRenderer lineLeft;
    public LineRenderer lineRight;
    public int resolution = 30; // 線の細かさ
    public float maxTime = 2.0f; // 予測する秒数
    public LayerMask groundLayer; // 地面レイヤー

    private Player_walk playerRef;

    void Start()
    {
        // プレイヤーからジャンプ力を取得するために参照を探す
        playerRef = GameObject.FindObjectOfType<Player_walk>();

        // LineRendererの設定（スクリプトから自動設定）
        SetupLine(lineLeft);
        SetupLine(lineRight);
    }

    void SetupLine(LineRenderer line)
    {
        if (line == null) return;
        line.positionCount = 0;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.useWorldSpace = true;
    }

    void Update()
    {
        // 実行モードの時は表示を消す
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            lineLeft.positionCount = 0;
            lineRight.positionCount = 0;
            return;
        }

        if (playerRef == null) return;

        // 左右の軌道を描画
        DrawPath(lineRight, 1); // 右方向
        DrawPath(lineLeft, -1); // 左方向
    }

    void DrawPath(LineRenderer line, int side)
    {
        if (line == null) return;

        List<Vector3> points = new List<Vector3>();

        // スタート地点（ブロックの中央上部）
        Vector3 startPos = transform.position + Vector3.up * 0.5f;

        // 初速の計算（Player_walkの数値を参照）
        float vx = playerRef.playerJumpForwardPower * side;
        float vy = playerRef.playerJumpPower;
        float gravity = Physics2D.gravity.y * playerRef.GetComponent<Rigidbody2D>().gravityScale;

        points.Add(startPos);

        Vector3 lastPos = startPos;

        for (int i = 1; i <= resolution; i++)
        {
            float t = (i / (float)resolution) * maxTime;

            // 放物線の公式: x = v0t, y = v0t + 0.5gt^2
            float x = vx * t;
            float y = (vy * t) + (0.5f * gravity * t * t);

            Vector3 nextPos = startPos + new Vector3(x, y, 0);

            // 途中に地面があるかチェック
            RaycastHit2D hit = Physics2D.Linecast(lastPos, nextPos, groundLayer);
            if (hit.collider != null)
            {
                points.Add(hit.point);
                break; // 地面に当たったら計算終了
            }

            points.Add(nextPos);
            lastPos = nextPos;
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }
}
