using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings instance;

    [Header("音量スライダー（1本化）")]
    public Slider masterVolumeSlider;

    public static float CurrentVolume { get; private set; } = 0.8f;

    void Awake()
    {
        instance = this;

        // 保存された音量をロード（初回は0.8）
        CurrentVolume = PlayerPrefs.GetFloat("SavedMasterVolume", 0.8f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
        }

        // ゲーム全体の音量を一発適用
        AudioListener.volume = CurrentVolume;
    }

    void OnEnable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = CurrentVolume;
        }
    }

    // ★スライダーを動かした時に呼ばれる関数（これ1つで全部動きます）
    public void OnVolumeChanged(float dummy)
    {
        if (masterVolumeSlider != null)
        {
            CurrentVolume = masterVolumeSlider.value;
        }

        // ★Unity全体（BGMもSEも全部）の音量を一発変更！
        AudioListener.volume = CurrentVolume;

        PlayerPrefs.SetFloat("SavedMasterVolume", CurrentVolume);
        PlayerPrefs.Save();

        Debug.Log($"<color=green>【全体音量】{Mathf.RoundToInt(CurrentVolume * 100)}% に設定しました</color>");
    }
}