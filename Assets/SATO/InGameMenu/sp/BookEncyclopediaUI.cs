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
        public string animalName;
        public Sprite illustration;

        [Header("説明文（3枠）")]
        [TextArea(2, 5)] public string descriptionBox1;
        [TextArea(2, 5)] public string descriptionBox2;
        [TextArea(2, 5)] public string descriptionBox3;
    }

    [Header("図鑑データ集（1ページ＝1体）")]
    public List<AnimalPage> pages;

    [Header("本の背景画像差し替え設定")]
    public Image bookBackgroundImage;        // 本の背景Image
    public Sprite normalBookSprite;          // 通常の見開き画像
    public Sprite flippingBookSprite;        // めくる瞬間の一瞬の画像

    [Header("演出時間・強調設定")]
    [Tooltip("ページめくり全体の時間（0.16〜0.22秒が一番リアルに見えます）")]
    public float flipDuration = 0.18f;
    [Tooltip("めくる時の紙の傾き具合（度数）")]
    public float paperTiltAngle = 4.0f;

    [Header("ページ枠（めくる対象）")]
    public RectTransform leftPageTransform;  // 左ページの枠（Pivot X: 1推奨）
    public RectTransform rightPageTransform; // 右ページの枠（Pivot X: 0推奨）

    [Header("左ページ（絵）")]
    public Image illustrationImage;

    [Header("右ページ（名前 ＆ 3枠）")]
    public TextMeshProUGUI nameText;
    public GameObject frameBox1;
    public TextMeshProUGUI descriptionText1;
    public GameObject frameBox2;
    public TextMeshProUGUI descriptionText2;
    public GameObject frameBox3;
    public TextMeshProUGUI descriptionText3;

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

        // 背景とページの回転・サイズを初期化
        if (bookBackgroundImage != null && normalBookSprite != null)
        {
            bookBackgroundImage.sprite = normalBookSprite;
        }
        ResetPageTransforms();

        UpdatePageDisplayInstant();
    }

    // --- 次へ ▶ ---
    public void NextPage()
    {
        if (isFlipping || currentPageIndex >= pages.Count - 1) return;
        StartCoroutine(EmphasizedFlipRoutine(true));
    }

    // --- ◀ 前へ ---
    public void PrevPage()
    {
        if (isFlipping || currentPageIndex <= 0) return;
        StartCoroutine(EmphasizedFlipRoutine(false));
    }

    // ★【強調めくり ＋ 背景差し替えの合体コルーチン】
    IEnumerator EmphasizedFlipRoutine(bool isNext)
    {
        isFlipping = true;

        // 1. 本の背景を「めくり中の絵」に切り替え！
        if (bookBackgroundImage != null && flippingBookSprite != null)
        {
            bookBackgroundImage.sprite = flippingBookSprite;
        }

        PlayFlipSound();

        // めくる対象のページ（次へなら右、戻るなら左）
        RectTransform foldingPage = isNext ? rightPageTransform : leftPageTransform;
        RectTransform unfoldingPage = isNext ? leftPageTransform : rightPageTransform;

        float halfDuration = flipDuration * 0.5f;
        float elapsed = 0f;

        // --- 前半：紙が立体的にめくれ上がりながら真ん中へたたまれる ---
        if (foldingPage != null)
        {
            float tiltDir = isNext ? -1f : 1f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;

                // 横幅の縮小
                float scaleX = Mathf.Lerp(1.0f, 0.0f, t);
                // 紙が上に弓なりにしなる動き（縦が少し伸びる）
                float scaleY = Mathf.Lerp(1.0f, 1.08f, t);
                // 角が持ち上がる傾き（Z軸回転）
                float angle = Mathf.Lerp(0f, paperTiltAngle * tiltDir, t);

                foldingPage.localScale = new Vector3(scaleX, scaleY, 1f);
                foldingPage.localEulerAngles = new Vector3(0, 0, angle);
                yield return null;
            }

            foldingPage.localScale = new Vector3(0f, 1f, 1f);
            foldingPage.localEulerAngles = Vector3.zero;
        }

        // --- 中間地点：真ん中を通過した瞬間にデータを差し替え！ ---
        if (isNext) currentPageIndex++;
        else currentPageIndex--;
        UpdatePageDisplayInstant();

        // --- 後半：新しい紙が反対側からフワッと広がりながら収まる ---
        if (unfoldingPage != null)
        {
            elapsed = 0f;
            float tiltDir = isNext ? 1f : -1f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;

                // 横幅の展開
                float scaleX = Mathf.Lerp(0.0f, 1.0f, t);
                // しなりが元に戻る
                float scaleY = Mathf.Lerp(1.08f, 1.0f, t);
                // 傾きが0に戻る
                float angle = Mathf.Lerp(paperTiltAngle * tiltDir, 0f, t);

                unfoldingPage.localScale = new Vector3(scaleX, scaleY, 1f);
                unfoldingPage.localEulerAngles = new Vector3(0, 0, angle);
                yield return null;
            }

            unfoldingPage.localScale = Vector3.one;
            unfoldingPage.localEulerAngles = Vector3.zero;
        }

        // 2. 本の背景を「通常の見開き」に戻す！
        if (bookBackgroundImage != null && normalBookSprite != null)
        {
            bookBackgroundImage.sprite = normalBookSprite;
        }

        ResetPageTransforms();
        isFlipping = false;
    }

    void ResetPageTransforms()
    {
        if (leftPageTransform != null)
        {
            leftPageTransform.localScale = Vector3.one;
            leftPageTransform.localEulerAngles = Vector3.zero;
        }
        if (rightPageTransform != null)
        {
            rightPageTransform.localScale = Vector3.one;
            rightPageTransform.localEulerAngles = Vector3.zero;
        }
    }

    void UpdatePageDisplayInstant()
    {
        if (pages == null || pages.Count == 0) return;

        AnimalPage page = pages[currentPageIndex];

        if (illustrationImage != null)
        {
            illustrationImage.sprite = page.illustration;
            illustrationImage.preserveAspect = true;
        }

        if (nameText != null) nameText.text = page.animalName;

        SetBoxContent(frameBox1, descriptionText1, page.descriptionBox1);
        SetBoxContent(frameBox2, descriptionText2, page.descriptionBox2);
        SetBoxContent(frameBox3, descriptionText3, page.descriptionBox3);

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {pages.Count}";
        }

        if (prevButton != null) prevButton.interactable = (currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentPageIndex < pages.Count - 1);
    }

    void SetBoxContent(GameObject frame, TextMeshProUGUI textComp, string content)
    {
        bool hasContent = !string.IsNullOrEmpty(content);
        if (frame != null) frame.SetActive(hasContent);
        if (textComp != null && hasContent) textComp.text = content;
    }

    void PlayFlipSound()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);
    }
}