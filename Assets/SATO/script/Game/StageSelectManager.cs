using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // ★Keyboardクラス使用のため追加
using System.Collections;

public class StageSelectManager : MonoBehaviour
{
    public Button[] stageButtons;
    public string[] stageSceneNames;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip enterSE;

    // ★セッション中一度だけリセットするためのフラグ
    private static bool sessionResetDone = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // ★修正点1：起動（プレイボタンを押した後）して最初にこのシーンに来た時だけ全消去
        if (!sessionResetDone)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            sessionResetDone = true; // これで、ステージをクリアして戻ってきた時は消されなくなる
            Debug.Log("セーブデータをプレイ開始につき初期化しました");
        }

        RefreshStageButtons();
    }

    // ★ショートカット監視
    void Update()
    {
        // ★修正点2：デバッグ用ショートカット
        // キーボードの「L」キーを押すと全開放
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            UnlockAllStages();
        }
    }

    // ボタンの開放状態を更新する処理を関数にまとめました
    void RefreshStageButtons()
    {
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        Debug.Log("現在のクリア済み状況: ステージ " + clearedStage);

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            bool isUnlocked = (i <= clearedStage);
            stageButtons[i].interactable = isUnlocked;

            // リスナーを一度クリア
            stageButtons[i].onClick.RemoveAllListeners();

            if (isUnlocked)
            {
                AddHoverEvent(stageButtons[i]);
                string sceneName = stageSceneNames.Length > i ? stageSceneNames[i] : "";
                stageButtons[i].onClick.AddListener(() => {
                    if (!string.IsNullOrEmpty(sceneName)) LoadStage(sceneName);
                });
            }
        }
    }

    // デバッグ全開放処理
    void UnlockAllStages()
    {
        Debug.Log("デバッグ：全ステージを開放します");
        // ボタンの数だけクリアしたことにする
        PlayerPrefs.SetInt("StageCleared", stageButtons.Length);
        PlayerPrefs.Save();

        // UIを即時更新
        RefreshStageButtons();
    }

    void AddHoverEvent(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => {
            if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
        });
        trigger.triggers.Add(entryEnter);
    }

    public void LoadStage(string sceneName)
    {
        StartCoroutine(LoadSequence(sceneName));
    }

    IEnumerator LoadSequence(string sceneName)
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(sceneName);
    }
}