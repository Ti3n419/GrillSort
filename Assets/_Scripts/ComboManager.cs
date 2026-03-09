using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{
    [Header("Combo Config")]
    [SerializeField] private GameObject _comboUIParent; // Object cha chứa toàn bộ UI Combo
    [SerializeField] private Image _comboFillImage;// Thanh Slider tụt dần
    [SerializeField] private TextMeshProUGUI _comboText;// Text hiển thị "Combo x1, x2..."

    private int _currentCombo = 0;
    private float _comboTimeLeft = 0f;
    private float _currentMaxComboTime = 0f;
    // Mảng thời gian tương ứng với từng cấp độ Combo (Index 0 bỏ trống)
    // Cấp 1: 10s | Cấp 2: 8s | Cấp 3: 6s | Cấp 4: 4s | Cấp 5: 3s
    private float[] _comboDurationLimits = new float[] { 0f, 10f, 8f, 6f, 4f, 3f };
    private bool _isComboWarningActive = false; // Cờ đánh dấu đang nháy đỏ
    private Color _originalComboColor = Color.white; // Ghi nhớ màu gốc của thanh Combo

    public int MaxComboAchieved { get; private set; } = 0;// Thuộc tính để GameManager có thể lấy điểm kỷ lục lúc show bảng Win
    private void Awake()
    {
        if (_comboFillImage != null) _originalComboColor = _comboFillImage.color;
        if (_comboUIParent != null) _comboUIParent.SetActive(false);
    }
    private void Update()
    {
        // ĐẾM NGƯỢC THỜI GIAN COMBO
        if (_currentCombo > 0)
        {
            _comboTimeLeft -= Time.deltaTime;
            if (_comboTimeLeft <= _currentMaxComboTime * 0.3 && !_isComboWarningActive)
            {
                _isComboWarningActive = true;
                TriggerComboWarningVFX();
            }
            if (_comboTimeLeft <= 0)
            {
                BreakCombo();
            }
            else
            {
                UpdateComboUI();
            }
        }
    }
        // Hàm này sẽ được GameManager (hoặc GrillStation) gọi khi có 1 merge thành công
    public void IncreaseCombo() 
    {
        // 1. RESET TRẠNG THÁI HÌNH ẢNH (VISUALS)
        // Nếu thanh Combo đang nhấp nháy đỏ (báo động sắp hết giờ), ta phải dừng nó lại ngay
        if (_comboFillImage != null)
        {
            _comboFillImage.DOKill(); // Dừng mọi tween (hiệu ứng) đang chạy trên thanh này
            Color resetColor = _originalComboColor;
            resetColor.a = 1f;
            _comboFillImage.color = resetColor; // Trả về màu gốc (thường là trắng/xanh)
        }
        _isComboWarningActive = false; // Tắt cờ báo động
        
        // 2. TĂNG CẤP ĐỘ COMBO
        // Chỉ tăng tối đa đến 5 (vì mảng _comboDurationLimits chỉ định nghĩa thời gian đến cấp 5)
        if (_currentCombo < 5) _currentCombo++;

        // 3. PHÁT ÂM THANH (SFX)
        // Tùy vào cấp độ combo mà phát tiếng kêu khác nhau (càng cao càng phấn khích)
        switch (_currentCombo)
        {
            case 1: SoundManager.Instance.PlaySFX(SoundType.Combo1); break;
            case 2: SoundManager.Instance.PlaySFX(SoundType.Combo2); break;
            case 3: SoundManager.Instance.PlaySFX(SoundType.Combo3); break;
            case 4: SoundManager.Instance.PlaySFX(SoundType.Combo4); break;
            case 5: SoundManager.Instance.PlaySFX(SoundType.ComboMax); break;
        }

        // 4. GHI NHẬN KỶ LỤC
        // Nếu combo hiện tại cao hơn kỷ lục cũ -> Cập nhật kỷ lục mới (để tính Sao cuối game)
        if (_currentCombo > MaxComboAchieved)
        {
            MaxComboAchieved = _currentCombo;
        }

        // 5. NẠP LẠI THỜI GIAN (REFILL TIMER)
        // Lấy thời gian quy định từ mảng cấu hình (VD: Combo 1 được 10s, Combo 5 chỉ được 3s)
        _comboTimeLeft = _comboDurationLimits[_currentCombo];
        _currentMaxComboTime = _comboTimeLeft;

        // 6. HIỂN THỊ UI & HIỆU ỨNG CHỮ
        if (_comboUIParent != null) _comboUIParent.SetActive(true); // Bật thanh Combo lên
        if (_comboText != null)
        {
            _comboText.text = "Combo X" + _currentCombo; // Cập nhật nội dung text
            
            // Tạo hiệu ứng giật nảy (Punch Scale) cho chữ để tạo cảm giác lực va đập
            _comboText.transform.DOKill();
            _comboText.transform.localScale = Vector3.one;
            _comboText.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 1);
        }        
        // 7. CẬP NHẬT THANH FILL (Đầy lại 100%)
        UpdateComboUI();
    }
    // HÀM HỦY COMBO (KHI HẾT GIỜ HOẶC GAME OVER)
    public void BreakCombo()
    {
        if (_comboFillImage != null)
        {
            _comboFillImage.DOKill(); // Dừng ngay lập tức các hiệu ứng nhấp nháy/fade
            
            // Khôi phục màu sắc ban đầu (tránh bị kẹt ở màu đỏ báo động)
            Color resetColor = _originalComboColor;
            resetColor.a = 1f;
            _comboFillImage.color = resetColor;
        }
        _isComboWarningActive = false; // Tắt trạng thái báo động
        _currentCombo = 0; // Reset cấp độ combo về 0
        _comboTimeLeft = 0f;
        
        // Ẩn toàn bộ UI Combo đi cho gọn màn hình
        if (_comboUIParent != null) _comboUIParent.SetActive(false);
    }
    // CẬP NHẬT THANH HIỂN THỊ (CHẠY MỖI FRAME)
    private void UpdateComboUI()
    {
        if (_comboFillImage != null)
        {
            // Tính toán tỷ lệ lấp đầy: Thời gian còn lại / Tổng thời gian của cấp độ đó
            _comboFillImage.fillAmount = _comboTimeLeft / _currentMaxComboTime;
        }
    }
    // KÍCH HOẠT HIỆU ỨNG CẢNH BÁO (NHẤP NHÁY ĐỎ)
    private void TriggerComboWarningVFX()
    {
        if (_comboFillImage != null)
        {
            _comboFillImage.color = Color.red; // Chuyển sang màu đỏ
            
            // Dùng DOTween để làm mờ đi rồi hiện lại (Yoyo Loop) -> Tạo hiệu ứng nhấp nháy liên tục
            _comboFillImage.DOFade(0.3f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}
