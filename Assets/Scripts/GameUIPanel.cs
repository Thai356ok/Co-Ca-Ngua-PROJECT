using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameUIPanel : BasePanel
{
    [Header("Giao diện UI")]
    public TextMeshProUGUI txtTurnInfo;
    public Button btnRollDice;
    public Image imgDiceDisplay;     

    [Header("Dữ liệu Xúc Xắc")]
    public Sprite[] diceFaces;

    [Header("Cài đặt Hiệu Ứng Xúc Xắc")]
    public float throwHeight = 80f;      // Độ cao tung lên (pixel)
    public float throwDuration = 0.3f;   // Thời gian bay lên
    public float landDuration = 0.2f;    // Thời gian rớt xuống

    // ==========================================
    // TRẠM BẬT ĂNG-TEN TỔNG HỢP
    // ==========================================
    private void OnEnable()
    {
        InputManager.OnObjectClicked += UpdateUIText;
        GameManager.OnTurnChanged += CapNhatChuToiLuotAi;
    }

    // ==========================================
    // TRẠM TẮT ĂNG-TEN TỔNG HỢP
    // ==========================================
    private void OnDisable()
    {
        InputManager.OnObjectClicked -= UpdateUIText;
        GameManager.OnTurnChanged -= CapNhatChuToiLuotAi;
    }

    // ==========================================
    // HÀM XỬ LÝ CLICK VẬT THỂ
    // (Đã xoá dòng "Bạn vừa click vào")
    // ==========================================
    private void UpdateUIText(GameObject clickedObj)
    {
        QuancoMovement quanCo = clickedObj.GetComponent<QuancoMovement>();
        if (quanCo != null)
        {
            GameManager.Instance.ChonQuan(quanCo);
        }
    }

    // ==========================================
    // NÚT BẤM ĐỔ XÚC XẮC
    // ==========================================
    public void OnRollDiceClicked()
    {
        if (GameManager.Instance.CurrentState != GameState.Wait_For_Roll)
        {
            Debug.LogWarning("Không thể đổ xúc xắc lúc này! Đang chờ đi quân hoặc chờ lượt.");
            return;
        }

        int diceResult = Random.Range(1, 7);
        StartCoroutine(PlayDiceRollAnimation(diceResult));
    }

    // ==========================================
    // COROUTINE HIỆU ỨNG TUNG XÚC XẮC
    // ==========================================
    private IEnumerator PlayDiceRollAnimation(int finalResult)
    {
        // Khoá nút lại để không bấm 2 lần trong khi đang diễn hoạt hình
        btnRollDice.interactable = false;

        RectTransform diceRect = imgDiceDisplay.GetComponent<RectTransform>();
        Vector2 originalPos = diceRect.anchoredPosition;
        Vector3 originalScale = diceRect.localScale;

        // Ẩn chữ ngay khi bắt đầu tung
        txtTurnInfo.gameObject.SetActive(false);

        // ------------------------------------------
        // PHASE 1: TUNG LÊN (bay lên theo parabol)
        // ------------------------------------------
        float t = 0f;
        while (t < throwDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / throwDuration);

            float yOffset = Mathf.Sin(progress * Mathf.PI) * throwHeight;
            float scaleMult = 1f + Mathf.Sin(progress * Mathf.PI) * 0.3f;

            diceRect.anchoredPosition = originalPos + new Vector2(0f, yOffset);
            diceRect.localScale = originalScale * scaleMult;

            yield return null;
        }

        // ------------------------------------------
        // PHASE 2: LẬT MẶT NGẪU NHIÊN (2-3 lần)
        // ------------------------------------------
        float interval = 0.12f;
        float elapsed = 0f;
        int flipCount = 0;
        int maxFlips = Random.Range(2, 4); // Ngẫu nhiên 2 hoặc 3 lần

        while (flipCount < maxFlips)
        {
            int randomFace = Random.Range(0, diceFaces.Length);
            imgDiceDisplay.sprite = diceFaces[randomFace];

            float shakeX = Mathf.Sin(elapsed * 35f) * 6f;
            diceRect.anchoredPosition = originalPos + new Vector2(shakeX, 22f);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
            flipCount++;

            // Chậm lại ở lần lật cuối để có cảm giác "sắp rớt"
            if (flipCount == maxFlips - 1)
                interval = 0.22f;
        }

        // ------------------------------------------
        // PHASE 3: RỚT XUỐNG + NẢY NHẸ KHI ĐÁP
        // ------------------------------------------
        // Hiện đúng mặt kết quả TRƯỚC khi rớt xuống
        imgDiceDisplay.sprite = diceFaces[finalResult - 1];

        t = 0f;
        while (t < landDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / landDuration);

            float yOffset = Mathf.Lerp(22f, 0f, progress);

            // Squash effect khi chạm đất
            float squashY = 1f - Mathf.Sin(progress * Mathf.PI) * 0.18f;
            float squashX = 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f;

            diceRect.anchoredPosition = originalPos + new Vector2(0f, yOffset);
            diceRect.localScale = new Vector3(
                originalScale.x * squashX,
                originalScale.y * squashY,
                originalScale.z
            );

            yield return null;
        }

        // ------------------------------------------
        // KẾT THÚC: Reset hoàn toàn về trạng thái gốc
        // ------------------------------------------
        diceRect.anchoredPosition = originalPos;
        diceRect.localScale = originalScale;

        // Mở khoá nút + thông báo kết quả cho GameManager
        btnRollDice.interactable = true;
        GameManager.Instance.YeuCauTungXucXac(finalResult);
    }

    // ==========================================
    // CẬP NHẬT CHỮ KHI CHUYỂN LƯỢT
    // ==========================================
    [Header("Viền Highlight Lượt Chơi")]
    public GameObject borderXanhLa;
    public GameObject borderVang;
    public GameObject borderXanhDuong;
    public GameObject borderDo;
    private Coroutine blinkCoroutine; // Lưu coroutine đang chạy để tắt đúng cách

    private void CapNhatChuToiLuotAi(MauNguoiChoi mau)
    {

        // Dừng coroutine cũ nếu đang chạy
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        // Tắt hết viền trước
        borderXanhLa.SetActive(false);
        borderVang.SetActive(false);
        borderXanhDuong.SetActive(false);
        borderDo.SetActive(false);

        // Bật đúng viền rồi cho nhấp nháy
        GameObject borderHienTai = null;
        switch (mau)
        {
            case MauNguoiChoi.XanhLa: borderHienTai = borderXanhLa; break;
            case MauNguoiChoi.Vang: borderHienTai = borderVang; break;
            case MauNguoiChoi.XanhDuong: borderHienTai = borderXanhDuong; break;
            case MauNguoiChoi.Do: borderHienTai = borderDo; break;
        }

        if (borderHienTai != null)
            blinkCoroutine = StartCoroutine(NhapNhay(borderHienTai));
    }

    // ==========================================
    // COROUTINE NHẤP NHÁY
    // ==========================================
    private IEnumerator NhapNhay(GameObject border)
    {
        SpriteRenderer sr = border.GetComponent<SpriteRenderer>();
        border.SetActive(true);

        while (true) // Nhấp nháy liên tục cho đến khi bị StopCoroutine
        {
            // Fade dần từ hiện -> ẩn
            for (float alpha = 1f; alpha >= 0f; alpha -= Time.deltaTime * 3f)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                yield return null;
            }

            // Fade dần từ ẩn -> hiện
            for (float alpha = 0f; alpha <= 1f; alpha += Time.deltaTime * 3f)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                yield return null;
            }
        }
    }
}