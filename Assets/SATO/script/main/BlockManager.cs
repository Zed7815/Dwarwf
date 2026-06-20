using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

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
    }

    public GameManager gameManager; // モード判定
    public List<BlockData> blockTypes; // インスペクターで種類を増やす

    private GameObject draggingBlock; // ドラックしているブロック
    private SpriteRenderer previewSR; // 色
    private GameObject previewBlock; // グリッドの影
    private int activeTypeIndex; // 今ドラッグしているブロックの番号

    private List<GameObject> ghostBlocks = new List<GameObject>(); // ゴーストたちをおぼえるリスト


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
        previewBlock = Instantiate(blockTypes[typeIndex].prefab,mouseWorldPos, Quaternion.identity);

        previewSR = previewBlock.GetComponent<SpriteRenderer>();
        previewBlock.GetComponent<Collider2D>().enabled=false;

        previewBlock.transform.position += new Vector3(0, 0, 0.1f);
    }

    void UpdateDraggingPosition()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        // マウスの位置に追従
        draggingBlock.transform.position = mousePos;

       // マウスの下にある枠を探す 
       Collider2D frameHit = GetColliderAtPos(mousePos,"DropFrame");


        // 重なりの判定で予告の色を変更
        if (frameHit != null)
        {
            previewBlock.SetActive(true); // 予告表示

            // 枠の真ん中の座標を取得して吸い付かせる
            Vector3 snapPos = frameHit.transform.position;
            previewBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0.1f);

            // ブロックがすでにあるかチェック
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            if (blockHit != null)
            {
                previewSR.color = new Color(1f, 0f, 0f, 0.5f); // すでにあるなら赤        
            }

            else
            {
                previewSR.color = new Color(0.5f, 1f, 0.5f, 0.5f); // 空いていれば緑
            }
        }

        else
        {
            previewBlock.SetActive(false);// 予告を隠す
        }
  
    }

    // 最大値まで設置されているかを確認する
    public bool IsAllBlocksPlaced()
    {
        foreach (var type in blockTypes)
        {
            // 一つでも最大数に達していない種類があれば
            if (type.currentCount < type.maxCount) return false;
        }
        return true;
    }

    void DropBlock()
    {
        // 1x1のグリッドにスナップさせる座標計算
        Vector3 mousePos = GetMouseWorldPosition();

        // その場所にブロックがないかチェック
        Collider2D frameHit = GetColliderAtPos(mousePos,"DropFrame");

        if(frameHit != null)
        {
            Vector3 snapPos = frameHit.transform.position;
            Collider2D blockHit = GetColliderAtPos(snapPos, "PlacedBlock");

            if (blockHit == null)
            {
                // 配置成功
                draggingBlock.transform.position = new Vector3(snapPos.x,snapPos.y,0);
                draggingBlock.GetComponent<Collider2D>().enabled = true; // 当たり判定を戻す
                draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;

                draggingBlock.tag = "PlacedBlock";

                BlockInfo info = draggingBlock.AddComponent<BlockInfo>();
                info.typeIndex = activeTypeIndex;

                // ドラッグ中の種類の個数を増やす
                blockTypes[activeTypeIndex].currentCount++;
                draggingBlock = null;
            }

            else
            {
                Destroy(draggingBlock);
                draggingBlock = null;
            }

        }

        else
        {
            Destroy(draggingBlock);// 枠以外で放したら消去
            draggingBlock= null;
        }

        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }

        UpdateUI();

    }

    // 特定のタグを持ったコライダーをその場所から探すための関数
    Collider2D GetColliderAtPos(Vector3 pos, string tag)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
        foreach(var hit in hits)
        {
            if (hit.CompareTag(tag)) return hit;
        }
        return null;
    }

    // マウスの座標をワールド座標に変換
    Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        return worldPos;
    }

    public void ResetAllBlocks()
    {
        // 古いゴールドを全部消去
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

        UpdateUI();
    }

    // ゴースト生成
    void CreateGhostBlock(Vector3 pos, int typeIndex)
    {
        // 本物と同じプレハブを生成
        GameObject ghost = Instantiate(blockTypes[typeIndex].prefab, pos, Quaternion.identity);

        // ゴーストが邪魔しないようにする
        Collider2D col = ghost.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        //タグの変更
        ghost.tag = "Untagged";

        // リストに入れる
        ghostBlocks.Add(ghost);

        StartCoroutine(FadeInGhost(ghost));
    }

    IEnumerator FadeInGhost(GameObject ghost)
    {
        SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        sr.sortingOrder = -1; // 本物より少し奥に

        float duration = 0.5f; // 0.5秒かけて現れる
        float elapsed = 0f;

        Color targetColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        Color starColor = new Color(0.6f, 0.6f, 0.6f, 0f); // 最初は透明

        while (elapsed < duration)
        {
            if (ghost == null) yield break ;

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

        // マウスの下にあるブロックを探す
        Collider2D hit = GetColliderAtPos(mousePos, "PlacedBlock");

        if (hit != null)
        {
             // そのブロックが持っている情報を読み取る
             BlockInfo info = hit.GetComponent<BlockInfo>();

            if (info != null)
            {
                // 対応する種類のカウントを1つ減らす
                blockTypes[info.typeIndex].currentCount--;
            }

            // オブジェクトを削除
            Destroy(hit.gameObject);
            
            // UI更新
            UpdateUI();
        }
    }
}
