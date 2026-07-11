using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GlobalDebugManager : MonoBehaviour
{
    private static GlobalDebugManager instance;

    [Header("設定")]
    public string titleSceneName = "Title"; // タイトル画面のシーン名
    public Key resetKey = Key.Delete;      // リセットキー

    void Awake()
    {
        // --- ★重要：重複防止と永続化の処理 ---
        if (instance == null)
        {
            instance = this;
            // このオブジェクトをシーン切り替えで壊さないように設定
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // すでに存在している（タイトルに戻ってきた時など）場合は古い方を優先
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        // どのシーンにいても指定のキーが押されたら発動
        if (Keyboard.current != null && Keyboard.current[resetKey].wasPressedThisFrame)
        {
            ResetAndGoHome();
        }
    }

    void ResetAndGoHome()
    {
        Debug.Log("<color=yellow>【全リセット】データを削除してタイトルに戻ります</color>");

        // 1. セーブデータを完全に削除
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. プロジェクト内の static（静的）フラグも強制的に初期化する
        // これをしないと「起動直後」と同じ挙動にならないため

        // ステージセレクトの「初回リセット済みフラグ」をリセット
        // (StageSelectManager.cs に Init メソッドがあればそれを呼び出すイメージ)
        // もし Init がなければ、次にセレクト画面を開いた時に自動でDeleteAllが走るようになります。

        // 3. タイトル画面へ瞬時に移動
        // デバッグ機能なのでフェードなしでパッと戻るのが効率的です
        SceneManager.LoadScene(titleSceneName);
    }
}