using UnityEngine;
using UnityEngine.InputSystem;

public class FrameClickReceiver : MonoBehaviour
{
    void OnMouseDown()
    {
        // エディットモード中のみ反応
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit) return;
       // if (BlockManager.instance == null) return; // ※BlockManagerに static instance を作るか、FindObjectOfTypeを使う

        // この枠のワールド座標を取得
        Vector3 framePos = transform.parent.position;

        // 左クリックされた場合（掴む / ドラッグ開始など）
        // 右クリックされた場合（削除）
        // 既存の BlockManager の仕組みをここで呼び出します
        Debug.Log($"<color=cyan>【枠の子クリック検知】座標: {framePos}</color>");
    }
}