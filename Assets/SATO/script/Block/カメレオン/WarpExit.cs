using UnityEngine;

public class WarpExit : MonoBehaviour
{
    [Header("出現位置の補正")]
    [Tooltip("ワープしてきた際、床埋まり防止のため、少し上がベスト")]
    public Vector2 spawnOffset = new Vector2(0f, 0.5f);

    [Header("演出")]
    [Tooltip("到着した際のエフェクト")]
    public GameObject arrivalEffectPrefab;

    [Tooltip("アニメーション")]
    public string warpInTrigger = "OnWarp";

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (!gameObject.CompareTag("WarpExit"))
        {
            gameObject.tag = "WarpExit";
        }
    }

    // 入り口から転送が完了した瞬間に呼び出される
    public void OnWarpedIn()
    {
        if (arrivalEffectPrefab != null)
        {
            Instantiate(arrivalEffectPrefab,transform.position + (Vector3)spawnOffset, Quaternion.identity);
        }

        if (anim != null && !string.IsNullOrEmpty(warpInTrigger))
        {
            anim.SetTrigger(warpInTrigger);
        }
    }

}
