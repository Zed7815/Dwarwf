using UnityEngine;
using System.Collections;

public class GimmickGhostPreview : MonoBehaviour
{
    public static GimmickGhostPreview instance;

    [Header("ゴースト自機の参照")]
    [Tooltip("半透明にした自機のオブジェクト（SpriteRenderer + Animator）")]
    public GameObject ghostPlayer;
    public SpriteRenderer ghostSpriteRenderer;
    public Animator ghostAnimator;

    [Header("透明度設定")]
    [Range(0.1f, 1f)]
    public float ghostAlpha = 0.5f; // 半透明の濃さ

    private GameObject currentHoveredBlock = null;
    private Coroutine currentDemoRoutine = null;
    private Camera mainCam;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;

        // ゴースト自機の初期化（半透明にして最初は消しておく）
        if (ghostPlayer != null)
        {
            if (ghostSpriteRenderer == null) ghostSpriteRenderer = ghostPlayer.GetComponentInChildren<SpriteRenderer>();
            if (ghostAnimator == null) ghostAnimator = ghostPlayer.GetComponentInChildren<Animator>();

            if (ghostSpriteRenderer != null)
            {
                Color c = ghostSpriteRenderer.color;
                c.a = ghostAlpha;
                ghostSpriteRenderer.color = c;
            }

            ghostPlayer.SetActive(false);
        }
    }

    void Update()
    {
        // エディットモード中のみ動作
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            StopPreview();
            return;
        }

        // ブロックをドラッグ中ならプレビューは消す
        if (GameManager.instance.blockManager != null && GameManager.instance.blockManager.IsDragging)
        {
            StopPreview();
            return;
        }

        // マウス位置にあるオブジェクトを検知
        CheckMouseHover();
    }

    void CheckMouseHover()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

        if (hit != null && (hit.CompareTag("PlacedBlock") || hit.GetComponentInParent<BlockInfo>() != null))
        {
            GameObject blockObj = hit.gameObject;

            // 新しいブロックにマウスが乗ったら実演開始
            if (currentHoveredBlock != blockObj)
            {
                currentHoveredBlock = blockObj;
                StartPreview(blockObj);
            }
        }
        else
        {
            // マウスが外れたら実演終了
            if (currentHoveredBlock != null)
            {
                StopPreview();
            }
        }
    }

    void StartPreview(GameObject block)
    {
        if (ghostPlayer == null) return;

        if (currentDemoRoutine != null) StopCoroutine(currentDemoRoutine);
        ghostPlayer.SetActive(true);

        // 動物の種類に合わせてミニ寸劇を分岐
        if (block.GetComponentInChildren<PlayerJumpBlock>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoJumpRoutine(block.transform));
        }
        else if (block.GetComponentInChildren<ReverseGimmick>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoReverseRoutine(block.transform));
        }
        else if (block.GetComponentInChildren<BirdCarrier>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoBirdRoutine(block.transform));
        }
        else if (block.GetComponentInChildren<LiftBlock>() != null || block.GetComponentInChildren<SpiderLift>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoLiftRoutine(block.transform));
        }
        else if (block.GetComponentInChildren<VanishingBlock>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoVanishRoutine(block.transform));
        }
        else
        {
            // その他：ブロックの上に立たせるだけ
            ghostPlayer.transform.position = block.transform.position + Vector3.up * 0.5f;
        }
    }

    void StopPreview()
    {
        currentHoveredBlock = null;
        if (currentDemoRoutine != null)
        {
            StopCoroutine(currentDemoRoutine);
            currentDemoRoutine = null;
        }
        if (ghostPlayer != null) ghostPlayer.SetActive(false);
    }

    // --- ① ネズミ（ジャンプ台）：ポンポン跳ねる実演 ---
    IEnumerator DemoJumpRoutine(Transform block)
    {
        Vector3 basePos = block.position + Vector3.up * 0.6f;

        while (true)
        {
            ghostPlayer.transform.position = basePos;
            if (ghostAnimator != null) ghostAnimator.SetBool("isWalk", false);
            yield return new WaitForSecondsRealtime(0.4f);

            // ジャンプ上昇
            float t = 0f;
            float jumpHeight = 1.8f;
            float duration = 0.6f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float progress = t / duration;
                // 放物線の動き
                float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                ghostPlayer.transform.position = basePos + new Vector3(progress * 1.2f, yOffset, 0);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    // --- ② ヘビ（反転）：来て、回って、戻る実演 ---
    IEnumerator DemoReverseRoutine(Transform block)
    {
        Vector3 centerPos = block.position + Vector3.up * 0.2f;

        while (true)
        {
            // 左から歩いてくる
            ghostPlayer.transform.localScale = new Vector3(1, 1, 1);
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = Vector3.Lerp(centerPos + Vector3.left * 1.2f, centerPos, t / 0.4f);
                yield return null;
            }

            // クルッと反転！
            yield return new WaitForSecondsRealtime(0.2f);
            ghostPlayer.transform.localScale = new Vector3(-1, 1, 1); // 向き反転
            yield return new WaitForSecondsRealtime(0.2f);

            // 左へ歩いて戻っていく
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = Vector3.Lerp(centerPos, centerPos + Vector3.left * 1.2f, t / 0.4f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    // --- ③ 鳥：ぶら下がって飛んでいく実演 ---
    IEnumerator DemoBirdRoutine(Transform block)
    {
        Vector3 startPos = block.position + new Vector3(0, -0.7f, 0);

        while (true)
        {
            ghostPlayer.transform.position = startPos;
            yield return new WaitForSecondsRealtime(0.3f);

            float t = 0f;
            float flyDist = 2.5f;
            float duration = 0.8f;

            // 前方へスーッと運ばれていく
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float progress = t / duration;
                ghostPlayer.transform.position = startPos + new Vector3(progress * flyDist, 0, 0);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    // --- ④ クモ（リフト）：上下に揺れる実演 ---
    IEnumerator DemoLiftRoutine(Transform block)
    {
        Vector3 basePos = block.position + Vector3.up * 0.5f;

        while (true)
        {
            float t = 0f;
            // 上下にスーッと動いて見せる
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                float y = Mathf.Sin(t * Mathf.PI) * 1.5f;
                ghostPlayer.transform.position = basePos + new Vector3(0, y, 0);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    // --- ⑤ 綿（消える足場）：乗って落ちる実演 ---
    IEnumerator DemoVanishRoutine(Transform block)
    {
        Vector3 standPos = block.position + Vector3.up * 0.5f;

        while (true)
        {
            ghostPlayer.transform.position = standPos;
            yield return new WaitForSecondsRealtime(0.4f);

            // ストンと下に落ちる
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = standPos + Vector3.down * (t * 4f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}