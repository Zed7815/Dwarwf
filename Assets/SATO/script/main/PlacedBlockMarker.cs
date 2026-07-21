using UnityEngine;

public class PlacedBlockMarker : MonoBehaviour
{
    private Vector3 placedPosition;
    private Quaternion placedRotation;

    // 配置された瞬間にこれを呼んで位置を覚えさせる
    public void SavePlacedState()
    {
        placedPosition = transform.position;
        placedRotation = transform.rotation;
    }

    // リセット時に呼ばれる
    public void OnGimmickReset()
    {
        transform.position = placedPosition;
        transform.rotation = placedRotation;

        // 物理挙動が残らないように停止させる
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
    }
}