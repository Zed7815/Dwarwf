using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FinalStageLock : MonoBehaviour
{
    [Header("İ’è")]
    public int requiredStars = 5;
    public string finalStageSceneName;
    public TextMeshProUGUI needStarText;

    [Header("‰‰o—p")]
    public GameObject lockGraphic;

    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        RefreshLockStatus();
    }

    public void RefreshLockStatus()
    {
        int currentStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) currentStars++;
        }

        bool isUnlocked = (currentStars >= requiredStars);

        if (needStarText != null)
        {
            if (isUnlocked) needStarText.text = "5 / 5";
            else needStarText.text = currentStars + " / " + requiredStars;
        }

        if (lockGraphic != null) lockGraphic.SetActive(!isUnlocked);

        if (btn != null)
        {
            btn.interactable = isUnlocked;
            btn.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                btn.onClick.AddListener(() => {
                    // StageSelectManager‚Ì‰¹•t‚«ˆÚ“®‚ğŒÄ‚Ô
                    StageSelectManager ssm = FindObjectOfType<StageSelectManager>();
                    if (ssm != null) ssm.LoadStage(finalStageSceneName);
                    else UnityEngine.SceneManagement.SceneManager.LoadScene(finalStageSceneName);
                });
            }
        }
    }
}