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

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターでAudioSourceを割り当て
    public AudioClip startGameSE;  // 実行ボタンを押した時の音
    public AudioClip resetGameSE;  // リセットボタンを押した時の音

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
        // AudioSourceが未設定なら取得を試みる
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
        allItems.AddRange(items);

        UpdateItemUI();
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

        // ★SE再生: ゲーム開始
        PlaySE(startGameSE);

        currentState = GameState.Play;
        player.StartMove();

        if (editUIController != null)
        {
            editUIController.HideEditUI();
        }

        SetUI();
    }

    public void ResetGame()
    {
        currentState = GameState.Edit;

        // 1. ★名簿を使ってすべてのギミックを強制リセット
        // これにより、画面から消えている足場も、遠くへ行ったリフトも戻ります
        foreach (var res in GimmickResetter.allResetters)
        {
            if (res != null) res.ResetGimmick();
        }

        // 2. プレイヤーを戻す（親子関係解除を含む）
        player.ResetPosition();

        // 3. プレイヤーが配置したブロックを消去
        blockManager.ResetAllBlocks();

        // UIやアイテムのリセット
        if (editUIController != null) editUIController.ShowEditUI();
        totalItemCount = 0;
        UpdateItemUI();
        foreach (GameObject item in allItems)
        {
            if (item != null) item.SetActive(true);
        }

        SetUI();
    }

    // 音再生用ヘルパー
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void SetUI()
    {
        if (currentState == GameState.Edit)
        {
            startButton.SetActive(true);
            resetButton.SetActive(false);
        }
        else
        {
            startButton.SetActive(false);
            resetButton.SetActive(true);
        }
    }
}