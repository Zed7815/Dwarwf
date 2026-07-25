using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("参照設定")]
    public GameManager gameManager;
    public RectTransform startPointA;
    public RectTransform placementTargetUI;
    public RectTransform startButtonUI;

    [Header("ガイドオブジェクト")]
    public RectTransform placementHand;
    public GameObject scrollIcon;
    public GameObject startArrowWorld;

    [Header("演出設定")]
    public float moveSpeed = 2.0f;
    public float startDelay = 0.5f;
    public float endDelay = 0.8f;
    public Vector3 startArrowOffset; // インスペクターで (0, 0.5, 0) など調整

    private bool isPlacementDone = false;
    private Camera mainCam;
    private Transform startArrowTransform;

    void Start()
    {
        mainCam = Camera.main;

        if (placementHand) placementHand.gameObject.SetActive(true);
        if (scrollIcon) scrollIcon.SetActive(true);

        if (startArrowWorld)
        {
            startArrowWorld.SetActive(false);
            startArrowTransform = startArrowWorld.transform;

            // ★裏技：可能であれば指のオブジェクトをカメラの子供に強制設定する
            // これにより、カメラが動いた時のガタつきを根本から防ぎます
            // startArrowTransform.SetParent(mainCam.transform); 
        }

        StartCoroutine(HideScrollIconAfterDelay(5f));
        StartCoroutine(PlacementAnimationRoutine());
    }

    // カメラの移動(Update)が完全に終わった直後の LateUpdate で実行
    void LateUpdate()
    {
        if (gameManager == null || gameManager.blockManager == null) return;

        int placedCount = GetPlacedCount();

        // 1. 設置ガイドの消去判定
        if (!isPlacementDone && placedCount >= 1)
        {
            isPlacementDone = true;
            if (placementHand) placementHand.gameObject.SetActive(false);
        }

        // 2. スタートガイドの表示と【Viewport精密同期】
        if (gameManager.currentState == GameManager.GameState.Edit)
        {
            if (placedCount >= 1 && startArrowWorld != null)
            {
                startArrowWorld.SetActive(true);
                SyncWorldObjectToUIPrecise(startArrowTransform, startButtonUI);
            }
            else if (startArrowWorld != null)
            {
                startArrowWorld.SetActive(false);
            }
        }
        else
        {
            HideAllGuides();
        }
    }

    // 最もズレにくい精密同期メソッド
    void SyncWorldObjectToUIPrecise(Transform worldObj, RectTransform uiTarget)
    {
        if (worldObj == null || uiTarget == null || mainCam == null) return;

        // 1. UIボタンのスクリーン上の位置を Viewport（画面の左下0,0〜右上1,1の比率）に変換
        // CanvasのRenderModeを問わず、現在の画面上の位置を 0.0〜1.0 で取得します
        Vector2 screenPoint = uiTarget.position;
        Vector3 viewportPoint = mainCam.ScreenToViewportPoint(screenPoint);

        // 2. 奥行き(Z)をカメラから見たゲーム平面（通常はカメラの反対側）に設定
        viewportPoint.z = Mathf.Abs(mainCam.transform.position.z);

        // 3. Viewport座標をワールド座標に再変換
        Vector3 worldPoint = mainCam.ViewportToWorldPoint(viewportPoint);

        // 4. 座標を適用
        worldObj.position = worldPoint + startArrowOffset;
    }

    // --- 以下、既存のコルーチン等はそのまま ---

    IEnumerator PlacementAnimationRoutine()
    {
        while (!isPlacementDone)
        {
            if (startPointA == null || placementTargetUI == null || placementHand == null) yield break;
            placementHand.position = startPointA.position;
            yield return new WaitForSeconds(startDelay);
            float t = 0;
            while (t < 1.0f)
            {
                if (isPlacementDone) yield break;
                t += Time.deltaTime * moveSpeed;
                placementHand.position = Vector3.Lerp(startPointA.position, placementTargetUI.position, t);
                yield return null;
            }
            yield return new WaitForSeconds(endDelay);
        }
    }

    int GetPlacedCount()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("PlacedBlock");
        int count = 0;
        foreach (var b in blocks)
        {
            Collider2D col = b.GetComponent<Collider2D>();
            if (col != null && col.enabled) count++;
        }
        return count;
    }

    void HideAllGuides()
    {
        if (startArrowWorld) startArrowWorld.SetActive(false);
        if (placementHand) placementHand.gameObject.SetActive(false);
        if (scrollIcon) scrollIcon.SetActive(false);
    }

    IEnumerator HideScrollIconAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (scrollIcon) scrollIcon.SetActive(false);
    }

    void OnGimmickReset()
    {
        StopAllCoroutines();
        isPlacementDone = false;
        if (placementHand) placementHand.gameObject.SetActive(true);
        if (scrollIcon) scrollIcon.SetActive(true);
        StartCoroutine(HideScrollIconAfterDelay(5f));
        StartCoroutine(PlacementAnimationRoutine());
    }
}