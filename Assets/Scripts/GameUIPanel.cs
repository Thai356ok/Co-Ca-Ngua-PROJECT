using UnityEngine;
using TMPro;
using UnityEngine.UI; // Thư viện bắt buộc để dùng UI Image và Button

public class GameUIPanel : BasePanel
{
    [Header("Giao diện UI")]
    public TextMeshProUGUI txtTurnInfo;
    public Button btnRollDice;

    // Biến để điều khiển cái màn chiếu hình ảnh xúc xắc
    public Image imgDiceDisplay;

    [Header("Dữ liệu Xúc Xắc")]
    // Khai báo một Mảng (Array) để chứa 6 bức ảnh mặt xúc xắc
    public Sprite[] diceFaces;

    // ==========================================
    // TRẠM BẬT ĂNG-TEN TỔNG HỢP
    // ==========================================
    private void OnEnable()
    {
        // 1. Nghe click chuột (Bài của bạn)
        InputManager.OnObjectClicked += UpdateUIText;

        // 2. Nghe lệnh chuyển lượt (Từ Dev 2)
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

    // Hàm đổi chữ khi click chuột trúng vật thể
    // Hàm đổi chữ khi click chuột trúng vật thể
    private void UpdateUIText(GameObject clickedObj)
    {
        txtTurnInfo.text = "Bạn vừa click vào: " + clickedObj.name;
        txtTurnInfo.color = Color.green;

        // ====================================================
        // ĐƯỜNG DÂY MỚI: BÁO CHO DEV 2 BIẾT QUÂN NÀO BỊ CLICK
        // ====================================================

        // 1. "Nội soi" vật thể vừa click xem nó có chứa đoạn code QuancoMovement của Dev 1 không?
        QuancoMovement quanCo = clickedObj.GetComponent<QuancoMovement>();

        // 2. Nếu có (tức là người chơi click chuẩn xác vào quân cờ, chứ không click nhầm ra nền đất)
        if (quanCo != null)
        {
            // 3. Gọi Bộ não của Dev 2 dậy và ném quân cờ này vào cho nó kiểm tra!
            GameManager.Instance.ChonQuan(quanCo);
        }
    }

    // Hàm gắn vào nút bấm Đổ Xúc Xắc
    public void OnRollDiceClicked()
    {
        // ====================================================
        // CHỐT CỬA BẢO VỆ: Nếu Dev 2 bảo chưa phải lúc đổ xúc xắc thì KHÔNG LÀM GÌ CẢ
        // ====================================================
        if (GameManager.Instance.CurrentState != GameState.Wait_For_Roll)
        {
            Debug.LogWarning("Không thể đổ xúc xắc lúc này! Đang chờ đi quân hoặc chờ lượt.");
            return; // Lệnh này giúp văng ra khỏi hàm ngay lập tức, các dòng dưới sẽ không chạy!
        }

        // 1. Dev 3 random ra số (từ 1 đến 6)
        int diceResult = Random.Range(1, 7);

        // 2. Cập nhật hình ảnh lên màn hình (diceResult - 1 vì mảng bắt đầu từ 0)
        imgDiceDisplay.sprite = diceFaces[diceResult - 1];

        // 3. CẮM DÂY: Ném số điểm này thẳng vào bụng GameManager của Dev 2
        GameManager.Instance.YeuCauTungXucXac(diceResult);
    }

    // Hàm này sẽ TỰ ĐỘNG CHẠY mỗi khi Dev 2 phát loa báo chuyển lượt
    private void CapNhatChuToiLuotAi(MauNguoiChoi mau)
    {
        txtTurnInfo.text = "Bây giờ là lượt của màu: " + mau.ToString() + " !!!";
        txtTurnInfo.color = Color.yellow; // Mình để màu vàng cho nổi bật nhé
    }
}