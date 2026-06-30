using UnityEngine;
using System.Collections;

public class main_chu2 : MonoBehaviour
{
    public main_GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    bool waiting = false;
    SpriteRenderer sr;
    public Transform pointA;
    public Transform pointB;
    public float moveTime = 3f; // A→Bにかかる秒数
    public bool a;

   void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Color color = sr.color;
    }
    void Update()
    {
        if(a)
        {
            Color resetColor = sr.color;
            resetColor.a = 1f;
            sr.color = resetColor;
            float t = Mathf.PingPong(Time.time / moveTime, 1f);

            transform.position = Vector3.Lerp(
                pointA.position,
                pointB.position,
                t
            );
        }
        else
        {
            Color resetColor = sr.color;
            resetColor.a = 0f;
            sr.color = resetColor;
        }
        StartCoroutine(U());
    }
        IEnumerator U()
        {
            if (gameManager.blockManager != null && !gameManager.blockManager.IsAllBlocksPlaced())
            {
                Color resetColor = sr.color;
                resetColor.a = 0f;
                sr.color = resetColor;
            yield return new WaitForSeconds(3f);
            a = true;
        }
            else
            {
            a = false;
                Color resetColor = sr.color;
                resetColor.a = 1f;
                sr.color = resetColor;
                float t = Mathf.PingPong(Time.time / moveTime, 1f);

                transform.position = Vector3.Lerp(
                    pointA.position,
                    pointB.position,
                    t
                );
            }
            
        }
    }
