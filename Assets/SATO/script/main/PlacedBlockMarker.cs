using UnityEngine;

public class PlacedBlockMarker : MonoBehaviour
{
    private Vector3 placedPosition;
    private Quaternion placedRotation;

    [Header("ロック状態")]
    public bool isLocked = false;           // 完全に固定されているか
    public bool isUsedInThisRun = false;   // このプレイで自機が触れたか

    [Header("南京錠アイコン（自動生成または指定）")]
    public GameObject lockIconObject;      // 頭上に表示する南京錠

    void Start()
    {
        // 南京錠アイコンの初期非表示
        if (lockIconObject != null) lockIconObject.SetActive(isLocked);
    }

    public void SavePlacedState()
    {
        placedPosition = transform.position;
        placedRotation = transform.rotation;
    }

    // 自機が乗ったり触れたりした瞬間を検知
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayerContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckPlayerContact(collision);
    }

    void CheckPlayerContact(Collider2D col)
    {
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Play) return;

        if (col.CompareTag("Player") || col.GetComponentInParent<Player_walk>() != null)
        {
            isUsedInThisRun = true; // このプレイで使用された！
        }
    }

    // ★旗を踏んだ時に呼ばれる：ブロックをロックして南京錠を出す
    public void LockBlock()
    {
        isLocked = true;
        if (lockIconObject != null) lockIconObject.SetActive(true);
    }

    // ロックを解除して南京錠を隠す
    public void UnlockBlock()
    {
        isLocked = false;
        isUsedInThisRun = false;
        if (lockIconObject != null) lockIconObject.SetActive(false);
    }

    public void ResetUsedFlag()
    {
        isUsedInThisRun = false;
    }

    // リセット時に呼ばれる
    public void OnGimmickReset()
    {
        transform.position = placedPosition;
        transform.rotation = placedRotation;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }

        // ロック状態に合わせて南京錠の表示を更新
        if (lockIconObject != null) lockIconObject.SetActive(isLocked);
    }
}