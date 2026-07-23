using UnityEngine;
using System.Collections;
public class main_chu4 : MonoBehaviour
{
    public main_GameManager gameManager;
    public float rotateTime = 1f;
    public GameObject b;
    private bool rotating = false;
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
}
