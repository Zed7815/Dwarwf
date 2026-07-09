using System.Collections;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;
    public Transform pointA;
    public Transform pointB;

    [Header("出現・移動タイミング")]
    [Tooltip("ブロックが合計何個設置されたら、このオブジェクトを表示して動かすか")]
    public int requiredPlacedCount = 0;
    public float startDelay = 0.5f; // 出現してから動き出すまでのタメ
    public float speed = 2.0f;

    private SpriteRenderer sr;
    private Coroutine tutorialRoutine;
    private bool isMoveFinished = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (gameManager == null) gameManager = GameManager.instance;

        // 最初は位置をAにして、透明にしておく
        if (pointA != null) transform.position = pointA.position;
        SetAlpha(0f);

        RestartTutorial();
    }

    void Update()
    {
        if (gameManager == null || gameManager.blockManager == null) return;

        // 現在の設置数を確認
        int currentCount = GetCurrentPlacedCount();

        // 【表示条件】
        // 1. 編集モードである
        // 2. 設置数がこのオブジェクトの指定数とピッタリ一致している
        // 3. まだ移動演出が終わっていない（移動完了したら消す場合）
        bool shouldShow = gameManager.currentState == GameManager.GameState.Edit &&
                          currentCount == requiredPlacedCount &&
                          !isMoveFinished;

        SetAlpha(shouldShow ? 0.6f : 0f);
    }

    IEnumerator TutorialSequence()
    {
        isMoveFinished = false;
        if (pointA != null) transform.position = pointA.position;

        // 指定の数になるまで待機
        while (GetCurrentPlacedCount() < requiredPlacedCount)
        {
            yield return null;
        }

        // 指定の数になった瞬間（＝設置された瞬間）
        if (GetCurrentPlacedCount() == requiredPlacedCount)
        {
            // 少し待ってから移動開始
            yield return new WaitForSeconds(startDelay);

            while (pointB != null && Vector3.Distance(transform.position, pointB.position) > 0.05f)
            {
                // 移動中にさらにブロックが置かれたら即終了
                if (GetCurrentPlacedCount() != requiredPlacedCount) break;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    pointB.position,
                    speed * Time.deltaTime
                );
                yield return null;
            }
        }

        // 移動が終わった、または次のブロックが置かれたら終了
        isMoveFinished = true;
    }

    int GetCurrentPlacedCount()
    {
        // ステージ上の「PlacedBlock」タグの数を数える
        GameObject[] placedBlocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        return placedBlocks.Length;
    }

    void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            if (c.a != alpha)
            {
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    void RestartTutorial()
    {
        if (tutorialRoutine != null) StopCoroutine(tutorialRoutine);
        tutorialRoutine = StartCoroutine(TutorialSequence());
    }

    // リセットボタンが押された時
    void OnGimmickReset()
    {
        RestartTutorial();
    }
}