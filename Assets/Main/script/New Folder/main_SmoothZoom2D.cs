using UnityEngine;
using UnityEngine.InputSystem;

public class MouseZoom2D : MonoBehaviour
{
    public Camera cam;
    public float zoomSpeed = 0.01f;
    public float minZoom = 2f;
    public float maxZoom = 10f;
    void Update()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll == 0) return;

        // ズーム前のマウス位置のワールド座標
        Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldBefore.z = 0;

        // ズーム
        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        // ズーム後のマウス位置のワールド座標
        Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldAfter.z = 0;

        // 差分だけカメラを移動
        cam.transform.position += mouseWorldBefore - mouseWorldAfter;
                
    }

    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

    void LateUpdate()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}