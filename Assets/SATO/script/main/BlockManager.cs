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
        public string name;       // 種類名(管理用)
        public GameObject prefab; // プレハブ
        public int maxCount;      // 置ける最大数
        public int currentCount;  // 現在置いている数
        public TextMeshProUGUI individualCountText; // テキストUI

        [Header("拡張サイズ設定")]
        [Tooltip("この動物・ギミックブロックをドラッグした際、枠（DropFrame）をどれくらい広げるか（縦横の拡張倍率）")]
        public Vector3 targetFrameScale = new Vector3(1.5f, 1.5f, 1.0f); // 例えば1.5倍に拡大
    }

    public GameManager gameManager; // モード判定
    public List<BlockData> blockTypes; // インスペクターで種類を増やす

    private GameObject draggingBlock; // ドラッグしているブロック
    private SpriteRenderer previewSR; // 色
    private GameObject previewBlock; // グリッドの影
    private int activeTypeIndex; // 今ドラッグしているブロックの番号

    private List<GameObject> ghostBlocks = new List<GameObject>(); // ゴーストたちをおぼえるリスト

    // 現在ドラッグによってのDropFrameを一時保存する
    private DynamicDropFrame activeFrame;

    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            DeleteBlock();
        }

        // マウスの位置にブロックを追従
        if (draggingBlock != null)
        {
            UpdateDraggingPosition();
        }

        // 配置
        if (Mouse.current.leftButton.wasReleasedThisFrame && draggingBlock != null)
        {
            DropBlock();
        }
    }

    public void StartDragging(int typeIndex)
    {
        // モードチェック
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        // 個数制限チェック
        if (blockTypes[typeIndex].currentCount >= blockTypes[typeIndex].maxCount) return;

        activeTypeIndex = typeIndex; // 今の番号を保存

        // マウスの位置に新しくブロックを生成
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // マウスに追従する本体
        draggingBlock = Instantiate(blockTypes[typeIndex].prefab, mouseWorldPos, Quaternion.identity);
        draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;
        draggingBlock.GetComponent<Collider2D>().enabled = false;

        // グリッド予告
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

            // 枠を拡大させる制御 
            DynamicDropFrame hitFrame = frameHit.GetComponent<DynamicDropFrame>();
            if (hitFrame != null)
            {
                // 新しく枠に入った、または別の枠に乗り換えた場合
                if (activeFrame != hitFrame)
                {
                    // もし直前に他の枠に重なっていたら、それを一旦元に戻す
                    if (activeFrame != null)
                    {
                        activeFrame.ResetScale();
                    }

                    activeFrame = hitFrame;
                    // インスペクターで設定したその動物の拡張倍率（targetFrameScale）を渡す
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

            // ドラッグ中のマウスが枠から完全に外れたら、ぬるっと元の大きさに縮小
            if (activeFrame != null)
            {
                activeFrame.ResetScale();
                activeFrame = null;
            }
        }
    }

    // 最大値まで設置されているかを確認する
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

        // その場所にブロックがないかチェック
        Collider2D frameHit = GetColliderAtPos(mousePos, "DropFrame");

        if (frameHit != null)
        {
            Vector3 snapPos = frameHit.transform.position;
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            if (blockHit == null)
            {
                // 配置成功
                draggingBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0);
                draggingBlock.GetComponent<Collider2D>().enabled = true; // 当たり判定を戻す
                draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;

                draggingBlock.tag = "PlacedBlock";

                BlockInfo info = draggingBlock.AddComponent<BlockInfo>();
                info.typeIndex = activeTypeIndex;

                blockTypes[activeTypeIndex].currentCount++;
                draggingBlock = null;

                // 配置に成功した場合は、枠を拡大させたまま（Expand維持）に
                // したがって、ここではResetScaleは呼ばず、ドラッグ用の記憶(activeFrame)だけをクリア
                activeFrame = null;
            }
            else
            {
                Destroy(draggingBlock);
                draggingBlock = null;

                // ドロップ先が重複していて失敗した場合は、ぬるっと元の大きさに縮小
                if (activeFrame != null)
                {
                    activeFrame.ResetScale();
                    activeFrame = null;
                }
            }
        }
        else
        {
            Destroy(draggingBlock); // 枠以外で放したら消去
            draggingBlock = null;

            //  枠以外の場所でドラッグを離して消去された場合も、ぬるっと元の大きさに戻
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
        ClearGhostBlocks();

        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null)
            {
                CreateGhostBlock(b.transform.position, info.typeIndex);
            }
            Destroy(b);
        }

        foreach (var type in blockTypes)
        {
            type.currentCount = 0;
        }

        // 盤面を一括クリアした際は、シーン内全ての枠（DynamicDropFrame）を初期サイズにリセット
        DynamicDropFrame[] allFrames = FindObjectsOfType<DynamicDropFrame>();
        foreach (DynamicDropFrame frame in allFrames)
        {
            if (frame != null) frame.ResetScale();
        }
        activeFrame = null;

        UpdateUI();
    }

    void CreateGhostBlock(Vector3 pos, int typeIndex)
    {
        GameObject ghost = Instantiate(blockTypes[typeIndex].prefab, pos, Quaternion.identity);

        Collider2D col = ghost.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        ghost.tag = "Untagged";
        ghostBlocks.Add(ghost);

        StartCoroutine(FadeInGhost(ghost));
    }

    IEnumerator FadeInGhost(GameObject ghost)
    {
        SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        sr.sortingOrder = -1;

        float duration = 0.5f;
        float elapsed = 0f;

        Color targetColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        Color starColor = new Color(0.6f, 0.6f, 0.6f, 0f);

        while (elapsed < duration)
        {
            if (ghost == null) yield break;

            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(starColor, targetColor, elapsed / duration);
            yield return null;
        }
    }

    public void ClearGhostBlocks()
    {
        foreach (GameObject g in ghostBlocks)
        {
            if (g != null) Destroy(g);
        }
        ghostBlocks.Clear();
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
            Vector3 blockPos = hit.transform.position; //  削除されたブロックの位置を記憶

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