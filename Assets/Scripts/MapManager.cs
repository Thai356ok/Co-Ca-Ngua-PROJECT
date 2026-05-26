using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Gắn các thư mục cha từ Hierarchy vào đây")]
    public GameObject vongTronChungObj;
    public GameObject duongVeDichXanhLaObj;
    public GameObject duongVeDichVangObj;
    public GameObject duongVeDichXanhDuongObj;
    public GameObject duongVeDichDoObj;

    [Header("Dữ liệu Bản Đồ (Hệ thống tự nạp)")]
    public List<Transform> vongTronChung = new List<Transform>();
    public List<Transform> chuongXanhLa = new List<Transform>();
    public List<Transform> chuongVang = new List<Transform>();
    public List<Transform> chuongXanhDuong = new List<Transform>();
    public List<Transform> chuongDo = new List<Transform>();

    void Awake()
    {
        // Thiết lập Singleton để các Dev khác dễ gọi dữ liệu bản đồ
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        NapDuLieuBanDo();
    }

    void NapDuLieuBanDo()
    {
        // 1. Nạp 56 ô vòng tròn chung
        if (vongTronChungObj != null)
        {
            foreach (Transform child in vongTronChungObj.transform)
            {
                vongTronChung.Add(child);
            }
        }

        // 2. Nạp 6 bậc chuồng Xanh Lá
        NapChuong(duongVeDichXanhLaObj, chuongXanhLa);

        // 3. Nạp 6 bậc chuồng Vàng
        NapChuong(duongVeDichVangObj, chuongVang);

        // 4. Nạp 6 bậc chuồng Xanh Dương
        NapChuong(duongVeDichXanhDuongObj, chuongXanhDuong);

        // 5. Nạp 6 bậc chuồng Đỏ
        NapChuong(duongVeDichDoObj, chuongDo);

        Debug.Log($"[MapManager] Đã nạp thành công {vongTronChung.Count} ô chung và các chuồng!");
    }

    void NapChuong(GameObject chuongObj, List<Transform> danhSachChuong)
    {
        if (chuongObj != null)
        {
            foreach (Transform child in chuongObj.transform)
            {
                danhSachChuong.Add(child);
            }
        }
    }
}