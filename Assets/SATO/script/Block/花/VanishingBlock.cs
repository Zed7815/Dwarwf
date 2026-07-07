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
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip touchSE;       // 触れた瞬間の音
    public AudioClip vanishSE;      // 消滅する瞬間の音

    private bool isTouched = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;

    void Start()
    {
        // コンポーネントの自動取得
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!isTouched)
            {
                isTouched = true;

                // ★SE再生：触れた瞬間
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
            yield return new WaitForSeconds(vanishDelay);
        }

        // --- 消滅処理 ---

        // ★SE再生：消滅する瞬間
        if (audioSource != null && vanishSE != null)
        {
            audioSource.PlayOneShot(vanishSE);
        }

        // 当たり判定と見た目を先に消す（オブジェクトはまだ壊さない）
        if (blockCollider != null) blockCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 音が鳴り終わるまで少し待つ（vanishSEの長さ分待機）
        float waitTime = (vanishSE != null) ? vanishSE.length : 0.1f;
        yield return new WaitForSeconds(waitTime);

        // ヒエラルキーから完全に消去
        //Destroy(gameObject);
        gameObject.SetActive(false);
    }

    // スクリプト内のどこでも良いので追記
    void OnGimmickReset()
    {
        StopAllCoroutines();

        isTouched = false;
    }

}