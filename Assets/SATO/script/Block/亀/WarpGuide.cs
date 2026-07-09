using UnityEngine;

public class WarpGuide : MonoBehaviour
{
    private LineRenderer line;
    private WarpEntrance entrance;

    void Start()
    {
        // 自分自身、または子要素から LineRenderer を探す
        line = GetComponent<LineRenderer>();
        if (line == null) line = GetComponentInChildren<LineRenderer>();

        // 親要素、または自分自身から WarpEntrance を探す
        entrance = GetComponentInParent<WarpEntrance>();
        if (entrance == null) entrance = GetComponent<WarpEntrance>();
    }

    void Update()
    {
        // ★安全対策：LineRendererが無い場合は何もしない（エラー防止）
        if (line == null) return;

        // 実行モードの時は非表示
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            line.positionCount = 0;
            return;
        }

        // 入口と出口が両方存在する場合のみ線を描画
        if (entrance != null && entrance.exitPoint != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, entrance.transform.position);
            line.SetPosition(1, entrance.exitPoint.position);

            // アニメーション（点線が流れる処理）
            if (line.material != null)
            {
                float offset = Time.time * -1f;
                line.material.mainTextureOffset = new Vector2(offset, 0);
            }
        }
        else
        {
            line.positionCount = 0;
        }
    }
}