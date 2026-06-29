using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class VanishingBlock : MonoBehaviour
{
    [Header("演出")]
    [Tooltip("プレイヤーが触れてからブロックが消滅するまでの時間（秒）")]
    public float vanishDelay = 1.0f;

    [Tooltip("Animatorコンポーネント（未設定時は自身から自動取得）")]
    public Animator animator;
    [Tooltip("消滅アニメーションを開始するAnimatorのTriggerパラメータ名")]
    public string vanishTriggerParam = "Vanish";
    [Tooltip("AnimatorControllerの遷移設定(矢印)を無視し、C#から直接アニメーションを再生する場合はON")]
    public bool useDirectPlay = false;
    [Tooltip("直接再生する場合のアニメーションステート名（useDirectPlayがONの時のみ有効）")]
    public string vanishStateName = "VanishState";

    [Header("ビジュアル・代替演出（Animatorが無い場合）")]
    [Tooltip("Animatorが無い場合に、消滅の直前に点滅（警告フリッカー）演出を行うか")]
    public bool useFlickerEffect = false;

    private bool isTouched = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;

    void Start()
    {
        // コンポーネントの自動取得
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isTouched)
            {
                isTouched = true;
                StartCoroutine(VanishSequence());
            }
        }

    }

    private IEnumerator VanishSequence()
    {
        // 1. アニメーションフェーズ
        // 待機(Idol)状態から、消滅(Vanish)アニメーションをトリガー/再生
        if (animator != null)
        {
            if (useDirectPlay && !string.IsNullOrEmpty(vanishStateName))
            {
                animator.Play(vanishStateName); // 矢印なしで直接再生
            }
            else if (!string.IsNullOrEmpty(vanishTriggerParam))
            {
                animator.SetTrigger(vanishTriggerParam); // トリガー送信
            }
        }

        // 2. フォールバック
        if (animator == null && useFlickerEffect && spriteRenderer != null)
        {
            float elapsed = 0f;
            float flickerInterval = 0.1f;
            while (elapsed < vanishDelay)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(flickerInterval);
                elapsed += flickerInterval;
            }
            spriteRenderer.enabled = false;
        }
        else
        {
            // アニメーションが再生されている間（指定秒数）は、そのまま待機
            yield return new WaitForSeconds(vanishDelay);
        }

        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        // ヒエラルキーから完全に消去
        Destroy(gameObject);
    }
}

