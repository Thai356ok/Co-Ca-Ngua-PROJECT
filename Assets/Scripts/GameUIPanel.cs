using UnityEngine;


public class GameUIPanel : BasePanel
{
    
    public override void ShowPanel()
    {
        base.ShowPanel(); // Chạy lệnh của cha
        Debug.Log("UI Gameplay Đã Sẵn Sàng Nhận Lệnh!"); // Thêm lệnh riêng của con
    }
}