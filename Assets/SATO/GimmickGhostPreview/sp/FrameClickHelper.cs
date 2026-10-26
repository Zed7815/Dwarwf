using UnityEngine;
using UnityEngine.EventSystems;

public class FrameClickHelper : MonoBehaviour, IPointerClickHandler
{
    // 枠がクリックされた時、BlockManagerに「ここがクリックされたよ！」と直接伝える
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.instance == null || GameManager.instance.blockManager == null) return;
        if (GameManager.instance.currentState != GameManager.GameState.Edit) return;

        BlockManager bm = GameManager.instance.blockManager;

        // 右クリックの場合（削除）
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // この枠の位置にあるPlacedBlockを消す処理を呼び出す
            // (BlockManager側で自動処理されます)
        }

        Debug.Log("<color=green>【枠クリック検知】</color>");
    }
}