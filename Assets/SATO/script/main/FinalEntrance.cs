using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class FinalEntrance : MonoBehaviour, IPointerClickHandler
{
    public string endingSceneName = "Ending";
    public GameObject guideFinger;
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip clickSE;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        RefreshEntrance(); // 開始時に表示チェック
    }

    // ★追加・修正：表示状態を最新にする関数
    public void RefreshEntrance()
    {
        bool isFullCleared = PlayerPrefs.GetInt("GameFullCleared", 0) == 1;

        Debug.Log("エンディング入口の表示更新: " + isFullCleared);

        // 自分（入口）の表示
        gameObject.SetActive(isFullCleared);

        // 指ガイドの表示
        if (guideFinger != null)
        {
            guideFinger.SetActive(isFullCleared);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(GoToEnding());
    }

    IEnumerator GoToEnding()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        yield return new WaitForSecondsRealtime(0.2f);
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(endingSceneName);
    }
}