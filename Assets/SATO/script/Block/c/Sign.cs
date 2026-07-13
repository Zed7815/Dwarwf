using UnityEngine;
using System.Collections;

public class Sign : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;

    [Header("回転設定")]
    public float rotateTime = 0.6f;
    public Vector3 rotationAmount = new Vector3(-90, 0, 0);

    [Header("倒れる質感の設定")]
    [Tooltip("インスペクターのグラフを『右肩上がりのJの字』にすると、重力でバタンと倒れる感じになります")]
    // 安全な初期化方法に変更
    public AnimationCurve fallCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    [Header("跳ね返り設定")]
    public bool useBounce = true;
    public float bounceAmount = 10f;
    public float bounceTime = 0.2f;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip rotateSE;

    private bool rotating = false;
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        if (gameManager == null) gameManager = GameManager.instance;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (gameManager.currentState == GameManager.GameState.Edit &&
            !rotating &&
            gameManager.blockManager != null &&
            gameManager.blockManager.IsAllBlocksPlaced())
        {
            StartCoroutine(RotateRoutine());
        }
    }

    IEnumerator RotateRoutine()
    {
        rotating = true;

        if (audioSource != null && rotateSE != null)
        {
            audioSource.PlayOneShot(rotateSE);
        }

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(rotationAmount);

        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / rotateTime;
            // インスペクターで設定したカーブを適用
            float curveT = fallCurve.Evaluate(t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, curveT);
            yield return null;
        }
        transform.rotation = endRot;

        if (useBounce)
        {
            Quaternion bounceRot = endRot * Quaternion.Euler(-bounceAmount, 0, 0);

            float bt = 0;
            while (bt < 1.0f)
            {
                bt += Time.deltaTime / (bounceTime * 0.5f);
                transform.rotation = Quaternion.Lerp(endRot, bounceRot, bt);
                yield return null;
            }
            bt = 0;
            while (bt < 1.0f)
            {
                bt += Time.deltaTime / (bounceTime * 0.5f);
                transform.rotation = Quaternion.Lerp(bounceRot, endRot, bt);
                yield return null;
            }
        }
        transform.rotation = endRot;
    }

    void OnGimmickReset()
    {
        StopAllCoroutines();
        transform.rotation = initialRotation;
        rotating = false;
        if (audioSource != null) audioSource.Stop();
    }
}