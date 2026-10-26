using UnityEngine;
using System.Collections;

public class TutorialCameraMover : MonoBehaviour
{
    public Transform playerTransform;
    public float puzzleCameraX = 10f; // パズルエリアでのカメラのX座標
    public float smoothTime = 0.3f;

    private bool lockCameraToPuzzle = false;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (playerTransform == null)
        {
            Player_walk p = FindObjectOfType<Player_walk>();
            if (p != null) playerTransform = p.transform;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // 自機がパズルエリアの境界線（例: X = 5）を越えたら、カメラをパズルエリアの中心へ固定または追従させる
        if (!lockCameraToPuzzle && playerTransform.position.x > 3.0f)
        {
            lockCameraToPuzzle = true;
        }

        if (lockCameraToPuzzle)
        {
            Vector3 targetPos = new Vector3(puzzleCameraX, transform.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        }
    }
}