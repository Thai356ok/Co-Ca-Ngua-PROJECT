using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceThrowAnimation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform diceThrowImage;
    [SerializeField] private RectTransform startPoint;   // Có thể để trống hoặc giữ nguyên
    [SerializeField] private RectTransform targetPoint;  // DiceLandingPoint

    [Header("Dice Image")]
    [SerializeField] private Image diceImage;
    [SerializeField] private Sprite[] diceFaces;

    [Header("Front Throw Settings")]
    [SerializeField] private float dropDuration = 0.45f;
    [SerializeField] private float rollDuration = 0.55f;

    [SerializeField] private float startScale = 2.3f;
    [SerializeField] private float impactScale = 1.05f;
    [SerializeField] private float endScale = 1.0f;

    [SerializeField] private float frontStartOffsetY = 80f;
    [SerializeField] private float impactBounceHeight = 35f;
    [SerializeField] private float rollDistance = 90f;
    [SerializeField] private float spinAmount = 900f;

    private Coroutine currentRoutine;

    public void PlayThrow(int finalResult, Action onComplete)
    {
        if (diceThrowImage == null || targetPoint == null || diceImage == null)
        {
            Debug.LogWarning("[DiceThrowAnimation] Thiếu reference DiceThrowImage / TargetPoint / DiceImage.");
            onComplete?.Invoke();
            return;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(FrontThrowRoutine(finalResult, onComplete));
    }

    private IEnumerator FrontThrowRoutine(int finalResult, Action onComplete)
    {
        finalResult = Mathf.Clamp(finalResult, 1, 6);

        diceThrowImage.gameObject.SetActive(true);

        Vector2 landPos = GetAnchoredPosition(targetPoint.position);

        // Giả lập xúc xắc từ chính diện màn hình:
        // bắt đầu gần vị trí rơi, nhưng to hơn và hơi cao hơn.
        Vector2 frontStartPos = landPos + new Vector2(0f, frontStartOffsetY);

        diceThrowImage.anchoredPosition = frontStartPos;
        diceThrowImage.localScale = Vector3.one * startScale;
        diceThrowImage.localRotation = Quaternion.identity;

        float time = 0f;

        // ===============================
        // PHASE 1: RƠI TỪ CHÍNH DIỆN XUỐNG BÀN CỜ
        // ===============================
        while (time < dropDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / dropDuration);

            // Ease in: ban đầu chậm, cuối nhanh hơn như đang rơi xuống bàn
            float eased = t * t;

            Vector2 pos = Vector2.Lerp(frontStartPos, landPos, eased);

            // Nảy/dao động nhẹ để giống vật được quăng xuống
            float shake = Mathf.Sin(t * Mathf.PI * 3f) * Mathf.Lerp(18f, 0f, t);
            pos.x += shake * 0.25f;
            pos.y += Mathf.Sin(t * Mathf.PI) * impactBounceHeight;

            diceThrowImage.anchoredPosition = pos;

            // To → nhỏ dần để tạo cảm giác từ trước mặt rơi xuống bàn cờ
            float scale = Mathf.Lerp(startScale, impactScale, eased);
            diceThrowImage.localScale = Vector3.one * scale;

            // Xoay khi rơi
            diceThrowImage.localRotation = Quaternion.Euler(0f, 0f, -spinAmount * t);

            SetRandomFace();

            yield return null;
        }

        // ===============================
        // PHASE 2: CHẠM BÀN + LĂN NHẸ
        // ===============================
        Vector2 rollStart = landPos;
        Vector2 rollEnd = landPos + new Vector2(rollDistance, -rollDistance * 0.22f);

        time = 0f;

        while (time < rollDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / rollDuration);

            // Lăn chậm dần
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            Vector2 pos = Vector2.Lerp(rollStart, rollEnd, eased);

            // Nảy nhỏ dần khi lăn
            float smallBounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 5f)) * Mathf.Lerp(20f, 0f, t);
            pos.y += smallBounce;

            diceThrowImage.anchoredPosition = pos;

            float scale = Mathf.Lerp(impactScale, endScale, t);
            diceThrowImage.localScale = Vector3.one * scale;

            // Xoay khi lăn
            diceThrowImage.localRotation = Quaternion.Euler(0f, 0f, -spinAmount - spinAmount * eased);

            SetRandomFace();

            yield return null;
        }

        // ===============================
        // PHASE 3: DỪNG Ở KẾT QUẢ THẬT
        // ===============================
        diceThrowImage.anchoredPosition = rollEnd;
        diceThrowImage.localScale = Vector3.one * endScale;
        diceThrowImage.localRotation = Quaternion.identity;

        if (diceFaces != null && diceFaces.Length >= finalResult)
        {
            diceImage.sprite = diceFaces[finalResult - 1];
        }

        currentRoutine = null;
        onComplete?.Invoke();
    }

    private void SetRandomFace()
    {
        if (diceFaces == null || diceFaces.Length == 0 || diceImage == null)
            return;

        int index = UnityEngine.Random.Range(0, diceFaces.Length);
        diceImage.sprite = diceFaces[index];
    }

    private Vector2 GetAnchoredPosition(Vector3 worldPosition)
    {
        RectTransform parentRect = diceThrowImage.parent as RectTransform;
        return parentRect.InverseTransformPoint(worldPosition);
    }
}