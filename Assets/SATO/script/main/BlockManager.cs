using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターでAudioSourceを割り当て
    public AudioClip dragSE;   // ドラッグ開始時の音
    public AudioClip dropSE;   // 配置（ドロップ）成功時の音
    public AudioClip deleteSE; // 削除時の音

    private GameObject draggingBlock;
    private SpriteRenderer previewSR;
    private GameObject previewBlock;
    private int activeTypeIndex;

    private DynamicDropFrame activeFrame;

    void Start()
    {
        // もしAudioSourceが未設定なら自分自身から取得を試みる
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            DeleteBlock();
        }

        if (draggingBlock != null)
        {
            UpdateDraggingPosition();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && draggingBlock != null)
        {
            DropBlock();
        }
    }

    public void StartDragging(int typeIndex)
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;
        if (blockTypes[typeIndex].currentCount >= blockTypes[typeIndex].maxCount) return;

        activeTypeIndex = typeIndex;

        // ★SE再生: ドラッグ開始
        PlaySE(dragSE);

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        draggingBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;
        draggingBlock.GetComponent<Collider2D>().enabled = false;

        previewBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);

        previewSR = previewBlock.GetComponent<SpriteRenderer>();
        previewBlock.GetComponent<Collider2D>().enabled = false;

        previewBlock.transform.position += new Vector3(0, 0, 0.1f);
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
            if (hitFrame != null)
            {
                if (activeFrame != hitFrame)
                {
                    if (activeFrame != null)
                    {
                        activeFrame.ResetScale();
                    }

                    activeFrame = hitFrame;
                    activeFrame.Expand(blockTypes[activeTypeIndex].targetFrameScale);
                }
            }

            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");
            if (blockHit != null)
            {
                previewSR.color = new Color(1f, 0f, 0f, 0.5f);
            }
            else
            {
                previewSR.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            }
        }
        else
        {
            previewBlock.SetActive(false);

            if (activeFrame != null)
            {
                activeFrame.ResetScale();
                activeFrame = null;
            }
        }
    }

    public bool IsAllBlocksPlaced()
    {
        foreach (var type in blockTypes)
        {
            if (type.currentCount < type.maxCount) return false;
        }
        return true;
    }

    void DropBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");

        if (frameHit != null)
        {
            Vector3 snapPos = frameHit.transform.position;
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            if (blockHit == null)
            {
                // ★SE再生: 配置成功
                PlaySE(dropSE);

                draggingBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0);
                draggingBlock.GetComponent<Collider2D>().enabled = true;
                draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;

                draggingBlock.tag = "PlacedBlock";

                BlockInfo info = draggingBlock.AddComponent<BlockInfo>();
                info.typeIndex = activeTypeIndex;

                blockTypes[activeTypeIndex].currentCount++;
                draggingBlock = null;
                activeFrame = null;
            }
            else
            {
                Destroy(draggingBlock);
                draggingBlock = null;

                if (activeFrame != null)
                {
                    activeFrame.ResetScale();
                    activeFrame = null;
                }
            }
        }
        else
        {
            Destroy(draggingBlock);
            draggingBlock = null;

            if (activeFrame != null)
            {
                activeFrame.ResetScale();
                activeFrame = null;
            }
        }

        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }

        UpdateUI();
    }

    // 共通の音再生メソッド
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    Collider2D GetColliderAtPos(Vector3 pos, string tag)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(tag)) return hit;
        }
        return null;
    }

    Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Camera.main is null");
            return Vector3.zero;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return draggingBlock != null ? draggingBlock.transform.position : Vector3.zero;
        }

        try
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            worldPos.z = 0;
            return worldPos;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ScreenToWorldPoint error: {e.Message}");
            return Vector3.zero;
        }
    }

    public void ResetAllBlocks()
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            Destroy(b);
        }

        foreach (var type in blockTypes)
        {
            type.currentCount = 0;
        }

        DynamicDropFrame[] allFrames = FindObjectsOfType<DynamicDropFrame>();
        foreach (DynamicDropFrame frame in allFrames)
        {
            if (frame != null) frame.ResetScale();
        }
        activeFrame = null;

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

    void DeleteBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
            // ★SE再生: 削除
            PlaySE(deleteSE);

            Vector3 blockPos = hit.transform.position;

            BlockInfo info = hit.GetComponent<BlockInfo>();
            if (info != null)
            {
                blockTypes[info.typeIndex].currentCount--;
            }
            Destroy(hit.gameObject);

            Collider2D frameHit = GetColliderAtPos(blockPos, "DropFrame");
            if (frameHit != null)
            {
                DynamicDropFrame frame = frameHit.GetComponent<DynamicDropFrame>();
                if (frame != null)
                {
                    frame.ResetScale();
                }
            }

            UpdateUI();
        }
    }
}