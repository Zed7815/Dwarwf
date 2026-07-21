using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CustomCursor : MonoBehaviour
{
    public static CustomCursor instance;

    [Header("カーソル画像")]
    public Sprite normalSprite;
    public Sprite clickSprite;

    [Header("サイズ・位置設定")]
    public Vector2 cursorScale = Vector2.one;
    public Vector2 offset = Vector2.zero;

    private Image cursorImage;
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        // --- シーンをまたいでも消えないようにする設定 ---
        if (instance == null)
        {
            instance = this;
            // 親のCanvasごと残す
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            // 既に存在していれば新しい方は削除
            Destroy(transform.root.gameObject);
            return;
        }

        cursorImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // カーソルの非表示設定（一回呼べば基本OK）
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined; // 画面外に逃げないようにする場合

        if (cursorImage != null) cursorImage.raycastTarget = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. 位置の更新
        Vector2 mousePos = Mouse.current.position.ReadValue();
        rectTransform.position = mousePos + offset;

        // 2. サイズの適用
        rectTransform.localScale = new Vector3(cursorScale.x, cursorScale.y, 1.0f);

        // 3. 画像の切り替え
        if (Mouse.current.leftButton.isPressed)
        {
            if (clickSprite != null) cursorImage.sprite = clickSprite;
        }
        else
        {
            if (normalSprite != null) cursorImage.sprite = normalSprite;
        }

        // 念押し：常にマウスを隠し続ける
        if (Cursor.visible) Cursor.visible = false;
    }

    // ★重要：OnDisable で Cursor.visible = true にしない！
    // こうすることで、シーン切り替え中もマウスが復活しなくなります
}