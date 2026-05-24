using UnityEngine;
using TMPro;
using UnityEngine.UI; // Thư viện bắt buộc để dùng UI Image và Button

public class GameUIPanel : BasePanel
{
    [Header("Giao diện UI")]
    public TextMeshProUGUI txtTurnInfo;
    public Button btnRollDice;

    // TIÊU CHÍ: Thêm biến để điều khiển cái màn chiếu hình ảnh
    public Image imgDiceDisplay;

    [Header("Dữ liệu Xúc Xắc")]
    // Khai báo một Mảng (Array) để chứa 6 bức ảnh mặt xúc xắc
    public Sprite[] diceFaces;

    private void OnEnable()
    {
        InputManager.OnObjectClicked += UpdateUIText;
    }

    private void OnDisable()
    {
        InputManager.OnObjectClicked -= UpdateUIText;
    }

    private void UpdateUIText(GameObject clickedObj)
    {
        txtTurnInfo.text = "Bạn vừa click vào: " + clickedObj.name;
        txtTurnInfo.color = Color.green;
    }

    // Hàm gắn vào nút bấm
    public void OnRollDiceClicked()
    {
        // Random từ 1 đến 6
        int diceResult = Random.Range(1, 7);

        // Cập nhật chữ
        txtTurnInfo.text = "Xúc xắc đổ ra: " + diceResult + " điểm!";
        txtTurnInfo.color = Color.yellow;

        // CẬP NHẬT HÌNH ẢNH:
        // Vì Mảng trong C# bắt đầu từ vị trí số 0 (Index 0).
        // Nên xúc xắc ra 1 điểm -> Lấy ảnh ở vị trí [0]. Ra 6 điểm -> Lấy ảnh ở vị trí [5].
        // Công thức sẽ là: diceResult - 1
        imgDiceDisplay.sprite = diceFaces[diceResult - 1];

        Debug.Log("Đã đổ xúc xắc ra số: " + diceResult);
    }
}