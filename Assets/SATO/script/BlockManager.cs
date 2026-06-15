using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;

public class BlockManager : MonoBehaviour
{
    public GameManager gameManager; // モード判定
    public GameObject blockPrefab; // 設置したいブロックのプレハブ

    public int maxBlockCount = 5; // 最大個数
    private int currentBlockCount = 0; // 配置している個数
    public TextMeshProUGUI countText; // 残り個数を表示するUI

    private GameObject draggingBlock; // ドラックしているブロック
    private SpriteRenderer previewSR; // 色
    private GameObject previewBlock; // グリッドの影


    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        // ドラッグ開始
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                StartDragging();
            }
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


    void StartDragging()
    {
        // 個数制限チェック
        if (currentBlockCount >= maxBlockCount) return;

        // マウスの位置に新しくブロックを生成
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // マウスに追従する本体
        draggingBlock = Instantiate(blockPrefab, mouseWorldPos, Quaternion.identity);
        draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;
        draggingBlock.GetComponent<Collider2D>().enabled = false;

        // グリッド予告
        previewBlock = Instantiate(blockPrefab,mouseWorldPos, Quaternion.identity);

        previewSR = previewBlock.GetComponent<SpriteRenderer>();
        previewSR.color = new Color(1f, 1f, 1f, 0.5f);
        previewBlock.GetComponent<Collider2D>().enabled=false;

        previewBlock.transform.position += new Vector3(0, 0, 0.1f);
    }

    void UpdateDraggingPosition()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        // マウスの位置に追従
        draggingBlock.transform.position = mousePos;

        // 
        Vector3 snapPos = new Vector3(Mathf.Round(mousePos.x), Mathf.Round(mousePos.y), 0);
        previewBlock.transform.position = new Vector3(snapPos.x, snapPos.y, 0.1f);

        bool canPlace = CheckCanPlace(snapPos);

        // 重なりの判定で予告の色を変更
        if (canPlace)
        {
            // 設置不可の場合赤色に
            previewSR.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }
        else
        {
            // 設置可能なら緑色に
            previewSR.color = new Color(1f, 0f, 0f, 0.5f);
        }
    }


    // 枠があってブロックがないかを判断
    bool CheckCanPlace(Vector3 pos)
    {
        bool hasFrame = false;
        bool hasBlock = false;

        // 当たり判定を調べる
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);

        foreach(var hit in hits)
        {
            if (hit.CompareTag("DropFrame")) hasFrame = true; // 枠を発見
            if (hit.CompareTag("PlacedBlock")) hasBlock = true; // 配置済みをブロックを発見
        }

        return hasFrame && !hasBlock; // 枠があり、ブロックがない時だけtrue
    }


    void DropBlock()
    {
        // 1x1のグリッドにスナップさせる座標計算
        Vector3 snapPos = previewBlock.transform.position;

        // その場所にブロックがないかチェック
        Collider2D hit = Physics2D.OverlapPoint(snapPos);

        if(CheckCanPlace(snapPos))
        {
            // 配置成功
            draggingBlock.transform.position = snapPos;
            draggingBlock.GetComponent<Collider2D>().enabled = true; // 当たり判定を戻す

            draggingBlock.GetComponent<SpriteRenderer>().color = Color.white;

            draggingBlock.tag = "PlacedBlock";

            currentBlockCount++;
            draggingBlock = null; // ドラッグ終了
        }

        else
        {
            // 配置失敗
            Destroy(draggingBlock);
            draggingBlock = null;
        }

        Destroy(previewBlock);
        previewBlock = null;
        previewSR = null;

        UpdateUI();

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
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        foreach (GameObject b in placedBlocks){ Destroy(b); }
        currentBlockCount = 0;
        UpdateUI();
    }


    void UpdateUI()
    {
        if (countText != null)
        {
            // 残り: 2 / 5
            countText.text = (maxBlockCount - currentBlockCount) + " / " + maxBlockCount;
        }
    }
}
