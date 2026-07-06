using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class StageSelectManager : MonoBehaviour
{
    public Button[] stageButtons;
    private static bool sessionResetDone = false;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip enterSE;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // テスト用リセット（必要に応じて）
        if (!sessionResetDone)
        {
            // PlayerPrefs.DeleteAll(); // 開発中だけ有効にする
            PlayerPrefs.Save();
            sessionResetDone = true;
        }

        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            bool isUnlocked = (i <= clearedStage);
            stageButtons[i].interactable = isUnlocked;

            // アニメーターを取得
            Animator anim = stageButtons[i].GetComponent<Animator>();

            if (anim != null)
            {
                // 初期状態をセット
                anim.SetBool("isUnlocked", isUnlocked);

                if (isUnlocked)
                {
                    AddHoverEvents(stageButtons[i], anim);
                }
            }
            else
            {
                Debug.LogError($"{stageButtons[i].name} にAnimatorが付いていません！");
            }
        }
    }

    void AddHoverEvents(Button btn, Animator anim)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => {
            if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
            anim.SetBool("isHover", true);
        });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            anim.SetBool("isHover", false);
        });
        trigger.triggers.Add(entryExit);
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