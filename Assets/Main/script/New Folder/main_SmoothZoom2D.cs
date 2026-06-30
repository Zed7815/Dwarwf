using UnityEngine;

public class SmoothZoom2D : MonoBehaviour
{
    public Camera cam;
    public float zoomSpeed = 5f;
    public float smoothSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    float targetZoom;

    void Start()
    {
        targetZoom = cam.orthographicSize;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            Time.deltaTime * smoothSpeed
        );
    }
}

