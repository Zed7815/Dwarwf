using UnityEngine;

public class taki : MonoBehaviour
{
    public float speed = 2f;
    public float resetY = -20f;
    public float startY = 20f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y <= resetY)
        {
            transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        }
    }
}