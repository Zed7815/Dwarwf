using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public EditUIController editUIController;
    public static GameManager instance;
    public enum GameState { Edit, Play }
    public GameState currentState = GameState.Edit;

    public PlayerController player;
    public GameObject startButton;
    public GameObject resetButton;
    public BlockManager blockManager;

    [Header("ステージ番号設定")]
    public int stageNumber; // このステージの番号 (1, 2, 3...) を入れる
    public bool hasCollectedStarInThisRun = false; // 今回のプレイで星を取ったか

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip startGameSE;
    public AudioClip resetGameSE;

    public int totalItemCount = 0;
    public TextMeshProUGUI itemCountText;
    private List<GameObject> allItems = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
        allItems.AddRange(items);
        UpdateItemUI();
        SetUI();
    }

    public void AddItem()
    {
        totalItemCount++;
        hasCollectedStarInThisRun = true; // ★星取得フラグを立てる
        UpdateItemUI();
    }

    public void StartGame()
    {
        if (blockManager != null && !blockManager.IsAllBlocksPlaced()) return;
        PlaySE(startGameSE);
        currentState = GameState.Play;
        player.StartMove();
        if (editUIController != null) editUIController.HideEditUI();
        SetUI();
    }

    public void ResetGame()
    {
        PlaySE(resetGameSE);
        currentState = GameState.Edit;
        hasCollectedStarInThisRun = false; // ★リセット時はフラグもリセット

        foreach (var res in GimmickResetter.allResetters)
        {
            if (res != null) res.ResetGimmick();
        }

        player.ResetPosition();
        blockManager.ResetAllBlocks();
        if (editUIController != null) editUIController.ShowEditUI();
        totalItemCount = 0;
        UpdateItemUI();
        foreach (GameObject item in allItems) { if (item != null) item.SetActive(true); }
        SetUI();
    }

    void UpdateItemUI()
    {
        if (itemCountText != null) itemCountText.text = "Star: " + totalItemCount;
    }

    void PlaySE(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void SetUI()
    {
        if (currentState == GameState.Edit) { startButton.SetActive(true); resetButton.SetActive(false); }
        else { startButton.SetActive(false); resetButton.SetActive(true); }
    }
}