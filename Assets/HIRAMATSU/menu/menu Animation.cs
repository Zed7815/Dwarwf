using UnityEngine;
using System.Collections;

public class menuAnimation : MonoBehaviour
{
    public float startY = 10f;//目標地点
    public float duration = 0.5f;//何秒で目標地点にいけるかの秒数

    private float targetY = 0f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.localPosition;

        Vector3 startPosition = targetPosition;
        startPosition.y = startY;

        transform.localPosition = startPosition;
    }

    public void menu_Drop()//上から下にメニューが降りてくるアニメーション
    {
        StartCoroutine(Drop());
    }
    public void menu_up()//舌から上にメニューが上がっていくアニメーション
    {
        StartCoroutine(Up());
    }
    IEnumerator Drop()
    {
        Vector3 startPosition = transform.localPosition;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition =
                Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        transform.localPosition = targetPosition;
    }
    IEnumerator Up()
    {
        Vector3 startPosition = transform.localPosition;

        // 戻る位置
        Vector3 endPosition = targetPosition;
        endPosition.y = startY;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition =
                Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        transform.localPosition = endPosition;
    }
}
