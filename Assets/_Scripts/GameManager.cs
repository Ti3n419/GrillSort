using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
public class GameManager : MonoBehaviour
{
    // [THÊM STRUCT NÀY VÀO CUỐI FILE GAMEMANAGER HOẶC BÊN TRONG CLASS GAMEMANAGER ĐỀU ĐƯỢC]
    // Cấu trúc dữ liệu để ghi nhớ vị trí của mục tiêu Nam châm
    private struct MagnetTarget
    {
        public FoodSlot slot;       // Nếu đồ ăn nằm trên vỉ, biến này sẽ có dữ liệu
        public Image trayImage;     // Nếu đồ ăn nằm dưới khay, biến này sẽ có dữ liệu
        public Sprite sprite;       // Hình ảnh của món ăn
    }
    [System.Serializable] // Dòng này giúp hiển thị Struct này ra ngoài bảng Inspector
    public struct BoosterUI
    {
        public Button boosterButton; // Kéo Nút bấm vào đây
        public GameObject lockImage; // Kéo ảnh ổ khóa của nút đó vào đây
    }
    private static GameManager instance;
    public static GameManager Instance => instance;
    [Header("Managers")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private ComboManager comboManager;
    private float _timeFormData;
    private bool _isGameOver = false;
    [Header("Level Data")]
    private LevelData currentLevelData; // Kéo file Level_1.asset của bạn vào đây
    private int _currentLevelIndex = 1; // Biến nhớ xem đang ở màn mấy

    [Header("Cấu hình Level")]
    private int _allFood;// số bộ 3 item 
    private int _totalFood; // Tổng số LOẠI thức ăn muốn xuất hiện (Ví dụ: 5 loại: Táo, Cam, Nho...)
    private int _totalGrill; // Số lượng bếp được phép hoạt động
    private int _lockedGrillCount; // Số bếp BỊ KHÓA
    [SerializeField] private Transform _gridGrill; // Object cha chứa các bếp con
    [SerializeField] private int _autoMatchStart = 1;//
    // Các biến dùng nội bộ
    private List<GrillStation> _listGrill; // Danh sách các bếp thực tế
    private float _avgTray; // Hệ số trung bình để tính số lượng khay (Cẩn thận số này!)
    private List<Sprite> _totalSpriteFood; // Kho chứa tất cả ảnh thức ăn load từ Resources

    // CẤU HÌNH BOOSTER TIME FREEZE (ĐÓNG BĂNG THỜI GIAN)
    [Header("Time Freeze Booster Config")]
    [SerializeField] private Image _timeFreezeIcon;
    [SerializeField] private Image _timeFreezeDummyIcon;
    [SerializeField] private Transform _btnTimeFreezeStartPos; // Vị trí xuất phát (Cái nút)
    [SerializeField] private Transform _centerScreenPos; // vi tri giua man hinh
    [SerializeField] private ParticleSystem _timeFreezeParticles; // Hiệu ứng nổ hạt băng
    //private bool _isTimeFrozen = false; // Cờ đánh dấu thời gian đang bị đóng băng
    // KHAI BÁO BIẾN CHO BOOSTER NAM CHÂM
    [Header("Magnet Booster Config")]
    [SerializeField] private Image _magnetIcon;// Ảnh hình cái Nam Châm
    [SerializeField] private Image _magnetDummyIcon;//Ảnh Dummy của Nam Châm để diễn hoạt
    [SerializeField] private Transform _btnMagnetStartPos;// Vị trí của Nút bấm (Điểm xuất phát)
    [SerializeField] private Transform _magnetTopTargetPos;// Vị trí trên cùng màn hình (Điểm Nam châm bay tới)
    [SerializeField] private ParticleSystem _magnetParticles;// Hiệu ứng hạt (Tia hút)
    [SerializeField] private List<Image> _magnetDummy;// Danh sách 3 ảnh Dummy dùng để diễn anim bay bay

    // CẤU HÌNH HIỆU ỨNG KHÓI (SMOKE VFX)
    // ==========================================
    [Header("Smoke VFX Config")]
    [SerializeField] private List<Sprite> _smokeSprites; // Kéo thả 3 ảnh khói của bạn vào đây
    [SerializeField] private List<Image> _smokeDummies;  // Kho chứa khoảng 3-4 cái ảnh Dummy để diễn hoạt khói
    private bool _isBoosterRunning = false;// Khóa an toàn chống bấm liên tục khi Booster đang diễn anim
    public bool IsBoosterRunning => _isBoosterRunning;
    // CẤU HÌNH PROGRESS UI (TIẾN ĐỘ LEVEL)
    [Header("Progress Config")]
    [SerializeField] private TextMeshProUGUI _progressText;
    [Header("UI Panels")]
    [SerializeField] private GameObject _settingsPanel; // Kéo Panel Setting của bạn vào đây
    // CẤU HÌNH WIN / LOSE UI
    [Header("Win/Lose UI Config")]
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _losepanel;
    [SerializeField] private List<GameObject> _brightStars; // Kéo 3 NGÔI SAO SÁNG (theo thứ tự 1, 2, 3) vào đây
    // [THÊM MỚI]: Nhóm chứa 2 nút bấm để làm hiệu ứng Fade In
    [SerializeField] private CanvasGroup _winButtonsGroup;
    [SerializeField] private CanvasGroup _loseButtonGroup;
    [Header("Booster Lock Config")]
    [SerializeField] private List<BoosterUI> _allBoosters; // Danh sách quản lý tất cả 4 Booster
    private int _lockedBoosterCount; // Biến nhận data từ LevelData

    //private int _maxComboAchieved = 0;// Kỷ lục combo cao nhất đạt được trong màn chơi
    private int _totalMatchTarget;
    [Header("Level Info UI")]
    [SerializeField] private TextMeshProUGUI _levelLabelText; // Kéo Text "LEVEL 1" của bạn vào đây
    [Header("BoosterButton")]
    [SerializeField] private Button _magnetBtn;
    [SerializeField] private Button _shuffleBtn;
    [SerializeField] private Button _addBtn;
    [SerializeField] private Button _timeFreezeBtn;

    private void Awake()
    {
        // 1. Tìm và lấy tất cả các bếp con đang nằm dưới _gridGrill
        _listGrill = Utils.GetListInChild<GrillStation>(_gridGrill);

        // 2. Load toàn bộ ảnh trong thư mục "Resources/item" vào bộ nhớ
        Sprite[] loadedSprite = Resources.LoadAll<Sprite>("item");
        _totalSpriteFood = loadedSprite.ToList();
        instance = this;

        // Đảm bảo Nam châm Dummy và Dummies đồ ăn đều ẩn khi mới vào game
        _magnetDummyIcon.gameObject.SetActive(false);
        foreach (var dummy in _magnetDummy) dummy.gameObject.SetActive(false);

        // Đảm bảo các cục khói bị ẩn đi khi mới vào game
        foreach (var smoke in _smokeDummies)
        {
            smoke.gameObject.SetActive(false);
            // Tự động gắn CanvasGroup nếu bạn quên
            if (smoke.GetComponent<CanvasGroup>() == null) smoke.gameObject.AddComponent<CanvasGroup>();
        }
        // 1. Đọc bộ nhớ máy xem người chơi đang ở level mấy (Mặc định tải game lần đầu là level 1)
        _currentLevelIndex = PlayerPrefs.GetInt("CurrentSaveLevel", 1);

        // 2. Tìm vào thư mục Resources/Levels để móc đúng cuộn băng Level đó ra
        currentLevelData = Resources.Load<LevelData>($"Levels/Level_{_currentLevelIndex}");

        // 3. [Bảo hiểm]: Nếu người chơi chơi hết level 4 rồi mà bạn chưa làm level 5
        if (currentLevelData == null)
        {
            Debug.LogWarning("Đã chơi hết các Level hiện có! Tự động quay về Level 1 chơi lại từ đầu.");
            _currentLevelIndex = 1;
            PlayerPrefs.SetInt("CurrentSaveLevel", 1); // Reset save
            currentLevelData = Resources.Load<LevelData>($"Levels/Level_1");
        }
        // ĐỔ DỮ LIỆU TỪ SCRIPTABLE OBJECT VÀO GAME
        if (currentLevelData != null)
        {
            //_levelTime = currentLevelData.levelTime;
            _timeFormData = currentLevelData.levelTime;
            _allFood = currentLevelData.allFoodSets;
            _totalFood = currentLevelData.totalFoodTypes;

            _totalGrill = currentLevelData.normalGrillCount;
            _lockedGrillCount = currentLevelData.lockedGrillCount;
            // [THÊM MỚI]: Lấy số lượng Booster bị khóa
            _lockedBoosterCount = currentLevelData.lockedBoosterCount;
        }
        else
        {
            Debug.LogError("Chưa gắn Level Data vào GameManager!");
        }
        // Hiển thị chữ LEVEL 1, LEVEL 2 lên màn hình
        if (_levelLabelText != null)
        {
            _levelLabelText.text = $"LEVEL {_currentLevelIndex}";
        }
    }

    private void Start()
    {
        // Bắt đầu tạo màn chơi
        OnInitLevel();
        // [THÊM MỚI]: Bắt đầu vòng lặp tạo khói vô tận khi game bắt đầu
        StartCoroutine(IESpawnSmokeRoutine());
        // --- KHỞI ĐỘNG TIMER ---
        // Ra lệnh cho quản lý thời gian làm việc
        timeManager.InitTime(_timeFormData);
        // [THÊM MỚI]: KHỞI TẠO TIẾN ĐỘ
        _totalMatchTarget = _allFood;
        UpdateProgressUI();// Vẽ UI lần đầu tiên thành "0/10"
        SetupLockedBoosters();
    }
    private void Update()
    {
        if (_isGameOver) return;
        if (_isBoosterRunning) return;
    }
    private void SetupLockedBoosters()
    {
        // 1. CHUẨN BỊ BÀN ĐẠP: Mở khóa tất cả các Booster trước (phòng trường hợp chơi lại)
        foreach (var booster in _allBoosters)
        {
            if (booster.boosterButton != null) booster.boosterButton.interactable = true;
            if (booster.lockImage != null) booster.lockImage.SetActive(false);
        }

        // Nếu level này không yêu cầu khóa cái nào, hoặc quên kéo Booster vào Inspector -> Dừng hàm
        if (_lockedBoosterCount <= 0 || _allBoosters.Count == 0) return;

        // 2. CHỐT CHẶN AN TOÀN: Không thể khóa số lượng lớn hơn tổng số Booster đang có
        int actualLockCount = Mathf.Min(_lockedBoosterCount, _allBoosters.Count);

        // 3. THUẬT TOÁN BỐC THĂM (Fisher-Yates)
        // Tạo một bản sao của danh sách Booster để xáo trộn
        List<BoosterUI> shuffledBoosters = new List<BoosterUI>(_allBoosters);
        shuffledBoosters = shuffledBoosters.OrderBy(x => Random.value).ToList();

        // 4. TIẾN HÀNH KHÓA
        // Lấy đúng số lượng [actualLockCount] Booster đứng đầu danh sách sau khi xáo trộn để khóa
        for (int i = 0; i < actualLockCount; i++)
        {
            BoosterUI targetToLock = shuffledBoosters[i];

            // Tắt tương tác của Nút (Người chơi bấm vào sẽ không có phản hồi)
            if (targetToLock.boosterButton != null)
                targetToLock.boosterButton.interactable = false;

            // Bật hình Ổ khóa đè lên
            if (targetToLock.lockImage != null)
                targetToLock.lockImage.SetActive(true);
        }
    }
    public void OnLevelFailed_TimeOut()
    {
        _isGameOver = true;
        timeManager.CleanUp();
        Debug.Log("⏰ TIME OVER! HẾT GIỜ RỒI!");
        // TODO: Gọi hàm hiển thị màn hình Thua Cuộc (Lose Panel) ở đây
        comboManager.BreakCombo();
        StartCoroutine(IEShowLosePanelSequence());
    }
    public void OnClickNextLevel()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        // 1. Khôi phục thời gian (đề phòng trước đó dính timescale)
        Time.timeScale = 1f;

        // 2. CỘNG LEVEL: Tăng level lên 1 và LƯU LẠI VÀO MÁY
        PlayerPrefs.SetInt("CurrentSaveLevel", _currentLevelIndex + 1);
        PlayerPrefs.Save(); // Ép máy lưu ngay lập tức

        // 3. Dọn dẹp DOTween
        DOTween.KillAll();

        // 4. Load lại đúng cái Scene Gameplay này. 
        // Vì Scene load lại từ đầu, hàm Awake() sẽ tự động gọi Level 2!
        SceneLoader.LoadSceneWithLoadingScreen("GamePlay", "LoadingScene 1"); // Sửa lại tên Scene Loading cho đúng với game của bạn
    }
    public void OnClickBackToMainMenu()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        // Gọi hệ thống chuyển Scene qua màn hình Loading
        // Lưu ý: Thay "LoadingScene_ToMenu" bằng tên chính xác của Scene Loading 1 của bạn
        SceneLoader.LoadSceneWithLoadingScreen("MainMenu", "LoadingScene 1");
    }
    public void OnClickAgainLevel()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        SceneLoader.LoadSceneWithLoadingScreen("GamePlay", "LoadingScene 1");
    }
    // Gắn vào sự kiện OnClick() của nút "X" (Đóng) bên trong UI Setting
    public void OnClickCloseSettings()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }
    // 2. Gắn vào sự kiện OnClick() của Nút SETTING
    public void OnClickOpenSettings()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }
    public void OnClickMagnetBooster()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        if (IsBoosterRunning || DropDragCtrl.IsMouseBusy || IsAnyGrillBusy()) return;//Chặn bấm khi Bếp bận, Chuột bận, hoặc Booster khác đang chạy
        StartCoroutine(IEMagnetBoosterSequence());
    }
    public void OnClickTimeFreezeBooster()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        // Khóa không cho bấm khi đang chạy anim hoặc khi đang đóng băng rồi
        if (IsBoosterRunning || timeManager.IsTimeFrozen || DropDragCtrl.IsMouseBusy || IsAnyGrillBusy()) return;
        StartCoroutine(IETimeFreezeSequence());
    }
    public void OnClickShuffleBooster()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        if (IsBoosterRunning || DropDragCtrl.IsMouseBusy || IsAnyGrillBusy()) return;
        StartCoroutine(IEShuffleBoosterSequence());
    }
    public void OnClickAddGrillBooster()
    {
        SoundManager.Instance.PlaySFX(SoundType.ClickButton); // Thêm sound
        if (IsBoosterRunning || DropDragCtrl.IsMouseBusy || IsAnyGrillBusy()) return;
        StartCoroutine(IEAddGrillSequence());
    }
    // KỊCH BẢN CHÍNH CỦA NAM CHÂM (COROUTINE)
    private IEnumerator IEMagnetBoosterSequence()
    {
        _isBoosterRunning = true;//Sập cầu dao, khóa tương tác

        // PHASE 1: THUẬT TOÁN TÌM KIẾM ĐỒ ĂN TRÊN VỈ (THU THẬP DATA)

        Dictionary<string, List<MagnetTarget>> targetDict = new Dictionary<string, List<MagnetTarget>>();// Tạo cuốn từ điển: Chìa khóa (Key) là Tên món ăn, Giá trị (Value) là Danh sách các Ô đang chứa món đó

        foreach (var grill in _listGrill)// Duyệt qua tất cả các Bếp đang hoạt động
        {
            if (!grill.gameObject.activeInHierarchy) continue;

            // Duyệt qua các ô trên cùng (vỉ nướng) của bếp đó
            foreach (var slot in grill.TotalSlots)
            {
                if (slot.HasFood)
                {
                    string foodName = slot.GetSpriteFood.name;// Lấy tên ảnh làm Key

                    // Nếu cuốn từ điển chưa có tên món này -> Tạo trang mới cho nó
                    if (!targetDict.ContainsKey(foodName))
                    {
                        targetDict.Add(foodName, new List<MagnetTarget>());
                    }

                    // Ghi danh cái Ô (Slot) này vào trang của món đó
                    targetDict[foodName].Add(new MagnetTarget { slot = slot, sprite = slot.GetSpriteFood });
                }
            }
            // 2. Quét KHAY TRÊN CÙNG (Top Tray) của bếp đó
            if (grill.StackTrays != null && grill.StackTrays.Count > 0)
            {
                TrayItem topTray = grill.StackTrays.Peek(); // Chỉ nhìn (Peek) khay trên cùng, không lấy ra (Pop)

                foreach (Image foodImg in topTray.FoodList)
                {
                    // Kiểm tra xem món đồ ăn trong khay có đang hiển thị không
                    if (foodImg != null && foodImg.gameObject.activeInHierarchy && foodImg.sprite != null)
                    {
                        string foodName = foodImg.sprite.name;
                        if (!targetDict.ContainsKey(foodName)) targetDict.Add(foodName, new List<MagnetTarget>());

                        // Đánh dấu đây là mục tiêu nằm dưới Khay
                        targetDict[foodName].Add(new MagnetTarget { trayImage = foodImg, sprite = foodImg.sprite });
                    }
                }
            }

        }
        // Lọc ra NHỮNG MÓN CÓ TỪ 3 Ô TRỞ LÊN (Đủ điều kiện để hút)
        List<List<MagnetTarget>> validGroups = new List<List<MagnetTarget>>();
        foreach (var kvp in targetDict)
        {
            if (kvp.Value.Count >= 3)
            {
                validGroups.Add(kvp.Value);
            }
        }
        // Nếu trên bàn không có bất kỳ bộ 3 nào (Game Over ráng chịu hoặc do vỉ quá lộn xộn)
        if (validGroups.Count == 0)
        {
            Debug.LogWarning("Booster: Không tìm thấy 3 món giống nhau nào trên vỉ!");
            _isBoosterRunning = false;
            yield break; // Hủy chiêu
        }
        // [THÊM MỚI TẠI ĐÂY]: Đã quét thành công, chắc chắn hút -> Khóa nút vĩnh viễn!
        if (_magnetBtn != null)
        {
            _magnetBtn.interactable = false;
        }
        // CHỌN MỤC TIÊU: Lấy ngẫu nhiên 1 nhóm trong số các nhóm hợp lệ
        List<MagnetTarget> chosenGroup = validGroups[Random.Range(0, validGroups.Count)];
        // ÉP CHUẨN MỰC: Chỉ lấy chính xác 3 phần tử đầu tiên (Tránh trường hợp quét ra 4 cái hút hết 4 cái làm lẻ đồ)
        List<MagnetTarget> finalTargets = chosenGroup.Take(3).ToList();
        Sprite foodSpriteToSuck = finalTargets[0].sprite;//Lấy dung nhan món ăn sắp bị hút

        // PHASE 2: XUẤT HIỆN NAM CHÂM VÀ DIỄN HOẠT

        _magnetDummyIcon.sprite = _magnetIcon.sprite;//Gán hình ảnh từ nút gốc sang ảnh Dummy

        // Sử dụng _magnetDummyIcon thay vì _magnetIcon gốc
        _magnetDummyIcon.gameObject.SetActive(true);
        _magnetDummyIcon.rectTransform.position = _btnMagnetStartPos.position;//// Bắt đầu từ Nút bấm UI
        _magnetDummyIcon.rectTransform.localScale = Vector2.zero;
        // [SỬA LỖI ĐỒNG BỘ]: Di chuyển Particle đến đúng vị trí Nam châm bắt đầu bay
        if (_magnetParticles != null)
        {
            _magnetParticles.transform.position = _btnMagnetStartPos.position;
        }
        // Nam châm vừa bay lên vừa phình to ra
        _magnetDummyIcon.rectTransform.DOMove(_magnetTopTargetPos.position, 1.1f).SetEase(Ease.OutBack);
        _magnetDummyIcon.rectTransform.DOScale(Vector3.one * 2f, 1.2f).SetEase(Ease.OutBack);
        // [SỬA LỖI ĐỒNG BỘ]: Bắt Particle bay theo cùng lúc với Nam châm Dummy bằng DOTween
        if (_magnetParticles != null)
        {
            _magnetParticles.transform.DOMove(_magnetTopTargetPos.position, 1f).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(1f);// Chờ nam châm bay tới nơi

        // Bật Particle System để diễn tả "Lực hút từ trường"
        if (_magnetParticles != null) _magnetParticles.Play();
        SoundManager.Instance.PlaySFX(SoundType.MagnetUse);

        // PHASE 3: NHẤC DUMMY LÊN VÀ HÚT VỀ NAM CHÂM (CURVE PATH)
        for (int i = 0; i < 3; i++)
        {
            MagnetTarget target = finalTargets[i];
            Image dummy = _magnetDummy[i];
            Vector3 startPos = Vector3.zero;

            // XỬ LÝ DỮ LIỆU TÙY THEO VỊ TRÍ ĐỒ ĂN (Vỉ hay Khay)
            if (target.slot != null)
            {
                // Món ăn nằm trên vỉ
                startPos = target.slot.transform.position;
                target.slot.OnActiveFood(false);
            }
            else if (target.trayImage != null)
            {
                // Món ăn nằm dưới khay
                startPos = target.trayImage.transform.position;
                target.trayImage.gameObject.SetActive(false); // Xóa sổ khỏi khay luôn
            }
            // Thiết lập Dummy đóng thế
            dummy.gameObject.SetActive(true);
            dummy.sprite = foodSpriteToSuck;
            dummy.SetNativeSize();
            dummy.transform.position = startPos;// Đặt Dummy nằm đúng vị trí đồ thật vừa biến mất
            dummy.color = new Color(1, 1, 1, 1);// Đảm bảo rõ nét

            // [THUẬT TOÁN ĐƯỜNG VÒNG CUNG - BEZIER CURVE]:
            // Tạo 3 điểm để tạo đường vòng cung: Điểm đầu -> Điểm nảy ra giữa chừng -> Đích (Nam châm)
            Vector3 endPos = _magnetDummyIcon.rectTransform.position;

            // [CẬP NHẬT TẠO QUỸ ĐẠO CỐ ĐỊNH HƯỚNG VĂNG]
            // ========================================================
            float offsetX = 0f;

            if (i == 0)
            {
                // Dummy thứ 1: Ép độ lệch X là số ÂM -> Bắt buộc văng cong sang TRÁI
                offsetX = Random.Range(-0.5f, -0.2f);
            }
            else if (i == 1)
            {
                // Dummy thứ 2: Ép độ lệch X là số DƯƠNG -> Bắt buộc văng cong sang PHẢI
                offsetX = Random.Range(0.2f, 0.5f);
            }
            else
            {
                // Dummy thứ 3: Bay thẳng thẳng một chút ở chính giữa
                offsetX = Random.Range(-0.1f, 0.1f);
            }

            // Tạo điểm uốn cong ở giữa đường bay
            Vector3 midPoint = startPos + (endPos - startPos) / 2 + new Vector3(offsetX, Random.Range(1f, 3f), 0);

            Vector3[] path = new Vector3[] { startPos, midPoint, endPos };

            // Phóng Dummy
            dummy.transform.DOPath(path, 0.9f, PathType.CatmullRom).SetEase(Ease.InSine);
            dummy.transform.DORotate(new Vector3(0, 0, Random.Range(180, 360)), 0.9f, RotateMode.FastBeyond360);
            dummy.DOFade(0f, 0.8f).SetDelay(0.3f);

            // Hút chệch nhịp
            yield return new WaitForSeconds(0.15f);
        }
        // Chờ thêm 0.5s cho con Dummy cuối cùng kịp bay vào lòng Nam châm
        yield return new WaitForSeconds(0.5f);

        // PHASE 4: DỌN DẸP CHIẾN TRƯỜNG & TÍNH ĐIỂM
        // Tắt lực hút nam châm
        if (_magnetParticles != null) _magnetParticles.Stop();

        // Nam châm mờ dần và thu nhỏ lại rồi biến mất
        _magnetDummyIcon.rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.5f);
        _magnetDummyIcon.gameObject.SetActive(false);

        // Dọn dẹp Dummy
        foreach (var dummy in _magnetDummy) dummy.gameObject.SetActive(false);

        // TÍNH ĐIỂM: Giảm AllFood vì 1 bộ 3 đã hoàn thành
        OnMinusFood(foodSpriteToSuck.name);// [FIX LỖI]: Báo cho hệ thống biết Nam châm vừa hút xong món gì

        // THÔNG BÁO CHO TỪNG BẾP XỬ LÝ HẬU QUẢ SAU KHI BỊ HÚT
        foreach (var grill in _listGrill)
        {
            if (!grill.gameObject.activeInHierarchy) continue;

            // 1. Quét xem bếp có bị hút mất đồ làm Vỉ bị trống không -> Kéo khay mới lên
            foreach (var slot in grill.TotalSlots)
            {
                if (!slot.HasFood) // Dùng !HasFood thay vì check theo target để chắc chắn quét sạch
                {
                    slot.OnCheckPrepareTray();
                }
            }

            // 2. [GỌI HÀM MỚI TẠO Ở TRÊN]: Quét xem khay trên cùng có bị hút cạn kiệt đồ ăn không -> Xóa khay đó đi
            grill.CheckAndRemoveEmptyTopTray();
        }


        _isBoosterRunning = false; // Mở cầu dao, cho phép người chơi chơi tiếp
    }
    private IEnumerator IEShuffleBoosterSequence()
    {
        _isBoosterRunning = true;
        // PHASE 1: THU HOẠCH (GOM ĐỒ VỀ KHO)
        List<FoodSlot> targetSlots = new List<FoodSlot>();// Danh sách các Ô trên Vỉ đang có đồ
        List<Image> targetTrayImages = new List<Image>();// Danh sách các Ảnh đồ ăn nằm dưới Khay
        List<Sprite> poolSprites = new List<Sprite>();// Rổ chứa toàn bộ hình ảnh đồ ăn vừa gom được

        foreach (var grill in _listGrill)
        {
            if (!grill.gameObject.activeInHierarchy) continue;
            // 1A. Quét Vỉ Nướng
            foreach (var slot in grill.TotalSlots)
            {
                if (slot.HasFood)
                {
                    targetSlots.Add(slot);
                    poolSprites.Add(slot.GetSpriteFood);// Ném ảnh vào rổ
                }
            }
            // 1B. Quét Khay trên cùng
            if (grill.StackTrays != null && grill.StackTrays.Count > 0)
            {
                TrayItem topTray = grill.StackTrays.Peek();
                foreach (Image img in topTray.FoodList)
                {
                    if (img != null && img.gameObject.activeInHierarchy && img.sprite != null)
                    {
                        targetTrayImages.Add(img);
                        poolSprites.Add(img.sprite);// Ném ảnh vào rổ
                    }
                }
            }
        }
        // Chốt chặn: Nếu trên bàn có ít hơn 3 món, thì thôi không xáo nữa
        if (poolSprites.Count < 3)
        {
            Debug.LogWarning("Booster: Bàn chơi quá ít đồ, không thể xáo trộn!");
            _isBoosterRunning = false;
            yield break;
        }
        if (_shuffleBtn != null)
        {
            _shuffleBtn.interactable = false;
        }
        // PHASE 2: DIỄN HOẠT FADE OUT (Mờ dần đi)
        float fadeDuration = 0.5f;
        SoundManager.Instance.PlaySFX(SoundType.ShuffleUse);
        // Làm mờ các món trên Vỉ Nướng
        foreach (var slot in targetSlots)
        {
            // Lấy CanvasGroup, nếu quên chưa gắn trên Inspector thì Code tự động gắn hộ luôn
            CanvasGroup cg = slot.GetComponent<CanvasGroup>();
            if (cg == null) cg = slot.gameObject.AddComponent<CanvasGroup>();

            // Fade Alpha của CanvasGroup về 0
            cg.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
        }
        // Làm mờ các món dưới Khay
        foreach (var img in targetTrayImages)
        {
            // img chính là Image rồi nên gọi thẳng
            img.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
        }
        yield return new WaitForSeconds(fadeDuration + 0.1f);

        // PHASE 3: THUẬT TOÁN "CHIA BÀI BỊP" (SHUFFLE & RIGGING)
        List<Sprite> riggedSprites = new List<Sprite>();
        // BƯỚC 3A: Tìm 1 bộ 3 món giống nhau trong Rổ để lát nữa "Mớm mồi"
        var grouped = poolSprites.GroupBy(x => x.name).Where(g => g.Count() >= 3).ToList();

        if (grouped.Count > 0)
        {
            // Bốc đại 1 nhóm đủ tiêu chuẩn
            var choosen = grouped[Random.Range(0, grouped.Count)];
            riggedSprites = choosen.Take(3).ToList();// Cắt lấy đúng 3 phần tử

            // Rút vĩnh viễn 3 món này ra khỏi Rổ
            foreach (var s in riggedSprites)
            {
                poolSprites.Remove(s);
            }
        }

        // BƯỚC 3B: Xáo lộn xộn các món còn lại trong Rổ
        for (int i = 0; i < poolSprites.Count; i++)
        {
            int rand = Random.Range(i, poolSprites.Count);
            (poolSprites[i], poolSprites[rand]) = (poolSprites[rand], poolSprites[i]);
        }
        // BƯỚC 3C: Xáo lộn thứ tự nhận lại item của các ô có đồ ăn trước đó
        for (int i = 0; i < targetSlots.Count; i++)
        {
            int rand = Random.Range(i, targetSlots.Count);
            (targetSlots[i], targetSlots[rand]) = (targetSlots[rand], targetSlots[i]);
        }

        // PHASE 4: PHÁT BÀI LẠI & DIỄN HOẠT FADE IN (Hiện rõ lên)

        // 4A: Phát đồ ăn cho Vỉ Nướng và Fade In
        foreach (var slot in targetSlots)
        {
            // Gán ảnh mới
            if (riggedSprites.Count > 0)
            {
                slot.OnSetSlot(riggedSprites[0]);
                riggedSprites.RemoveAt(0);
            }
            else
            {
                slot.OnSetSlot(poolSprites[0]);
                poolSprites.RemoveAt(0);
            }
            // Diễn hoạt Fade In bằng Canvas Group
            CanvasGroup cg = slot.GetComponent<CanvasGroup>();
            cg.alpha = 0f; // Ép mờ trước khi chạy hiệu ứng
            cg.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }
        // 4B: Phát đồ ăn cho Khay và Fade In
        foreach (var img in targetTrayImages)
        {
            img.sprite = poolSprites[0]; // Cập nhật hình ảnh mới
            poolSprites.RemoveAt(0);

            // Đảm bảo nó đang mờ trước khi fade lên
            Color c = img.color; c.a = 0f; img.color = c;
            // Diễn hoạt Fade In
            img.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }
        yield return new WaitForSeconds(fadeDuration);
        // [CẬP NHẬT MỚI]: BẮT ÉP CÁC BẾP KIỂM TRA MERGE SAU KHI SHUFFLE
        yield return new WaitForSeconds(0.5f);
        // Phải mở cầu dao trước thì hàm OnCheckMerge mới không bị gián đoạn
        _isBoosterRunning = false;
        foreach (var grill in _listGrill)
        {
            if (grill.gameObject.activeInHierarchy)
            {
                grill.OnCheckMerge();
            }
        }
        yield return new WaitForSeconds(0.7f);
        // (Tùy chọn) Gọi Bếp rung lắc để nhắc nhở
        OnCheckAndShake();

        //_isBoosterRunning = false;//Mở cầu dao
    }
    private IEnumerator IEAddGrillSequence()
    {
        _isBoosterRunning = true; // Sập cầu dao
        GrillStation newGrill = null;
        // 1. TÌM BẾP ĐANG NGỦ ĐÔNG
        // Duyệt qua toàn bộ 12 bếp trong danh sách
        foreach (var grill in _listGrill)
        {
            if (!grill.gameObject.activeInHierarchy)
            {
                newGrill = grill;
                break;
            }
        }
        // 2. NẾU ĐÃ BẬT HẾT 12 BẾP RỒI
        if (newGrill == null)
        {
            Debug.LogWarning("Booster: Đã mở khóa tối đa toàn bộ Bếp, không thể thêm nữa!");
            // (Tùy chọn: Ở đây bạn có thể show UI thông báo "Đã Max Bếp" cho người chơi)

            _isBoosterRunning = false;
            yield break; // Ngắt kịch bản
        }
        if (_addBtn != null)
        {
            _addBtn.interactable = false;
        }
        // 3. DIỄN HOẠT XUẤT HIỆN BẾP MỚI
        newGrill.gameObject.SetActive(true); // Bật điện lên

        // Gọi hàm rửa bếp ta vừa tạo để đảm bảo nó sạch bong sáng bóng
        newGrill.OnInitEmptyGrill();

        // Cập nhật lại số lượng bếp quản lý của hệ thống (Rất quan trọng cho các hàm khác như Shuffle hoạt động đúng)
        _totalGrill++;
        // Bắt đầu từ kích thước bằng 0 (tàng hình)
        newGrill.transform.localScale = Vector3.zero;

        // Nhịp 1: Bơm phồng to lên GẤP ĐÔI (Vector3.one * 2f) rất nhanh trong 0.25 giây
        newGrill.transform.DOScale(Vector3.one * 2f, 0.5f).SetEase(Ease.OutQuad);

        // Đợi 0.25 giây cho nhịp 1 phình xong
        yield return new WaitForSeconds(0.5f);

        // Nhịp 2: Xẹp về lại kích thước CHUẨN (Vector3.one) với hiệu ứng nảy nhẹ (OutBack) trong 0.25 giây
        newGrill.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        SoundManager.Instance.PlaySFX(SoundType.AddGrillUse);

        // Đợi thêm 0.25 giây cho nhịp 2 thu nhỏ xong
        yield return new WaitForSeconds(0.5f);

        _isBoosterRunning = false; // Mở cầu dao
    }
    private IEnumerator IETimeFreezeSequence()
    {
        _isBoosterRunning = true;
        if (_timeFreezeBtn != null)
        {
            _timeFreezeBtn.interactable = false;
        }
        // --- NHỊP 1: BAY RA GIỮA MÀN HÌNH (0.5s) ---
        _timeFreezeDummyIcon.gameObject.SetActive(true);
        _timeFreezeDummyIcon.sprite = _timeFreezeIcon.sprite;
        _timeFreezeDummyIcon.transform.position = _btnTimeFreezeStartPos.position;
        _timeFreezeDummyIcon.transform.localScale = Vector3.one;

        _timeFreezeDummyIcon.transform.DOMove(_centerScreenPos.position, 0.5f).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(0.5f);

        // --- NHỊP 2: THU NHỎ LẠI 30% RỒI PHÌNH TO 150% (0.5s) ---
        _timeFreezeDummyIcon.transform.DOScale(Vector3.one * 0.3f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        _timeFreezeDummyIcon.transform.DOScale(Vector3.one * 4f, 0.5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.1f);
        // --- NHỊP 3: NỔ PARTICLE VÀ BIẾN MẤT ---
        if (_timeFreezeParticles != null)
        {
            _timeFreezeParticles.transform.position = _centerScreenPos.position;
            _timeFreezeParticles.Play();
        }
        SoundManager.Instance.PlaySFX(SoundType.TimeFreezeUse);
        yield return new WaitForSeconds(1f);

        _timeFreezeDummyIcon.gameObject.SetActive(false);

        _isBoosterRunning = false;
        StartCoroutine(IEStartFreezeTimer(10f));
    }
    private IEnumerator IEStartFreezeTimer(float duration)
    {
        timeManager.FreezeTime(true);
        yield return new WaitForSeconds(duration);
        timeManager.FreezeTime(false);
    }
    // VÒNG LẶP SINH KHÓI BẾP
    // ==========================================
    private IEnumerator IESpawnSmokeRoutine()
    {
        // Vòng lặp chạy liên tục miễn là chưa thắng game
        while (_allFood > 0)
        {
            // Nghỉ 3 giây trước khi tạo luồng khói tiếp theo
            yield return new WaitForSeconds(4f);

            // 1. TÌM BẾP ĐỦ ĐIỀU KIỆN (Bếp đang bật VÀ trên vỉ đang có đồ ăn)
            List<GrillStation> cookingGrills = new List<GrillStation>();
            foreach (var grill in _listGrill)
            {
                // Hàm HasGrillEmpty() trả về True nếu rỗng. Vậy !HasGrillEmpty() nghĩa là ĐANG CÓ ĐỒ ĂN.
                if (grill.gameObject.activeInHierarchy && !grill.HasGrillEmpty())
                {
                    cookingGrills.Add(grill);
                }
            }

            // Nếu không có bếp nào đang nướng đồ, bỏ qua nhịp này, chờ 3 giây sau quét lại
            if (cookingGrills.Count == 0) continue;

            // 2. CHỌN NGẪU NHIÊN 1 BẾP TRONG SỐ ĐÓ
            GrillStation targetGrill = cookingGrills[Random.Range(0, cookingGrills.Count)];

            // 3. TÌM 1 DUMMY KHÓI ĐANG RẢNH RỖI TỪ TRONG KHO
            Image freeSmoke = null;
            foreach (var smoke in _smokeDummies)
            {
                if (!smoke.gameObject.activeInHierarchy)
                {
                    freeSmoke = smoke;
                    break;
                }
            }

            // Nếu tất cả khói đều đang bay (hết hàng), thì bỏ qua nhịp này
            if (freeSmoke == null) continue;

            // 4. DIỄN HOẠT KHÓI BAY (Dùng Sequence để chạy song song nhiều hiệu ứng)
            PlaySmokeAnimation(freeSmoke, targetGrill.transform);
        }
    }

    private void PlaySmokeAnimation(Image smokeImg, Transform grillTransform)
    {
        smokeImg.gameObject.SetActive(true);

        // 1. Chọn random 1 ảnh khói
        Sprite chosenSmoke = _smokeSprites[Random.Range(0, _smokeSprites.Count)];
        smokeImg.sprite = chosenSmoke;

        // Đặt vị trí xuất phát
        smokeImg.transform.position = grillTransform.position + new Vector3(0, 0.5f, 0);
        smokeImg.transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);

        // Chuẩn bị thông số random kích thước và lật ảnh (trái/phải)
        float randomSize = Random.Range(0.9f, 1.5f);
        int flip = Random.Range(0, 2) == 0 ? 1 : -1;

        // ========================================================
        // [XỬ LÝ THÔNG MINH]: NHẬN DIỆN VÀ XOAY DỰNG ĐỨNG ẢNH NGANG
        // ========================================================
        // Kiểm tra xem chiều rộng (width) của ảnh gốc có lớn hơn chiều cao (height) không?
        if (chosenSmoke.bounds.size.x > chosenSmoke.bounds.size.y)
        {
            // ĐÂY LÀ ẢNH NGANG -> Bắt nó xoay dựng đứng lên (Xoay trục Z góc 90 độ)
            // (Nếu khói của bạn bị lộn ngược đầu xuống đất, hãy đổi 90f thành -90f nhé)
            smokeImg.transform.localEulerAngles = new Vector3(0, 0, 90f);

            // Vì ảnh đã xoay 90 độ, nên trục Y bây giờ đóng vai trò là chiều ngang.
            // Ta dùng biến 'flip' vào trục Y để lật trái/phải ngẫu nhiên
            smokeImg.transform.localScale = new Vector3(randomSize, randomSize * flip, 1);
        }
        else
        {
            // ĐÂY LÀ ẢNH DỌC -> Giữ nguyên, không xoay (Góc 0 độ)
            smokeImg.transform.localEulerAngles = Vector3.zero;

            // Ảnh dọc bình thường thì lật trái/phải bằng trục X
            smokeImg.transform.localScale = new Vector3(randomSize * flip, randomSize, 1);
        }
        // ========================================================

        CanvasGroup cg = smokeImg.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        smokeImg.transform.DOKill();
        cg.DOKill();

        DG.Tweening.Sequence smokeSequence = DOTween.Sequence();

        // Bay từ từ lên trên thêm khoảng 1.5 unit, mất 2 giây
        smokeSequence.Append(smokeImg.transform.DOMoveY(smokeImg.transform.position.y + 1.5f, 2f).SetEase(Ease.Linear));

        // Fade In
        smokeSequence.Join(cg.DOFade(0.8f, 0.5f));

        // Fade Out
        smokeSequence.Insert(1.2f, cg.DOFade(0f, 0.8f));

        // Cất vào kho
        smokeSequence.OnComplete(() =>
        {
            smokeImg.gameObject.SetActive(false);
        });
    }
    private void UpdateProgressUI()
    {
        if (_progressText != null)
        {
            int currentMerged = _totalMatchTarget - _allFood;

            _progressText.text = $"{currentMerged}/{_totalMatchTarget}";

            // Polish: Cho Text giật nảy lên một chút để ăn mừng mỗi khi nhảy số
            _progressText.transform.DOKill();
            _progressText.transform.localScale = Vector3.one;
            _progressText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 5, 1);
        }
    }
    private void OnInitLevel()
    {
        // BƯỚC 1: CHỌN THỰC ĐƠN (MENU)
        // Lấy ngẫu nhiên _totalFood loại từ kho ảnh, sau đó sắp xếp lộn xộn
        List<Sprite> takeFood = _totalSpriteFood.OrderBy(x => Random.value).Take(_totalFood).ToList();

        List<Sprite> useFood = new List<Sprite>(); // Danh sách tổng tất cả các miếng thức ăn sẽ sinh ra

        // BƯỚC 2: NHÂN BẢN (LOGIC MATCH-3)
        // Với mỗi loại thức ăn được chọn, ta nhân bản lên 3 lần.
        // Tại sao? Để đảm bảo người chơi luôn có đủ 3 miếng để ghép (Match-3). 
        // Nếu không có đoạn này, game có thể không bao giờ thắng được.
        for (int i = 0; i < _allFood; i++)
        {
            int n = i % takeFood.Count;
            for (int j = 0; j < 3; j++)
            {
                useFood.Add(takeFood[n]);
            }
        }

        // BƯỚC 3: TRỘN BÀI (SHUFFLE)
        // Thuật toán Fisher-Yates Shuffle: Đảo lộn vị trí các món ăn để chúng không đứng cạnh nhau (AAA BBB CCC -> ABC BCA CAB)
        for (int i = 0; i < useFood.Count; i++)
        {
            int rand = Random.Range(i, useFood.Count);
            // Cú pháp hoán đổi vị trí (Swap) trong C# mới
            (useFood[i], useFood[rand]) = (useFood[rand], useFood[i]);
        }
        // =========================================================
        // [XỬ LÝ MỚI]: RÚT LÕI VÀ PHÂN TÁN 3 MÓN GIỐNG NHAU LÊN 3 BẾP
        // =========================================================
        // Tạo một danh sách 2 chiều: Mỗi Bếp sẽ có một "Giỏ đồ VIP" riêng (sẽ ép nằm lên Vỉ nướng)
        List<List<Sprite>> forcedTopFoodPerGrill = new List<List<Sprite>>();
        for (int i = 0; i < _totalGrill; i++)
        {
            forcedTopFoodPerGrill.Add(new List<Sprite>());// Cứ mỗi 1 vòng lặp(1 cái bếp), ta Add một cái List<Sprite> mới tinh(1 cái giỏ rỗng).
        }//Sau đoạn này, bạn có 9 cái giỏ rỗng không có gì bên trong.

        // Đảm bảo không mớm mồi nhiều hơn số lượng bộ 3 đang có
        int matchToSpawn = Mathf.Min(_autoMatchStart, _allFood);
        for (int i = 0; i < matchToSpawn; i++)
        {
            // 1. Nhắm mắt lấy bừa 1 món trong kho làm "Mục tiêu"
            Sprite targetSprite = useFood[0];
            // 2. Tìm và bốc đủ 3 món giống hệt mục tiêu đó ra
            List<Sprite> threeItem = useFood.Where(x => x.name == targetSprite.name).Take(3).ToList();
            // 3. Xóa vĩnh viễn 3 món này khỏi kho tổng (để không bị chia lẫn lộn xuống khay nữa)
            foreach (Sprite item in threeItem)
            {
                useFood.Remove(item);
            }
            // 4. Tìm 3 cái BẾP KHÁC NHAU (Và đảm bảo bếp đó chưa bị nhét đầy 3 món VIP)
            List<int> validGrillIndexs = new List<int>();
            for (int g = 0; g < _totalGrill; g++) //lấy cái giỏ của bếp thứ g ra
            {
                if (forcedTopFoodPerGrill[g].Count < 3)// Đếm xem trong cái giỏ đó đang có mấy món rồi? Nếu nhỏ hơn 3 (vì vỉ nướng chỉ có 3 ô) thì bếp này hợp lệ để nhận thêm đồ
                {
                    validGrillIndexs.Add(g);
                }
            }
            // Chọn ngẫu nhiên 3 bếp trong số các bếp hợp lệ
            // List<int> chosenGrills = validGrillIndexs.OrderBy(x => Random.value).Take(3).ToList();
            // // 5. Rải 3 món ăn VIP này vào 3 Bếp vừa chọn (Mỗi bếp 1 món)
            // for (int j = 0; j < 3; j++)
            // {
            //     forcedTopFoodPerGrill[chosenGrills[j]].Add(threeItem[j]);//chosenGrills[j]: ID của cái bếp được chọn (Ví dụ: số 4).
            //                                                              //forcedTopFoodPerGrill[4]: Lấy cái giỏ của Bếp số 4 ra.
            //                                                              //.Add(threeItems[j]): Nhét miếng xúc xích vào cái giỏ đó.
            // }

            // [CẬP NHẬT MỚI]: THUẬT TOÁN RẢI ĐỒ ĂN (1-1-1 hoặc 2-1)
            // =======================================================

            // Quyết định số bếp sẽ nhận quà: Random từ 2 đến 3 bếp.
            // Dùng Mathf.Min để đề phòng level thiết kế sai, chỉ còn lại ít hơn 2 bếp hợp lệ
            int numGrillsToPick = Mathf.Min(Random.Range(2, 4), validGrillIndexs.Count);

            // Chọn ngẫu nhiên 2 hoặc 3 bếp trong số các bếp hợp lệ
            List<int> chosenGrills = validGrillIndexs.OrderBy(x => Random.value).Take(numGrillsToPick).ToList();

            // Rải 3 món ăn VIP này vào các bếp vừa chọn
            for (int j = 0; j < 3; j++)
            {
                bool itemPlaced = false;

                // Xáo lộn ngẫu nhiên danh sách bếp được chọn để không bị ưu tiên dồn vào bếp đầu tiên
                var randomizedChosenGrills = chosenGrills.OrderBy(x => Random.value).ToList();

                foreach (int grillIndex in randomizedChosenGrills)
                {
                    // [CHỐT CHẶN AN TOÀN]: Kiểm tra xem bếp này có bị lấp đầy 3 ô chưa?
                    if (forcedTopFoodPerGrill[grillIndex].Count < 3)
                    {
                        forcedTopFoodPerGrill[grillIndex].Add(threeItem[j]); // Nhét thành công
                        itemPlaced = true;
                        break; // Thoát vòng lặp, chuyển sang rải món đồ tiếp theo
                    }
                }

                // Nếu lỡ xui xẻo mọi bếp được chọn đều đã chật cứng (rất hiếm khi xảy ra)
                if (!itemPlaced)
                {
                    // Trả lại miếng thịt này vào kho tổng (useFood). 
                    // Nó sẽ không xuất hiện trên Vỉ nướng nữa mà sẽ rơi xuống nằm trong Khay (Tray)
                    useFood.Add(threeItem[j]);
                }
            }
        }

        // BƯỚC 4: TÍNH TOÁN SỐ LƯỢNG KHAY (TRAY)
        // Random số lượng trung bình thức ăn trên 1 khay.
        // CẢNH BÁO: Nếu số này quá cao -> Số lượng đĩa (totalTray) sẽ ít đi -> Dễ gây Crash game ở GrillStation
        _avgTray = Random.Range(1.7f, 1.9f);

        // Tổng số đĩa cần thiết = Tổng thức ăn / Sức chứa trung bình
        int totalTray = Mathf.RoundToInt(useFood.Count / _avgTray);// Lưu ý: useFood.Count lúc này đã giảm đi do ta vừa rút lõi ở trên

        // BƯỚC 5: CHIA ĐỀU (LOAD BALANCING)
        // Dùng thuật toán chia đều để tính xem mỗi bếp nhận bao nhiêu cái đĩa, bao nhiêu miếng thịt
        List<int> trayPerGrill = this.DistributeEvelyn(_totalGrill, totalTray);
        List<int> foodPerGrill = this.DistributeEvelyn(_totalGrill, useFood.Count);

        // BƯỚC 6: PHÂN PHỐI VỀ TỪNG BẾP VÀ XỬ LÝ BẾP KHÓA
        int totalActiveGrillsNeeded = _totalGrill + _lockedGrillCount;
        // [THÊM MỚI CHỖ NÀY]: Lấy danh sách các LOẠI đồ ăn đang có trong màn này
        // (Hàm Distinct() giúp lọc ra các món không bị trùng lặp)
        List<Sprite> uniqueFoodTypes = useFood.Distinct().ToList();

        // --- A. THUẬT TOÁN BỐC THĂM (RANDOM HÓA VỊ TRÍ) ---
        // Tạo một danh sách vai trò (true = Bếp Thường, false = Bếp Khóa)
        List<bool> grillRoles = new List<bool>();
        for (int i = 0; i < _totalGrill; i++) grillRoles.Add(true);        // Nhét phiếu Bếp Thường vào rổ
        for (int i = 0; i < _lockedGrillCount; i++) grillRoles.Add(false); // Nhét phiếu Bếp Khóa vào rổ

        // Xáo trộn rổ phiếu lên (Fisher-Yates Shuffle)
        for (int i = 0; i < grillRoles.Count; i++)
        {
            int rand = Random.Range(i, grillRoles.Count);
            (grillRoles[i], grillRoles[rand]) = (grillRoles[rand], grillRoles[i]);
        }

        // Biến đếm phụ: Dùng để phát đúng khay đĩa cho bếp thường mà không bị lệch
        int normalGrillIndex = 0;

        // --- B. ÁP DỤNG VAI TRÒ CHO TỪNG BẾP ---
        for (int i = 0; i < _listGrill.Count; i++)
        {
            GrillStation currentGrill = _listGrill[i];

            // 1. Nếu bếp nằm ngoài tổng số lượng cần dùng -> Tắt hẳn cho cút vào kho
            if (i >= totalActiveGrillsNeeded)
            {
                currentGrill.gameObject.SetActive(false);
                continue;
            }

            // 2. Bật bếp lên
            currentGrill.gameObject.SetActive(true);

            // 3. Cho bếp bốc thăm xem nó trúng vai trò gì
            bool isNormalGrill = grillRoles[i];

            if (isNormalGrill)
            {
                // ĐÂY LÀ BẾP THƯỜNG
                // Dùng normalGrillIndex thay vì i để lấy đúng phần đồ ăn đã chia cho bếp thường
                List<Sprite> normalFoodList = Utils.TakeAndRemoveRandom<Sprite>(useFood, foodPerGrill[normalGrillIndex]);
                currentGrill.OnInitGrill(trayPerGrill[normalGrillIndex], normalFoodList, forcedTopFoodPerGrill[normalGrillIndex]);

                normalGrillIndex++; // Tăng biến đếm lên để bếp thường tiếp theo nhận phần quà tiếp theo
            }
            else
            {
                // ĐÂY LÀ BẾP KHÓA
                // Bốc ngẫu nhiên 1 loại đồ ăn làm chìa khóa
                Sprite unlockKeySprite = uniqueFoodTypes[Random.Range(0, uniqueFoodTypes.Count)];

                // Ra lệnh khóa bếp
                currentGrill.InitLockedState(unlockKeySprite);
            }
        }
    }
    // --- THUẬT TOÁN CHIA ĐỀU (Load Balancing Algorithm) ---
    // Mục đích: Chia 'totalItems' cho 'bucketCount' (số bếp) sao cho đều nhất có thể.
    // Ví dụ: Có 10 kẹo chia 3 người -> Kết quả: [3, 3, 4] (Không để ai quá đói hoặc quá no)
    private List<int> DistributeEvelyn(int grillCount, int totalItems)
    {
        List<int> result = new List<int>();

        float avg = (float)totalItems / grillCount; // Ví dụ: 10/3 = 3.33
        int low = Mathf.FloorToInt(avg); // Làm tròn xuống = 3
        int high = Mathf.CeilToInt(avg); // Làm tròn lên = 4

        // Tính toán xem có bao nhiêu người nhận phần nhiều (high) và bao nhiêu người nhận phần ít (low)
        int highCount = totalItems - low * grillCount;
        int lowCount = grillCount - highCount;

        // Thêm phần ít vào danh sách
        for (int i = 0; i < lowCount; i++) result.Add(low);
        // Thêm phần nhiều vào danh sách
        for (int i = 0; i < highCount; i++) result.Add(high);

        // Trộn ngẫu nhiên danh sách kết quả để không phải lúc nào người đầu tiên cũng nhận phần ít
        for (int i = 0; i < result.Count; i++)
        {
            int rand = Random.Range(i, result.Count);
            (result[i], result[rand]) = (result[rand], result[i]);
        }
        return result;
    }
    public void OnMinusFood(string mergedFoodName)
    {
        --_allFood;

        comboManager.IncreaseCombo();
        this.UpdateProgressUI();
        // PHÁT LOA: Yêu cầu các bếp kiểm tra xem có đúng chìa khóa mở nắp không
        foreach (var grill in _listGrill)
        {
            if (grill.gameObject.activeInHierarchy)
            {
                grill.CheckAndUnlock(mergedFoodName);
            }
        }
        if (_allFood <= 0)
        {
            Debug.Log("Level Complete");
            _isGameOver = true;
            timeManager.StopTimer();
            timeManager.CleanUp();
            comboManager.BreakCombo();
            StartCoroutine(IEShowWinPanelSequence());
        }
    }
    public void AddOneActiveGrill()
    {
        _totalGrill++; // Tăng số lượng bếp thường lên để thuật toán Shuffle/Booster không bị lỗi
    }
    public void OnCheckAndShake()
    {
        Dictionary<string, List<FoodSlot>> groups = new Dictionary<string, List<FoodSlot>>();
        foreach (var grill in _listGrill)
        {
            if (grill.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < grill.TotalSlots.Count; i++)
                {
                    FoodSlot slot = grill.TotalSlots[i];
                    if (slot.HasFood)
                    {
                        string name = slot.GetSpriteFood.name;
                        if (!groups.ContainsKey(name))
                            groups.Add(name, new List<FoodSlot>());
                        groups[name].Add(slot);
                    }
                }
            }
        }
        foreach (var kvp in groups)
        {
            if (kvp.Value.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    kvp.Value[i].DoShake();
                }
                return;
            }
        }
    }
    // ==========================================
    // KỊCH BẢN HIỂN THỊ WIN PANEL (FIX BUG SAO)
    // ==========================================
    private IEnumerator IEShowWinPanelSequence()
    {
        yield return new WaitForSeconds(0.5f);
        SoundManager.Instance.PlaySFX(SoundType.Win);
        RectTransform winRect = _winPanel.GetComponent<RectTransform>();
        winRect.anchoredPosition = new Vector2(0, 1500f);

        // 1. ÉP TÀNG HÌNH SAO SÁNG (Dọn sạch DOTween rác)
        foreach (var star in _brightStars)
        {
            if (star != null)
            {
                star.transform.DOKill(); // <-- Cực kỳ quan trọng: Giết các hiệu ứng cũ
                star.SetActive(false);
                star.transform.localScale = Vector3.zero;
            }
        }

        if (_winButtonsGroup != null)
        {
            _winButtonsGroup.DOKill(); // Dọn dẹp nút luôn
            _winButtonsGroup.alpha = 0f;
            _winButtonsGroup.interactable = false;
            _winButtonsGroup.blocksRaycasts = false;
        }

        _winPanel.SetActive(true);

        // 2. PANEL RƠI XUỐNG
        winRect.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);

        // 3. TÍNH SAO
        int starsEarned = comboManager.MaxComboAchieved;
        if (comboManager.MaxComboAchieved >= 5) starsEarned = 3;
        else if (comboManager.MaxComboAchieved >= 3) starsEarned = 2;
        else if (comboManager.MaxComboAchieved >= 1) starsEarned = 1;

        // [LOG KIỂM TRA]: In ra console xem game có đang tính đúng sao không
        Debug.Log($"Max Combo: {comboManager.MaxComboAchieved} -> Earned: {starsEarned} Stars");

        yield return new WaitForSeconds(0.2f);

        // 4. DIỄN HOẠT ĐẬP SAO (DÙNG SEQUENCE CHỐNG LỖI)
        for (int i = 0; i < starsEarned; i++)
        {
            GameObject brightStar = _brightStars[i];

            // Nếu bạn quên kéo sao vào Inspector, nó sẽ bỏ qua để không văng lỗi game
            if (brightStar == null) continue;

            brightStar.SetActive(true);

            // Dùng Sequence tạo 2 nhịp đập liên tiếp (Không dùng OnComplete nữa)
            DG.Tweening.Sequence starSeq = DOTween.Sequence();

            // Nhịp 1: Bơm bự lên 1.5 lần rất nhanh (0.2s)
            starSeq.Append(brightStar.transform.DOScale(Vector3.one * 1.5f, 0.2f).SetEase(Ease.OutQuad));

            // Nhịp 2: Co về kích thước thường 1.0 với độ nảy (0.2s)
            starSeq.Append(brightStar.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
            SoundManager.Instance.PlaySFX(SoundType.winStar);
            // Đợi ngôi sao đập xong rồi mới sang ngôi sao tiếp theo
            yield return new WaitForSeconds(0.4f);
        }

        // 5. FADE IN NÚT BẤM
        yield return new WaitForSeconds(0.2f);

        if (_winButtonsGroup != null)
        {
            _winButtonsGroup.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad);
            _winButtonsGroup.interactable = true;
            _winButtonsGroup.blocksRaycasts = true;
        }
    }
    private IEnumerator IEShowLosePanelSequence()
    {
        yield return new WaitForSeconds(0.5f);
        SoundManager.Instance.PlaySFX(SoundType.Lose);
        RectTransform loseRect = _losepanel.GetComponent<RectTransform>();
        loseRect.anchoredPosition = new Vector2(0, 1500f);

        if (_loseButtonGroup != null)
        {
            _loseButtonGroup.DOKill();
            _loseButtonGroup.alpha = 0;
            _loseButtonGroup.interactable = false;
            _loseButtonGroup.blocksRaycasts = false;

        }
        _losepanel.SetActive(true);
        // 2. PANEL RƠI XUỐNG
        loseRect.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);
        // 5. FADE IN NÚT BẤM
        yield return new WaitForSeconds(0.1f);

        if (_loseButtonGroup != null)
        {
            _loseButtonGroup.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad);
            _loseButtonGroup.interactable = true;
            _loseButtonGroup.blocksRaycasts = true;
        }
    }
    public bool IsAnyGrillBusy()
    {
        foreach (var grill in _listGrill)
        {
            if (grill.gameObject.activeInHierarchy && (grill.IsMerging || grill.IsPreparingTray))
                return true; // Có bếp đang bận!
        }
        return false;
    }
    // Hàm này tự động chạy khi GameManager bị tiêu diệt (Lúc chuyển Scene hoặc tắt game)
    private void OnDestroy()//hàm này vào để nó đi hỏi thăm 12 cái bếp xem có ai đang Merge đồ không:
    {
        // Chốt chặn cuối cùng: Đảm bảo không có cái Tween "ma" nào lọt sang Scene khác
        DOTween.KillAll();
    }

}