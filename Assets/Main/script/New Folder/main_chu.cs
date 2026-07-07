using System.Collections;
using UnityEngine;

public class TutorialMove : MonoBehaviour
{
    public main_GameManager gameManager;
    public Transform pointA;
    public Transform pointB;
    public float speed = 1f;
    public float moveTime = 3f; // A→Bにかかる秒数
    bool waiting = false;
    SpriteRenderer sr;
    public main_chu2 chu2;
    void Start()
    {
        transform.position = pointA.position;
        sr = GetComponent<SpriteRenderer>();
        Color color = sr.color;
    }

    void Update()
    {
        if (waiting) return;
        if (gameManager.blockManager != null && !gameManager.blockManager.IsAllBlocksPlaced())
        {
            Color resetColor = sr.color;
            resetColor.a = 0.5f;
            sr.color = resetColor;
            transform.position = Vector3.MoveTowards(
            transform.position,
            pointB.position,
            speed * Time.deltaTime
        );

            if (Vector3.Distance(transform.position, pointB.position) < 0.01f)
            {
                StartCoroutine(ResetPosition());
            }
        }
        else
        {
            Color resetColor = sr.color;
            resetColor.a = 0f;
            sr.color = resetColor;
        }
    }

    IEnumerator ResetPosition()
    {
        waiting = true;

            yield return new WaitForSeconds(1f);


        transform.position = pointA.position;
        waiting = false;
    }
}