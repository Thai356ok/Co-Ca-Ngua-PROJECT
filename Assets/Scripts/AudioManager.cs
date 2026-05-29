using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Cấu trúc Singleton: Cho phép các script khác truy cập AudioManager từ bất kỳ đâu
    public static AudioManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource bgmSource; // Nguồn phát nhạc nền
    [SerializeField] private AudioSource sfxSource; // Nguồn phát hiệu ứng âm thanh

    [Header("--- Audio Clips ---")]
    public AudioClip bgmMain;         // Nhạc nền chính
    public AudioClip sfxDiceRoll;     // Tiếng đổ xúc xắc
    public AudioClip sfxTokenMove;    // Tiếng quân di chuyển
    public AudioClip sfxTokenKick;    // Tiếng đá quân
    public AudioClip sfxWin;          // Tiếng thắng trận

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (bgmMain != null)
        {
            PlayBGM(bgmMain);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayDiceRollSFX()
    {
        PlaySFX(sfxDiceRoll);
    }

    public void PlayTokenMoveSFX()
    {
        PlaySFX(sfxTokenMove);
    }

    public void PlayTokenKickSFX()
    {
        PlaySFX(sfxTokenKick);
    }

    public void PlayWinSFX()
    {
        PlaySFX(sfxWin);
    }
}