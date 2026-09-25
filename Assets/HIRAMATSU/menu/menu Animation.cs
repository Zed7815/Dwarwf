using UnityEngine;
using System.Collections;

public class menuAnimation : MonoBehaviour
{
    public float startY = 800f;
    public float targetY = 0f;
    public float duration = 0.5f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.localPosition;

        Vector3 startPosition = targetPosition;
        startPosition.y = startY;

        transform.localPosition = startPosition;
    }

    public void menu()
    {
        StartCoroutine(Drop());
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
}
