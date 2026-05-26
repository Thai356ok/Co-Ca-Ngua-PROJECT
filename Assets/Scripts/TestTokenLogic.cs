using UnityEngine;

public class TestTokenLogic : MonoBehaviour, IInteractable
{
    public void OnInteract()
    {
        // Khi bị click trúng, báo ra Console và đổi màu ngẫu nhiên
        Debug.Log("Quân cờ vừa bị click!");
        GetComponent<SpriteRenderer>().color = Random.ColorHSV();
    }
}