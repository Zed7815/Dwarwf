using UnityEngine;
using UnityEngine.InputSystem; // マウス判定のために追加
using System.Collections;

public class TutorialSignSequence : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;
    public GameObject signA;
    public GameObject signB;
    public GameObject signC;

    [Header("回転設定")]
    public float rotateTime = 0.5f;
    public Vector3 fallAmount = new Vector3(-90, 0, 0);
    private Vector3 riseAmount = Vector3.zero;

    private int currentPhase = 0;
    private Quaternion initRotA, initRotB, initRotC;
    private int lastConfirmedCount = 0;

    void Start()
    {
        if (gameManager == null) gameManager = GameManager.instance;

        initRotA = signA.transform.rotation;
        initRotB = signB.transform.rotation;
        initRotC = signC.transform.rotation;

        // BとCは初期状態で倒しておく
        signB.transform.rotation = initRotB * Quaternion.Euler(fallAmount);
        signC.transform.rotation = initRotC * Quaternion.Euler(fallAmount);
    }

    private void Update()
    {
        if (gameManager == null || gameManager.currentState != GameManager.GameState.Edit) return;

        // ★修正ポイント1：マウスが押されている間（ドラッグ中）は判定しない
        if (Mouse.current.leftButton.isPressed) return;

        // 正式に設置されたブロックの数を取得
        int placedCount = GetConfirmedPlacedCount();

        // ★修正ポイント2：前回確認した数より増えた瞬間だけ処理を行う
        if (placedCount > lastConfirmedCount)
        {
            if (placedCount == 1 && currentPhase == 0)
            {
                currentPhase = 1;
                StartCoroutine(SwitchSigns(signA, signB));
            }
            else if (placedCount == 2 && currentPhase == 1)
            {
                currentPhase = 2;
                StartCoroutine(SwitchSigns(signB, signC));
            }

            lastConfirmedCount = placedCount;
        }
    }

    IEnumerator SwitchSigns(GameObject oldSign, GameObject nextSign)
    {
        float elapsed = 0;

        // 1. 前の看板を倒す
        Quaternion startRotOld = oldSign.transform.rotation;
        Quaternion endRotOld = Quaternion.Euler(fallAmount);

        while (elapsed < rotateTime)
        {
            elapsed += Time.deltaTime;
            oldSign.transform.rotation = Quaternion.Lerp(startRotOld, endRotOld, elapsed / rotateTime);
            yield return null;
        }
        oldSign.transform.rotation = endRotOld;

        yield return new WaitForSeconds(0.2f); // 設置完了の余韻

        // 2. 次の看板を立ち上げる
        elapsed = 0;
        Quaternion startRotNext = nextSign.transform.rotation;
        Quaternion endRotNext = Quaternion.Euler(riseAmount);

        while (elapsed < rotateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateTime;
            // イージング：最後少しだけ行き過ぎて戻るような動き
            float bounceT = t * t * (3f - 2f * t);

            nextSign.transform.rotation = Quaternion.Lerp(startRotNext, endRotNext, bounceT);
            yield return null;
        }
        nextSign.transform.rotation = endRotNext;
    }

    // ★修正ポイント3：ドラッグ中のブロックを無視して数える
    int GetConfirmedPlacedCount()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        int confirmedCount = 0;

        foreach (GameObject b in blocks)
        {
            Collider2D col = b.GetComponent<Collider2D>();
            // BlockManager.csで、ドラッグ中のブロックは collider.enabled = false になっているため、
            // enabled が true のものだけを「正式な設置済み」としてカウントする
            if (col != null && col.enabled)
            {
                confirmedCount++;
            }
        }
        return confirmedCount;
    }

    void OnGimmickReset()
    {
        StopAllCoroutines();
        currentPhase = 0;
        lastConfirmedCount = 0; // ここもしっかりリセット

        signA.transform.rotation = initRotA;
        signB.transform.rotation = initRotB * Quaternion.Euler(fallAmount);
        signC.transform.rotation = initRotC * Quaternion.Euler(fallAmount);
    }
}