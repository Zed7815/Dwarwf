using UnityEngine;

public class PlacedBlockMarker : MonoBehaviour
{
    private Vector3 placedPosition;
    private Quaternion placedRotation;

    public void SavePlacedState()
    {
        placedPosition = transform.position;
        placedRotation = transform.rotation;
    }

    // リセット時に初期位置へ戻す
    public void OnGimmickReset()
    {
        transform.position = placedPosition;
        transform.rotation = placedRotation;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
    }
}