using UnityEngine;

public class CheckpointFlag : MonoBehaviour
{
    [Header("ビジュアル設定")]
    public SpriteRenderer flagRenderer;
    public Color inactiveColor = Color.gray;   // 未通過の色
    public Color activeColor = new Color(1f, 0.85f, 0.2f); // 通過中（金色など）

    [Header("SE設定")]
    public AudioClip activateSE;
    private AudioSource audioSource;

    [Header("出現位置の微調整")]
    public Vector3 respawnOffset = Vector3.zero;

    private bool isActive = false;

    void Start()
    {
        if (flagRenderer == null) flagRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        SetVisualState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイモード中のみ反応
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;

        if (collision.CompareTag("Player") && !isActive)
        {
            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk != null && CheckpointManager.instance != null)
            {
                // マネージャーに旗と「プレイヤーの現在の向き」を渡す
                CheckpointManager.instance.ActivateCheckpoint(this, pWalk.direction);
            }
        }
    }

    public void SetVisualState(bool active)
    {
        isActive = active;

        if (flagRenderer != null)
        {
            flagRenderer.color = active ? activeColor : inactiveColor;
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