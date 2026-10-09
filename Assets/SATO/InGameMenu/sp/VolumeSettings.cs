using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings instance;

    [Header("スライダー参照")]
    public Slider bgmSlider;
    public Slider seSlider;

    [Header("SE音量テスト用の効果音")]
    public AudioClip testSound; // スライダーをいじった時に「ピピッ」と鳴らす音
    private AudioSource audioSource;

    // ゲーム全体から参照できる音量変数
    public static float BGMVolume { get; private set; } = 0.7f;
    public static float SEVolume { get; private set; } = 0.8f;

    void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // セーブデータから前回の音量をロード（初回ならデフォルト値）
        BGMVolume = PlayerPrefs.GetFloat("SavedBGMVolume", 0.7f);
        SEVolume = PlayerPrefs.GetFloat("SavedSEVolume", 0.8f);
    }

    void Start()
    {
        // スライダーの初期位置をセーブデータに合わせる
        if (bgmSlider != null)
        {
            bgmSlider.value = BGMVolume;
            // スライダーを動かした時のイベントを登録
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }

        if (seSlider != null)
        {
            seSlider.value = SEVolume;
            seSlider.onValueChanged.AddListener(OnSEChanged);
        }

        // 開始時にBGM音量を適用
        ApplyBGMVolume();
    }

    // --- BGMスライダーが動いた時 ---
    public void OnBGMChanged(float value)
    {
        BGMVolume = value;
        PlayerPrefs.SetFloat("SavedBGMVolume", BGMVolume);
        ApplyBGMVolume();
    }

    // --- SEスライダーが動いた時 ---
    public void OnSEChanged(float value)
    {
        SEVolume = value;
        PlayerPrefs.SetFloat("SavedSEVolume", SEVolume);

        // 音量確認用に「ピピッ」とテスト音を鳴らす（連続再生制限付き）
        if (testSound != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.volume = SEVolume;
            audioSource.PlayOneShot(testSound);
        }
    }

    // シーン内のBGM再生コンポーネント（SimpleBGMManagerなど）に音量を反映
    void ApplyBGMVolume()
    {
        // SimpleBGMManagerを探して音量を適用
        SimpleBGMManager bgm = FindObjectOfType<SimpleBGMManager>();
        if (bgm != null)
        {
            var aSource = bgm.GetComponent<AudioSource>();
            if (aSource != null) aSource.volume = BGMVolume;
        }
    }
}