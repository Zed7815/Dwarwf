using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BookEncyclopediaUI : MonoBehaviour
{
    [System.Serializable]
    public class AnimalPage
    {
        public string animalName;       // 動物・ギミックの名前
        public Sprite illustration;     // 左ページに表示する大きな絵
        [TextArea(3, 8)]
        public string description;      // 右ページの説明文
    }

    [Header("図鑑データ集（ページ順）")]
    public List<AnimalPage> pages;

    [Header("UI部品の参照")]
    public TextMeshProUGUI nameText;         // 右ページ：名前
    public TextMeshProUGUI descriptionText;  // 右ページ：解説
    public Image illustrationImage;          // 左ページ：イラスト
    public TextMeshProUGUI pageNumberText;   // ページ番号 (例: 1 / 5)

    [Header("ページめくりボタン")]
    public Button prevButton; // ◀
    public Button nextButton; // ▶

    [Header("SE")]
    public AudioSource audioSource;
    public AudioClip pageFlipSE; // ペラッ（紙をめくる音）

    private int currentPageIndex = 0;

    void OnEnable()
    {
        // 図鑑を開いた時は最初のページから
        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    // 次のページ ▶
    public void NextPage()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            PlayFlipSound();
            UpdatePageDisplay();
        }
    }

    // ◀ 前のページ
    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            PlayFlipSound();
            UpdatePageDisplay();
        }
    }

    // 画面の更新
    void UpdatePageDisplay()
    {
        if (pages == null || pages.Count == 0) return;

        AnimalPage page = pages[currentPageIndex];

        if (nameText != null) nameText.text = page.animalName;
        if (descriptionText != null) descriptionText.text = page.description;
        if (illustrationImage != null)
        {
            illustrationImage.sprite = page.illustration;
            illustrationImage.preserveAspect = true; // 比率を維持
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {pages.Count}";
        }

        // 端のページではボタンを押せなくする
        if (prevButton != null) prevButton.interactable = (currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentPageIndex < pages.Count - 1);
    }

    void PlayFlipSound()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);
    }
}