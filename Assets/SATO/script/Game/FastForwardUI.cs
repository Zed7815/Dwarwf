using UnityEngine;
using UnityEngine.UI;

public class FastForwardUI : MonoBehaviour
{
    [Header("設定")]
    public float fastForwardSpeed = 3.0f;

    [Header("表示設定")]
    public Image buttonImage;
    public Sprite normalSprite;
    public Sprite fastSprite;
    public Button button;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip clickSE;

    private bool isFastForwarding = false;

    void Start()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        ResetToNormal();
    }

    // 3つの状態を切り替える
    public void SetUIState(string state)
    {
        switch (state)
        {
            case "Hidden": // 編集中：完全に消す
                gameObject.SetActive(false);
                break;

            case "Locked": // 1回目実行中：半透明・反応なし
                gameObject.SetActive(true);
                if (button != null) button.interactable = false;
                SetAlpha(0.4f);
                break;

            case "Active": // 2回目以降実行中：不透明・反応あり
                gameObject.SetActive(true);
                if (button != null) button.interactable = true;
                SetAlpha(1.0f);
                break;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (buttonImage != null)
        {
            Color c = buttonImage.color;
            c.a = alpha;
            buttonImage.color = c;
        }
    }

    public void OnButtonClick()
    {
        isFastForwarding = !isFastForwarding;
        if (isFastForwarding)
        {
            Time.timeScale = fastForwardSpeed;
            if (buttonImage != null) buttonImage.sprite = fastSprite;
        }
        else
        {
            Time.timeScale = 1.0f;
            if (buttonImage != null) buttonImage.sprite = normalSprite;
        }

        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }

    public void ResetToNormal()
    {
        isFastForwarding = false;
        Time.timeScale = 1.0f;
        if (buttonImage != null) buttonImage.sprite = normalSprite;
    }
}