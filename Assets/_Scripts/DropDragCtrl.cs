using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Thư viện tạo Animation mượt mà

public class DropDragCtrl : MonoBehaviour
{
    // =========================================================
    // PHẦN 1: KHAI BÁO DIỄN VIÊN (BIẾN TRẠNG THÁI)
    // =========================================================
    [Header("UI References - Thành phần giao diện")]
    // Bức ảnh "Đóng thế" (Dummy): Đồ ăn thật không bao giờ di chuyển. 
    // Khi người chơi nhấc đồ lên, ta giấu đồ ăn thật đi và cho bức ảnh này hiện ra bay theo chuột.
    [SerializeField] private Image _imgFoodDrag;

    [Header("State Variables - Bộ nhớ của hệ thống")]
    // VỊ TRÍ XUẤT PHÁT: Ghi nhớ cái đĩa/vỉ nướng gốc mà món đồ ăn đang đứng trước khi bị nhấc lên.
    private FoodSlot _currentFood;

    // VỊ TRÍ ĐÍCH ĐẾN: Ghi nhớ cái ô trống mà con chuột đang trỏ vào để in "bóng mờ" (preview) lên đó.
    private FoodSlot _cacheFood;


    // CỜ KÉO CHUỘT: Bằng TRUE khi người chơi đang Giữ chuột để di chuyển đồ ăn. Bằng FALSE khi thả ra.
    private bool _hasDrag;

    // ĐỘ LỆCH TỌA ĐỘ: Tính khoảng cách từ tâm bức ảnh Dummy đến mũi tên chuột. 
    // Giúp lúc vừa click vào, bức ảnh không bị giật cục "nhảy" tâm vào mũi chuột.
    private Vector3 _offset;

    // Ổ KHÓA AN TOÀN: Bằng TRUE khi đồ ăn đang bận chạy Animation (đang bay, phóng to, thu nhỏ).
    // [FIX BUG KẸT GAME]: Chặn mọi cú click chuột mới khi đang Animating để chống người chơi spam click gây lỗi hệ thống.
    private bool _isAnimating;

    // BIẾN RUNG LẮC: Đếm thời gian người chơi không thao tác để gợi ý (Shake)
    private float _timer;
    [SerializeField] private float _timeCount; // Thời gian chờ (giây) trước khi rung lắc

    // ĐỒNG HỒ BẤM GIỜ: Ghi lại khoảnh khắc (giây thứ mấy) người chơi ấn chuột xuống.
    // Dùng để phân biệt họ đang TAP (Click nhanh) hay đang DRAG (Giữ lâu).
    private float _timeAtClick;

    // CỜ CHẠM LẠI: Bằng TRUE nếu người chơi click vào đúng cái món đồ ăn mà họ đang nhấc lơ lửng trên tay.
    private bool _clickedSameFood;
    // [THÊM MỚI TẠI ĐÂY]: Biến báo hiệu con chuột đang bận thao tác
    public static bool IsMouseBusy { get; private set; }
    // =========================================================
    // PHẦN 2: KHỞI TẠO
    // =========================================================
    private void Awake()
    {
        // Khi game vừa chạy, chưa ai cầm nắm gì cả -> Giấu bức ảnh đóng thế đi
        _imgFoodDrag.gameObject.SetActive(false);
    }

