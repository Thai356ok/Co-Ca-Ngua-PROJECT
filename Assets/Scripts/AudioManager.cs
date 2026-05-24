using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Cấu trúc Singleton: Cho phép các Dev khác truy cập AudioManager từ bất kỳ đâu
    public static AudioManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource bgmSource; // Nguồn phát nhạc nền (lặp đi lặp lại)
    [SerializeField] private AudioSource sfxSource; // Nguồn phát âm thanh hiệu ứng (phát 1 lần)

    [Header("--- Audio Clips (Nhạc và Âm thanh) ---")]
    public AudioClip bgmMain;         // Nhạc nền chính
    public AudioClip sfxDiceRoll;      // Tiếng đổ xúc xắc
    public AudioClip sfxTokenMove;     // Tiếng ngựa di chuyển
    public AudioClip sfxTokenKick;     // Tiếng đá ngựa
    public AudioClip sfxWin;           // Tiếng thắng trận

    private void Awake()
    {
        // Khởi tạo và bảo vệ Singleton không bị trùng lặp
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ âm thanh không bị ngắt khi đổi Scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Thử phát nhạc nền ngay khi vào game (nếu đã kéo file nhạc vào)
        if (bgmMain != null)
        {
            PlayBGM(bgmMain);
        }
    }

    // Hàm phát nhạc nền (BGM)
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // Hàm phát âm thanh hiệu ứng (SFX) - Hàm này các Dev khác sẽ gọi rất nhiều
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}