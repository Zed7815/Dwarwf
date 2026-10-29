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

    [Header("エフェクト設定")]
    public GameObject poofEffectPrefab;

    private GameObject draggingBlock;
    private SpriteRenderer previewSR;
    private GameObject previewBlock;
    private int activeTypeIndex;
    private DynamicDropFrame activeFrame;

    private GameObject highlightedOldBlock;

    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        if (InGameMenuController.IsMenuOpen) return;

        if (draggingBlock == null && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 右クリック（削除・キャンセル）
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

        // 左クリック（掴む）
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

        // 左クリック離す（配置）
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

        RecalculateAllCounts();
    }

    // ★修正：ロックチェックを削除し、いつでも回収可能に
    void TryGrabPlacedBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit == null)
        {
            Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");
            if (frameHit != null)
            {
                hit = GetColliderAtPos(frameHit.transform.position, "PlacedBlock");
            }
        }

        if (hit != null)
        {
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

        activeTypeIndex = typeIndex;
        PlaySE(dragSE);

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        draggingBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        draggingBlock.GetComponent<Collider2D>().enabled = false;

        previewBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        previewSR = previewBlock.GetComponent<SpriteRenderer>();
        previewBlock.GetComponent<Collider2D>().enabled = false;
        previewBlock.transform.position += new Vector3(0, 0, 0.1f);

        previewBlock.tag = "Untagged";
        MonoBehaviour[] scripts = previewBlock.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = false;
        Animator anim = previewBlock.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;
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

    // ★修正：ロックチェックを削除し、いつでも上書き配置可能に
    void DropBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");

        if (frameHit != null)
        {
            Vector3 snapPos = frameHit.transform.position;
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            // すでにブロックがあるなら削除して上書き
            if (blockHit != null)
            {
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

            blockTypes[activeTypeIndex].currentCount++;
            draggingBlock = null;
            activeFrame = null;

            Physics2D.SyncTransforms();
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

    // ★修正：ロックチェックを削除し、いつでも右クリック削除可能に
    void DeleteBlockAtMouse()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit == null)
        {
            Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");
            if (frameHit != null)
            {
                hit = GetColliderAtPos(frameHit.transform.position, "PlacedBlock");
            }
        }

        if (hit != null)
        {
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
                type.individualCountText.text = "× " + type.currentCount;
            }
        }
    }

    public bool IsAllBlocksPlaced()
    {
        return true;
    }
}