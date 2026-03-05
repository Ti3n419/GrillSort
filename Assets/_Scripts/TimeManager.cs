using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    [Header("Time Config")]
    [SerializeField] private Image _timerFillImage;
    [SerializeField] private TextMeshProUGUI _timeText;

    private float _levelTime;
    private float _currentTime;
    private bool _isTimerRunning = false;
    private int _lastSeconds = -1;// Biến tối ưu hóa, chống cập nhật Text vô tội vạ
    private bool _isTimeWarningActive = false;
    private bool _isTimeFrozen = false; // Cờ đánh dấu thời gian đang bị đóng băng
    public bool IsTimeFrozen => _isTimeFrozen;
    public void InitTime(float totalTime)// Khởi tạo thời gian từ GameManager truyền vào
    {
        _levelTime = totalTime;
        _currentTime = totalTime;
        _isTimerRunning = true;
        _isTimeFrozen = false;
        _isTimeWarningActive = false;
    }
    public void StopTimer() => _isTimerRunning = false;// Tắt đồng hồ

    public void FreezeTime(bool isFreeze) => _isTimeFrozen = isFreeze;
    public void Update()
    {
        if (!_isTimerRunning) return;

        if (!_isTimeFrozen)
        {
            _currentTime -= Time.deltaTime;
            if (_currentTime <= (_levelTime * 0.15) && !_isTimeWarningActive)
            {
                _isTimeWarningActive = true;
                TriggerWarningActiveVFX();
            }
        }
        if (_currentTime <= 0)
        {
            _currentTime = 0;
            _isTimerRunning = false;

            GameManager.Instance.OnLevelFailed_TimeOut();
        }
        UpdateTimerUI();
    }
    private void UpdateTimerUI()
    {
        if (_timerFillImage != null) _timerFillImage.fillAmount = _currentTime / _levelTime;
        if (_timeText != null)
        {
            int currentSecondsInt = Mathf.CeilToInt(_currentTime);
            if (currentSecondsInt != _lastSeconds)
            {
                _lastSeconds = currentSecondsInt;
                _timeText.text = string.Format("{0:00}:{1:00}", currentSecondsInt / 60, currentSecondsInt % 60);
            }
        }
    }
    private void TriggerWarningActiveVFX()
    {
        SoundManager.Instance.PlaySFX(SoundType.TimeLeft);
        if (_timerFillImage != null)
        {
            _timerFillImage.color = Color.red;
            _timerFillImage.DOFade(0.3f, 0.25f).SetLoops(-1, LoopType.Yoyo);
        }
    }
    public void CleanUp()
    {
        if(_timerFillImage != null) _timerFillImage.DOKill();
        if(_timeText != null) _timeText.transform.DOKill();
    }
}
