using UnityEngine;
using System.Collections;

public class TutorialDemoPlayer : MonoBehaviour
{
    [Header("参照")]
    public Player_walk playerWalk;
    public Transform demoStartPoint;  // デモが歩き始める位置（左側）
    public Transform gimmickTarget;   // ギミックの位置
    public Transform puzzleAreaStart; // パズルエリアが始まる境界線

    [Header("演出設定")]
    public float walkSpeed = 3.0f;
    public bool skipDemoForSpring = false; // バネステージ等の場合はTrueにするとデモをスキップ

    private Rigidbody2D rb;

    void Start()
    {
        if (playerWalk == null) playerWalk = FindObjectOfType<Player_walk>();
        if (rb == null && playerWalk != null) rb = playerWalk.GetComponent<Rigidbody2D>();

        if (skipDemoForSpring)
        {
            // デモを飛ばしていいきなりパズルを始める場合
            EnablePuzzleMode();
            return;
        }

        // 開始時はパズルを禁止（自機を自動制御）
        StartCoroutine(DemoSequence());
    }

    IEnumerator DemoSequence()
    {
        if (playerWalk == null || demoStartPoint == null || gimmickTarget == null) yield break;

        // 1. 自機をデモ開始位置にワープさせ、操作を無効化
        playerWalk.transform.position = demoStartPoint.position;
        playerWalk.StateChange(0); // 停止

        // エディットUIやスタートボタンを隠す（ GameManager経由など）
        if (GameManager.instance != null)
        {
            if (GameManager.instance.startButton != null) GameManager.instance.startButton.SetActive(false);
            if (GameManager.instance.editUIController != null) GameManager.instance.editUIController.HideEditUI();
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // 2. ギミックの場所に向かってトコトコ歩く
        playerWalk.StateChange(1); // 歩行状態
        playerWalk.direction = 1;  // 右向き

        while (playerWalk.transform.position.x < gimmickTarget.position.x - 0.5f)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(walkSpeed, rb.linearVelocity.y);
            }
            yield return null;
        }

        // 3. ギミックの前でピタッと止まって見せる
        if (rb != null) rb.linearVelocity = Vector2.zero;
        playerWalk.StateChange(0); // 停止

        // （※ここで必要ならギミックの演出をここで一時的に呼ぶことも可能）
        yield return new WaitForSecondsRealtime(1.5f);

        // 4. さらに右の「パズルエリア（簡単ステージ）」へ歩いていく
        playerWalk.StateChange(1);
        while (playerWalk.transform.position.x < puzzleAreaStart.position.x)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(walkSpeed, rb.linearVelocity.y);
            }
            yield return null;
        }

        // 5. パズルエリアに到達！ここで編集タイム（Editモード）を解禁する
        EnablePuzzleMode();
    }

    void EnablePuzzleMode()
    {
        if (playerWalk != null)
        {
            // 自機を一度初期位置（パズル開始地点）に固定またはIdleにする
            if (rb != null) rb.linearVelocity = Vector2.zero;
            playerWalk.StateChange(0);
        }

        // エディットUI（ブロックパレット）とスタートボタンを出現させる
        if (GameManager.instance != null)
        {
            GameManager.instance.currentState = GameManager.GameState.Edit;
            if (GameManager.instance.startButton != null) GameManager.instance.startButton.SetActive(true);
            if (GameManager.instance.editUIController != null) GameManager.instance.editUIController.ShowEditUI();
        }

        Debug.Log("<color=green>【チュートリアル終了】パズルエディットタイム開始！</color>");
    }
}