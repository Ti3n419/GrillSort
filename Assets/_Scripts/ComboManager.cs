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
        if (_comboFillImage != null)
        {
            _comboFillImage.DOKill();
            Color resetColor = _originalComboColor;
            resetColor.a = 1f;
            _comboFillImage.color = resetColor;
        }
        _isComboWarningActive = false;
        if (_currentCombo < 5) _currentCombo++;
        switch (_currentCombo)
        {
            case 1: SoundManager.Instance.PlaySFX(SoundType.Combo1); break;
            case 2: SoundManager.Instance.PlaySFX(SoundType.Combo2); break;
            case 3: SoundManager.Instance.PlaySFX(SoundType.Combo3); break;
            case 4: SoundManager.Instance.PlaySFX(SoundType.Combo4); break;
            case 5: SoundManager.Instance.PlaySFX(SoundType.ComboMax); break;
        }
        if (_currentCombo > MaxComboAchieved)
        {
            MaxComboAchieved = _currentCombo;
        }
        _comboTimeLeft = _comboDurationLimits[_currentCombo];
        _currentMaxComboTime = _comboTimeLeft;
        if (_comboUIParent != null) _comboUIParent.SetActive(true);
        if (_comboText != null)
        {
            _comboText.text = "Combo X" + _currentCombo;
            _comboText.transform.DOKill();
            _comboText.transform.localScale = Vector3.one;
            _comboText.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 1);
        }
        UpdateComboUI();
    }
    public void BreakCombo()
    {
        if (_comboFillImage != null)
        {
            _comboFillImage.DOKill();
            Color resetColor = _originalComboColor;
            resetColor.a = 1f;
            _comboFillImage.color = resetColor;
        }
        _isComboWarningActive = false;
        _currentCombo = 0;
        _comboTimeLeft = 0f;
        if (_comboUIParent != null) _comboUIParent.SetActive(false);
    }
    private void UpdateComboUI()
    {
        if (_comboFillImage != null)
        {
            _comboFillImage.fillAmount = _comboTimeLeft / _currentMaxComboTime;
        }
    }
    private void TriggerComboWarningVFX()
    {
        if (_comboFillImage != null)
        {
            _comboFillImage.color = Color.red;
            _comboFillImage.DOFade(0.3f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}
