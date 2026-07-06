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
    public string entranceBoolName = "isWarpingEntrance"; // 入口用
    public string exitBoolName = "isExiting";             // 出口用

    [Header("自動取得されるコンポーネント")]
    public Transform exitPoint;
    public Animator entranceAnimator;
    public Animator exitAnimator;

    private bool isWarping = false;
    private float cooldownTimer = 0f;

    private void Start()
    {
        SetupReferences();
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

        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        if (rb != null)
        {
            originalBodyType = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        pWalk.transform.position = new Vector3(transform.position.x, pWalk.transform.position.y, pWalk.transform.position.z);

        // --- 入口演出の開始 ---
        if (entranceAnimator != null)
        {
            if (HasParameter(entranceAnimator, entranceBoolName))
                entranceAnimator.SetBool(entranceBoolName, true);

            if (HasParameter(entranceAnimator, "EnterRight")) entranceAnimator.ResetTrigger("EnterRight");
            if (HasParameter(entranceAnimator, "EnterLeft")) entranceAnimator.ResetTrigger("EnterLeft");

            if (pWalk.direction == 1 && HasParameter(entranceAnimator, "EnterRight")) entranceAnimator.SetTrigger("EnterRight");
            else if (HasParameter(entranceAnimator, "EnterLeft")) entranceAnimator.SetTrigger("EnterLeft");
        }

        // プレイヤーを非表示に
        if (playerSR != null) playerSR.enabled = false;

        yield return new WaitForSeconds(0.7f);

        // 入口の演出リセット
        if (entranceAnimator != null)
        {
            if (HasParameter(entranceAnimator, entranceBoolName))
                entranceAnimator.SetBool(entranceBoolName, false);

            entranceAnimator.ResetTrigger("EnterRight");
            entranceAnimator.ResetTrigger("EnterLeft");
        }

        yield return new WaitForSeconds(hideDuration);

        // 出口へ移動
        if (exitPoint != null)
        {
            pWalk.transform.position = new Vector3(exitPoint.position.x, exitPoint.position.y + exitYOffset, pWalk.transform.position.z);
            Physics2D.SyncTransforms();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 出口演出開始
        if (exitAnimator != null)
        {
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
            exitAnimator.ResetTrigger("ExitRight");
            exitAnimator.ResetTrigger("ExitLeft");
        }

        if (rb != null)
        {
            rb.bodyType = originalBodyType;
            rb.linearVelocity = Vector2.zero;
        }

        // ★修正：リセットボタンと同じ「完全リセット」を適用
        if (pAnim != null)
        {
            // これでワープ用アニメの残骸をすべて消し、Entry(Idol)へ戻す
            pAnim.Rebind();
            pAnim.Update(0f);
        }

        // 移動制限の解除
        pWalk.StateChange(1);
        cooldownTimer = Time.time + 0.1f;
        isWarping = false;
    }
}