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

    // ★追加：リフトの位置を記憶するディクショナリ
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
        if (currentActiveCheckpoint != null && currentActiveCheckpoint != newFlag)
        {
            currentActiveCheckpoint.SetVisualState(false);
        }

        currentActiveCheckpoint = newFlag;
        savedPlayerDirection = playerDirection;
        currentActiveCheckpoint.SetVisualState(true);

        CaptureSnapshot();

        Debug.Log($"<color=green>【旗セーブ】「{newFlag.gameObject.name}」通過！世界のセーブ完了</color>");
    }

    // ★旗を踏んだ瞬間：リフトの位置も丸ごと記録する！
    void CaptureSnapshot()
    {
        // 1. 星の記録
        snapshotCollectedItems.Clear();
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            Collider2D col = item.GetComponent<Collider2D>();
            if (!item.gameObject.activeSelf || (col != null && !col.enabled))
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

        // ★4. リフト（LiftBlock & SpiderLift）の現在座標を記録！
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

    // ★リセット時に呼ばれる：リフトの位置も旗を踏んだ瞬間の高さへ復元！
    public void RestoreSnapshot()
    {
        if (!HasActiveCheckpoint) return;

        // 1. 星の復元
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            bool wasCollectedBeforeFlag = snapshotCollectedItems.Contains(item.gameObject);
            if (wasCollectedBeforeFlag)
            {
                item.gameObject.SetActive(false);
            }
            else
            {
                item.OnGimmickReset();
            }
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.totalItemCount = snapshotStarCount;
            GameManager.instance.SendMessage("UpdateItemUI", SendMessageOptions.DontRequireReceiver);
        }

        // 2. 綿ブロックの復元
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            bool wasVanishedBeforeFlag = snapshotVanishedBlocks.Contains(vb);
            if (wasVanishedBeforeFlag)
            {
                vb.ForceVanishedState();
            }
            else
            {
                vb.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
            }
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

        // ★4. リフトの位置を、旗を踏んだ瞬間の高さへ復元！
        foreach (var pair in snapshotLiftPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.position = pair.Value;
                // 移動コルーチンを止め、物理速度をゼロにする
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

        Debug.Log($"<color=cyan>【「{currentActiveCheckpoint.gameObject.name}」の状態へリフトも含めて復元完了】</color>");
    }

    public Vector3 GetRespawnPosition()
    {
        return currentActiveCheckpoint != null ? currentActiveCheckpoint.GetRespawnPosition() : stageDefaultSpawnPos;
    }

    public int GetRespawnDirection()
    {
        return currentActiveCheckpoint != null ? savedPlayerDirection : stageDefaultDirection;
    }

    // 最初からやり直す（全リセット）用：リフトの位置データも初期化
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