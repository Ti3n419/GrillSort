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
        // Cập nhật thanh thời gian (fill amount) dựa trên tỷ lệ thời gian còn lại.
        if (_timerFillImage != null) _timerFillImage.fillAmount = _currentTime / _levelTime;

        // Chỉ xử lý nếu có đối tượng Text để hiển thị
        if (_timeText != null)
        {
            // Làm tròn thời gian hiện tại (float) lên số nguyên gần nhất (ví dụ: 59.1s -> 60s).
            // Dùng CeilToInt để người chơi thấy 00:01 thay vì 00:00 khi còn rất ít thời gian.
            int currentSecondsInt = Mathf.CeilToInt(_currentTime);

            // [TỐI ƯU HÓA] Chỉ cập nhật lại UI Text khi số giây thực sự thay đổi.
            // Điều này giúp tránh việc gọi hàm gán text mỗi frame, tiết kiệm hiệu năng.
            if (currentSecondsInt != _lastSeconds)
            {
                _lastSeconds = currentSecondsInt; // Lưu lại số giây mới nhất để so sánh ở frame sau.
                // Định dạng chuỗi hiển thị thành "phút:giây" (ví dụ: 01:30)
                _timeText.text = string.Format("{0:00}:{1:00}", currentSecondsInt / 60, currentSecondsInt % 60);
            }
        }
    }
    private void TriggerWarningActiveVFX()
    {
        // Phát âm thanh cảnh báo sắp hết giờ (tiếng đồng hồ tích tắc dồn dập)
        SoundManager.Instance.PlaySFX(SoundType.TimeLeft);
        if (_timerFillImage != null)
        {
            // Chuyển màu thanh timer sang đỏ để báo động khẩn cấp
            _timerFillImage.color = Color.red;
            // Tạo hiệu ứng nhấp nháy (Fade in/out) liên tục bằng DOTween
            // 0.3f: Độ trong suốt mục tiêu (Alpha giảm xuống còn 30%)
            // 0.25f: Thời gian chạy hiệu ứng (nhanh hay chậm)
            // SetLoops(-1, LoopType.Yoyo): Lặp vô hạn (-1) theo kiểu Yoyo (Mờ đi -> Sáng lại -> Mờ đi...)
            _timerFillImage.DOFade(0.3f, 0.25f).SetLoops(-1, LoopType.Yoyo);
        }
    }
    public void CleanUp() // Dọn dẹp các hiệu ứng DOTween đang chạy trên UI khi kết thúc màn chơi
    {
        if(_timerFillImage != null) _timerFillImage.DOKill(); 
        if(_timeText != null) _timeText.transform.DOKill();
    }
}
