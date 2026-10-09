using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BookEncyclopediaUI : MonoBehaviour
{
    [System.Serializable]
    public class AnimalPage
    {
        public string animalName;       // 名前
        public Sprite illustration;     // 左ページの絵

        [Header("説明文（最大3つの枠に分割可能）")]
        [TextArea(2, 5)]
        public string descriptionBox1;  // 枠1（例：基本特徴）
        [TextArea(2, 5)]
        public string descriptionBox2;  // 枠2（例：使い方・ヒント）
        [TextArea(2, 5)]
        public string descriptionBox3;  // 枠3（例：注意点 ※空欄なら自動で隠れます）
    }

    [Header("図鑑データ集（1ページ＝1体）")]
    public List<AnimalPage> pages;

    [Header("ページ枠（めくる対象）")]
    public RectTransform leftPageTransform;
    public RectTransform rightPageTransform;

    [Header("左ページ（絵）")]
    public Image illustrationImage;

    [Header("右ページ（名前 ＆ 3つの説明枠）")]
    public TextMeshProUGUI nameText;

    // ★枠1のセット
    public GameObject frameBox1;             // 枠1の親（枠画像など）
    public TextMeshProUGUI descriptionText1; // 枠1のテキスト

    // ★枠2のセット
    public GameObject frameBox2;             // 枠2の親
    public TextMeshProUGUI descriptionText2; // 枠2のテキスト

    // ★枠3のセット（任意）
    public GameObject frameBox3;             // 枠3の親
    public TextMeshProUGUI descriptionText3; // 枠3のテキスト

    [Header("共通UI")]
    public TextMeshProUGUI pageNumberText;
    public Button prevButton;
    public Button nextButton;

    [Header("SE")]
    public AudioSource audioSource;
    public AudioClip pageFlipSE;

    private int currentPageIndex = 0;
    private bool isFlipping = false;

    void OnEnable()
    {
        currentPageIndex = 0;
        if (leftPageTransform != null) leftPageTransform.localScale = Vector3.one;
        if (rightPageTransform != null) rightPageTransform.localScale = Vector3.one;
        UpdatePageDisplayInstant();
    }

    public void NextPage()
    {
        if (isFlipping || currentPageIndex >= pages.Count - 1) return;
        StartCoroutine(FlipRoutine(true));
    }

    public void PrevPage()
    {
        if (isFlipping || currentPageIndex <= 0) return;
        StartCoroutine(FlipRoutine(false));
    }

    IEnumerator FlipRoutine(bool isNext)
    {
        isFlipping = true;
        float duration = 0.07f;
        float elapsed = 0f;

        RectTransform foldingPage = isNext ? rightPageTransform : leftPageTransform;
        RectTransform unfoldingPage = isNext ? leftPageTransform : rightPageTransform;

        if (foldingPage != null)
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scaleX = Mathf.Lerp(1.0f, 0.0f, t);
                foldingPage.localScale = new Vector3(scaleX, 1.0f + (1f - scaleX) * 0.05f, 1f);
                yield return null;
            }
            foldingPage.localScale = new Vector3(0f, 1f, 1f);
        }

        PlayFlipSound();
        if (isNext) currentPageIndex++;
        else currentPageIndex--;
        UpdatePageDisplayInstant();

        if (unfoldingPage != null)
        {
            unfoldingPage.localScale = new Vector3(0f, 1f, 1f);
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scaleX = Mathf.Lerp(0.0f, 1.0f, t);
                unfoldingPage.localScale = new Vector3(scaleX, 1.0f, 1f);
                yield return null;
            }
            unfoldingPage.localScale = Vector3.one;
        }

        if (leftPageTransform != null) leftPageTransform.localScale = Vector3.one;
        if (rightPageTransform != null) rightPageTransform.localScale = Vector3.one;

        isFlipping = false;
    }

    // 表示内容の更新
    void UpdatePageDisplayInstant()
    {
        if (pages == null || pages.Count == 0) return;

        AnimalPage page = pages[currentPageIndex];

        // 1. 左ページ（絵）
        if (illustrationImage != null)
        {
            illustrationImage.sprite = page.illustration;
            illustrationImage.preserveAspect = true;
        }

        // 2. 右ページ（名前）
        if (nameText != null) nameText.text = page.animalName;

        // 3. ★3つの説明枠への流し込み（文字が空なら枠ごと隠す）
        SetBoxContent(frameBox1, descriptionText1, page.descriptionBox1);
        SetBoxContent(frameBox2, descriptionText2, page.descriptionBox2);
        SetBoxContent(frameBox3, descriptionText3, page.descriptionBox3);

        // ページ番号
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {pages.Count}";
        }

        if (prevButton != null) prevButton.interactable = (currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentPageIndex < pages.Count - 1);
    }

    // 枠とテキストの表示制御
    void SetBoxContent(GameObject frame, TextMeshProUGUI textComp, string content)
    {
        bool hasContent = !string.IsNullOrEmpty(content);

        // 枠自体の表示・非表示
        if (frame != null) frame.SetActive(hasContent);

        // テキストの適用
        if (textComp != null && hasContent)
        {
            textComp.text = content;
        }
    }

    void PlayFlipSound()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);
    }
}