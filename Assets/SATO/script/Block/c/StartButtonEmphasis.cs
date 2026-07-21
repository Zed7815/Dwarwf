using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StartButtonEmphasis : MonoBehaviour
{
    private Vector3 initialScale;
    private bool isEmphasizing = false;
    private Coroutine currentRoutine;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        // 全ブロック配置済みかチェック
        bool allPlaced = GameManager.instance.blockManager.IsAllBlocksPlaced();

        if (allPlaced && !isEmphasizing)
        {
            StartEmphasis();
        }
        else if (!allPlaced && isEmphasizing)
        {
            StopEmphasis();
        }
    }

    void StartEmphasis()
    {
        isEmphasizing = true;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(EmphasisRoutine());
    }

    void StopEmphasis()
    {
        isEmphasizing = false;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        transform.localScale = initialScale;
    }

    IEnumerator EmphasisRoutine()
    {
        while (isEmphasizing)
        {
            // ポヨンポヨンと拡大縮小させる演出
            float t = Mathf.PingPong(Time.time * 2f, 0.2f);
            transform.localScale = initialScale * (1f + t);
            yield return null;
        }
    }
}