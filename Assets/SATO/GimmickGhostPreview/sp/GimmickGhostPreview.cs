using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class GimmickGhostPreview : MonoBehaviour
{
    public static GimmickGhostPreview instance;

    [Header("ゴースト自機の参照")]
    public GameObject ghostPlayer;
    public SpriteRenderer ghostSpriteRenderer;
    public Animator ghostAnimator;

    [Header("透明度・サイズ設定")]
    [Range(0.1f, 1f)]
    public float ghostAlpha = 0.5f;
    public Vector3 customGhostScale = Vector3.zero;

    [Header("ホバー演出の遅延設定")]
    [Tooltip("マウスを乗せてからゴーストが出るまでの待ち時間（秒）")]
    public float hoverDelay = 0.2f;

    [Header("ヘビ実演時の高さ微調整")]
    [Tooltip("ヘビ実演時の自機の高さ。少し下げたい場合は -0.2 や -0.3 などを設定")]
    public float snakePlayerYOffset = -0.25f; // ★復活させました！

    private DynamicDropFrame currentHoveredFrame = null;
    private GameObject currentActiveBlock = null;
    private Coroutine currentDemoRoutine = null;
    private Coroutine hoverDelayRoutine = null;
    private Camera mainCam;
    private Player_walk playerWalkRef;
    private Rigidbody2D playerRbRef;
    private Vector3 baseScale = Vector3.one;

    private SpriteRenderer currentAnimalRenderer = null;
    private Color originalAnimalColor = Color.white;
    private Vector3 originalAnimalPos = Vector3.zero;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        mainCam = Camera.main;

        Player_walk realPlayer = FindObjectOfType<Player_walk>();
        if (realPlayer != null)
        {
            playerWalkRef = realPlayer;
            playerRbRef = realPlayer.GetComponent<Rigidbody2D>();
            baseScale = realPlayer.transform.localScale;
        }

        if (customGhostScale != Vector3.zero) baseScale = customGhostScale;

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

            ghostPlayer.transform.localScale = baseScale;
            ghostPlayer.SetActive(false);
        }
    }

    void Update()
    {
        if (GameManager.instance == null || GameManager.instance.currentState != GameManager.GameState.Edit)
        {
            StopPreview();
            return;
        }

        if (GameManager.instance.blockManager != null && GameManager.instance.blockManager.IsDragging)
        {
            StopPreview();
            return;
        }

        CheckFrameHover();
    }

    // ★修正：枠（DropFrame）の判定を広げ、マウスが少しカスっただけでも確実に検知する
    void CheckFrameHover()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCam.transform.position.z));
        mouseWorld.z = 0f;

        // マウス位置にあるコライダーをすべて取得
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
        DynamicDropFrame foundFrame = null;

        foreach (var hit in hits)
        {
            // DropFrame自体か、あるいはその親・子からDynamicDropFrameを探す
            DynamicDropFrame frame = hit.GetComponent<DynamicDropFrame>() ?? hit.GetComponentInParent<DynamicDropFrame>();
            if (frame != null)
            {
                foundFrame = frame;
                break;
            }
        }

        if (foundFrame != null)
        {
            if (currentHoveredFrame != foundFrame)
            {
                StopPreview();
                currentHoveredFrame = foundFrame;

                GameObject blockOnFrame = GetBlockOnPosition(foundFrame.transform.position);
                if (blockOnFrame != null)
                {
                    if (hoverDelayRoutine != null) StopCoroutine(hoverDelayRoutine);
                    hoverDelayRoutine = StartCoroutine(StartHoverDelayRoutine(blockOnFrame, foundFrame));
                }
            }
        }
        else
        {
            if (currentHoveredFrame != null)
            {
                StopPreview();
            }
        }
    }

    IEnumerator StartHoverDelayRoutine(GameObject block, DynamicDropFrame frame)
    {
        yield return new WaitForSecondsRealtime(hoverDelay);

        if (currentHoveredFrame == frame)
        {
            StartPreview(block, frame);
        }
    }

    GameObject GetBlockOnPosition(Vector3 pos)
    {
        // わずかな座標のズレを吸収するため、OverlapCircleで周辺のブロックも確実に拾う
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("PlacedBlock") || hit.GetComponentInParent<BlockInfo>() != null || hit.GetComponentInParent<PlacedBlockMarker>() != null)
            {
                return hit.GetComponentInParent<PlacedBlockMarker>()?.gameObject
                       ?? hit.GetComponentInParent<BlockInfo>()?.gameObject
                       ?? hit.gameObject;
            }
        }
        return null;
    }

    void StartPreview(GameObject block, DynamicDropFrame frame)
    {
        if (ghostPlayer == null || block == null || frame == null) return;

        if (currentDemoRoutine != null) StopCoroutine(currentDemoRoutine);
        currentActiveBlock = block;

        int dir = (int)frame.previewDirection;
        float birdDist = frame.birdFlightDistance;

        currentAnimalRenderer = block.GetComponentInChildren<SpriteRenderer>();
        if (currentAnimalRenderer != null)
        {
            originalAnimalColor = currentAnimalRenderer.color;
            Color c = originalAnimalColor;
            c.a = ghostAlpha + 0.15f;
            currentAnimalRenderer.color = c;
        }
        originalAnimalPos = block.transform.position;

        if (block.GetComponentInChildren<PlayerJumpBlock>() != null)
        {
            ghostPlayer.SetActive(true);
            currentDemoRoutine = StartCoroutine(DemoJumpRoutine(block.GetComponentInChildren<PlayerJumpBlock>(), dir));
        }
        else if (block.GetComponentInChildren<ReverseGimmick>() != null)
        {
            ghostPlayer.SetActive(true);
            currentDemoRoutine = StartCoroutine(DemoReverseRoutine(block.transform, dir));
        }
        else if (block.GetComponentInChildren<BirdCarrier>() != null)
        {
            // 鳥の場合：スイッチ（enableFlight）がONなら自機を表示、OFFなら自機は隠す！
            if (frame.enableBirdFlight) ghostPlayer.SetActive(true);
            else ghostPlayer.SetActive(false);

            currentDemoRoutine = StartCoroutine(DemoBirdRoutine(block.GetComponentInChildren<BirdCarrier>(), dir, frame.enableBirdFlight, birdDist));
        }
        else if (block.GetComponentInChildren<LiftBlock>() != null)
        {
            ghostPlayer.SetActive(true);
            currentDemoRoutine = StartCoroutine(DemoLiftRoutine(block.GetComponentInChildren<LiftBlock>()));
        }
        else if (block.GetComponentInChildren<VanishingBlock>() != null)
        {
            ghostPlayer.SetActive(true);
            currentDemoRoutine = StartCoroutine(DemoVanishRoutine(block.GetComponentInChildren<VanishingBlock>()));
        }
    }

    void StopPreview()
    {
        if (hoverDelayRoutine != null)
        {
            StopCoroutine(hoverDelayRoutine);
            hoverDelayRoutine = null;
        }

        currentHoveredFrame = null;

        if (currentAnimalRenderer != null)
        {
            currentAnimalRenderer.color = originalAnimalColor;
            currentAnimalRenderer = null;
        }
        if (currentActiveBlock != null)
        {
            currentActiveBlock.transform.position = originalAnimalPos;
            Animator anim = currentActiveBlock.GetComponentInChildren<Animator>();
            if (anim != null) { anim.Rebind(); anim.Update(0f); }
            currentActiveBlock = null;
        }

        if (currentDemoRoutine != null)
        {
            StopCoroutine(currentDemoRoutine);
            currentDemoRoutine = null;
        }

        if (ghostPlayer != null)
        {
            ResetGhostAnim();
            ghostPlayer.SetActive(false);
        }
    }

    void ResetGhostAnim()
    {
        if (ghostAnimator != null)
        {
            ghostAnimator.SetBool("isWalk", false);
            ghostAnimator.SetBool("isCharging", false);
            ghostAnimator.SetBool("isFalling", false);
            ghostAnimator.SetBool("isLanding", false);
        }
    }

    // --- ① ネズミ ---
    IEnumerator DemoJumpRoutine(PlayerJumpBlock jumpBlock, int dir)
    {
        while (true)
        {
            if (jumpBlock == null) { StopPreview(); yield break; }
            Transform blockT = jumpBlock.transform;
            Vector3 startPos = blockT.position + Vector3.up * 0.5f;

            float vx = (playerWalkRef != null ? playerWalkRef.playerJumpForwardPower : 3.6f) * dir;
            float vy = (playerWalkRef != null ? playerWalkRef.playerJumpPower : 11.5f);
            float gravity = playerRbRef != null ? Physics2D.gravity.y * playerRbRef.gravityScale : -9.81f * 3f;

            ghostPlayer.transform.position = startPos;
            ghostPlayer.transform.localScale = new Vector3(baseScale.x * dir, baseScale.y, baseScale.z);
            ResetGhostAnim();

            if (ghostAnimator != null) ghostAnimator.SetBool("isCharging", true);
            yield return new WaitForSecondsRealtime(0.45f);

            if (jumpBlock == null) { StopPreview(); yield break; }
            jumpBlock.TriggerJumpAnimation();

            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isCharging", false);
                ghostAnimator.SetBool("isFalling", true);
            }

            float t = 0f;
            Vector3 lastPos = startPos;
            LayerMask groundMask = playerWalkRef != null ? playerWalkRef.groundLayer : LayerMask.GetMask("Ground");

            while (t < 3.0f)
            {
                if (jumpBlock == null) { StopPreview(); yield break; }
                t += Time.unscaledDeltaTime;
                float x = vx * t;
                float y = (vy * t) + (0.5f * gravity * t * t);
                Vector3 currentPos = startPos + new Vector3(x, y, 0);

                RaycastHit2D hit = Physics2D.Linecast(lastPos, currentPos, groundMask);
                if (hit.collider != null && t > 0.08f)
                {
                    ghostPlayer.transform.position = hit.point;
                    break;
                }
                ghostPlayer.transform.position = currentPos;
                lastPos = currentPos;
                yield return null;
            }

            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isFalling", false);
                ghostAnimator.SetBool("isLanding", true);
            }
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    // --- ② ヘビ（高さ調整対応） ---
    IEnumerator DemoReverseRoutine(Transform block, int approachDir)
    {
        while (true)
        {
            if (block == null) { StopPreview(); yield break; }
            LayerMask groundMask = playerWalkRef != null ? playerWalkRef.groundLayer : LayerMask.GetMask("Ground");
            RaycastHit2D groundHit = Physics2D.Raycast(block.position + Vector3.down * 0.5f, Vector2.down, 4.0f, groundMask);
            float groundY = groundHit.collider != null ? groundHit.point.y + 0.35f : block.position.y;

            // ★インスペクターで設定できるオフセットを適用
            groundY += snakePlayerYOffset;

            Vector3 centerPos = new Vector3(block.position.x, groundY, block.position.z);

            float speed = playerWalkRef != null ? playerWalkRef.PlayerSpeed : 5.0f;
            float walkDist = 1.2f;

            ResetGhostAnim();
            ghostPlayer.transform.localScale = new Vector3(baseScale.x * approachDir, baseScale.y, baseScale.z);

            if (ghostAnimator != null) ghostAnimator.SetBool("isWalk", true);
            float elapsed = 0f;
            float dur = walkDist / speed;
            while (elapsed < dur)
            {
                if (block == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = Vector3.Lerp(centerPos - Vector3.right * approachDir * walkDist, centerPos, elapsed / dur);
                yield return null;
            }

            if (ghostAnimator != null) ghostAnimator.SetBool("isWalk", false);
            yield return new WaitForSecondsRealtime(0.2f);

            ghostPlayer.transform.localScale = new Vector3(-baseScale.x * approachDir, baseScale.y, baseScale.z);
            yield return new WaitForSecondsRealtime(0.2f);

            if (ghostAnimator != null) ghostAnimator.SetBool("isWalk", true);
            elapsed = 0f;
            while (elapsed < dur)
            {
                if (block == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = Vector3.Lerp(centerPos, centerPos - Vector3.right * approachDir * walkDist, elapsed / dur);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    // --- ③ 鳥（ON/OFFスイッチ連動） ---
    IEnumerator DemoBirdRoutine(BirdCarrier bird, int dir, bool enableFlight, float flightDist)
    {
        while (true)
        {
            if (bird == null || bird.gameObject == null) { StopPreview(); yield break; }

            Vector3 birdOrigin = originalAnimalPos;
            Vector3 startPlayerPos = birdOrigin + new Vector3(0, bird.grabOffsetY, 0);

            bird.transform.position = birdOrigin;

            // スイッチOFFなら自機を隠す、ONなら表示
            ghostPlayer.SetActive(enableFlight);
            if (enableFlight)
            {
                ghostPlayer.transform.position = startPlayerPos;
                ghostPlayer.transform.localScale = new Vector3(baseScale.x * dir, baseScale.y, baseScale.z);
            }

            Vector3 birdScale = bird.transform.localScale;
            bird.transform.localScale = new Vector3(Mathf.Abs(birdScale.x) * dir, birdScale.y, birdScale.z);

            ResetGhostAnim();
            if (ghostAnimator != null) ghostAnimator.SetBool("isFalling", true);

            // スイッチOFFなら「鳥がその場で羽ばたくだけ」
            if (!enableFlight)
            {
                if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, true);
                yield return new WaitForSecondsRealtime(1.0f);
                if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, false);
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            // スイッチONなら飛ぶ！
            Vector3 destPos = startPlayerPos + new Vector3(dir * flightDist, 0, 0);
            LayerMask groundMask = playerWalkRef != null ? playerWalkRef.groundLayer : LayerMask.GetMask("Ground");
            RaycastHit2D groundHit = Physics2D.Raycast(destPos + Vector3.up * 2f, Vector2.down, 10f, groundMask);
            if (groundHit.collider != null)
            {
                destPos = new Vector3(destPos.x, groundHit.point.y, 0);
            }

            yield return new WaitForSecondsRealtime(0.3f);
            if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, true);

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                if (bird == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.5f;
                bird.transform.position = Vector3.Lerp(birdOrigin, birdOrigin + new Vector3(0, bird.heightOffset, 0), t);
                if (enableFlight) ghostPlayer.transform.position = bird.transform.position + new Vector3(0, bird.grabOffsetY, 0);
                yield return null;
            }

            Vector3 flyStart = bird.transform.position;
            Vector3 flyEnd = destPos + new Vector3(0, bird.heightOffset - bird.grabOffsetY, 0);
            elapsed = 0f;
            float flyDur = Vector3.Distance(flyStart, flyEnd) / bird.speed;
            while (elapsed < flyDur)
            {
                if (bird == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flyDur;
                bird.transform.position = Vector3.Lerp(flyStart, flyEnd, t);
                if (enableFlight) ghostPlayer.transform.position = bird.transform.position + new Vector3(0, bird.grabOffsetY, 0);
                yield return null;
            }

            if (ghostAnimator != null) ghostAnimator.SetBool("isFalling", false);
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                if (bird == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.5f;
                bird.transform.position = Vector3.Lerp(flyEnd, destPos, t);
                if (enableFlight) ghostPlayer.transform.position = Vector3.Lerp(ghostPlayer.transform.position, destPos, t);
                yield return null;
            }

            if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, false);
            if (ghostAnimator != null) ghostAnimator.SetBool("isLanding", true);
            yield return new WaitForSecondsRealtime(0.5f);

            bird.transform.position = birdOrigin;
        }
    }

    // --- ④ クモ ---
    IEnumerator DemoLiftRoutine(LiftBlock lift)
    {
        while (true)
        {
            if (lift == null) { StopPreview(); yield break; }
            Vector3 liftOrigin = originalAnimalPos;
            Vector3 startP = liftOrigin + new Vector3(0, lift.playerYOffsetOnLift, 0);
            lift.transform.position = liftOrigin;
            ghostPlayer.transform.position = startP;
            ghostPlayer.transform.localScale = baseScale;
            ResetGhostAnim();

            Vector3 targetDestination;
            bool hasDestination = lift.FindNextDestination(liftOrigin.x, out targetDestination);

            if (!hasDestination)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            Vector3 endPos = new Vector3(startP.x, targetDestination.y + lift.playerYOffsetOnLift, 0);
            yield return new WaitForSecondsRealtime(0.3f);

            if (lift.spiderAnimator != null) lift.spiderAnimator.SetBool(lift.animBoolName, true);

            float elapsed = 0f;
            float dur = Mathf.Abs(startP.y - endPos.y) / lift.speed;
            if (dur < 0.2f) dur = 1.0f;

            while (elapsed < dur)
            {
                if (lift == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / dur);
                Vector3 cur = Vector3.Lerp(startP, endPos, t);
                ghostPlayer.transform.position = cur;
                lift.transform.position = cur - new Vector3(0, lift.playerYOffsetOnLift, 0);
                yield return null;
            }

            if (lift.spiderAnimator != null) lift.spiderAnimator.SetBool(lift.animBoolName, false);
            yield return new WaitForSecondsRealtime(0.4f);

            lift.transform.position = liftOrigin;
        }
    }

    // --- ⑤ 綿 ---
    IEnumerator DemoVanishRoutine(VanishingBlock vanish)
    {
        while (true)
        {
            if (vanish == null) { StopPreview(); yield break; }
            Vector3 standPos = vanish.transform.position + Vector3.up * 0.5f;
            ghostPlayer.transform.position = standPos;
            ghostPlayer.transform.localScale = baseScale;
            ResetGhostAnim();

            if (vanish.animator != null) vanish.animator.SetTrigger(vanish.vanishTriggerParam);
            yield return new WaitForSecondsRealtime(vanish.vanishDelay);

            if (vanish == null) { StopPreview(); yield break; }
            if (ghostAnimator != null) ghostAnimator.SetBool("isFalling", true);

            float t = 0f;
            while (t < 0.35f)
            {
                if (vanish == null) { StopPreview(); yield break; }
                t += Time.unscaledDeltaTime;
                ghostPlayer.transform.position = standPos + Vector3.down * (t * 6f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            if (vanish.animator != null) { vanish.animator.Rebind(); vanish.animator.Update(0f); }
        }
    }
}