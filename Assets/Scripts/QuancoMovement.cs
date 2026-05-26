using System.Collections;
using UnityEngine;

public class QuancoMovement : MonoBehaviour
{
    // Thứ tự màu đã được chỉnh lại theo đúng ý bạn
    public enum MauSac { XanhLa, Vang, XanhDuong, Do }
    public MauSac mauCuaToi;

    [Header("Trạng thái di chuyển")]
    public int viTriHienTai = -1;
    public int soBuocDaDi = 0;
    public bool dangOChuongDich = false;
    public int bacChuongHienTai = 0;

    // Biến cờ hiệu để báo cho các Dev khác biết ngựa có đang di chuyển hay không
    public bool dangDiChuyen = false;

    // ==========================================
    // BẢN VÁ CỦA DEV 3: LƯU TỌA ĐỘ NHÀ
    // ==========================================
    [Header("Tọa độ vật lý")]
    public Vector3 viTriChuongBanDau; // Nơi lưu tọa độ gốc

    private void Start()
    {
        // Khi game vừa bật lên, ngựa đang đứng ở đâu (trong chuồng), lưu ngay vị trí đó lại!
        viTriChuongBanDau = transform.position;
    }
    // ==========================================

    // --- MOVEMENT CONTROLLER: Nơi nhận lệnh chạy ---
    public void BatDauDiChuyen(int soBuoc)
    {
        if (dangDiChuyen) return;
        StartCoroutine(DiChuyenMuot(soBuoc));
    }

    IEnumerator DiChuyenMuot(int soBuoc)
    {
        dangDiChuyen = true;

        for (int i = 0; i < soBuoc; i++)
        {
            Vector3 diemDich = Vector3.zero;

            // --- PATH ROUTING: Logic định tuyến chia ngã rẽ ---
            // Bàn cờ 56 ô, đi đủ 55 bước là tới sát cửa chuồng, bước tiếp theo phải rẽ
            if (soBuocDaDi == 55 || dangOChuongDich)
            {
                dangOChuongDich = true;
                bacChuongHienTai++; // Cộng dồn bậc chuồng
                diemDich = LayToaDoChuongDich(bacChuongHienTai);
            }
            else
            {
                // Đang đi dạo trên 56 ô vòng tròn chung
                if (viTriHienTai == -1)
                {
                    // Lần đầu xuất chuồng thì nhảy ra đúng tọa độ xuất phát
                    viTriHienTai = LayViTriXuatPhat();
                }
                else
                {
                    viTriHienTai = (viTriHienTai + 1) % 56; // Bàn cờ 56 ô nên chia dư cho 56
                    soBuocDaDi++;
                }

                diemDich = MapManager.Instance.vongTronChung[viTriHienTai].position;
            }

            // --- DI CHUYỂN TỊNH TIẾN ---
            float tocDo = 8f;
            while (Vector3.Distance(transform.position, diemDich) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemDich, tocDo * Time.deltaTime);
                yield return null;
            }
        }

        dangDiChuyen = false;
        Debug.Log($"[Dev 1 - Hạ tầng] Quân {mauCuaToi} đã chạy xong điểm đến. Các Dev khác làm luật ăn/đá/thắng thua tiếp quản nhé!");
    }

    // Tọa độ xuất phát chính xác theo thiết kế mới của bạn
    public int LayViTriXuatPhat()
    {
        switch (mauCuaToi)
        {
            case MauSac.XanhLa: return 0;
            case MauSac.Vang: return 14;
            case MauSac.XanhDuong: return 28;
            case MauSac.Do: return 42;
            default: return 0;
        }
    }

    // Lấy tọa độ 1 trong 6 bậc chuồng đích
    Vector3 LayToaDoChuongDich(int bac)
    {
        int indexMANG = bac - 1; // Vì List đếm từ 0, nên Bậc 1 sẽ là vị trí số 0
        switch (mauCuaToi)
        {
            case MauSac.XanhLa: return MapManager.Instance.chuongXanhLa[indexMANG].position;
            case MauSac.Vang: return MapManager.Instance.chuongVang[indexMANG].position;
            case MauSac.XanhDuong: return MapManager.Instance.chuongXanhDuong[indexMANG].position;
            case MauSac.Do: return MapManager.Instance.chuongDo[indexMANG].position;
            default: return Vector3.zero;
        }
    }
}