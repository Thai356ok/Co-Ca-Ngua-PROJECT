using UnityEngine;
using TMPro; // Thư viện dùng Text của Unity
using UnityEngine.UI; // Thư viện để xài Nút bấm (Button)

public class GameUIPanel : BasePanel
{
    [Header("UI Elements")]
    public TextMeshProUGUI txtTurnInfo;
    public Button btnRollDice;

    // Ghi đè phương thức từ class cha 
    public override void ShowPanel()
    {
        base.ShowPanel();
        txtTurnInfo.text = "Trò chơi bắt đầu!";
    }

    // Khi UI bật lên -> Cắm ăng-ten lắng nghe sự kiện click chuột
    private void OnEnable()
    {
        InputManager.OnObjectClicked += HandleObjectClicked;
    }

    // Khi UI tắt đi -> Phải rút ăng-ten để không bị lỗi tràn bộ nhớ
    private void OnDisable()
    {
        InputManager.OnObjectClicked -= HandleObjectClicked;
    }

    // Hàm này sẽ TỰ ĐỘNG CHẠY khi InputManager "Có người vừa click!"
    private void HandleObjectClicked(GameObject clickedObj)
    {
        txtTurnInfo.text = "Bạn vừa nhấn vào: " + clickedObj.name;
        txtTurnInfo.color = Color.green; // Đổi màu chữ cho sinh động
    }

    // Hàm này bắt buộc phải có chữ "public" để Unity có thể nhìn thấy và gán vào nút bấm
    public void OnRollDiceClicked()
    {      
        int diceResult = Random.Range(1, 7);

        // Cập nhật lên màn hình UI
        txtTurnInfo.text = "Xúc xắc đổ ra: " + diceResult + " điểm!";
        txtTurnInfo.color = Color.yellow;

        Debug.Log("Đã đổ xúc xắc ra số: " + diceResult);
    }
}