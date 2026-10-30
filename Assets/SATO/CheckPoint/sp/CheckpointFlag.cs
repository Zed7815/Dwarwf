using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    [Header("ビジュアル参照")]
    public SpriteRenderer flagRenderer;

    [Header("色の設定")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = new Color(1f, 0.84f, 0f, 1f);

    [Header("スプライトの差し替え（任意）")]
    public Sprite inactiveSprite;
    public Sprite activeSprite;

    [Header("SE設定")]
    public AudioClip activateSE;
    private AudioSource audioSource;

    [Header("出現位置の微調整")]
    public Vector3 respawnOffset = Vector3.zero;

    [HideInInspector] public bool isActive = false;

    void Start()
    {
        if (flagRenderer == null) flagRenderer = GetComponent<SpriteRenderer>();
        if (flagRenderer == null) flagRenderer = GetComponentInChildren<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        SetVisualState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;
        if (collision.isTrigger) return;

        if (collision.CompareTag("Player"))
        {
            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk == null) pWalk = collision.GetComponentInParent<Player_walk>();

            if (pWalk != null && CheckpointManager.instance != null)
            {
                // ★修正：同じ旗であっても、星を取った後の状態を確実に再セーブする！
                CheckpointManager.instance.ActivateCheckpoint(this, pWalk.direction);
            }
        }
    }

    public void SetVisualState(bool active)
    {
        isActive = active;

        if (flagRenderer == null)
        {
            flagRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        if (flagRenderer != null)
        {
            flagRenderer.color = active ? activeColor : inactiveColor;

            if (active && activeSprite != null) flagRenderer.sprite = activeSprite;
            else if (!active && inactiveSprite != null) flagRenderer.sprite = inactiveSprite;
        }

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