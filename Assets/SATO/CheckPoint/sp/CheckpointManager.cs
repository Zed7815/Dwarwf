using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    [Header("チェックポイント状態")]
    public CheckpointFlag currentActiveCheckpoint = null;
    public int savedPlayerDirection = 1;

    // --- スナップショット記憶用 ---
    private List<GameObject> snapshotCollectedItems = new List<GameObject>(); // 取得済みの星
    private List<VanishingBlock> snapshotVanishedBlocks = new List<VanishingBlock>(); // 消えていた足場
    private List<int> snapshotCollectedKeyIDs = new List<int>(); // 取得済みの鍵ID
    private int snapshotStarCount = 0; // その時の星の数

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

    // 旗を踏んだ瞬間：スナップショットを記録
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

        Debug.Log("<color=green>【チェックポイント＆世界のセーブ完了】</color>");
    }

    void CaptureSnapshot()
    {
        // 1. 星（Item）の記録
        snapshotCollectedItems.Clear();
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            if (!item.gameObject.activeSelf)
            {
                snapshotCollectedItems.Add(item.gameObject);
            }
        }
        if (GameManager.instance != null)
        {
            snapshotStarCount = GameManager.instance.totalItemCount;
        }

        // 2. 消える足場（VanishingBlock）の記録
        snapshotVanishedBlocks.Clear();
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            Collider2D col = vb.GetComponent<Collider2D>();
            if (col != null && !col.enabled)
            {
                snapshotVanishedBlocks.Add(vb);
            }
        }

        // 3. 鍵（KeyItem）の記録
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

    // リセット時に呼ばれる：スナップショットから復元
    public void RestoreSnapshot()
    {
        if (!HasActiveCheckpoint) return;

        // 1. 星の復元
        Item[] allItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            bool wasCollectedBeforeFlag = snapshotCollectedItems.Contains(item.gameObject);
            item.gameObject.SetActive(!wasCollectedBeforeFlag);
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.totalItemCount = snapshotStarCount;
            // 非公開メソッドでも安全に呼べるSendMessageを使用
            GameManager.instance.SendMessage("UpdateItemUI", SendMessageOptions.DontRequireReceiver);
        }

        // 2. 消える足場の復元（専用メソッド不要で直接コンポーネントを制御）
        VanishingBlock[] allVBlocks = FindObjectsByType<VanishingBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var vb in allVBlocks)
        {
            bool wasVanishedBeforeFlag = snapshotVanishedBlocks.Contains(vb);

            vb.StopAllCoroutines();
            var sr = vb.GetComponent<SpriteRenderer>();
            var col = vb.GetComponent<Collider2D>();
            if (sr != null) sr.enabled = !wasVanishedBeforeFlag;
            if (col != null) col.enabled = !wasVanishedBeforeFlag;

            // 復活する場合はアニメーションを巻き戻す
            if (!wasVanishedBeforeFlag)
            {
                var anim = vb.GetComponent<Animator>();
                if (anim != null) { anim.Rebind(); anim.Update(0f); }
            }
        }

        // 3. 鍵の復元
        if (FinalKeyManager.instance != null)
        {
            // いったん鍵マネージャーを初期化
            FinalKeyManager.instance.OnGimmickReset();

            // 旗を踏む前に取っていた鍵だけ、正式に「取得」し直す
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

        Debug.Log("<color=cyan>【チェックポイントの状態へ世界を復元しました】</color>");
    }

    public Vector3 GetRespawnPosition()
    {
        return currentActiveCheckpoint != null ? currentActiveCheckpoint.GetRespawnPosition() : stageDefaultSpawnPos;
    }

    public int GetRespawnDirection()
    {
        return currentActiveCheckpoint != null ? savedPlayerDirection : stageDefaultDirection;
    }

    // 最初からやり直す（全リセット）用
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