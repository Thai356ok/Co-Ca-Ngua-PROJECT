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
    public DiceThrowAnimation diceThrowAnimation;

    [Header("Bảng thông tin bên phải")]
    public TextMeshProUGUI infoContentText;

    private MauNguoiChoi mauHienTai;
    private int ketQuaXucXacGanNhat = 0;
    private bool daHienThiNguoiDiTruoc = false;

    [Header("Dữ liệu Xúc Xắc")]
    public Sprite[] diceFaces;

    [Header("Viền Highlight Lượt Chơi")]
    public GameObject borderXanhLa;
    public GameObject borderVang;
    public GameObject borderXanhDuong;
    public GameObject borderDo;

    private Coroutine blinkCoroutine;
    private Coroutine thongBaoNguoiDiTruocCoroutine;

    // ==========================================
    // TRẠM BẬT ĂNG-TEN TỔNG HỢP
    // ==========================================
    private void OnEnable()
    {
        InputManager.OnObjectClicked += UpdateUIText;
        GameManager.OnTurnChanged += CapNhatChuToiLuotAi;
        GameManager.OnStateChanged += CapNhatTheoTrangThaiGame;
    }

    // ==========================================
    // TRẠM TẮT ĂNG-TEN TỔNG HỢP
    // ==========================================
    private void OnDisable()
    {
        InputManager.OnObjectClicked -= UpdateUIText;
        GameManager.OnTurnChanged -= CapNhatChuToiLuotAi;
        GameManager.OnStateChanged -= CapNhatTheoTrangThaiGame;
    }

    private void Start()
    {
        AnODiceBenPhai();
        CapNhatTrangThaiTrai("ĐANG ĐỢI\nNGƯỜI CHƠI");
        CapNhatBangThongTin("Nhấn “ĐỔ XÚC XẮC”\nđể bắt đầu.");
    }

    // ==========================================
    // HÀM XỬ LÝ CLICK VẬT THỂ
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
    // CẬP NHẬT UI THEO STATE
    // ==========================================
    private void CapNhatTheoTrangThaiGame(GameState state)
    {
        if (btnRollDice == null) return;

        btnRollDice.interactable = state == GameState.Wait_For_Roll;
    }

    // ==========================================
    // CẬP NHẬT PANEL TRẠNG THÁI BÊN TRÁI
    // ==========================================
    private void CapNhatTrangThaiTrai(string noiDung)
    {
        if (txtTurnInfo == null) return;

        txtTurnInfo.gameObject.SetActive(true);
        txtTurnInfo.text = noiDung;
    }

    // ==========================================
    // CẬP NHẬT BẢNG THÔNG TIN BÊN PHẢI
    // ==========================================
    private void CapNhatBangThongTin(string huongDan)
    {
        if (infoContentText == null) return;

        string tenMau = LayTenMau(mauHienTai);
        string ketQua = ketQuaXucXacGanNhat > 0 ? ketQuaXucXacGanNhat.ToString() : "---";

        infoContentText.text =
            "LƯỢT HIỆN TẠI\n" +
            tenMau + "\n\n" +
            "KẾT QUẢ XÚC XẮC\n" +
            ketQua + "\n\n" +
            "HƯỚNG DẪN\n" +
            huongDan;
    }

    private string LayTenMau(MauNguoiChoi mau)
    {
        switch (mau)
        {
            case MauNguoiChoi.XanhLa:
                return "XANH LÁ";

            case MauNguoiChoi.Vang:
                return "VÀNG";

            case MauNguoiChoi.XanhDuong:
                return "XANH DƯƠNG";

            case MauNguoiChoi.Do:
                return "ĐỎ";

            default:
                return "---";
        }
    }

    private void AnODiceBenPhai()
    {
        if (imgDiceDisplay == null) return;

        Color c = imgDiceDisplay.color;
        c.a = 0f;
        imgDiceDisplay.color = c;
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDiceRollSFX();
        }

        int diceResult = Random.Range(1, 7);

        ketQuaXucXacGanNhat = 0;
        AnODiceBenPhai();

        CapNhatBangThongTin("Đang tung xúc xắc...");
        CapNhatTrangThaiTrai("ĐANG TUNG\nXÚC XẮC");

        if (btnRollDice != null)
        {
            btnRollDice.interactable = false;
        }

        if (diceThrowAnimation != null)
        {
            diceThrowAnimation.PlayThrow(diceResult, () =>
            {
                FinishDiceRoll(diceResult);
            });
        }
        else
        {
            FinishDiceRoll(diceResult);
        }
    }

    private void FinishDiceRoll(int diceResult)
    {
        ketQuaXucXacGanNhat = diceResult;

        AnODiceBenPhai();

        CapNhatBangThongTin("Chọn quân hợp lệ\nđể di chuyển.");
        CapNhatTrangThaiTrai("CHỌN QUÂN\nĐỂ DI CHUYỂN");

        if (imgDiceDisplay != null && diceFaces != null && diceFaces.Length >= diceResult)
        {
            imgDiceDisplay.sprite = diceFaces[diceResult - 1];

            Color c = imgDiceDisplay.color;
            c.a = 0f;
            imgDiceDisplay.color = c;
        }

        GameManager.Instance.YeuCauTungXucXac(diceResult);
    }

    // ==========================================
    // CẬP NHẬT CHỮ KHI CHUYỂN LƯỢT
    // ==========================================
    private void CapNhatChuToiLuotAi(MauNguoiChoi mau)
    {
        mauHienTai = mau;
        ketQuaXucXacGanNhat = 0;

        AnODiceBenPhai();

        if (!daHienThiNguoiDiTruoc)
        {
            daHienThiNguoiDiTruoc = true;

            if (thongBaoNguoiDiTruocCoroutine != null)
            {
                StopCoroutine(thongBaoNguoiDiTruocCoroutine);
            }

            thongBaoNguoiDiTruocCoroutine = StartCoroutine(HienThongBaoNguoiDiTruoc(mau));
        }
        else
        {
            CapNhatBangThongTin("Nhấn “ĐỔ XÚC XẮC”\nđể bắt đầu.");
            CapNhatTrangThaiTrai("ĐANG ĐỢI\nNGƯỜI CHƠI");
        }

        CapNhatVienHighlight(mau);
    }

    private IEnumerator HienThongBaoNguoiDiTruoc(MauNguoiChoi mau)
    {
        string tenMau = LayTenMau(mau);

        CapNhatTrangThaiTrai("NGƯỜI ĐI TRƯỚC\n" + tenMau);

        CapNhatBangThongTin(
            "Người chơi " + tenMau +
            "\nđược đi đầu tiên."
        );

        yield return new WaitForSeconds(1.3f);

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Wait_For_Roll)
        {
            CapNhatTrangThaiTrai("ĐANG ĐỢI\nNGƯỜI CHƠI");

            CapNhatBangThongTin(
                "Nhấn “ĐỔ XÚC XẮC”\nđể bắt đầu."
            );
        }

        thongBaoNguoiDiTruocCoroutine = null;
    }

    // ==========================================
    // HIGHLIGHT LƯỢT CHƠI
    // ==========================================
    private void CapNhatVienHighlight(MauNguoiChoi mau)
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        TatTatCaVienHighlight();

        GameObject borderHienTai = null;

        switch (mau)
        {
            case MauNguoiChoi.XanhLa:
                borderHienTai = borderXanhLa;
                break;

            case MauNguoiChoi.Vang:
                borderHienTai = borderVang;
                break;

            case MauNguoiChoi.XanhDuong:
                borderHienTai = borderXanhDuong;
                break;

            case MauNguoiChoi.Do:
                borderHienTai = borderDo;
                break;
        }

        if (borderHienTai != null)
        {
            blinkCoroutine = StartCoroutine(NhapNhay(borderHienTai));
        }
    }

    private void TatTatCaVienHighlight()
    {
        if (borderXanhLa != null) borderXanhLa.SetActive(false);
        if (borderVang != null) borderVang.SetActive(false);
        if (borderXanhDuong != null) borderXanhDuong.SetActive(false);
        if (borderDo != null) borderDo.SetActive(false);
    }

    private IEnumerator NhapNhay(GameObject border)
    {
        SpriteRenderer sr = border.GetComponent<SpriteRenderer>();

        if (sr == null)
            yield break;

        border.SetActive(true);

        while (true)
        {
            for (float alpha = 1f; alpha >= 0f; alpha -= Time.deltaTime * 3f)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                yield return null;
            }

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