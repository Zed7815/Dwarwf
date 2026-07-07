using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // テスト用：データをリセットしたい場合は有効にする 
        //PlayerPrefs.DeleteAll(); 
        //PlayerPrefs.Save();

        // 保存された「クリア済みステージ番号」を読み込む（デフォルトは0）
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

        // デバッグログ：今いくつと判定されているかコンソールで確認できます
        Debug.Log("セーブデータ読み込み：現在ステージ " + clearedStage + " までクリア済み");

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            // i=0(Stage1) は 0 <= clearedStage なので最初から開いている
            // Stage1クリアで clearedStage=1 になれば、i=1(Stage2) が 1 <= 1 で開く
            bool isUnlocked = (i <= clearedStage);

            stageButtons[i].interactable = isUnlocked;
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