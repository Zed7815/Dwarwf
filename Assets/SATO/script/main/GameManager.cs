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
    public int stageNumber;
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

        // ★非アクティブになっている星も含めてシーン内から漏れなく検索
        Item[] items = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        allItems.Clear();
        foreach (var it in items)
        {
            allItems.Add(it.gameObject);
        }

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

        if (Keyboard.current.lKey.wasPressedThisFrame && currentState == GameState.Edit)
        {
            Debug.Log("Debug: Force Start");
            currentState = GameState.Play;
            player.StartMove();
            if (editUIController != null) editUIController.HideEditUI();
            SetUI();
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            ResetStarSaveDataForDebug();
        }
    }

    public void ResetStarSaveDataForDebug()
    {
        PlayerPrefs.DeleteKey("StarCollected_Stage_" + stageNumber);
        GoalPoint goal = FindObjectOfType<GoalPoint>();
        if (goal != null)
        {
            PlayerPrefs.DeleteKey($"Nest_{goal.nestIndex}_{(goal.isEasyStage ? "Easy" : "Hard")}_Star");
        }
        PlayerPrefs.Save();

        hasCollectedStarInThisRun = false;
        totalItemCount = 0;
        UpdateItemUI();

        Item[] allStageItems = FindObjectsByType<Item>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allStageItems)
        {
            item.gameObject.SetActive(true);
            item.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"<color=yellow>【デバッグ】星データをリセットし、完全復活させました</color>");
    }

    public void AddItem()
    {
        totalItemCount++;
        hasCollectedStarInThisRun = true;
        UpdateItemUI();
    }

    public void StartGame()
    {
        playCount++;

        PlaySE(startGameSE);
        currentState = GameState.Play;
        player.StartMove();
        if (editUIController != null) editUIController.HideEditUI();

        if (ffScript != null)
        {
            if (playCount >= 2) ffScript.SetUIState("Active");
            else ffScript.SetUIState("Locked");

            ffScript.ResetToNormal();
        }

        SetUI();
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

        // 4. ブロックのリセット
        if (blockManager != null) blockManager.ResetAllBlocks();

        // 5. ★アイテムの初期化（セーブデータがある星は復活させない！）
        totalItemCount = 0;
        foreach (GameObject item in allItems)
        {
            if (item != null)
            {
                Item itemComp = item.GetComponent<Item>();
                if (itemComp != null && itemComp.IsAlreadySavedCollected())
                {
                    itemComp.ForceCollectedState();
                }
                else
                {
                    item.SetActive(true);
                    item.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);
                }
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

    // ★最初から星がないステージはUIを隠す！
    public void UpdateItemUI()
    {
        if (itemCountText == null) return;

        // シーン内に星が1つもないステージなら、UIを非表示にする
        if (allItems == null || allItems.Count == 0)
        {
            itemCountText.gameObject.SetActive(false);
            return;
        }

        itemCountText.gameObject.SetActive(true);
        itemCountText.text = "Star: " + totalItemCount;
    }

    void PlaySE(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void SetUI()
    {
        if (startButton != null) startButton.SetActive(currentState == GameState.Edit);
        if (resetButton != null) resetButton.SetActive(currentState == GameState.Play);
    }
}