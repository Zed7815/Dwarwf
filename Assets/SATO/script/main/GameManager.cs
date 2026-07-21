using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    public AudioClip startDeniedSE; // 配置不足の時に鳴らす音

    public int totalItemCount = 0;
    public TextMeshProUGUI itemCountText;
    private List<GameObject> allItems = new List<GameObject>();

    [Header("早送り設定")]
    public GameObject fastForwardButton; // インスペクターで早送りボタンをアタッチ
    public FastForwardUI ffScript;// インスペクターでボタンのスクリプトをアタッチ
    private int playCount = 0; // 実行回数を数える

    void Update()
    {
        // デバッグ用：Lキーを押すと無理やり全ブロック配置済みのフラグを立てて開始する
        if (Keyboard.current.lKey.wasPressedThisFrame && currentState == GameState.Edit)
        {
            Debug.Log("Debug: Force Start");
            currentState = GameState.Play;
            player.StartMove();
            if (editUIController != null) editUIController.HideEditUI();
            SetUI();
        }
    }

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

        if (ffScript != null)
        {
            fastForwardButton.SetActive(true); // オブジェクトは出したまま
            if (ffScript != null) ffScript.SetUIState("Hidden");
            SetUI();
        }

        SetUI();
    }

    public void AddItem()
    {
        totalItemCount++;
        hasCollectedStarInThisRun = true;
        UpdateItemUI();
    }

    public void StartGame()
    {
        // すべてのブロックが置かれていない場合
        if (blockManager != null && !blockManager.IsAllBlocksPlaced())
        {
            PlaySE(startDeniedSE); // 警告音を鳴らす

            int remaining = 0;
            foreach (var b in blockManager.blockTypes)
            {
                remaining += (b.maxCount - b.currentCount);
            }

            if (TutorialMessageUI.instance != null)
            {
                TutorialMessageUI.instance.ShowMessage("すべて置き切らないとスタートできないよ！");
            }

            Debug.Log("まだすべてのブロックを配置していません");
            return;
        }

        playCount++; // 実行回数をカウント

        PlaySE(startGameSE); // 正常な開始音
        currentState = GameState.Play;
        player.StartMove();
        if (editUIController != null) editUIController.HideEditUI();

        // 2回目以降のスタートなら早送りボタンを表示
        if (ffScript != null)
        {
            if (playCount >= 2)
            {
                ffScript.SetUIState("Active"); // 2回目以降：有効
            }
            else
            {
                ffScript.SetUIState("Locked"); // 1回目：半透明ロック
            }
            ffScript.ResetToNormal();
        }


        SetUI();
    }

    // GameManager.cs の ResetGame() 内

    public void ResetGame()
    {
        if (ffScript != null)
        {
            ffScript.ResetToNormal();      // 速度を1.0に戻す
            ffScript.SetUIState("Hidden"); // ボタンを完全に隠す(SetActive(false))
        }
        else
        {
            Time.timeScale = 1.0f; // 万が一スクリプトがなくても時間を戻す
        }

        PlaySE(resetGameSE);
        currentState = GameState.Edit;
        hasCollectedStarInThisRun = false;

        // 1. マップに最初から置いてあるギミックのリセット
        foreach (var res in GimmickResetter.allResetters)
        {
            if (res != null) res.ResetGimmick();
        }

        // 2. プレイヤーが配置したブロックを含む全オブジェクトへのリセット通知
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            script.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }

        // 3. プレイヤー・UIのリセット
        player.ResetPosition();
        if (editUIController != null) editUIController.ShowEditUI();

        // 4. ★最重要：ブロックの状態と個数カウントの完全リセット
        blockManager.ResetAllBlocks();

        // 星アイテムなどの復活
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