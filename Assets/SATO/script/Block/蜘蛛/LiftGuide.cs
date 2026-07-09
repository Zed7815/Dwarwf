using UnityEngine;

public class LiftGuide : MonoBehaviour
{
    private LineRenderer line;
    private LiftBlock liftScript;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        liftScript = GetComponentInParent<LiftBlock>();
    }

    void Update()
    {
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            line.positionCount = 0;
            return;
        }

        // LiftBlockの目的地検索ロジックを利用して描画
        // レイキャストの幅などはLiftBlockの設定に合わせる
        float myX = transform.parent.position.x;
        float myZ = transform.parent.position.z;

        // LiftBlockに目的地を探させる（引数に合わせたダミーを用意）
        // ※LiftBlock.csのFindNextDestinationをpublicにする必要があります
        Vector3 target;
        if (liftScript.gameObject.activeSelf)
        {
            // エディタ上での位置から予測
            DrawLiftPath();
        }
    }

    void DrawLiftPath()
    {
        // 簡易版：リフトは上下に動くので、現在の位置と予測地点を線で結ぶ
        // 本来はLiftBlock内のFindNextDestinationの結果を使うのがベスト
        // ここでは単純化のため、現在の位置を中心に表示
        line.positionCount = 2;
        line.SetPosition(0, transform.parent.position);

        // 目的地（本来はRayの結果を入れる）
        // ここでは仮に上下5マス分などの範囲を表示する設定も可能
    }
}