    // =========================================================
    // PHẦN 3: VÒNG LẶP CHÍNH (KỊCH BẢN CHẠY MỖI FRAME)
    // =========================================================
    private void Update()
    {
        // 0. HỆ THỐNG NHẮC NHỞ (HINT)
        _timer += Time.deltaTime; // Cộng dồn thời gian trôi qua
        if (_timer >= _timeCount) // Nếu lâu quá không ai chơi
        {
            _timer = 0; // Reset đồng hồ
            GameManager.Instance?.OnCheckAndShake(); // Gọi bếp trưởng ra rung đĩa gợi ý
        }
        // [THÊM MỚI TẠI ĐÂY]: Cập nhật trạng thái chuột liên tục
        IsMouseBusy = _hasDrag || _currentFood != null || _isAnimating;
        // ---------------------------------------------------------
        // GIAI ĐOẠN 1: BẮT ĐẦU TƯƠNG TÁC (ẤN CHUỘT XUỐNG)
        // Lệnh Input.GetMouseButtonDown(0) chỉ kích hoạt đúng 1 lần vào khoảnh khắc ngón tay ấn xuống
        // ---------------------------------------------------------
        // [FIX BUG RACE CONDITION]: Chỉ nhận click mới nếu game KHÔNG bận Animating VÀ KHÔNG cầm đồ ăn trên tay (!_hasDrag)
        if (Input.GetMouseButtonDown(0) && !_isAnimating && !_hasDrag && !GameManager.Instance.IsBoosterRunning)
        {
            _timer = 0; // Có tương tác -> Hủy đếm giờ nhắc nhở

            // Bắn 1 tia laser từ vị trí chuột đâm xuyên màn hình xem có trúng ô đồ ăn (FoodSlot) nào không
            FoodSlot tapSlot = Utils.GetRayCastUI<FoodSlot>(Input.mousePosition);
            // ========================================================
            // [THÊM MỚI BỘ LỌC KHIÊN CHẮN TẠI ĐÂY]
            // Nếu bấm trúng 1 ô, nhưng ô đó thuộc về cái Bếp đang Nổ hoặc Đang kéo khay
            if (tapSlot != null)
            {
                GrillStation targetGrill = tapSlot.GrillStation;
                if (targetGrill != null && (targetGrill.IsMerging || targetGrill.IsPreparingTray))
                {
                    // Ép nó thành Null! Đánh lừa code bên dưới là ta đang bấm ra ngoài đất trống
                    tapSlot = null;
                }
            }
            // ========================================================
            if (tapSlot != null) // TRÚNG MỘT CÁI Ô (TRÊN VỈ NƯỚNG / ĐĨA)
            {
                // Kiểm tra xem cái ô vừa click có phải là cái gốc của món đang bay lơ lửng không?
                bool isClickingLiftedFood = (_currentFood != null && tapSlot.GetInstanceID() == _currentFood.GetInstanceID());

                if (tapSlot.HasFood || isClickingLiftedFood)
                {
                    SoundManager.Instance.PlaySFX(SoundType.ClickButton);
                    // TRƯỜNG HỢP 1A: TAP LẠI VÀO MÓN ĐANG LƠ LỬNG TRÊN TAY
                    if (isClickingLiftedFood)
                    {
                        _clickedSameFood = true; // Bật cờ ghi nhận
                        _hasDrag = true; // Chuẩn bị tinh thần nếu họ không nhả ra mà Giữ chuột kéo tiếp

                        // Tính lại độ lệch (Offset) để lỡ họ có kéo thì ảnh trượt đi mượt mà
                        Vector3 mouseWordPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        _offset = _imgFoodDrag.transform.position - mouseWordPos;
                        _offset.z = 0f;
                    }
                    // TRƯỜNG HỢP 1B: CLICK VÀO MỘT MÓN MỚI TOANH NẰM TRÊN BẾP
                    else
                    {
                        _clickedSameFood = false;

                        // [FIX BUG GIẬT HÌNH]: Dừng ngay lập tức mọi Animation (phóng to/bay) của món cũ đang chạy dở
                        _imgFoodDrag.transform.DOKill();

                        // Nếu trên tay đang cầm dở 1 món khác, ta phải "đặt" món đó xuống bếp trước
                        if (_currentFood != null)
                        {
                            _currentFood.OnActiveFood(true); // Hiện ảnh thật món cũ lên
                            if (_cacheFood != null && _cacheFood != _currentFood)
                            {
                                _cacheFood.OnHideFood(); // Lau sạch cái bóng mờ món cũ in trên đĩa
                            }
                        }

                        // --- Bắt đầu quy trình nhấc món MỚI lên tay ---
                        _hasDrag = true;
                        _currentFood = tapSlot; // Lưu ô xuất phát
                        _cacheFood = tapSlot;   // Đặt bóng mờ ngay tại chỗ đó

                        // Thiết lập ảnh đóng thế (Dummy)
                        _imgFoodDrag.gameObject.SetActive(true); // Hiện Dummy lên
                        _imgFoodDrag.sprite = _currentFood.GetSpriteFood; // Copy hình dáng
                        _imgFoodDrag.SetNativeSize(); // Chỉnh kích thước chuẩn
                        _imgFoodDrag.transform.position = _currentFood.transform.position; // Đặt đè lên ảnh thật

                        // Tính độ lệch chuột
                        Vector3 mouseWordPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        _offset = _currentFood.transform.position - mouseWordPos;
                        _offset.z = 0f;

                        _currentFood.OnActiveFood(false); // Xong xuôi thì giấu đồ ăn thật đi (Tạo ảo giác nhấc lên)
                        _imgFoodDrag.transform.DOScale(Vector3.one * 1.2f, 0.2f); // Phóng to Dummy ra 1.2 lần cho sinh động
                    }
                }
                else // TRƯỜNG HỢP 1C: CLICK VÀO Ô TRỐNG (TÍNH NĂNG CLICK-TO-MOVE)
                {
                    if (_currentFood != null) // Điều kiện: Phải đang cầm đồ ăn trên tay thì mới bay tới ô trống được
                    {
                        _isAnimating = true; // Khóa màn hình, cấm click cái khác

                        // Xóa bóng mờ cũ (nếu có)
                        if (_cacheFood != null && _cacheFood != _currentFood)
                        {
                            _cacheFood.OnHideFood();
                        }

                        // Ép Dummy bay vèo tới ô trống vừa click trong 0.4 giây
                        _imgFoodDrag.transform.DOMove(tapSlot.transform.position, 0.4f).OnComplete(() =>
                        {
                            // Sau khi bay tới đích thành công:
                            tapSlot.OnSetSlot(_currentFood.GetSpriteFood); // Đổ data vào ô mới
                            tapSlot.OnActiveFood(true); // Hiện đồ ăn thật ở ô mới lên
                            tapSlot.OnCheckMerge(); // Hỏi Bếp trưởng xem đủ 3 món để ăn điểm chưa
                            _currentFood?.OnCheckPrepareTray(); // Hỏi Khay bên dưới xem bếp trống thì đẩy khay mới lên

                            // Dọn dẹp tay cầm
                            _currentFood = null;
                            _cacheFood = null;
                            _imgFoodDrag.gameObject.SetActive(false); // Giấu Dummy đi
                            _isAnimating = false; // Mở khóa màn hình
                        });
                        _imgFoodDrag.transform.DOScale(Vector3.one, 0.4f); // Thu nhỏ Dummy về bình thường trong lúc bay
                    }
                }
            }
            else // TRƯỜNG HỢP 1D: CLICK RA NGOÀI ĐẤT TRỐNG (HỦY THAO TÁC)
            {
                if (_currentFood != null) // Nếu đang cầm đồ ăn
                {
                    _isAnimating = true; // Khóa màn hình
                    // Ép bay về lại vị trí xuất phát
                    _imgFoodDrag.transform.DOMove(_currentFood.transform.position, 0.2f).OnComplete(() =>
                    {
                        // [FIX BUG HỐ ĐEN]: Không được xóa bóng mờ nếu nó đè lên ô gốc, sẽ làm mất luôn đồ ăn
                        if (_cacheFood != null && _cacheFood != _currentFood)
                        {
                            _cacheFood.OnHideFood();
                        }
                        _cacheFood = null;

                        _currentFood.OnActiveFood(true); // Hiện lại đồ ăn ở ô gốc
                        _currentFood = null; // Trắng tay

                        _imgFoodDrag.gameObject.SetActive(false);
                        _isAnimating = false;
                    });
                    _imgFoodDrag.transform.DOScale(Vector3.one, 0.2f);
                }
            }

            _timeAtClick = Time.time; // Chốt thời gian bắt đầu bấm chuột
        }

        // ---------------------------------------------------------
        // GIAI ĐOẠN 2: DI CHUYỂN CHUỘT (DRAGGING)
        // Code này chạy liên tục mỗi frame MIỄN LÀ người chơi đang GIỮ chuột
        // ---------------------------------------------------------
        if (_hasDrag)
        {
            // Cập nhật vị trí Dummy đi theo con trỏ chuột (+ bù trừ offset)
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 foodPos = mouseWorldPos + _offset;
            foodPos.z = 0; // Đảm bảo UI không bị tụt chiều sâu 3D
            _imgFoodDrag.transform.position = foodPos;

            _timer = 0; // Đang kéo đồ thì không được rung lắc nhắc nhở

            // Bắn tia quét xem đang bay trên bầu trời của ô nào
            FoodSlot slot = Utils.GetRayCastUI<FoodSlot>(Input.mousePosition);

            if (slot != null) // Chuột đang lượn lờ trên một cái Bếp / Khay
            {
                // [TỐI ƯU HÓA & FIX BUG BIẾN HÌNH]: 
                // Lấy thông tin Bếp Cha cực nhanh từ Cache của FoodSlot
                GrillStation targetGrill = slot.GrillStation;
                // Nếu chuột lướt qua cái Bếp ĐANG BẬN (Merge hoặc Kéo Khay) 
                // -> CẤM TUYỆT ĐỐI KHÔNG CHO IN BÓNG MỜ LÊN ĐÂY!
                if (targetGrill != null && (targetGrill.IsMerging || targetGrill.IsPreparingTray))
                {
                    this.OnClearCacheSlot(); // Xóa bóng mờ cũ (nếu có) rồi chuồn luôn
                    //return; // Dừng chạy đoạn code bên dưới
                }
                else
                {
                    if (!slot.HasFood) // Ô ĐANG TRỐNG
                    {
                        // Tránh gọi lệnh in bóng mờ liên tục gây lag. Chỉ in khi chuột đổi sang ô MỚI
                        if (_cacheFood == null || _cacheFood.GetInstanceID() != slot.GetInstanceID())
                        {
                            _cacheFood?.OnHideFood(); // Lau bóng mờ ở ô cũ
                            _cacheFood = slot;        // Gán ô đích mới
                            _cacheFood.OnFadeFood();  // Làm mờ ô đích
                            _cacheFood.OnSetSlot(_currentFood.GetSpriteFood); // In hình món ăn đang cầm lên đó
                        }
                    }
                    else // Ô ĐÃ CÓ ĐỒ ĂN
                    {
                        // Nhờ Bếp tổng (GrillStation) tìm giúp xem trong cái bếp này có ô nào trống và GẦN CHUỘT NHẤT không
                        FoodSlot slotAvailable = slot.GetSlotNull;

                        if (slotAvailable != null) // Tìm thấy chỗ trống
                        {
                            if (_cacheFood == null || _cacheFood.GetInstanceID() != slotAvailable.GetInstanceID())
                            {
                                _cacheFood?.OnHideFood();
                                _cacheFood = slotAvailable;
                                _cacheFood.OnFadeFood();
                                _cacheFood.OnSetSlot(_currentFood.GetSpriteFood);
                            }
                        }
                        else // BẾP ĐÃ KÍN 3 Ô
                        {
                            this.OnClearCacheSlot(); // Không có chỗ in bóng mờ -> Xóa sạch
                        }
                    }
                }

            }
            else // CHUỘT BAY RA NGOÀI VÙNG ĐẤT TRỐNG
            {
                this.OnClearCacheSlot();
            }
        }

        // ---------------------------------------------------------
        // GIAI ĐOẠN 3: KẾT THÚC THAO TÁC (NHẢ CHUỘT RA)
        // Lệnh Input.GetMouseButtonUp(0) kích hoạt đúng 1 lần khi ngón tay rời chuột
        // ---------------------------------------------------------
        if (Input.GetMouseButtonUp(0) && _hasDrag)
        {
            // Bấm đồng hồ xem người chơi đã giữ chuột được bao lâu
            float timeHeld = Time.time - _timeAtClick;

            if (timeHeld < 0.15f) // --- NHÁNH A: NHẤP THẢ RẤT NHANH (TAP) ---
            {
                if (_clickedSameFood) // Nhấp lần 2 vào món lơ lửng -> MUỐN ĐẶT XUỐNG
                {
                    _isAnimating = true;
                    // Bay trả Dummy về tâm ô gốc
                    _imgFoodDrag.transform.DOMove(_currentFood.transform.position, 0.15f);
                    _imgFoodDrag.transform.DOScale(Vector3.one, 0.15f).OnComplete(() =>
                    {
                        _imgFoodDrag.gameObject.SetActive(false);

                        // [FIX BUG HỐ ĐEN]: Tránh xóa nhầm ô xuất phát
                        if (_cacheFood != null && _cacheFood != _currentFood)
                        {
                            _cacheFood.OnHideFood();
                        }
                        _cacheFood = null;

                        _currentFood.OnActiveFood(true); // Bật đồ ăn thật lên lại
                        _currentFood = null;

                        _isAnimating = false;
                    });
                }
                else // Nhấp lần 1 vào món mới -> MUỐN NHẤC LÊN NGẮM (LƠ LỬNG)
                {
                    // Ép Dummy bay về tâm ô để mượt mà (chống giật ảnh do offset)
                    _imgFoodDrag.transform.DOMove(_currentFood.transform.position, 0.1f);

                    // [FIX BUG BÓNG MA]: Xóa bóng mờ nếu người chơi lỡ rê chuột ra đĩa khác rồi nhả ra siêu nhanh
                    if (_cacheFood != null && _cacheFood != _currentFood)
                    {
                        _cacheFood.OnHideFood();
                        _cacheFood = _currentFood; // Reset bóng mờ về đúng vị trí lơ lửng
                    }
                    // LƯU Ý QUAN TRỌNG: Không gán _currentFood = null. Code giữ nguyên món ăn trên tay!
                }
            }
            else // --- NHÁNH B: GIỮ LÂU HƠN 0.15s (DRAG & DROP - KÉO VÀ THẢ) ---
            {
                _isAnimating = true;

                // THÊM CHỐT CHẶN Ở ĐÂY: Kiểm tra lần cuối trước khi quyết định thả đồ xuống!
                bool canDrop = true;
                if (_cacheFood != null)
                {
                    var targetGrill = _cacheFood.GrillStation;// tạo biến tham chiếu tới GrillStation qua FoodSlot
                    if (targetGrill != null && targetGrill.IsMerging || targetGrill.IsPreparingTray)// Nếu lúc thả tay ra mà bếp đó ĐANG MERGE hoặc ĐANG KÉO KHAY -> Cấm thả!
                    {
                        canDrop = false;
                    }
                }

                // Nếu chỗ in bóng mờ (Đích đến) KHÁC VỚI chỗ xuất phát -> KÉO THẢ THÀNH CÔNG VÀO Ô MỚI
                if (_cacheFood != null && _cacheFood.GetInstanceID() != _currentFood.GetInstanceID() && canDrop)
                {
                    // Cho Dummy bay nốt quãng đường vào ô đích
                    _imgFoodDrag.transform.DOMove(_cacheFood.transform.position, 0.2f).OnComplete(() =>
                    {
                        _imgFoodDrag.gameObject.SetActive(false);
                        _cacheFood.OnSetSlot(_currentFood.GetSpriteFood); // Gắn data vào ô mới
                        _cacheFood.OnActiveFood(true); // Hiện đồ thật lên
                        _cacheFood.OnCheckMerge(); // Check ăn điểm
                        _currentFood?.OnCheckPrepareTray(); // Check nạp khay mới

                        _cacheFood = null;
                        _currentFood = null;
                        _isAnimating = false;
                    });
                    _imgFoodDrag.transform.DOScale(Vector3.one, 0.22f); // Thu nhỏ vừa phải
                }
                // Nếu đích đến TRÙNG xuất phát, HOẶC thả ra ngoài -> THẢ TRƯỢT (HỦY THAO TÁC)
                else
                {
                    // Bay về chỗ cũ tị nạn
                    _imgFoodDrag.transform.DOMove(_currentFood.transform.position, 0.3f).OnComplete(() =>
                    {
                        _imgFoodDrag.gameObject.SetActive(false);

                        // [FIX BUG HỐ ĐEN]
                        if (_cacheFood != null && _cacheFood != _currentFood)
                        {
                            _cacheFood.OnHideFood();
                        }
                        _cacheFood = null;

                        _currentFood.OnActiveFood(true);
                        _currentFood = null;

                        _isAnimating = false;
                    });
                    _imgFoodDrag.transform.DOScale(Vector3.one, 0.3f);
                }
            }

            // Kết thúc kịch bản, tắt cờ Kéo chuột
            _hasDrag = false;
        }
    }

    // =========================================================
    // PHẦN 4: HÀM PHỤ TRỢ (HELPERS)
    // =========================================================
    // Hàm này dọn dẹp (tắt) các bóng mờ bị in sai chỗ
    private void OnClearCacheSlot()
    {
        // [QUAN TRỌNG]: Không bao giờ được phép xóa bóng mờ nếu nó đang đè lên ô gốc (_currentFood)
        // Vì nếu gọi OnHideFood lên ô gốc, biến HasFood = false -> Đồ ăn thật sẽ tàng hình luôn!
        if (_cacheFood != null && _cacheFood.GetInstanceID() != _currentFood.GetInstanceID())
        {
            _cacheFood.OnHideFood();
            _cacheFood = null;
        }
    }
}