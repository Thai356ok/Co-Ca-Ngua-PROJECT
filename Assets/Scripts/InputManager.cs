using UnityEngine;
using System;


public class InputManager : MonoBehaviour
{

    private bool isInputEnabled = true;


    public bool IsInputEnabled
    {
        get { return isInputEnabled; }
        set { isInputEnabled = value; }
    }

    // Sự kiện phát sóng khi có vật thể bị click
    public static Action<GameObject> OnObjectClicked;

    void Update()
    {
        // 0 là chuột trái
        if (IsInputEnabled && Input.GetMouseButtonDown(0))
        {
            DetectClick(); // Sử dụng Phương thức 
        }
    }

    private void DetectClick()
    {
        try
        {
            Vector3 screenPos = Input.mousePosition;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                // Vẫn giữ lại phần xử lý IInteractable cho các nút bấm cũ (nếu có)
                IInteractable interactableObj = hit.collider.GetComponent<IInteractable>();
                if (interactableObj != null)
                {
                    interactableObj.OnInteract();
                }

                // ĐIỂM QUYẾT ĐỊNH: Mang cái loa phát sóng ra ngoài chữ IF!
                // Bây giờ, cứ click trúng bất kỳ vật thể nào có Collider là nó sẽ báo cho UI biết.
                OnObjectClicked?.Invoke(hit.collider.gameObject);
            }
        }
        catch (NullReferenceException e)
        {
            Debug.LogError("Lỗi: Không tìm thấy Camera hoặc Vật thể. Chi tiết: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Có lỗi hệ thống Input: " + e.Message);
        }
    }
}