//using System.Collections;
//using System.Runtime.CompilerServices;
//using Unity.Mathematics;
//using UnityEngine;

//public class  : MonoBehaviour
//{
//    [Header("演出")]
//    [Tooltip("プレイヤーが触れてからブロックが消滅するまでの時間（秒）")]
//    pbulic float vanishDelay = 1.0f;
//    [Tooltip("消滅する直前に点滅演出を行うか")]
//    public bool useFlickerEffect = true;

//    private bool isTouched = false; 
//    private SpriteRenderer spriteRenderer;
//    private Collider2D blockCollider;

//    void Start()
//    {
//        // コンポーネントの自動取得
//        spriteRenderer = GetComponent<spriteRenderer>();
//        boolCollider = GetComponent<Collider2D>();
//    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        if(collision.gameObject.CompareTag("Player")
//        {
//            if (!isTouched)
//            {
//                isTouched = true;
//                StartCoroutine(vanishSequence());
//            }
//        }

//    }

//    private IEnumerator VanishSequence()
//    {
//        float elapsed = 0f;

//        // 消滅前の点滅演出
//        if (useFlickerEffect && spriteRenderer != null)
//        {
//            float2 flickerInterval = 0.1f;
//        }
//    }
//}

