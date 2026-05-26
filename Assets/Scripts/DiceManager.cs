using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Dev 2 tự quản lý xúc xắc vì Dev 1 không làm.
/// Gắn script này vào cùng GameObject với GameManager.
/// </summary>
public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { get; private set; }

    [Header("Kết quả xúc xắc (xem trong Inspector khi chạy)")]
    [SerializeField] private int lastResult = 0;
    public int LastResult => lastResult;

    // Animation delay giả lập (Dev 3 sẽ thay bằng animation thật)
    [SerializeField] private float rollDuration = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// GameManager gọi hàm này. Callback trả về kết quả sau khi "animation" xong.
    /// </summary>
    public void RollDice(Action<int> onComplete)
    {
        StartCoroutine(RollCoroutine(onComplete));
    }

    private IEnumerator RollCoroutine(Action<int> onComplete)
    {
        // TODO: Dev 3 trigger animation xúc xắc ở đây
        yield return new WaitForSeconds(rollDuration);

        lastResult = UnityEngine.Random.Range(1, 7); // 1-6
        Debug.Log($"[DiceManager] Kết quả xúc xắc: {lastResult}");
        onComplete?.Invoke(lastResult);
    }
}