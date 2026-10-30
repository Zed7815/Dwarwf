using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    [Header("チェックポイント状態")]
    public CheckpointFlag currentActiveCheckpoint = null;
    public int savedPlayerDirection = 1;

    // --- スナップショット記憶用 ---
    private List<GameObject> snapshotCollectedItems = new List<GameObject>();
    private List<VanishingBlock> snapshotVanishedBlocks = new List<VanishingBlock>();
    private List<int> snapshotCollectedKeyIDs = new List<int>();
    private int snapshotStarCount = 0;

    private Dictionary<LiftBlock, Vector3> snapshotLiftPositions = new Dictionary<LiftBlock, Vector3>();
    private Dictionary<SpiderLift, Vector3> snapshotSpiderLiftPositions = new Dictionary<SpiderLift, Vector3>();

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

    public void ActivateCheckpoint(CheckpointFlag newFlag, int playerDirection)
    {
        // 別の旗に切り替わった場合、古い旗をグレーに戻す
        if (currentActiveCheckpoint != null && currentActiveCheckpoint != newFlag)
        {
            currentActiveCheckpoint.SetVisualState(false);
        }

        bool isNewFlag = (currentActiveCheckpoint != newFlag);
        currentActiveCheckpoint = newFlag;
        savedPlayerDirection = playerDirection;
        currentActiveCheckpoint.SetVisualState(true);

        // 初めて旗に触れた時だけチュートリアル表示
        if (isNewFlag)
        {
            bool hasSeenCheckTutorial = PlayerPrefs.GetInt("Tutorial_Seen_Checkpoint", 0) == 1;
            if (!hasSeenCheckTutorial)
            {
                PlayerPrefs.SetInt("Tutorial_Seen_Checkpoint", 1);
                PlayerPrefs.Save();

                Time.timeScale = 0f;
                if (TutorialMessageUI.instance != null)
                {
                    TutorialMessageUI.instance.ShowMessage("チェックポイント通過！\nリセットしても、ここからやり直せるよ！\n(クリックで再開)");
                }
            }
        }

        // ★通過するたびに、星や世界の状態を最新にスナップショット保存！
        CaptureSnapshot();

        Debug.Log($"<color=green>【旗セーブ】「{newFlag.gameObject.name}」通過！ 星の所持数({snapshotStarCount}個)を記録完了</color>");
    }

    void CaptureSnapshot()
    {
        // 1. 星の確実な記録（isCollectedフラグで直接判定）
        snapshotCollectedItems.Clear();
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            Collider2D col = item.GetComponent<Collider2D>();
            // isCollectedがTrue、または非アクティブ、またはコライダーOFFなら「獲得済み」と確定
            if (item.isCollected || !item.gameObject.activeSelf || (col != null && !col.enabled))
            {
                snapshotCollectedItems.Add(item.gameObject);
            }
        }

        if (GameManager.instance != null)
        {
            snapshotStarCount = GameManager.instance.totalItemCount;
        }

        // 2. 綿ブロックの記録
        snapshotVanishedBlocks.Clear();
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            Collider2D col = vb.GetComponent<Collider2D>();
            if (vb.isTouched || (col != null && !col.enabled))
            {
                snapshotVanishedBlocks.Add(vb);
            }
        }

        // 3. 鍵の記録
        snapshotCollectedKeyIDs.Clear();
        KeyItem[] allKeys = FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var key in allKeys)
        {
            Collider2D col = key.GetComponent<Collider2D>();
            if (col != null && !col.enabled)
            {
                snapshotCollectedKeyIDs.Add(key.keyID);
            }
        }

        // 4. リフトの記録
        snapshotLiftPositions.Clear();
        LiftBlock[] allLifts = FindObjectsByType<LiftBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var lift in allLifts)
        {
            snapshotLiftPositions[lift] = lift.transform.position;
        }

        snapshotSpiderLiftPositions.Clear();
        SpiderLift[] allSpiderLifts = FindObjectsByType<SpiderLift>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sl in allSpiderLifts)
        {
            snapshotSpiderLiftPositions[sl] = sl.transform.position;
        }
    }

    public void RestoreSnapshot()
    {
        if (!HasActiveCheckpoint) return;

        // 1. 星の確実な復元
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            bool wasCollectedBeforeFlag = snapshotCollectedItems.Contains(item.gameObject);
            if (wasCollectedBeforeFlag)
            {
                // 旗の手前で取った星は、消えた状態を強制維持
                item.ForceCollectedState();
            }
            else
            {
                item.OnGimmickReset();
            }
        }

        // ★GameManagerの星所持フラグとUIを直接確実に復元！
        if (GameManager.instance != null)
        {
            GameManager.instance.totalItemCount = snapshotStarCount;

            if (snapshotStarCount > 0)
            {
                GameManager.instance.hasCollectedStarInThisRun = true;
            }

            // 直接UIを更新（SendMessageを使わず確実に実行）
            GameManager.instance.UpdateItemUI();
        }

        // 2. 綿ブロックの復元
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            bool wasVanishedBeforeFlag = snapshotVanishedBlocks.Contains(vb);
            if (wasVanishedBeforeFlag) vb.ForceVanishedState();
            else vb.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }

        // 3. 鍵の復元
        if (FinalKeyManager.instance != null)
        {
            FinalKeyManager.instance.OnGimmickReset();
            foreach (int keyId in snapshotCollectedKeyIDs)
            {
                FinalKeyManager.instance.CollectKey(keyId, Vector3.zero);
            }
        }

        KeyItem[] allKeys = FindObjectsByType<KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var key in allKeys)
        {
            bool wasGotBeforeFlag = snapshotCollectedKeyIDs.Contains(key.keyID);
            var sr = key.GetComponent<SpriteRenderer>();
            var col = key.GetComponent<Collider2D>();
            if (sr != null) sr.enabled = !wasGotBeforeFlag;
            if (col != null) col.enabled = !wasGotBeforeFlag;
        }

        // 4. リフトの復元
        foreach (var pair in snapshotLiftPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.position = pair.Value;
                pair.Key.StopAllCoroutines();
                var rb = pair.Key.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        foreach (var pair in snapshotSpiderLiftPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.position = pair.Value;
                pair.Key.StopAllCoroutines();
            }
        }

        Debug.Log($"<color=cyan>【チェックポイント復元】星の所持数({snapshotStarCount}個)を完全に復元しました！</color>");
    }

    public Vector3 GetRespawnPosition()
    {
        return currentActiveCheckpoint != null ? currentActiveCheckpoint.GetRespawnPosition() : stageDefaultSpawnPos;
    }

    public int GetRespawnDirection()
    {
        return currentActiveCheckpoint != null ? savedPlayerDirection : stageDefaultDirection;
    }

    public void ClearCheckpoint()
    {
        if (currentActiveCheckpoint != null)
        {
            currentActiveCheckpoint.SetVisualState(false);
            currentActiveCheckpoint = null;
        }
        savedPlayerDirection = stageDefaultDirection;
        snapshotCollectedItems.Clear();
        snapshotVanishedBlocks.Clear();
        snapshotCollectedKeyIDs.Clear();
        snapshotLiftPositions.Clear();
        snapshotSpiderLiftPositions.Clear();
        snapshotStarCount = 0;
    }
}