using UnityEngine;

public class GoalChainLock : MonoBehaviour
{
    [Header("参照")]
    public GameObject chainGraphics;
    public Collider2D goalCollider;

    void Start()
    {
        ResetStatus(); // 開始時に初期化
    }

    void Update()
    {
        // 鍵がアンロックされているかチェック
        if (FinalKeyManager.instance != null && FinalKeyManager.instance.isUnlocked)
        {
            // アンロックされているなら鎖を消し、判定を出す
            if (chainGraphics != null && chainGraphics.activeSelf)
            {
                chainGraphics.SetActive(false);
                if (goalCollider != null) goalCollider.enabled = true;
            }
        }
    }

    // リセット命令
    public void OnGimmickReset()
    {
        ResetStatus();
        Debug.Log("ゴールの鎖をリセットしました");
    }

    private void ResetStatus()
    {
        // 鎖を表示し、当たり判定を無効にする
        if (chainGraphics != null) chainGraphics.SetActive(true);
        if (goalCollider != null) goalCollider.enabled = false;
    }
}