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

        [Header("アンロック条件")]
        [Tooltip("このステージ番号以降で図鑑が公開されます（例: ステージ3なら3）")]
        public int unlockStageNumber = 1;

        [Header("説明文（3枠）")]
        [TextArea(2, 5)] public string descriptionBox1;
        [TextArea(2, 5)] public string descriptionBox2;
        [TextArea(2, 5)] public string descriptionBox3;
    }

    [Header("図鑑データ集")]
    public List<AnimalPage> pages;

    [Header("本の背景画像差し替え設定")]
    public Image bookBackgroundImage;
    public Sprite normalBookSprite;
    public Sprite flippingBookSprite;
    public float flipDuration = 0.18f;
    public float paperTiltAngle = 4.0f;

    [Header("ページ枠（めくる対象）")]
    public RectTransform leftPageTransform;
    public RectTransform rightPageTransform;

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
        if (bookBackgroundImage != null && normalBookSprite != null)
        {
            bookBackgroundImage.sprite = normalBookSprite;
        }
        ResetPageTransforms();
        UpdatePageDisplayInstant();
    }

    public void NextPage()
    {
        if (isFlipping || currentPageIndex >= pages.Count - 1) return;
        StartCoroutine(EmphasizedFlipRoutine(true));
    }

    public void PrevPage()
    {
        if (isFlipping || currentPageIndex <= 0) return;
        StartCoroutine(EmphasizedFlipRoutine(false));
    }

    IEnumerator EmphasizedFlipRoutine(bool isNext)
    {
        isFlipping = true;

        if (bookBackgroundImage != null && flippingBookSprite != null)
        {
            bookBackgroundImage.sprite = flippingBookSprite;
        }

        PlayFlipSound();

        RectTransform foldingPage = isNext ? rightPageTransform : leftPageTransform;
        RectTransform unfoldingPage = isNext ? leftPageTransform : rightPageTransform;

        float halfDuration = flipDuration * 0.5f;
        float elapsed = 0f;

        if (foldingPage != null)
        {
            float tiltDir = isNext ? -1f : 1f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                float scaleX = Mathf.Lerp(1.0f, 0.0f, t);
                float scaleY = Mathf.Lerp(1.0f, 1.08f, t);
                float angle = Mathf.Lerp(0f, paperTiltAngle * tiltDir, t);

                foldingPage.localScale = new Vector3(scaleX, scaleY, 1f);
                foldingPage.localEulerAngles = new Vector3(0, 0, angle);
                yield return null;
            }
            foldingPage.localScale = new Vector3(0f, 1f, 1f);
            foldingPage.localEulerAngles = Vector3.zero;
        }

        if (isNext) currentPageIndex++;
        else currentPageIndex--;
        UpdatePageDisplayInstant();

        if (unfoldingPage != null)
        {
            elapsed = 0f;
            float tiltDir = isNext ? 1f : -1f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                float scaleX = Mathf.Lerp(0.0f, 1.0f, t);
                float scaleY = Mathf.Lerp(1.08f, 1.0f, t);
                float angle = Mathf.Lerp(paperTiltAngle * tiltDir, 0f, t);

                unfoldingPage.localScale = new Vector3(scaleX, scaleY, 1f);
                unfoldingPage.localEulerAngles = new Vector3(0, 0, angle);
                yield return null;
            }
            unfoldingPage.localScale = Vector3.one;
            unfoldingPage.localEulerAngles = Vector3.zero;
        }

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

    // ★【重要】表示内容の更新（案Cのアンロック判定を適用）
    void UpdatePageDisplayInstant()
    {
        if (pages == null || pages.Count == 0) return;

        AnimalPage page = pages[currentPageIndex];

        // 現在プレイヤーが挑んでいるステージ番号を取得
        int currentStage = 1;
        if (StageInfo.instance != null) currentStage = StageInfo.instance.stageNumber;
        else if (GameManager.instance != null) currentStage = GameManager.instance.stageNumber;

        // ★アンロック判定：今のステージが、解禁ステージ以上ならオープン
        bool isUnlocked = (currentStage >= page.unlockStageNumber);

        if (isUnlocked)
        {
            // --- 【解禁済み】：カラーイラスト ＆ 通常の解説 ---
            if (illustrationImage != null)
            {
                illustrationImage.sprite = page.illustration;
                illustrationImage.color = Color.white; // 通常カラー
                illustrationImage.preserveAspect = true;
            }

            if (nameText != null) nameText.text = page.animalName;

            SetBoxContent(frameBox1, descriptionText1, page.descriptionBox1);
            SetBoxContent(frameBox2, descriptionText2, page.descriptionBox2);
            SetBoxContent(frameBox3, descriptionText3, page.descriptionBox3);
        }
        else
        {
            // --- 【未解禁（案C）】：シルエット ＆ 謎の手記 ---
            if (illustrationImage != null)
            {
                illustrationImage.sprite = page.illustration;
                illustrationImage.color = Color.black; // ★自動で真っ黒（シルエット）にする！
                illustrationImage.preserveAspect = true;
            }

            if (nameText != null) nameText.text = "？？？？？";

            // 案Cのメッセージを流し込む
            SetBoxContent(frameBox1, descriptionText1, "【未確認の生物】\nまだ出会ったことのない生物だ。この先のエリアに生息しているらしい……。");
            SetBoxContent(frameBox2, descriptionText2, ""); // 枠2と枠3はスッキリ消す
            SetBoxContent(frameBox3, descriptionText3, "");
        }

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