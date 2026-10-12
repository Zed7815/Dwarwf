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
        [Tooltip("このステージ番号をクリアしたら図鑑がカラー解放されます")]
        public int unlockStageNumber = 1;

        [Header("説明文（3枠）")]
        [TextArea(2, 5)] public string descriptionBox1;
        [TextArea(2, 5)] public string descriptionBox2;
        [TextArea(2, 5)] public string descriptionBox3;
    }

    [Header("図鑑データ集")]
    public List<AnimalPage> pages;

    [Header("NEW!マークの演出オブジェクト")]
    public GameObject newBadgeObject; // ★「NEW!」と書かれた画像オブジェクト

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

    // ★表示内容の更新（カラー判定 ＆ NEW!の管理）
    void UpdatePageDisplayInstant()
    {
        if (pages == null || pages.Count == 0) return;

        AnimalPage page = pages[currentPageIndex];

        // セーブデータからクリアステージ数を取得
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        bool isUnlocked = false;

        // --- ★【完全解決】アンロック判定 ---
        // 1. もし必要面が「1面」なら、図鑑が開けている時点で1面クリア後なので【絶対にカラー解放】！
        if (page.unlockStageNumber <= 1)
        {
            isUnlocked = true;
        }
        // 2. ゲーム本編をプレイ中の場合：そのステージの動物まで解放
        else if (StageInfo.instance != null && StageInfo.instance.stageNumber >= page.unlockStageNumber)
        {
            isUnlocked = true;
        }
        // 3. セレクト画面の場合：そのステージをクリア済みなら解放！
        else if (clearedStage >= page.unlockStageNumber)
        {
            isUnlocked = true;
        }

        // ★コンソールに判定理由をわかりやすく表示
        Debug.Log($"<color=cyan>【図鑑】「{page.animalName}」| 必要面: {page.unlockStageNumber} | クリア数: {clearedStage} | カラー判定: {isUnlocked}</color>");

        if (isUnlocked)
        {
            // --- 【解禁済み】：カラーイラスト ＆ 通常の解説 ---
            if (illustrationImage != null)
            {
                illustrationImage.sprite = page.illustration;
                illustrationImage.color = new Color(1f, 1f, 1f, 1f); // ★RGBとAlphaを確実に1.0（通常カラー）にする！
                illustrationImage.preserveAspect = true;
            }

            if (nameText != null) nameText.text = page.animalName;

            SetBoxContent(frameBox1, descriptionText1, page.descriptionBox1);
            SetBoxContent(frameBox2, descriptionText2, page.descriptionBox2);
            SetBoxContent(frameBox3, descriptionText3, page.descriptionBox3);

            // NEW!マークの管理
            string viewKey = "ViewedAnimal_" + page.animalName;
            bool hasViewed = PlayerPrefs.GetInt(viewKey, 0) == 1;
            if (newBadgeObject != null) newBadgeObject.SetActive(!hasViewed);

            if (!hasViewed)
            {
                PlayerPrefs.SetInt(viewKey, 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // --- 【未解禁】：シルエット ---
            if (illustrationImage != null)
            {
                illustrationImage.sprite = page.illustration;
                illustrationImage.color = new Color(0f, 0f, 0f, 1f); // ★真っ黒
                illustrationImage.preserveAspect = true;
            }

            if (nameText != null) nameText.text = "？？？？？";

            SetBoxContent(frameBox1, descriptionText1, "【未確認の生物】\nまだ出会ったことのない生物だ。この先のエリアに生息しているらしい……。");
            SetBoxContent(frameBox2, descriptionText2, null);
            SetBoxContent(frameBox3, descriptionText3, null);

            if (newBadgeObject != null) newBadgeObject.SetActive(false);
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
        if (textComp != null)
        {
            textComp.gameObject.SetActive(hasContent);
            textComp.text = hasContent ? content : "";
        }
    }

    void PlayFlipSound()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);
    }
}