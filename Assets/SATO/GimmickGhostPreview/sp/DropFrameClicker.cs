using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DropFrameClicker : MonoBehaviour
{
    void Update()
    {
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit) return;

        // UIの上をクリックしている時は無視
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // マウスのワールド座標を取得
        if (Camera.main == null) return;
        Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));
        mouseWorld.z = 0f;

        // 自分のコライダー（枠）の中にマウスがあるか？
        if (col.OverlapPoint(mouseWorld))
        {
            // 左クリックされた瞬間
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryGrabOrDelete();
            }
        }
    }

    void TryGrabOrDelete()
    {
        BlockManager bm = FindObjectOfType<BlockManager>();
        if (bm == null) return;

        // 枠の座標（transform.position）にある「PlacedBlock」を探す
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("PlacedBlock") || hit.GetComponentInParent<BlockInfo>() != null)
            {
                // ブロックが見つかった！ BlockManager側につかむ処理をさせたい場合はここに記述します
                Debug.Log("<color=green>【枠クリック】中のブロックを検知しました</color>");
                break;
            }
        }
    }
}