using UnityEngine;
using System.Collections; // ★追加：これが入っていないと IEnumerator でエラーになります！

public class PlacedBlockMarker : MonoBehaviour
{
    private Vector3 placedPosition;
    private Quaternion placedRotation;

    [Header("ロック状態")]
    public bool isLocked = false;           // 完全に固定されているか
    public bool isUsedInThisRun = false;   // このプレイで自機が触れたか

    [Header("南京錠アイコン（自動生成または指定）")]
    public GameObject lockIconObject;      // 頭上に表示する南京錠

    private Coroutine shakeRoutine;

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

    public void PlayLockDeniedEffect()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float duration = 0.25f; // 震える時間（0.25秒）
        float elapsed = 0f;
        float magnitude = 0.08f; // 震えの強さ

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = 1f - (elapsed / duration); // 徐々に収まる

            // サイン波で高速に左右にブルブルッと揺らす
            float offsetX = Mathf.Sin(elapsed * 60f) * magnitude * percent;
            transform.position = placedPosition + new Vector3(offsetX, 0, 0);
            yield return null;
        }

        // 最後に確実に元の位置にピタッと戻す（ズレ防止）
        transform.position = placedPosition;
        shakeRoutine = null;
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