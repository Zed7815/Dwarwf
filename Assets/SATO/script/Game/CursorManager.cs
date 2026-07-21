using UnityEngine;
using UnityEngine.InputSystem; // Input Systemを使用している場合

public class CursorManager : MonoBehaviour
{
    [Header("カーソル画像")]
    public Texture2D normalCursor;  // 通常時の画像
    public Texture2D clickCursor;   // クリック中（長押し中）の画像

    [Header("設定")]
    public Vector2 hotspot = Vector2.zero; // クリック判定の場所（画像の左上なら0,0）

    void Start()
    {
        // 最初のカーソルを設定
        SetCursor(normalCursor);
    }

    void Update()
    {
        // --- 新しい Input System を使っている場合 ---
        if (Mouse.current != null)
        {
            // 左クリックが押された瞬間
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursor(clickCursor);
            }
            // 左クリックが離された瞬間
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                SetCursor(normalCursor);
            }
        }

        /* 
        // --- もし古い Input (Input.GetMouseButton) を使っている場合はこちら ---
        if (Input.GetMouseButtonDown(0)) SetCursor(clickCursor);
        if (Input.GetMouseButtonUp(0)) SetCursor(normalCursor);
        */
    }

    void SetCursor(Texture2D tex)
    {
        if (tex != null)
        {
            Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
        }
    }

    // シーンが切り替わった時などのために、アプリ終了時や破棄時にカーソルを戻す設定
    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}