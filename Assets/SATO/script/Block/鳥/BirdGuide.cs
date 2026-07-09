using UnityEngine;

public class BirdGuide : MonoBehaviour
{
    private LineRenderer line;
    public LayerMask obstacleLayer;
    public float maxDistance = 20f;

    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            line.positionCount = 0;
            return;
        }

        // 右方向か左方向か（鳥の向きで判定）
        float dir = transform.parent.localScale.x > 0 ? 1f : -1f;
        Vector2 rayDir = new Vector2(dir, 0);
        Vector2 startPos = transform.parent.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos + rayDir * 1.5f, rayDir, maxDistance, obstacleLayer);

        if (hit.collider != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, startPos);
            line.SetPosition(1, new Vector3(hit.point.x, startPos.y, 0));
        }
        else
        {
            line.positionCount = 0;
        }
    }
  }
