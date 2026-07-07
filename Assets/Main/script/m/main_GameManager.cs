using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class main_GameManager : MonoBehaviour
{
    public static main_GameManager instance;
    public enum main_GameState { Edit, Play }
    public main_GameState currentState = main_GameState.Edit;

    public PlayerController player;

    public GameObject startButton;
    public GameObject resetButton;

    public main_BlockManager blockManager;

    // アイテム管理
    public int totalItemCount = 0; // 拾った数
    public TextMeshProUGUI itemCountText; // 拾った数を表すUI用

    // ステージ上にあるアイテムをすべて記憶
    private List<GameObject> allItems = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
        allItems.AddRange(items);

        UpdateItemUI(); // 起動時に表示をリセット
        SetUI();
    }

    public void AddItem()
    {
        totalItemCount++;
        UpdateItemUI();
    }

    void UpdateItemUI()
    {
        if (itemCountText != null)
        {
            itemCountText.text = "Star: " + totalItemCount;
        }
    }

    public void StartGame()
    {
        if (blockManager != null && !blockManager.IsAllBlocksPlaced())
        {
            Debug.Log("まだすべてのブロックを配置していません");
            return;
        }

        blockManager.ClearGhostBlocks();

        currentState = main_GameState.Play;
        player.StartMove();
        SetUI();
    }

    public void ResetGame()
    {
        currentState = main_GameState.Edit;
        player.ResetPosition();
        blockManager.ResetAllBlocks();

        totalItemCount = 0;
        UpdateItemUI();

        // itemタグのアイテムをすべて表示する
        foreach (GameObject item in allItems)
        {
            if (item != null)
            {
                item.SetActive(true);
            }
        }

        SetUI();
    }

    void SetUI()
    {
        if (currentState == main_GameState.Edit)
        {
            startButton.SetActive(true); // スタートボタン表示
            resetButton.SetActive(false); // リセットボタンを非表示
        }

        else
        {
            startButton.SetActive(false); // スタートボタンの非表示
            resetButton.SetActive(true); // リセットボタンを表示
        }
    }
}
