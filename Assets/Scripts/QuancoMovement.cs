using System.Collections;
using UnityEngine;

public class QuancoMovement : MonoBehaviour
{
    public enum MauSac { XanhLa, Vang, XanhDuong, Do }
    public MauSac mauCuaToi;

    [Header("Trạng thái di chuyển")]
    public int viTriHienTai = -1;
    public int soBuocDaDi = 0;
    public bool dangOChuongDich = false;
    public int bacChuongHienTai = 0;

    public bool dangDiChuyen = false;

    [Header("Tọa độ vật lý")]
    public Vector3 viTriChuongBanDau;

    private void Start()
    {
        viTriChuongBanDau = transform.position;
    }

    public void BatDauDiChuyen(int soBuoc)
    {
        if (dangDiChuyen) return;
        StartCoroutine(DiChuyenMuot(soBuoc));
    }

    IEnumerator DiChuyenMuot(int soBuoc)
    {
        dangDiChuyen = true;

        // =====================================================
        // LUẬT MỚI: Nếu đang trong chuồng (viTriHienTai == -1)
        // thì CHỈ di chuyển ra ô xuất phát, dừng lại luôn.
        // TokenLogic sẽ trả về ExitedBase để GameManager
        // cho tung thêm 1 lần nữa mới được đi tiếp.
        // =====================================================
        if (viTriHienTai == -1)
        {
            viTriHienTai = LayViTriXuatPhat();
            // soBuocDaDi giữ nguyên = 0 (chưa đi bước nào trên vòng ngoài)

            Vector3 diemXuatPhat = MapManager.Instance.vongTronChung[viTriHienTai].position;
            float tocDo = 8f;
            while (Vector3.Distance(transform.position, diemXuatPhat) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemXuatPhat, tocDo * Time.deltaTime);
                yield return null;
            }

            dangDiChuyen = false;
            Debug.Log($"[QuancoMovement] Quân {mauCuaToi} xuất chuồng, đứng tại ô {viTriHienTai}. Chờ tung thêm!");
            yield break; // Dừng hẳn, không đi thêm bước nào
        }

        // =====================================================
        // Di chuyển bình thường (đã ở trên bàn cờ)
        // =====================================================
        for (int i = 0; i < soBuoc; i++)
        {
            Vector3 diemDich = Vector3.zero;

            if (soBuocDaDi == 55 || dangOChuongDich)
            {
                dangOChuongDich = true;
                bacChuongHienTai++;
                diemDich = LayToaDoChuongDich(bacChuongHienTai);
            }
            else
            {
                viTriHienTai = (viTriHienTai + 1) % 56;
                soBuocDaDi++;
                diemDich = MapManager.Instance.vongTronChung[viTriHienTai].position;
            }

            float tocDo = 8f;
            while (Vector3.Distance(transform.position, diemDich) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemDich, tocDo * Time.deltaTime);
                yield return null;
            }
        }

        dangDiChuyen = false;
        Debug.Log($"[QuancoMovement] Quân {mauCuaToi} di chuyển xong, đứng tại ô {viTriHienTai}.");
    }

    public int LayViTriXuatPhat()
    {
        switch (mauCuaToi)
        {
            case MauSac.XanhLa:    return 0;
            case MauSac.Vang:      return 14;
            case MauSac.XanhDuong: return 28;
            case MauSac.Do:        return 42;
            default: return 0;
        }
    }

    Vector3 LayToaDoChuongDich(int bac)
    {
        int indexMANG = bac - 1;
        switch (mauCuaToi)
        {
            case MauSac.XanhLa:    return MapManager.Instance.chuongXanhLa[indexMANG].position;
            case MauSac.Vang:      return MapManager.Instance.chuongVang[indexMANG].position;
            case MauSac.XanhDuong: return MapManager.Instance.chuongXanhDuong[indexMANG].position;
            case MauSac.Do:        return MapManager.Instance.chuongDo[indexMANG].position;
            default: return Vector3.zero;
        }
    }
}