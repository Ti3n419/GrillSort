using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject _settingsPanel; // Kéo Panel Setting của bạn vào đây
    [Header("Scene Names")]
    [SerializeField] private string _gameplaySceneName = "Gameplay"; // Tên chính xác của Scene Game
    [SerializeField] private string _loadingToGameSceneName = "LoadingScene 1"; // Tên của Scene Loading thứ 2
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _levelText; // Kéo chữ Level ngoài Menu vào đây

    private void Start()
    {
        // Đảm bảo Panel Setting bị tắt khi mới vào Menu
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        // 1. Đọc trí nhớ của máy xem người chơi đang ở Level mấy.
        // Nếu là lần đầu tiên tải game (chưa có Save), nó sẽ tự động trả về số 1.
        int currentSaveLevel = PlayerPrefs.GetInt("CurrentSaveLevel", 1);

        // 2. Gán lên Text hiển thị
        if (_levelText != null)
        {
            _levelText.text = $"LEVEL {currentSaveLevel}";
        }
    }
    // 1. Gắn vào sự kiện OnClick() của Nút PLAY
    public void OnClickPlay()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        // Dùng cây cầu SceneLoader để chuyển sang Gameplay thông qua Loading 2
        SceneLoader.LoadSceneWithLoadingScreen(_gameplaySceneName, _loadingToGameSceneName);
    }
    // 2. Gắn vào sự kiện OnClick() của Nút SETTING
    public void OnClickOpenSettings()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }
    // Gắn vào sự kiện OnClick() của nút "X" (Đóng) bên trong UI Setting
    public void OnClickCloseSettings()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }
    // 3. Gắn vào sự kiện OnClick() của Nút QUIT
    public void OnClickQuit()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton);
        Debug.Log("Quit Game!"); // Log ra Console để test trên Editor
        Application.Quit();      // Lệnh này chỉ hoạt động khi Build ra file thật
    }
}
