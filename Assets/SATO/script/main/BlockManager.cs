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

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip dragSE;
    public AudioClip dropSE;
    public AudioClip deleteSE;

    [Header("エフェクト設定")]
    public GameObject poofEffectPrefab; // 配置・入れ替え時の煙エフェクト

    private GameObject draggingBlock;
    private SpriteRenderer previewSR;
    private GameObject previewBlock;
    private int activeTypeIndex;
    private DynamicDropFrame activeFrame;

    // 現在プレビューで重なっている「消える予定のブロック」
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
            // ★修正：ドラッグ中に右クリックしたら、配置せずにキャンセル（破棄）する
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
            // UIの上でなければ掴む
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

    // 設置済みブロックを掴む機能
    void TryGrabPlacedBlock()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
            BlockInfo info = hit.GetComponent<BlockInfo>();
            if (info != null)
            {
                // 枠のスケールを戻す
                Collider2D frameHit = GetColliderAtPos(hit.transform.position, "DropFrame");
                if (frameHit != null) frameHit.GetComponent<DynamicDropFrame>()?.ResetScale();

                // 掴んだので、実体を消してドラッグを開始する
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

        // 個数制限に達していたら、一番古い同じブロックを消してワープさせる
        if (blockTypes[typeIndex].currentCount >= blockTypes[typeIndex].maxCount)
        {
            RemoveOldestBlockOfType(typeIndex);
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

    void RemoveOldestBlockOfType(int typeIndex)
    {
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null && info.typeIndex == typeIndex)
            {
                // 最初に見つかった（＝シーンにある）ものを消す
                InstantiatePoof(b.transform.position);
                PlaySE(deleteSE);

                // 枠をリセット
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

            // 入れ替え予告の演出
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");
            if (blockHit != null)
            {
                // 違うブロック、または同じ場所のブロックがある時
                previewSR.color = new Color(1f, 1f, 1f, 0.7f);

                // 重なっているブロックを半透明にする演出
                if (highlightedOldBlock != blockHit.gameObject)
                {
                    ResetOldBlockHighlight();
                    highlightedOldBlock = blockHit.gameObject;
                    var sr = highlightedOldBlock.GetComponent<SpriteRenderer>();
                    if (sr) sr.color = new Color(1f, 0.5f, 0.5f, 0.5f); // 赤っぽく半透明に
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

            // すでに何かあったら消去する
            if (blockHit != null)
            {
                DeleteBlockAt(blockHit.gameObject);
                InstantiatePoof(snapPos);
            }

            // 配置成功
            PlaySE(dropSE);
            draggingBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0);
            draggingBlock.GetComponent<Collider2D>().enabled = true;
            draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;
            draggingBlock.tag = "PlacedBlock";

            BlockInfo info = draggingBlock.AddComponent<BlockInfo>();
            info.typeIndex = activeTypeIndex;

            PlacedBlockMarker marker = draggingBlock.AddComponent<PlacedBlockMarker>();
            marker.SavePlacedState();

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

    // 特定のオブジェクトを指定して消去
    void DeleteBlockAt(GameObject target)
    {
        BlockInfo info = target.GetComponent<BlockInfo>();
        if (info != null)
        {
            blockTypes[info.typeIndex].currentCount--;
        }
        Destroy(target);
    }

    // マウス位置のブロックを右クリック削除
    void DeleteBlockAtMouse()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
            InstantiatePoof(hit.transform.position);
            PlaySE(deleteSE);

            // 枠のリセット
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
        // 1. カメラが存在しない、または無効な時は計算しない
        if (Camera.main == null || !Camera.main.enabled) return Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 2. 画面の範囲外にマウスがある時は計算しない
        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return Vector3.zero;
        }

        try
        {
            // 3. 奥行き(Z)を0ではなく、ニアクリップ面より少し先に設定する
            float safeZ = Camera.main.nearClipPlane + 0.001f;
            Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, safeZ);

            // 4. ビューポート変換（0〜1の範囲内か）を確認してエラーを未然に防ぐ
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
        // 配置済みブロックを「削除」するのではなく「初期化」する
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            // もしブロック自体にアニメーションや消滅処理があるなら、
            // そのスクリプトの OnGimmickReset() を呼ぶだけにする
            b.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }
        // currentCount はリセットしない（配置したままなので）
        RecalculateAllCounts();
    }

    public void RecalculateAllCounts()
    {
        // 一旦全てのカウントを0にする
        foreach (var type in blockTypes)
        {
            type.currentCount = 0;
        }

        // シーン内の PlacedBlock をすべて探し、typeIndex を元にカウントし直す
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks)
        {
            BlockInfo info = b.GetComponent<BlockInfo>();
            if (info != null && info.typeIndex < blockTypes.Count)
            {
                blockTypes[info.typeIndex].currentCount++;
            }
        }

        UpdateUI(); // UIに反映
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