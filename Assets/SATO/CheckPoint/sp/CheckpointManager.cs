using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    [Header("チェックポイント状態")]
    public CheckpointFlag currentActiveCheckpoint = null;
    public int savedPlayerDirection = 1;

    // スナップショット記憶用
    private List<GameObject> snapshotCollectedItems = new List<GameObject>();
    private List<VanishingBlock> snapshotVanishedBlocks = new List<VanishingBlock>();
    private List<int> snapshotCollectedKeyIDs = new List<int>();
    private int snapshotStarCount = 0;

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

        Debug.Log($"<color=green>【旗セーブ】「{newFlag.gameObject.name}」を通過！世界を記録しました</color>");
    }

    void CaptureSnapshot()
    {
        // 1. 星の記録
        snapshotCollectedItems.Clear();
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            Collider2D col = item.GetComponent<Collider2D>();
            // 拾われた（非表示、またはコライダーが切れた）星を記録
            if (!item.gameObject.activeSelf || (col != null && !col.enabled))
            {
                snapshotCollectedItems.Add(item.gameObject);
            }
        }
        if (GameManager.instance != null)
        {
            snapshotStarCount = GameManager.instance.totalItemCount;
        }

        // 2. 綿ブロックの記録（★ここを改善！）
        snapshotVanishedBlocks.Clear();
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            Collider2D col = vb.GetComponent<Collider2D>();
            // 完全に消えているもの ＋ 「現在消滅カウントダウン中（isTouched=true）」のものも即座に記録！
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
    }

    public void RestoreSnapshot()
    {
        if (!HasActiveCheckpoint) return;

        // ★1. 星の復元（完全リフレッシュ）
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            bool wasCollectedBeforeFlag = snapshotCollectedItems.Contains(item.gameObject);

            if (wasCollectedBeforeFlag)
            {
                // 旗の前に取っていた星 ➔ 取ったまま（消す）
                item.gameObject.SetActive(false);
            }
            else
            {
                // 旗の後に取った星 ➔ 新品状態（位置・透明度・判定）に完全復活！
                item.OnGimmickReset();
            }
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.totalItemCount = snapshotStarCount;
            GameManager.instance.SendMessage("UpdateItemUI", SendMessageOptions.DontRequireReceiver);
        }

        // 2. 綿ブロックの復元（★ここを完全同期！）
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            bool wasVanishedBeforeFlag = snapshotVanishedBlocks.Contains(vb);

            if (wasVanishedBeforeFlag)
            {
                // 旗の前に壊れていたもの ➔ 壊れたままにする！
                vb.ForceVanishedState();
            }
            else
            {
                // 旗の後に壊れたもの ➔ 完全復活させる！
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

        Debug.Log($"<color=cyan>【「{currentActiveCheckpoint.gameObject.name}」の状態へタイムライン復元完了】</color>");
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
        snapshotStarCount = 0;
    }
}