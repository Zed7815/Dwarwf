using UnityEngine;

public class GoalChainLock : MonoBehaviour
{
    [Header("参照")]
    public GameObject chainGraphics; // 鎖の見た目
    public Collider2D goalCollider;  // ゴール自体の当たり判定

    void Start()
    {
        // 最初はゴールできないようにしておく
        if (goalCollider != null) goalCollider.enabled = false;
    }

    void Update()
    {
        // 鍵が揃ったら鎖を消してゴールを有効化
        if (FinalKeyManager.instance != null && FinalKeyManager.instance.isUnlocked)
        {
            if (chainGraphics != null && chainGraphics.activeSelf)
            {
                // 鎖を消す（破壊せずに非表示）
                chainGraphics.SetActive(false);
                if (goalCollider != null) goalCollider.enabled = true;

                // ここで「鎖が解けた音」などを鳴らすと良い
            }
        }
    }

    public void OnGimmickReset()
    {
        // 1. 鎖の画像を強制的に再表示
        if (chainGraphics != null)
        {
            chainGraphics.SetActive(true);
        }

        // 2. ゴール判定を無効化
        if (goalCollider != null)
        {
            goalCollider.enabled = false;
        }

        Debug.Log("ゴールの鎖をリセットしました");
    }
}