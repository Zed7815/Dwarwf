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

    [Header("演出設定")]
    public nextscene fadeInScript;

    [Header("ステージ番号設定")]
    public int stageNumber; // このステージの番号 (1, 2, 3...)
    public bool hasCollectedStarInThisRun = false;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip startGameSE;
    public AudioClip resetGameSE;
    public AudioClip startDeniedSE;

    public int totalItemCount = 0;
    public TextMeshProUGUI itemCountText;
    private List<GameObject> allItems = new List<GameObject>();

    [Header("早送り設定")]
    public GameObject fastForwardButton;
    public FastForwardUI ffScript;
    private int playCount = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
        allItems.Clear();
        allItems.AddRange(items);
        UpdateItemUI();

        if (fadeInScript != null)
        {
            StartCoroutine(fadeInScript.startKuro());
        }

        if (ffScript != null)
        {
            ffScript.SetUIState("Hidden");
        }

        SetUI();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Lキーで強制開始
        if (Keyboard.current.lKey.wasPressedThisFrame && currentState == GameState.Edit)
        {
            Debug.Log("Debug: Force Start");
            currentState = GameState.Play;
            player.StartMove();
            if (editUIController != null) editUIController.HideEditUI();
            SetUI();
        }

        // ★Rキーの判定を削除しました（画面上のリセットボタンからのみリセットされます）

        // デバッグ用：Oキーで星データを消去
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            ResetStarSaveDataForDebug();
        }
    }

    // デバッグ用星復活関数
    public void ResetStarSaveDataForDebug()
    {
        // 1. このステージの星セーブデータを消去
        PlayerPrefs.DeleteKey("StarCollected_Stage_" + stageNumber);
        PlayerPrefs.Save();

        // 2. 状態変数をリセット
        hasCollectedStarInThisRun = false;
        totalItemCount = 0;
        UpdateItemUI();

        // 3. シーン内のすべての星を強制的に新品状態で復活！
        Item[] allStageItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allStageItems)
        {
            item.gameObject.SetActive(true);
            // public/privateに関係なく呼べる安全なSendMessageを使用
            item.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"<color=yellow>【デバッグ】ステージ {stageNumber} の星セーブデータを削除し、星を完全復活させました！</color>");
    }

    public void AddItem()
    {
        totalItemCount++;
        hasCollectedStarInThisRun = true;
        UpdateItemUI();
    }

    public void StartGame()
    {
        // ★修正：「すべて置き切らないとスタートできない」ガードを完全に削除！
        // 0個でも、1個でも、何個置いていても即座にスタートします。

        playCount++; // 実行回数をカウント

        PlaySE(startGameSE); // 正常な開始音
        currentState = GameState.Play;
        player.StartMove();
        if (editUIController != null) editUIController.HideEditUI();

        // 2回目以降のスタートなら早送りボタンを表示
        if (ffScript != null)
        {
            if (playCount >= 2) ffScript.SetUIState("Active");
            else ffScript.SetUIState("Locked");

            ffScript.ResetToNormal();
        }

        SetUI();
    }

    // 倍速ボタンを押した瞬間の処理
    public void SetFastSpeed(bool isFast)
    {
        if (isFast)
        {
            Time.timeScale = 3.0f; // 3倍速

            // ★最重要：物理演算の間隔を広げて、通常時と同じ計算負荷（1秒に50回）に抑える！
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // ★最重要：計算遅れが発生しても、最大0.1秒分で打ち切る（デススパイラルの防止）
            Time.maximumDeltaTime = 0.1f;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.333f;
        }
    }
    public void ResetGame()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        Time.maximumDeltaTime = 0.333f;


        if (GameSpeedManager.instance != null) GameSpeedManager.instance.ResetToBaseSpeed();
        else Time.timeScale = 1.0f;

        if (ffScript != null)
        {
            ffScript.ResetToNormal();
            ffScript.SetUIState("Hidden");
        }

        PlaySE(resetGameSE);
        currentState = GameState.Edit;
        hasCollectedStarInThisRun = false;

        // 1. GimmickResetter
        foreach (var res in GimmickResetter.allResetters)
        {
            if (res != null) res.ResetGimmick();
        }

        // 2. 全オブジェクトへのリセット通知
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            script.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }

        // 3. プレイヤー・UIのリセット
        player.ResetPosition();
        if (editUIController != null) editUIController.ShowEditUI();

        // 4. ブロックの状態とカウントのリセット
        if (blockManager != null) blockManager.ResetAllBlocks();

        // 5. アイテムの初期化
        totalItemCount = 0;
        foreach (GameObject item in allItems)
        {
            if (item != null)
            {
                item.SetActive(true);
                item.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
            }
        }

        // 6. チェックポイントのスナップショット復元
        if (CheckpointManager.instance != null && CheckpointManager.instance.HasActiveCheckpoint)
        {
            CheckpointManager.instance.RestoreSnapshot();
        }
        else
        {
            UpdateItemUI();
        }

        SetUI();
    }

    public void UpdateItemUI()
    {
        if (itemCountText != null) itemCountText.text = "Star: " + totalItemCount;
    }

    void PlaySE(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void SetUI()
    {
        if (startButton != null) startButton.SetActive(currentState == GameState.Edit);
        if (resetButton != null) resetButton.SetActive(currentState == GameState.Play);
    }
}