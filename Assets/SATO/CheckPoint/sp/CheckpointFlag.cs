using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    [Header("ビジュアル参照")]
    [Tooltip("旗のスプライト（空なら自動で取得します）")]
    public SpriteRenderer flagRenderer;

    [Header("★色の設定（カラーパレットで自由に変更可能）")]
    [Tooltip("未通過のときの色（普段の色）")]
    public Color inactiveColor = Color.gray;

    [Tooltip("触れたときの色（目立つ色・ゴールドなど）")]
    public Color activeColor = new Color(1f, 0.84f, 0f, 1f); // 鮮やかなゴールド

    [Header("スプライトの差し替え（画像自体を変えたい場合・任意）")]
    public Sprite inactiveSprite; // 普段の絵
    public Sprite activeSprite;   // 触れた後の絵

    [Header("SE設定")]
    public AudioClip activateSE;
    private AudioSource audioSource;

    [Header("出現位置の微調整")]
    public Vector3 respawnOffset = Vector3.zero;

    [HideInInspector] public bool isActive = false;

    void Start()
    {
        // 自分自身、または子供からSpriteRendererを自動取得
        if (flagRenderer == null) flagRenderer = GetComponent<SpriteRenderer>();
        if (flagRenderer == null) flagRenderer = GetComponentInChildren<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 開始時は未通過の色に設定
        SetVisualState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;
        if (collision.isTrigger) return; // センサーは無視

        if (collision.CompareTag("Player"))
        {
            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk == null) pWalk = collision.GetComponentInParent<Player_walk>();

            if (pWalk != null && CheckpointManager.instance != null)
            {
                if (CheckpointManager.instance.currentActiveCheckpoint == this) return;
                CheckpointManager.instance.ActivateCheckpoint(this, pWalk.direction);
            }
        }
    }

    // ★色と見た目を切り替える処理
    public void SetVisualState(bool active)
    {
        isActive = active;

        if (flagRenderer == null)
        {
            flagRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        if (flagRenderer != null)
        {
            // 1. 色の適用（インスペクターで選んだ色）
            flagRenderer.color = active ? activeColor : inactiveColor;

            // 2. 画像の適用（もし画像がセットされていれば差し替え）
            if (active && activeSprite != null)
            {
                flagRenderer.sprite = activeSprite;
            }
            else if (!active && inactiveSprite != null)
            {
                flagRenderer.sprite = inactiveSprite;
            }
        }

        // 音の再生
        if (active && audioSource != null && activateSE != null)
        {
            audioSource.PlayOneShot(activateSE);
        }
    }

    public Vector3 GetRespawnPosition()
    {
        return transform.position + respawnOffset;
    }
}