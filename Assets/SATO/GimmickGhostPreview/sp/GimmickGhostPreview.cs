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

    private DynamicDropFrame currentHoveredFrame = null;
    private GameObject currentActiveBlock = null;
    private Coroutine currentDemoRoutine = null;
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

    void CheckFrameHover()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCam.transform.position.z));
        mouseWorld.z = 0f;

        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
        DynamicDropFrame foundFrame = null;

        foreach (var hit in hits)
        {
            DynamicDropFrame frame = hit.GetComponent<DynamicDropFrame>();
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
                    StartPreview(blockOnFrame, foundFrame);
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

    GameObject GetBlockOnPosition(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
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
        ghostPlayer.SetActive(true);
        currentActiveBlock = block;

        int dir = (int)frame.previewDirection;

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
            currentDemoRoutine = StartCoroutine(DemoJumpRoutine(block.GetComponentInChildren<PlayerJumpBlock>(), dir));
        }
        else if (block.GetComponentInChildren<ReverseGimmick>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoReverseRoutine(block.transform, dir));
        }
        else if (block.GetComponentInChildren<BirdCarrier>() != null)
        {
            // ★鳥の場合：ON/OFFスイッチと、設定された距離を渡す
            currentDemoRoutine = StartCoroutine(DemoBirdRoutine(block.GetComponentInChildren<BirdCarrier>(), dir, frame.enableBirdFlight, frame.birdFlightDistance));
        }
        else if (block.GetComponentInChildren<LiftBlock>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoLiftRoutine(block.GetComponentInChildren<LiftBlock>()));
        }
        else if (block.GetComponentInChildren<VanishingBlock>() != null)
        {
            currentDemoRoutine = StartCoroutine(DemoVanishRoutine(block.GetComponentInChildren<VanishingBlock>()));
        }
    }

    void StopPreview()
    {
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

    IEnumerator DemoJumpRoutine(PlayerJumpBlock jumpBlock, int dir)
    {
        while (true)
        {
            if (jumpBlock == null) { StopPreview(); yield break; }
            Transform blockT = jumpBlock.transform;
            Vector3 startPos = blockT.position + Vector3.up * 0.5f;
            float vx = (playerWalkRef != null ? playerWalkRef.playerJumpForwardPower : 3.6f) * dir;
            float vy = playerWalkRef != null ? playerWalkRef.playerJumpPower : 11.5f;
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

    IEnumerator DemoReverseRoutine(Transform block, int approachDir)
    {
        while (true)
        {
            if (block == null) { StopPreview(); yield break; }
            LayerMask groundMask = playerWalkRef != null ? playerWalkRef.groundLayer : LayerMask.GetMask("Ground");
            RaycastHit2D groundHit = Physics2D.Raycast(block.position + Vector3.down * 0.5f, Vector2.down, 4.0f, groundMask);
            float groundY = groundHit.collider != null ? groundHit.point.y + 0.35f : block.position.y;
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

    // --- ③ 鳥（ON/OFFスイッチ ＆ 距離指定対応） ---
    IEnumerator DemoBirdRoutine(BirdCarrier bird, int dir, bool enableFlight, float flightDist)
    {
        while (true)
        {
            if (bird == null || bird.gameObject == null) { StopPreview(); yield break; }

            Vector3 birdOrigin = originalAnimalPos;
            Vector3 startPlayerPos = birdOrigin + new Vector3(0, bird.grabOffsetY, 0);

            bird.transform.position = birdOrigin;
            ghostPlayer.transform.position = startPlayerPos;
            ghostPlayer.transform.localScale = new Vector3(baseScale.x * dir, baseScale.y, baseScale.z);

            Vector3 birdScale = bird.transform.localScale;
            bird.transform.localScale = new Vector3(Mathf.Abs(birdScale.x) * dir, birdScale.y, birdScale.z);

            ResetGhostAnim();
            if (ghostAnimator != null) ghostAnimator.SetBool("isFalling", true);

            // ★もしスイッチがOFFなら、飛ばずにその場で羽ばたき実演だけする
            if (!enableFlight)
            {
                if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, true);
                yield return new WaitForSecondsRealtime(1.0f);
                if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, false);
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            // スイッチがONなら設定された距離だけ飛ぶ！
            Vector3 destPos = startPlayerPos + new Vector3(dir * flightDist, 0, 0);
            yield return new WaitForSecondsRealtime(0.3f);

            if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, true);

            float elapsed = 0f;
            float dur = flightDist / bird.speed;
            while (elapsed < dur)
            {
                if (bird == null) { StopPreview(); yield break; }
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;

                Vector3 currentP = Vector3.Lerp(startPlayerPos, destPos, t);
                ghostPlayer.transform.position = currentP;
                bird.transform.position = currentP - new Vector3(0, bird.grabOffsetY, 0);
                yield return null;
            }

            if (bird.animator != null) bird.animator.SetBool(bird.flyBoolParam, false);
            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isFalling", false);
                ghostAnimator.SetBool("isLanding", true);
            }
            yield return new WaitForSecondsRealtime(0.4f);

            bird.transform.position = birdOrigin;
        }
    }

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