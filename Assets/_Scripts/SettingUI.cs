using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Objects (Ảnh hiển thị khi TẮT)")]
    [Tooltip("Kéo GameObject chứa ảnh báo hiệu nút đang TẮT vào đây")]
    [SerializeField] private Image _musicOffObj;
    [SerializeField] private Image _sfxOffObj;
    [SerializeField] private Image _vibrateOffObj;

    // Biến lưu trạng thái hiện tại
    private bool _isMusicOn;
    private bool _isSfxOn;
    private bool _isVibrateOn;

    // Hàm này tự chạy mỗi khi bạn MỞ Bảng Setting lên
    private void OnEnable()
    {
        // 1. Đọc dữ liệu từ máy
        _isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        _isSfxOn = PlayerPrefs.GetInt("SFXOn", 1) == 1;
        _isVibrateOn = PlayerPrefs.GetInt("VibrateOn", 1) == 1;

        // 2. Cập nhật Bật/Tắt UI
        UpdateUI();
    }

    public void OnClickMusicToggle()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        
        _isMusicOn = !_isMusicOn; 
        SoundManager.Instance.ToggleMusic(_isMusicOn); 
        UpdateUI(); 
    }

    public void OnClickSfxToggle()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        
        _isSfxOn = !_isSfxOn;
        SoundManager.Instance.ToggleSFX(_isSfxOn);
        UpdateUI();
    }

    public void OnClickVibrateToggle()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        
        _isVibrateOn = !_isVibrateOn;
        PlayerPrefs.SetInt("VibrateOn", _isVibrateOn ? 1 : 0);
        PlayerPrefs.Save();
        
        if (_isVibrateOn) Handheld.Vibrate(); 
        
        UpdateUI();
    }

    // Hàm Bật/Tắt các Object ảnh OFF
    private void UpdateUI()
    {
        // Logic: Nút đang bật (True) -> Ảnh OFF sẽ bị tắt (False). Và ngược lại.
        if (_musicOffObj != null) _musicOffObj.gameObject.SetActive(!_isMusicOn);
        if (_sfxOffObj != null) _sfxOffObj.gameObject.SetActive(!_isSfxOn);
        if (_vibrateOffObj != null) _vibrateOffObj.gameObject.SetActive(!_isVibrateOn);
    }
}