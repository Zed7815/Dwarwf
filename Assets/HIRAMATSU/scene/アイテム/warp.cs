//using UnityEngine;

//public class warp : MonoBehaviour
//{
//    [Header("ワープ設定")]
//    [Tooltip("ワープ先の出口")]
//    public Transform warpExitTarget;

//    [Tooltip("ワープ先の自動検索用タグ(デフォルトはWarpExit)")]
//    public string warpTag = "WarpExit";

//    [Tooltip("一同使用したときのクールタイム")]
//    public float warpCooldown = 0.5f;

//    [Tooltip("ワープ時に発生させるエフェクト（任意）")]
//    public GameObject warpEffectPrefab;

//    private float lastWarpTime = -999f;

//    private void Start()
//    {
//        // ワープの出口が未設定時の場合に自動取得
//        if (warpExitTarget == null)
//        {
//            FindAvailableExit();
//        }
//    }

//    private void FindAvailableExit()
//    {
//        // WarpExitコンポーネントを持つオブジェクトをシーンから探す

//        WarpExit exit = FindObjectOfType<WarpExit>();
//        if (exit != null)
//        {
//            warpExitTarget = exit.transform;
//            return;
//        }

//        // タグで検
//        GameObject exitObj = GameObject.FindWithTag(exitTag);
//        if (exitObj != null)
//        {
//            warpExitTarget = exitObj.transform;
//        }
//    }
//    private void nTriggerEnter2D(Collider2D collision)
//    {
//        TryWarp(collision.gameObject);
//    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        TryWarp(collision.gameObject);
//    }

//    private void TryWarp(GameObject targetObj)
//    {
//        if (targetObj.CompareTag("Player"))
//        {
//            // クールダウン中はワープしない
//            if (Time.time - lastWarpTime < warpCooldown) return;

//            if (warpExitTarget == null) FindAvailableExit();
            
//            if (warpExitTarget != null)
//            {
//                lastWarpTime = Time.time;

//                // ワープ元の位置にエフェクト
//                if (warpEffectPrefab != null)
//                {
//                    Instantiate(warpEffectPrefab, targetObj.transform.position, Quaternion.identity);
//                }

//                // 転送
//                WarpExit exitCompnent = warpExitTarget.GetComponent<WarpExit>();
//                Vector3 destPos = warpExitTarget.position;

//                if (exitCompnent != null)
//                {
//                    // 地面埋まり防止
//                    destPos += (Vector3)exitCompnent.spawnOffset;
//                    exitCompnent.OnWarpedIn(); // 出口側の演出
//                }

//                // プレイヤーの座標を書き換え
//                targetObj.transform.position = destPos;

//                // ワープ先にもエフェクトを生成
//                if (warpEffectPrefab != null)
//                {
//                    Instantiate(warpEffectPrefab, destPos, Quaternion.identity);
//                }

//            }
//        }
//    }
//}
