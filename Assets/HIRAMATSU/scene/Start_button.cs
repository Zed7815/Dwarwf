using UnityEngine;

public class Start_button : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("左クリック");
        }
    }

    void OnMouseDown()
    {
        Debug.Log("ボタンがクリックされた");
    }
}