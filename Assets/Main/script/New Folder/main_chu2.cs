using System.Collections;
using UnityEngine;
using static main_GameManager;

public class main_chu2 : MonoBehaviour
{
    public main_GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    bool waiting = false;
    SpriteRenderer sr;
    public Transform pointA;
    public Transform pointB;
    public float moveTime = 3f; // A→Bにかかる秒数
    public bool col;
    public bool start;
    public bool end;
   void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Color color = sr.color;
    }
        void Update()
        {
            if(!end)
            {
            action();
            blockManager();
            collar();
            }
        }
        void collar()
        {
            if (col)
            {
                Color resetColor = sr.color;
                resetColor.a = 0f;
                sr.color = resetColor;
            }
            else if (!col)
            {
                Color resetColor = sr.color;
                resetColor.a = 1f;
               sr.color = resetColor;
            }
        }
        void blockManager()
        {
            if (!start)
            {
                if (gameManager.blockManager != null && !gameManager.blockManager.IsAllBlocksPlaced())
                {
                    col = true;
                }
                else
                {
                    col = false;
                }
            }
        }
        void action()
        {
            float t = Mathf.PingPong(Time.time / moveTime, 1f);
            transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
        }
        
}
