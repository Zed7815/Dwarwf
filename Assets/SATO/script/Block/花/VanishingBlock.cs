using System.Collections;
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

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip touchSE;
    public AudioClip vanishSE;

    private bool isTouched = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isTouched)
            {
                isTouched = true;
                if (audioSource != null && touchSE != null)
                {
                    audioSource.PlayOneShot(touchSE);
                }
                StartCoroutine(VanishSequence());
            }
        }
    }

    private IEnumerator VanishSequence()
    {
        // 1. アニメーションフェーズ
        if (animator != null)
        {
            if (useDirectPlay && !string.IsNullOrEmpty(vanishStateName))
            {
                animator.Play(vanishStateName);
            }
            else if (!string.IsNullOrEmpty(vanishTriggerParam))
            {
                animator.SetTrigger(vanishTriggerParam);
            }
        }

        // 2. フォールバック（Animatorがない場合のフリッカー）
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
            yield return new WaitForSeconds(vanishDelay);
        }

        // --- 消滅処理 ---
        if (audioSource != null && vanishSE != null)
        {
            audioSource.PlayOneShot(vanishSE);
        }

        // 当たり判定と見た目を消す
        if (blockCollider != null) blockCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 音が鳴り終わるまで少し待つ
        float waitTime = (vanishSE != null) ? vanishSE.length : 0.1f;
        yield return new WaitForSeconds(waitTime);

        // ★修正ポイント：SetActive(false) は使わない
        // オブジェクトを非活性にするとリセット命令(SendMessage)が届かなくなるため、
        // ColliderとRendererをOFFにした状態で「シーンに残しておく」のが正解です。
        // gameObject.SetActive(false); 
    }

    // ★リセット機能を追加
    void OnGimmickReset()
    {
        // 1. 進行中の消滅処理を止める
        StopAllCoroutines();

        // 2. フラグを戻す
        isTouched = false;

        // 3. 見た目と当たり判定を復活させる
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (blockCollider != null) blockCollider.enabled = true;

        // 4. アニメーターを初期状態に巻き戻す
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // 5. 音を止める
        if (audioSource != null) audioSource.Stop();
    }
}