using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BlockManager : MonoBehaviour
{
    public GameManager gameManager; // モード判定
    public GameObject blockPrefab; // 設置したいブロックのプレハブ

    public int maxBlockCount = 5; // 最大個数
    private int currentBlockCount; // 配置している個数

    public TextMeshProUGUI countText; // 残り個数を表示するUI

    private void Start()
    {
        currentBlockCount = 0;
        UpdateUI();
    }


    void Update()
    {
        if (gameManager.currentState != GameManager.GameState.Edit) return;

        // 左クリックされたかどうかの判定
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceBlock();
        }
    }

    void PlaceBlock()
    {
        // マウスの位置をゲーム内座標に変換
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        // Z座標を0にし、XとYを四捨五入してグリッドに合わせる
        float snapX = Mathf.Round(worldPos.x);
        float snapY = Mathf.Round(worldPos.y);
        Vector3 spawnPos = new Vector3(snapX, snapY, 0);

        // すでにブロックが配置されているかの確認
        // Collider2Dがあるかチェックする命令
        Collider2D hit = Physics2D.OverlapPoint(spawnPos);

        if (hit == null)
        {
            // 置こうとした時、最大数を超えていないかチェック
            if (currentBlockCount < maxBlockCount)
            {
                GameObject newBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
                newBlock.tag = "PlacedBlock"; // 後で一括削除するためにタグをつける
                currentBlockCount++;
            }
        }
        else if (hit.CompareTag("PlacedBlock"))
        {
            // すでにあるブロックをクリックしたら消して、個数を回復
            Destroy(hit.gameObject);
            currentBlockCount--;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (countText != null)
        {
            // 残り: 2 / 5
            countText.text = "残り: " + (maxBlockCount - currentBlockCount) + " / " + maxBlockCount;
        }
    }

    public void ResetAllBlocks()
    {
        // "PlacedBlock"のtagをすべて削除
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");

        foreach(GameObject b in placedBlocks)
        {
            Destroy(b);
        }

        currentBlockCount = 0;
        UpdateUI();
    }
}
