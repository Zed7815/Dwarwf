using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BlockManager : MonoBehaviour
{
    [System.Serializable]
    public class BlockData
    {
        public string name;
        public GameObject prefab;
        public int maxCount;
        public int currentCount;
        public TextMeshProUGUI individualCountText;

        [Header("拡張サイズ設定")]
        public Vector3 targetFrameScale = new Vector3(1.5f, 1.5f, 1.0f);
    }

    public GameManager gameManager;
    public List<BlockData> blockTypes;

    public bool IsDragging => draggingBlock != null;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip dragSE;
    public AudioClip dropSE;
    public AudioClip deleteSE;
    public AudioClip startDeniedSE;
    public AudioClip lockDeniedSE;

    [Header("エフェクト設定")]
    public GameObject poofEffectPrefab;

    [Header("南京錠プレハブ（任意）")]
    public GameObject padlockPrefab;

    [Tooltip("動物の中心から南京錠をどれくらいずらして配置するか（Yを増やすと高くなります）")]
    public Vector3 padlockOffset = new Vector3(0, 0.8f, 0);

    private GameObject draggingBlock;
    private SpriteRenderer previewSR;
    private GameObject previewBlock;
    private int activeTypeIndex;
    private DynamicDropFrame activeFrame;

    private GameObject highlightedOldBlock;

    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        if (draggingBlock == null && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 右クリック
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (draggingBlock != null)
            {
                CancelDragging();
            }
            else
            {
                DeleteBlockAtMouse();
            }
        }

        // 左クリック押し込み
        if (Mouse.current.leftButton.wasPressedThisFrame && draggingBlock == null)
        {
            if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
            {
                TryGrabPlacedBlock();
            }
        }

        if (draggingBlock != null)
        {
            UpdateDraggingPosition();
        }

        // 左クリックを離して配置
        if (Mouse.current.leftButton.wasReleasedThisFrame && draggingBlock != null)
        {
            DropBlock();
        }
    }

    public void ClearAllPlacedBlocks()
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            b.tag = "Untagged";
            b.SetActive(false);
            Destroy(b);
        }

        foreach (var type in blockTypes)
        {
            type.currentCount = 0;
        }

        DynamicDropFrame[] allFrames = FindObjectsByType<DynamicDropFrame>(FindObjectsSortMode.None);
        foreach (DynamicDropFrame frame in allFrames)
        {
            if (frame != null) frame.ResetScale();
        }

        activeFrame = null;
        UpdateUI();

        Debug.Log("<color=yellow>【完全回収】タグを即時剥奪し、配置数を0個にリセットしました！</color>");
    }

    void CancelDragging()
    {
        if (draggingBlock != null) Destroy(draggingBlock);
        if (previewBlock != null) Destroy(previewBlock);

        if (activeFrame != null) activeFrame.ResetScale();
        ResetOldBlockHighlight();

        draggingBlock = null;
        previewBlock = null;
        activeFrame = null;
    }

    void TryGrabPlacedBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
            PlacedBlockMarker marker = hit.GetComponent<PlacedBlockMarker>();
            if (marker != null && marker.isLocked)
            {
                marker.PlayLockDeniedEffect();
                PlaySE(lockDeniedSE != null ? lockDeniedSE : startDeniedSE);

                // 初めてロックされた動物を触った時
                bool hasSeenLockTutorial = PlayerPrefs.GetInt("Tutorial_Seen_Lock", 0) == 1;
                if (!hasSeenLockTutorial)
                {
                    PlayerPrefs.SetInt("Tutorial_Seen_Lock", 1);
                    PlayerPrefs.Save();

                    // ★【重要】ゲームを一時停止して連打を防ぐ！
                    Time.timeScale = 0f;

                    if (TutorialMessageUI.instance != null)
                    {
                        TutorialMessageUI.instance.ShowMessage("チェックポイントで使った動物は\n固定されてもう動かせないよ！\n右上の本からやり直しが可能！\n(クリックで再開)");
                    }
                }

                return;
            }

            BlockInfo info = hit.GetComponent<BlockInfo>();
            if (info != null)
            {
                Collider2D frameHit = GetColliderAtPos(hit.transform.position, "DropFrame");
                if (frameHit != null) frameHit.GetComponent<DynamicDropFrame>()?.ResetScale();

                int index = info.typeIndex;
                blockTypes[index].currentCount--;
                Destroy(hit.gameObject);
                StartDragging(index);
            }
        }
    }

    public void StartDragging(int typeIndex)
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        if (blockTypes[typeIndex].currentCount >= blockTypes[typeIndex].maxCount)
        {
           
            bool removed = RemoveOldestUnlockedBlockOfType(typeIndex);

            // もしすべてロックされていて、消せるブロックが1つもないなら配置を拒否！
            if (!removed)
            {
                PlaySE(lockDeniedSE != null ? lockDeniedSE : startDeniedSE);
                Debug.Log("<color=yellow>この動物はすべてチェックポイントで固定されているため、これ以上配置できません！</color>");
                return; // ドラッグを開始させない！
            }
        }

        activeTypeIndex = typeIndex;
        PlaySE(dragSE);

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        draggingBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        draggingBlock.GetComponent<Collider2D>().enabled = false;

        previewBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        previewSR = previewBlock.GetComponent<SpriteRenderer>();
        previewBlock.GetComponent<Collider2D>().enabled = false;
        previewBlock.transform.position += new Vector3(0, 0, 0.1f);
    }

    bool RemoveOldestUnlockedBlockOfType(int typeIndex)
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null && info.typeIndex == typeIndex)
            {
                // ★最重要：ロックされているブロックなら絶対に消さない！スキップ！
                PlacedBlockMarker marker = b.GetComponent<PlacedBlockMarker>();
                if (marker != null && marker.isLocked)
                {
                    continue; // 次のブロックを探す
                }

                // ロックされていないブロックを見つけたら、それを消去して枠を空ける
                InstantiatePoof(b.transform.position);
                PlaySE(deleteSE);

                Collider2D frameHit = GetColliderAtPos(b.transform.position, "DropFrame");
                if (frameHit != null) frameHit.GetComponent<DynamicDropFrame>()?.ResetScale();

                b.tag = "Untagged"; // 即座にタグを外す
                b.SetActive(false);
                Destroy(b);
                blockTypes[typeIndex].currentCount--;
                return true; // 1つ消去成功
            }
        }

        // 消せるブロック（未ロックのもの）が1つも無かった
        return false;
    }

    void RemoveOldestBlockOfType(int typeIndex)
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null && info.typeIndex == typeIndex)
            {
                InstantiatePoof(b.transform.position);
                PlaySE(deleteSE);

                Collider2D frameHit = GetColliderAtPos(b.transform.position, "DropFrame");
                if (frameHit != null) frameHit.GetComponent<DynamicDropFrame>()?.ResetScale();

                Destroy(b);
                blockTypes[typeIndex].currentCount--;
                break;
            }
        }
    }

    void UpdateDraggingPosition()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        draggingBlock.transform.position = mousePos;

        Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");

        if (frameHit != null)
        {
            previewBlock.SetActive(true);
            Vector3 snapPos = frameHit.transform.position;
            previewBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0.1f);

            DynamicDropFrame hitFrame = frameHit.GetComponent<DynamicDropFrame>();
            if (hitFrame != null && activeFrame != hitFrame)
            {
                if (activeFrame != null) activeFrame.ResetScale();
                activeFrame = hitFrame;
                activeFrame.Expand(blockTypes[activeTypeIndex].targetFrameScale);
            }

            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");
            if (blockHit != null)
            {
                previewSR.color = new Color(1f, 1f, 1f, 0.7f);

                if (highlightedOldBlock != blockHit.gameObject)
                {
                    ResetOldBlockHighlight();
                    highlightedOldBlock = blockHit.gameObject;
                    var sr = highlightedOldBlock.GetComponent<SpriteRenderer>();
                    if (sr) sr.color = new Color(1f, 0.5f, 0.5f, 0.5f);
                }
            }
            else
            {
                previewSR.color = new Color(0.5f, 1f, 0.5f, 0.5f);
                ResetOldBlockHighlight();
            }
        }
        else
        {
            previewBlock.SetActive(false);
            ResetOldBlockHighlight();
            if (activeFrame != null)
            {
                activeFrame.ResetScale();
                activeFrame = null;
            }
        }
    }

    void ResetOldBlockHighlight()
    {
        if (highlightedOldBlock != null)
        {
            var sr = highlightedOldBlock.GetComponent<SpriteRenderer>();
            if (sr) sr.color = Color.white;
            highlightedOldBlock = null;
        }
    }

    void DropBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");

        if (frameHit != null)
        {
            Vector3 snapPos = frameHit.transform.position;
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            // すでに置いてあるブロックがロックされていたら上書き不可
            if (blockHit != null)
            {
                PlacedBlockMarker existingMarker = blockHit.GetComponent<PlacedBlockMarker>();
                if (existingMarker != null && existingMarker.isLocked)
                {
                    existingMarker.PlayLockDeniedEffect();
                    PlaySE(lockDeniedSE != null ? lockDeniedSE : startDeniedSE);

                    Destroy(draggingBlock);
                    draggingBlock = null;
                    if (previewBlock != null) Destroy(previewBlock);
                    if (activeFrame != null) { activeFrame.ResetScale(); activeFrame = null; }
                    ResetOldBlockHighlight();
                    return;
                }

                DeleteBlockAt(blockHit.gameObject);
                InstantiatePoof(snapPos);
            }

            PlaySE(dropSE);
            draggingBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0);
            draggingBlock.GetComponent<Collider2D>().enabled = true;
            draggingBlock.tag = "PlacedBlock";

            BlockInfo info = draggingBlock.AddComponent<BlockInfo>();
            info.typeIndex = activeTypeIndex;

            PlacedBlockMarker marker = draggingBlock.AddComponent<PlacedBlockMarker>();
            marker.SavePlacedState();

            SetupLockIconForMarker(marker);

            blockTypes[activeTypeIndex].currentCount++;
            draggingBlock = null;
            activeFrame = null;
        }
        else
        {
            Destroy(draggingBlock);
            draggingBlock = null;
            if (activeFrame != null) { activeFrame.ResetScale(); activeFrame = null; }
        }

        if (previewBlock != null) Destroy(previewBlock);
        ResetOldBlockHighlight();
        UpdateUI();
    }

    void DeleteBlockAt(GameObject target)
    {
        BlockInfo info = target.GetComponent<BlockInfo>();
        if (info != null)
        {
            blockTypes[info.typeIndex].currentCount--;
        }
        Destroy(target);
    }

    void DeleteBlockAtMouse()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
            // ロックされているブロックは削除不可
            PlacedBlockMarker marker = hit.GetComponent<PlacedBlockMarker>();
              if (marker != null && marker.isLocked)
            {
                marker.PlayLockDeniedEffect();
                PlaySE(lockDeniedSE != null ? lockDeniedSE : startDeniedSE);

                bool hasSeenLockTutorial = PlayerPrefs.GetInt("Tutorial_Seen_Lock", 0) == 1;
                if (!hasSeenLockTutorial)
                {
                    PlayerPrefs.SetInt("Tutorial_Seen_Lock", 1);
                    PlayerPrefs.Save();

                    // ★ゲームを一時停止
                    Time.timeScale = 0f; 

                    if (TutorialMessageUI.instance != null)
                    {
                        TutorialMessageUI.instance.ShowMessage("チェックポイントで使った動物は\n固定されてもう動かせないよ！\n右上の本からやり直しが可能！\n(クリックで再開)");
                    }
                }

                return;
            }

            InstantiatePoof(hit.transform.position);
            PlaySE(deleteSE);

            Collider2D frameHit = GetColliderAtPos(hit.transform.position, "DropFrame");
            if (frameHit != null) frameHit.GetComponent<DynamicDropFrame>()?.ResetScale();

            DeleteBlockAt(hit.gameObject);
            UpdateUI();
        }
    }

    void InstantiatePoof(Vector3 pos)
    {
        if (poofEffectPrefab != null) Instantiate(poofEffectPrefab, pos, Quaternion.identity);
    }

    void PlaySE(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    Collider2D GetColliderAtPos(Vector3 pos, string tag)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
        foreach (var hit in hits) { if (hit.CompareTag(tag)) return hit; }
        return null;
    }

    void SetupLockIconForMarker(PlacedBlockMarker marker)
    {
        if (padlockPrefab != null)
        {
            GameObject lockObj = Instantiate(padlockPrefab, marker.transform);

            lockObj.transform.localPosition = padlockOffset;

            lockObj.SetActive(false);
            marker.lockIconObject = lockObj;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null || !Camera.main.enabled) return Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return Vector3.zero;
        }

        try
        {
            float safeZ = Camera.main.nearClipPlane + 0.001f;
            Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, safeZ);

            Vector3 viewportPos = Camera.main.ScreenToViewportPoint(screenPos);
            if (viewportPos.x < -0.05f || viewportPos.x > 1.05f || viewportPos.y < -0.05f || viewportPos.y > 1.05f)
            {
                return Vector3.zero;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }
        catch (System.Exception)
        {
            return Vector3.zero;
        }
    }

    public void ResetAllBlocks()
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            b.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }
        RecalculateAllCounts();
    }

    public void RecalculateAllCounts()
    {
        foreach (var type in blockTypes)
        {
            type.currentCount = 0;
        }

        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null && info.typeIndex < blockTypes.Count)
            {
                blockTypes[info.typeIndex].currentCount++;
            }
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        foreach (var type in blockTypes)
        {
            if (type.individualCountText != null)
            {
                int remaining = type.maxCount - type.currentCount;
                type.individualCountText.text = remaining + " / " + type.maxCount;
            }
        }
    }

    public bool IsAllBlocksPlaced()
    {
        foreach (var type in blockTypes) { if (type.currentCount < type.maxCount) return false; }
        return true;
    }
}