using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpEntrance : MonoBehaviour
{
    [Header("演出設定")]
    public float hideDuration = 1.0f;
    public string playerExitTrigger = "isWarpExit";

    [Header("出口の高さ調整")]
    public float exitYOffset = 0f;

    [Header("アニメーション制御（Bool名）")]
    public string entranceBoolName = "isWarpingEntrance";
    public string exitBoolName = "isExiting";

    [Header("自動取得されるコンポーネント")]
    public Transform exitPoint;
    public Animator entranceAnimator;
    public Animator exitAnimator;

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip enterSE;       // 吸い込まれる時の音
    public AudioClip exitSE;        // 出てくる時の音

    private bool isWarping = false;
    private float cooldownTimer = 0f;

    private void Start()
    {
        SetupReferences();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void SetupReferences()
    {
        if (entranceAnimator == null) entranceAnimator = GetComponent<Animator>();

        if (transform.parent != null)
        {
            WarpExit exit = transform.parent.GetComponentInChildren<WarpExit>();
            if (exit != null)
            {
                exitPoint = exit.transform;
                exitAnimator = exit.exitAnimator;
                if (exitAnimator == null) exitAnimator = exit.GetComponent<Animator>();
            }
        }

        if (exitPoint == null)
        {
            WarpExit exit = FindObjectOfType<WarpExit>();
            if (exit != null)
            {
                exitPoint = exit.transform;
                exitAnimator = exit.GetComponent<Animator>();
            }
        }
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryStartWarp(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryStartWarp(collision);
    }

    private void TryStartWarp(Collider2D collision)
    {
        if (GameManager.instance != null && GameManager.instance.currentState != GameManager.GameState.Play) return;
        if (collision.CompareTag("Player") && !isWarping && Time.time > cooldownTimer)
        {
            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk != null)
            {
                StartCoroutine(WarpRoutine(pWalk));
            }
        }
    }

    IEnumerator WarpRoutine(Player_walk pWalk)
    {
        isWarping = true;

        if (exitPoint == null) SetupReferences();

        pWalk.StateChange(0);
        Rigidbody2D rb = pWalk.GetComponent<Rigidbody2D>();
        Animator pAnim = pWalk.GetComponent<Animator>();
        SpriteRenderer playerSR = pWalk.GetComponent<SpriteRenderer>();

        if (playerSR != null) playerSR.enabled = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        pWalk.transform.position = new Vector3(transform.position.x, pWalk.transform.position.y, pWalk.transform.position.z);

        // --- 入口演出の開始 ---
        if (entranceAnimator != null)
        {
            // ★SE再生：吸い込み
            if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);

            if (HasParameter(entranceAnimator, entranceBoolName))
                entranceAnimator.SetBool(entranceBoolName, true);

            if (pWalk.direction == 1 && HasParameter(entranceAnimator, "EnterRight")) entranceAnimator.SetTrigger("EnterRight");
            else if (HasParameter(entranceAnimator, "EnterLeft")) entranceAnimator.SetTrigger("EnterLeft");
        }

        yield return new WaitForSeconds(0.4f);

        if (playerSR != null) playerSR.enabled = false;

        if (entranceAnimator != null)
        {
            if (HasParameter(entranceAnimator, entranceBoolName))
                entranceAnimator.SetBool(entranceBoolName, false);

            entranceAnimator.ResetTrigger("EnterRight");
            entranceAnimator.ResetTrigger("EnterLeft");
        }

        yield return new WaitForSeconds(hideDuration);

        // --- 出口へ移動 ---
        if (exitPoint != null)
        {
            pWalk.transform.position = new Vector3(exitPoint.position.x, exitPoint.position.y + exitYOffset, pWalk.transform.position.z);
            Physics2D.SyncTransforms();
        }

        if (exitAnimator != null)
        {
            // ★SE再生：出現
            if (audioSource != null && exitSE != null) audioSource.PlayOneShot(exitSE);

            if (HasParameter(exitAnimator, exitBoolName)) exitAnimator.SetBool(exitBoolName, true);
            if (pWalk.direction == 1 && HasParameter(exitAnimator, "ExitRight")) exitAnimator.SetTrigger("ExitRight");
            else if (HasParameter(exitAnimator, "ExitLeft")) exitAnimator.SetTrigger("ExitLeft");
        }

        if (playerSR != null) playerSR.enabled = true;

        if (pAnim != null)
        {
            pAnim.SetBool("isWalk", false);
            if (HasParameter(pAnim, playerExitTrigger)) pAnim.SetTrigger(playerExitTrigger);
        }

        yield return new WaitForSeconds(0.3f);

        if (exitAnimator != null)
        {
            if (HasParameter(exitAnimator, exitBoolName)) exitAnimator.SetBool(exitBoolName, false);
        }

        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        // ★完全リセット（アニメーター初期化）
        if (pAnim != null)
        {
            pAnim.Rebind();
            pAnim.Update(0f);
        }

        pWalk.StateChange(1);
        cooldownTimer = Time.time + 0.1f;
        isWarping = false;
    }
}