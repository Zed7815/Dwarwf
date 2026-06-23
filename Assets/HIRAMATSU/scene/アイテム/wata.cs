using UnityEngine;

public class wata : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    { 
        if(CompareTag("Player"))
        {
            Destroy(collision.gameObject);  
        }
    }
}
