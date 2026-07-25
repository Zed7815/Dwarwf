using UnityEngine;

public class PlayerEditAnimator : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;
    public Animator playerAnimator;

    [Header("アニメーション設定")]
    [Tooltip("ドラッグ中にONにするAnimatorのBoolパラメータ名")]
    public string dragBoolParam = "isPointing";

    private BlockManager blockManager;

    void Start()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (gameManager == null) gameManager = GameManager.instance;

        if (gameManager != null)
        {
            blockManager = gameManager.blockManager;
        }
    }

    void Update()
    {
        // 1. 安全確認
        if (gameManager == null || blockManager == null || playerAnimator == null) return;

        // 2. 編集モード中のみ判定
        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            // BlockManager側でブロックを掴んでいるかチェック
            bool isDragging = blockManager.IsDragging;

            // Animatorのパラメータを更新
            // (これをAnimator側でLoop設定にしていればずっと動き続けます)
            playerAnimator.SetBool(dragBoolParam, isDragging);
        }
        else
        {
            // プレイモード（実行中）になったら強制的にOFFにする
            playerAnimator.SetBool(dragBoolParam, false);
        }
    }

    // リセット時にポーズが残らないようにするための安全策
    void OnGimmickReset()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(dragBoolParam, false);
        }
    }
}