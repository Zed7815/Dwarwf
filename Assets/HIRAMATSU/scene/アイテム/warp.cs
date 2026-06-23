using UnityEngine;

public class warp : MonoBehaviour
{
    public GameObject GameObject;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(CompareTag("Player"))
        {
            GameObject.transform.position = collision.transform.position;
        }
    }
}
