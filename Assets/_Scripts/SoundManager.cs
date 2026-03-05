using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum SoundType
{
    ClickButton,
    ClickItem,
    Combo1,
    Combo2,
    Combo3,
    Combo4,
    ComboMax,
    Win,
    winStar,
    Lose,
    MagnetUse,
    TimeFreezeUse,
    ShuffleUse,
    AddGrillUse,
    TimeLeft
}
// 2. KHUÔN ĐÚC DỮ LIỆU ĐỂ KÉO THẢ TRONG INSPECTOR
[System.Serializable]
public struct SoundConfig
{
    public SoundType soundType;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume; // Cho phép chỉnh to nhỏ riêng từng âm thanh
}
public class SoundManager : MonoBehaviour
{
    // 3. SINGLETON PATTERN - Chìa khóa để gọi SoundManager.Instance từ mọi file code
    public static SoundManager Instance { get; private set; }
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource; // Loa phát nhạc nền (Chạy lặp lại)
    [SerializeField] private AudioSource _sfxSource;   // Loa phát hiệu ứng (Phát đè lên nhau)
    [Header("Sound Library")]
    [SerializeField] private List<SoundConfig> _soundConfigs;

    // Bộ nhớ đệm Dictionary giúp tìm âm thanh cực nhanh (O(1)) thay vì dùng vòng lặp For
    private Dictionary<SoundType, SoundConfig> _soundDictionary;
    private void Awake()
    {
        // Khởi tạo Singleton và đảm bảo nó sống xuyên suốt các Scene (Không bị hủy khi load màn)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ lại Object này khi chuyển Scene
            InitializeDictionary();
        }
        else
        {
            Destroy(gameObject); // Hủy bản sao nếu lỡ tạo ra 2 cái
        }
    }
    private void Start()
    {
        // Đọc dữ liệu từ bộ nhớ (Mặc định tải game lần đầu là 1 - Bật)
        bool isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        bool isSfxOn = PlayerPrefs.GetInt("SFXOn", 1) == 1;

        // Ép trạng thái của Loa theo dữ liệu vừa đọc
        _musicSource.mute = !isMusicOn;
        _sfxSource.mute = !isSfxOn;
    }
    private void InitializeDictionary()
    {
        _soundDictionary = new Dictionary<SoundType, SoundConfig>();
        foreach (var config in _soundConfigs)
        {
            if (!_soundDictionary.ContainsKey(config.soundType))
            {
                _soundDictionary.Add(config.soundType, config);
            }
        }
    }
    // HÀM GỌI PHÁT ÂM THANH HIỆU ỨNG (SFX)
    // ========================================================
    public void PlaySFX(SoundType type)
    {
        if (_soundDictionary.TryGetValue(type, out SoundConfig config))
        {
            if (config.clip != null)
            {
                // Dùng PlayOneShot để các âm thanh chồng lên nhau mà không bị ngắt quãng
                _sfxSource.PlayOneShot(config.clip, config.volume);
            }
        }
        else
        {
            Debug.LogWarning($"[SoundManager] Không tìm thấy âm thanh cho loại: {type}");
        }
    }
    // HÀM QUẢN LÝ NHẠC NỀN (BGM)
    // ========================================================
    public void PlayMusic(AudioClip musicClip, float volume = 0.5f)
    {
        if (_musicSource.clip == musicClip) return; // Nếu đang phát bài này rồi thì thôi

        _musicSource.clip = musicClip;
        _musicSource.volume = volume;
        _musicSource.loop = true;
        _musicSource.Play();
    }
    // Các hàm cho Nút Setting gọi
    public void ToggleMusic(bool isOn)
    {
        _musicSource.mute = !isOn;
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0); // Lưu lại vào máy
        PlayerPrefs.Save();
    }

    public void ToggleSFX(bool isOn)
    {
        _sfxSource.mute = !isOn;
        PlayerPrefs.SetInt("SFXOn", isOn ? 1 : 0); // Lưu lại vào máy
        PlayerPrefs.Save();
    }

    // ========================================================
    // HÀM GỌI RUNG ĐIỆN THOẠI (HAPTICS)
    // ========================================================
    // public void PlayVibrate()
    // {
    //     // Chỉ rung nếu người chơi đang BẬT tính năng Rung trong Setting
    //     if (PlayerPrefs.GetInt("VibrateOn", 1) == 1)
    //     {
    //         Handheld.Vibrate(); // Lệnh rung mặc định của Unity
    //     }
    // }
}
