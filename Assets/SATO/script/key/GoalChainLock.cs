using UnityEngine;

public class GoalChainLock : MonoBehaviour
{
    [Header("参照")]
    public GameObject chainGraphics; // 鎖の見た目
    public Collider2D goalCollider;  // ゴールの判定(トリガー)

    void Start()
    {
        SetLockState(true); // 最初はロック
    }

    void Update()
    {
        if (FinalKeyManager.instance == null) return;

        // マネージャーの「鍵持ってるよフラグ」を常にチェック
        bool unlocked = FinalKeyManager.instance.isUnlocked;

        if (unlocked)
        {
            // 鍵があるなら、鎖を消して判定を出す（クリア可能）
            if (chainGraphics != null && chainGraphics.activeSelf)
            {
                SetLockState(false);
            }
        }
        else
        {
            // ★【ここが解決策】
            // 鍵がない（またはリセットされた）なら、強制的に鎖を出して判定を消す（クリア不可）
            if (chainGraphics != null && !chainGraphics.activeSelf)
            {
                SetLockState(true);
            }
        }
    }

    // ★ロック状態の切り替えを一括で行う
    private void SetLockState(bool isLocked)
    {
        if (isLocked)
        {
            // 鍵がかかっている状態
            if (chainGraphics != null) chainGraphics.SetActive(true); // 鎖を表示
            if (goalCollider != null) goalCollider.enabled = false;  // 当たり判定を消す
        }
        else
        {
            // 鍵が開いた状態
            if (chainGraphics != null) chainGraphics.SetActive(false); // 鎖を消す
            if (goalCollider != null) goalCollider.enabled = true;   // 当たり判定を出す
        }
    }

    // リセットボタンで呼ばれる
    public void OnGimmickReset()
    {
        SetLockState(true); // 強制的にロック状態に戻す
    }
}