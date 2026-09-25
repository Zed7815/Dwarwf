using UnityEngine;
using UnityEngine.SceneManagement;

public class Bookscenechanger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void OnMouseDown()
    {
        SceneManager.LoadScene("book");
    }

}