using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    [Header("チェックポイント状態")]
    public CheckpointFlag currentActiveCheckpoint = null;
    public int savedPlayerDirection = 1; // 1 = 右, -1 = 左

    private Vector3 stageDefaultSpawnPos;
    private int stageDefaultDirection = 1;

    public bool HasActiveCheckpoint => currentActiveCheckpoint != null;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            stageDefaultSpawnPos = pc.transform.position;
            if (pc.playerWalk != null) stageDefaultDirection = pc.playerWalk.direction;
        }
    }

    // 旗を踏んだ瞬間の登録処理
    public void ActivateCheckpoint(CheckpointFlag newFlag, int playerDirection)
    {
        // 前の旗を未通過色に戻す
        if (currentActiveCheckpoint != null && currentActiveCheckpoint != newFlag)
        {
            currentActiveCheckpoint.SetVisualState(false);
        }

        // 新しい旗を記憶
        currentActiveCheckpoint = newFlag;
        savedPlayerDirection = playerDirection;
        currentActiveCheckpoint.SetVisualState(true);

        Debug.Log($"<color=green>【チェックポイント通過】位置: {newFlag.transform.position}, 向き: {(playerDirection > 0 ? "右" : "左")}</color>");
    }

    // 復活する座標を取得
    public Vector3 GetRespawnPosition()
    {
        if (currentActiveCheckpoint != null)
        {
            return currentActiveCheckpoint.GetRespawnPosition();
        }
        return stageDefaultSpawnPos;
    }

    // 復活時の向きを取得
    public int GetRespawnDirection()
    {
        if (currentActiveCheckpoint != null)
        {
            return savedPlayerDirection;
        }
        return stageDefaultDirection;
    }

    // 全リセット（最初からやり直す）用
    public void ClearCheckpoint()
    {
        if (currentActiveCheckpoint != null)
        {
            currentActiveCheckpoint.SetVisualState(false);
            currentActiveCheckpoint = null;
        }
        savedPlayerDirection = stageDefaultDirection;
    }
}