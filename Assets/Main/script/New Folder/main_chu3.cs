using UnityEngine;
using System.Collections;

public class main_chu3 : MonoBehaviour
{
    public main_GameManager gameManager;
    public float rotateTime = 1f;
    public GameObject b;
    public GameObject c; //もう一枚の看板
    private bool rotating = false;

    private void Update()
    {
        if (!rotating &&
            gameManager.blockManager != null &&
            gameManager.blockManager.IsAllBlocksPlaced())
        {
            StartCoroutine(RotateRoutine());
            StartCoroutine(RotateRoutine2());
        }
    }

    IEnumerator RotateRoutine()
    {
        rotating = true;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90, 0, 0);

        float t = 0;

        while (t < rotateTime)
        {
            t += Time.deltaTime;

            transform.rotation = Quaternion.Lerp(
                startRot,
                endRot,
                t / rotateTime);

            yield return null;
        }
        //b.transform.position = new Vector3(-5, -4.5f, 0);
        transform.rotation = endRot;
        
    }
    IEnumerator RotateRoutine2()
    {
        rotating = true;

        Quaternion startRot = c.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90, 0, 0);

        float t = 0;

        while (t < rotateTime)
        {
            t += Time.deltaTime;

            c.transform.rotation = Quaternion.Lerp(
                startRot,
                endRot,
                t / rotateTime);

            yield return null;
        }
        //b.transform.position = new Vector3(-5, -4.5f, 0);
        c.transform.rotation = endRot;

    }
}