using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GrillStation : MonoBehaviour
{
    // Biến Cờ Hiệu: Đánh dấu xem Bếp có đang bận biểu diễn Animation ghép không
    public bool IsMerging { get; private set; } = false;
    // [THÊM MỚI]: Cờ hiệu đánh dấu Bếp đang bận kéo khay từ dưới gầm lên
    public bool IsPreparingTray { get; private set; } = false;
    [SerializeField] private Transform _trayContainer; // Chứa các đĩa (Tray)
    [SerializeField] private Transform _slotContainer; // Chứa các vỉ nướng (Slot - nơi click vào để ăn)

    private List<TrayItem> _totalTrays; // Danh sách component quản lý đĩa
    private List<FoodSlot> _totalSlots; // Danh sách component quản lý vỉ nướng
    private Stack<TrayItem> _stackTrays = new Stack<TrayItem>();// NGĂN XẾP (STACK): Hoạt động theo nguyên lý LIFO (Vào sau - Ra trước)
    // Giống như chồng đĩa ngoài đời thực, đĩa nào cất vào cuối cùng sẽ nằm trên cùng, khi lấy ra sẽ lấy đầu tiên.
    public Stack<TrayItem> StackTrays => _stackTrays;
    public List<FoodSlot> TotalSlots => _totalSlots;
    // CẤU HÌNH BẾP KHÓA (LOCKED GRILL)
    // ==========================================
    [Header("Locked State Config")]
    public GameObject normalGrillObj;   // Kéo NormalGrill_Obj vào đây
    public GameObject lockedOverlayObj; // Kéo LockedOverlay_Obj vào đây
    public Image targetFoodIcon;        // Kéo TargetFoodIcon vào đây
    // Biến công khai để GameManager biết bếp này đang khóa (Không được ném đồ ăn vào lúc đầu game)
    public bool IsLocked { get; private set; } = false;
    private string _targetUnlockName = "";
    private void Awake()
    {
        // Lấy danh sách các đĩa con và vỉ nướng con
        _totalTrays = Utils.GetListInChild<TrayItem>(_trayContainer);
        _totalSlots = Utils.GetListInChild<FoodSlot>(_slotContainer);
    }
    // 1. HÀM KHÓA BẾP KHI BẮT ĐẦU GAME
    public void InitLockedState(Sprite targetSprite)
    {
        IsLocked = true;
        _targetUnlockName = targetSprite.name;
        
        if(targetFoodIcon != null) targetFoodIcon.sprite = targetSprite;

        // Bật Nắp đậy, tắt Bếp thường
        lockedOverlayObj.SetActive(true);
        lockedOverlayObj.transform.localScale = Vector3.one; // Đảm bảo nắp to rõ
        normalGrillObj.SetActive(false); 
    }

    // 2. HÀM LẮNG NGHE KHI NGƯỜI CHƠI GHÉP ĐỒ ĂN
    public void CheckAndUnlock(string mergedFoodName)
    {
        // Nếu không khóa hoặc sai chìa khóa -> Bỏ qua
        if (!IsLocked || _targetUnlockName != mergedFoodName) return;

        // Đúng chìa khóa -> Tiến hành mở khóa
        StartCoroutine(IEUnlockSequence());
    }

    // 3. DIỄN HOẠT MỞ KHÓA BẰNG DOTWEEN
    private IEnumerator IEUnlockSequence()
    {
        IsLocked = false;

        // Nhịp 1: Nắp đậy thu nhỏ lại và biến mất
        lockedOverlayObj.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack);
        yield return new WaitForSeconds(0.4f);
        lockedOverlayObj.SetActive(false);

        // Chuẩn bị bếp thường (GỌI HÀM LÀM SẠCH BẾP NHƯ BOOSTER ADD GRILL)
        OnInitEmptyGrill(); 

        // Nhịp 2: Bếp thường hiện ra từ cõi hư vô
        normalGrillObj.SetActive(true);
        normalGrillObj.transform.localScale = Vector3.zero;
        normalGrillObj.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

        // Báo cho GameManager biết đã có thêm 1 bếp hoạt động
        GameManager.Instance.AddOneActiveGrill();
    }
    // Hàm khởi tạo riêng cho từng bếp
    public void OnInitGrill(int totalTray, List<Sprite> listFood, List<Sprite> forcedTopFood = null)
    {
        // --- GIAI ĐOẠN 1: BÀY LÊN VỈ NƯỚNG (SLOT) ---
        //xử lý item đuwocj ưu tiên trước
        List<Sprite> listSlot = new List<Sprite>();
        // 1. Lệnh từ Sếp: Nếu có "Đồ VIP" mớm mồi -> Bắt buộc nhét hết vào danh sách chuẩn bị nướng
        if (forcedTopFood != null && forcedTopFood.Count > 0)
        {
            listSlot.AddRange(forcedTopFood);
        }
        // 2. Tính toán xem vỉ nướng nên có tổng cộng bao nhiêu món (để nhìn tự nhiên)
        // Nếu đã có đồ VIP rồi thì số lượng ít nhất trên vỉ phải bằng số lượng đồ VIP
        int minFoodOnTop = Mathf.Max(1, listSlot.Count);
        int targetFoodCount = Random.Range(minFoodOnTop, _totalSlots.Count + 1);//lay random số món sẽ có của vỉ (2 hoặc 3 vì chắc chắn sẽ có ít 1 item ưu tiên rồi)

        // Xem cần bù thêm bao nhiêu đồ thường cho đủ target
        int needMore = targetFoodCount - listSlot.Count;
        if (needMore > 0 && listFood.Count > 0)
        {
            // Cắt đồ thường từ túi (listFood) bỏ thêm vào listSlot
            // Dùng Mathf.Min để đề phòng túi đồ thường không còn đủ số lượng cần thiết
            int actualTake = Mathf.Min(needMore, listFood.Count);
            listSlot.AddRange(Utils.TakeAndRemoveRandom(listFood, actualTake));
        }

        //int foodCount = Random.Range(1, _totalSlots.Count + 1);
        //List<Sprite> listSlot = Utils.TakeAndRemoveRandom<Sprite>(listFood, foodCount);

        // Xếp tất cả các món trong listSlot lên các vị trí ngẫu nhiên trên vỉ nướng
        for (int i = 0; i < listSlot.Count; i++)
        {
            FoodSlot slot = this.RandomSlot(); // Tìm 1 ô trống ngẫu nhiên
            slot.OnSetSlot(listSlot[i]); // Hiển thị hình ảnh lên
        }
        // --- GIAI ĐOẠN 2: CẤT VÀO KHAY (TRAY) ---
        // Tạo danh sách 2 chiều: Mỗi phần tử là một cái đĩa chứa danh sách các món ăn bên trong
        List<List<Sprite>> remainFood = new List<List<Sprite>>();
        // Nếu Bếp có đồ ăn, BẮT BUỘC phải tạo ít nhất 1 cái đĩa.
        if (listFood.Count > 0) totalTray = Mathf.Max(1, totalTray);
        // [QUAN TRỌNG]: Chốt chặn an toàn để không sinh ra số đĩa vượt quá UI có thật
        totalTray = Mathf.Min(totalTray, _totalTrays.Count);
        // Bước A: "Gieo mầm" (Seeding)
        // Đảm bảo mỗi cái đĩa có ÍT NHẤT 1 món ăn (để không bị đĩa trống)
        for (int i = 0; i < totalTray - 1; i++) // Logic này hơi lạ: totalTray - 1 có thể bỏ qua đĩa cuối? (* VD: totaltray co the là 4 cái nhưng duyệt vòng for lại từ cái đầu là vị trí thứ 0 nên với totaltray = 4 chỉ cần duyệt từ 0 đến 3)
        {
            remainFood.Add(new List<Sprite>()); // Tạo đĩa mới
            int n = Random.Range(0, listFood.Count); // Chọn bừa 1 món
            remainFood[i].Add(listFood[n]); // Bỏ vào đĩa
            listFood.RemoveAt(n); // Xóa khỏi danh sách chờ
        }

        // Bước B: "Nhồi nhét" (Filling)
        // Nhét tất cả số thức ăn còn thừa vào các đĩa ngẫu nhiên
        // !!! CẢNH BÁO NGUY HIỂM !!!: Đoạn này dễ gây Crash Game (Infinite Loop)
        // while (listFood.Count > 0)
        // {
        //     int rands = Random.Range(0, remainFood.Count); // Chọn bừa 1 đĩa

        //     // Chỉ bỏ thêm vào nếu đĩa đó chưa đầy (Sức chứa < 3)
        //     if (remainFood[rands].Count < 3)
        //     {
        //         int n = Random.Range(0, listFood.Count);
        //         remainFood[rands].Add(listFood[n]);
        //         listFood.RemoveAt(n); // Xóa thành công thì mới giảm số lượng listFood
        //     }
        // }
        int emergencyEscape = 0; // Bộ đếm thoát hiểm
        while (listFood.Count > 0 && emergencyEscape < 1000)
        {
            emergencyEscape++;
            int rands = Random.Range(0, remainFood.Count);

            if (remainFood[rands].Count < 3)
            {
                int n = Random.Range(0, listFood.Count);
                remainFood[rands].Add(listFood[n]);
                listFood.RemoveAt(n); 
                emergencyEscape = 0; // Xếp thành công thì reset bộ đếm
            }
        }

        // Nếu chạy 1000 lần vẫn dư đồ (do UI khay quá ít so với lượng đồ ăn ép xuống)
        // Ép nhét tất cả phần dư vào khay cuối cùng (Dù nó có vượt quá 3 món đi nữa để tránh mất item)
        if (listFood.Count > 0 && remainFood.Count > 0)
        {
            Debug.LogWarning($"[CẢNH BÁO]: Level Data chia đồ không hợp lý. Vượt quá sức chứa của Khay! Ép nhét {listFood.Count} món dư vào khay cuối.");
            foreach(var f in listFood) {
                remainFood[remainFood.Count - 1].Add(f);
            }
            listFood.Clear();
        }

        // --- GIAI ĐOẠN 3: HIỂN THỊ LÊN MÀN HÌNH ---
        for (int i = 0; i < _totalTrays.Count; ++i)
        {
            bool active = i < remainFood.Count;
            _totalTrays[i].gameObject.SetActive(active);
            if (active)
            {
                _totalTrays[i].OnSetFood(remainFood[i]);

                // NẠP ĐẠN: Mỗi khi một cái khay được tạo ra và có đồ ăn, 
                // ta "nhét" (Push) nó vào ngăn xếp để chờ được gọi lên bếp.
                TrayItem item = _totalTrays[i];
                _stackTrays.Push(item);
            }
        }
        Invoke(nameof(OnCheckMerge), 0.5f);//fix bug random ra 3 item giong nhau ngay khi genarate level (neu khong Check Merge o day 3 item do se khong merge complete)
    }

    // Tìm một vị trí trống ngẫu nhiên trên vỉ nướng
    // private FoodSlot RandomSlot()
    // {
    //     // Label 'rerand' dùng cho lệnh goto
    // rerand: int n = Random.Range(0, _totalSlots.Count);

    //     // Nếu ô thứ n đã có đồ ăn rồi -> Quay lại 'rerand' để chọn số khác
    //     // Lưu ý: Đây cũng là vòng lặp vô tận nếu TẤT CẢ các ô đều đã đầy. 
    //     // Nhưng ở code trên logic đảm bảo số món luôn <= số ô nên tạm an toàn.
    //     if (_totalSlots[n].HasFood) goto rerand;

    //     return _totalSlots[n];
    // }
    // Tìm một vị trí trống ngẫu nhiên trên vỉ nướng [ĐÃ FIX TỐI ƯU]
    private FoodSlot RandomSlot()
    {
        // Lọc ra danh sách chỉ chứa những ô CHƯA CÓ đồ ăn
        List<FoodSlot> emptySlots = _totalSlots.Where(slot => !slot.HasFood).ToList();

        if (emptySlots.Count > 0)
        {
            // Chọn ngẫu nhiên 1 ô trong số các ô trống
            int n = Random.Range(0, emptySlots.Count);
            return emptySlots[n];
        }

        // Nếu bếp lỡ bị đầy (không có ô trống), trả về null để code bên ngoài tự xử lý
        Debug.LogWarning("Không tìm thấy ô trống nào trên bếp để random!");
        return null; 
    }

    // DropDragCtrl gọi hàm này (thông qua FoodSlot) để xin 1 chỗ trống trên bếp
    public FoodSlot GetSlotNull()
    {
        FoodSlot tmp = null;
        // Duyệt qua tất cả các vỉ nướng trong bếp này
        for (int i = 0; i < _totalSlots.Count; i++)
        {
            // Nếu ô đó KHÔNG CÓ THỨC ĂN (Kiểm tra bằng biến HasFood thông minh ở trên)
            if (!_totalSlots[i].HasFood)
            {
                if (tmp == null)
                {
                    tmp = _totalSlots[i];
                }
                else
                {
                    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    float x1 = Mathf.Abs(mousePos.x - tmp.transform.position.x);
                    float x2 = Mathf.Abs(mousePos.x - _totalSlots[i].transform.position.x);
                    if (x2 < x1)
                    {
                        tmp = _totalSlots[i];
                    }
                }
            }
        }
        return tmp; ; // Báo cáo: Bếp đã đầy, không còn chỗ nhét!
    }
    // HÀM KIỂM TRA: Bếp có đang trống trơn không?
    public bool HasGrillEmpty()
    {
        for (int i = 0; i < _totalSlots.Count; i++)
        {
            if (_totalSlots[i].HasFood)
            {
                return false; // Chỉ cần 1 ô có đồ ăn -> Bếp KHÔNG rỗng
            }
        }
        return true; // Quét hết không thấy ai -> Bếp RỖNG
    }
    // =========================================================
    // TÍNH NĂNG MERGE (GHÉP 3 MÓN)
    // =========================================================
    public void OnCheckMerge()
    {
        // BƯỚC 1: BẾP ĐÃ KÍN CHỖ CHƯA?
        // Hàm GetSlotNull trả về null nghĩa là không còn ô trống nào -> Bếp đã đầy 3 ô.
        if (this.GetSlotNull() == null && !IsMerging && ! IsPreparingTray)// [FIX BẢO HIỂM]: Phải đảm bảo bếp đầy (GetSlotNull == null) VÀ không bị kẹt hiệu ứng (!IsMerging)
        {
            // BƯỚC 2: 3 MÓN CÓ GIỐNG HỆT NHAU KHÔNG?
            if (this.CanMerge())
            {
                Debug.Log("Complete Grill - Ghép thành công!");
                string nameOfMergedFood = _totalSlots[0].GetSpriteFood.name;// [FIX LỖI]: Lấy tên của tấm ảnh đang nằm trên ô số 0
               // 1. BẬT KHIÊN: Báo cho hệ thống biết bếp này đang xử lý, cấm chuột chạm vào!
                IsMerging = true;
                // [FIX BUG TUYỆT ĐỐI]: Trống rỗng hóa (Tẩy não) 3 ô này ngay lập tức!
                // Kẻ thù (Shuffle, Kéo Thả) khi gọi slot.HasFood sẽ nhận được kết quả FALSE
                // nên nó sẽ tưởng đây là ô trống và bỏ qua, không cướp đồ ăn nữa!
                foreach(var slot in _totalSlots)
                {
                    slot.MarkAsMerging(true);
                }
                StartCoroutine(IEMerge());
                
                // BƯỚC 4: DỌN ĐĨA MỚI LÊN (Vì bếp vừa bị xóa sạch đồ ăn)
                this.OnPrepareTray(false);
                GameManager.Instance.OnMinusFood(nameOfMergedFood);
            }
        }
        IEnumerator IEMerge()
        {
            // BƯỚC 3: ĂN ĐIỂM (Xóa 3 món trên bếp đi)
            for (int i = 0; i < _totalSlots.Count; i++)
            {
                _totalSlots[i].OnFadeOut();
                yield return new WaitForSeconds(0.1f);
            }
            // 4. MỞ KHIÊN: Dọn dẹp xong, cho phép tương tác bình thường
            IsMerging = false;
        }
    }
    // =========================================================
    // TÍNH NĂNG FILL (DỌN ĐĨA TỪ DƯỚI LÊN BẾP)
    // =========================================================

    // Trạm kiểm soát: DropDragCtrl sẽ gọi hàm này khi người chơi vừa nhấc đồ đi chỗ khác
    public void OnCheckPrepareTray()
    {
        // Phải chắc chắn bếp RỖNG thì mới được dọn khay mới lên
        if (this.HasGrillEmpty())
        {
            this.OnPrepareTray(true);
        }
    }
    // Động cơ chính: Kéo khay lên
    private void OnPrepareTray(bool isNow)
    {
        StartCoroutine(IEPrepare());
        IEnumerator IEPrepare()
        {
            // 1. BẬT KHIÊN BẢO VỆ: Không cho phép Booster nào được can thiệp lúc khay đang bay!
            IsPreparingTray = true;
            if (!isNow)
                yield return new WaitForSeconds(0.95f);
            // Nếu trong ngăn xếp (dưới gầm bàn) vẫn còn khay dự trữ
            if (_stackTrays.Count > 0)
            {
                // Lấy cái khay trên cùng ra khỏi ngăn xếp (Pop)
                TrayItem item = _stackTrays.Pop();

                // Duyệt qua từng món đồ ăn nằm trong cái khay đó
                for (int i = 0; i < item.FoodList.Count; i++)
                {
                    Image img = item.FoodList[i];

                    // Nếu vị trí đó trong khay có chứa đồ ăn
                    if (img.gameObject.activeInHierarchy)
                    {
                        // Truyền data của đồ ăn (ảnh) cho Vỉ Nướng (Slot) để nó tự diễn Animation bay lên
                        _totalSlots[i].OnPrepareItem(img);

                        // Tắt hình ảnh dưới khay đi (vì nó đang bay lên trên rồi)
                        img.gameObject.SetActive(false);
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                CanvasGroup canvas = item.GetComponent<CanvasGroup>();
                canvas.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    item.gameObject.SetActive(false);// Tắt luôn cả cái khay rỗng đi cho gọn màn hình
                    canvas.alpha = 1f;
                    // 2. TẮT KHIÊN: Khay đã lên xong, an toàn rồi!
                    IsPreparingTray = false;
                    // [THÊM MỚI TẠI ĐÂY]: GỌI HÀM KIỂM TRA GHÉP SAU KHI ĐÃ FILL XONG// fix lỗitrường hợp là 3 item giống nhau xuất hiện ở dưới khay và khi được đẩy lên bếp chúng không merge complete
                    // =====================================================
                    this.OnCheckMerge();
                });

            }
            else
            {
                // Nếu hết khay rồi thì cũng phải nhớ tắt khiên đi
                IsPreparingTray = false;
            }
        }
    }
    // [CẬP NHẬT MỚI]: HÀM KIỂM TRA KHAY RỖNG Do NAM CHÂM
    public void CheckAndRemoveEmptyTopTray()
    {
        // 1. Nếu không có khay nào thì thôi
        if (_stackTrays.Count == 0) return;

        // 2. Lấy khay trên cùng ra xem thử
        TrayItem topTray = _stackTrays.Peek();
        bool hasFood = false;

        // 3. Quét xem trong khay đó có còn món đồ ăn nào đang hiện không
        foreach (Image img in topTray.FoodList)
        {
            if (img.gameObject.activeInHierarchy)
            {
                hasFood = true; // Còn ít nhất 1 món
                break;
            }
        }

        // 4. NẾU KHAY RỖNG (Nam châm đã hút sạch đồ của khay này)
        if (!hasFood)
        {
            // Bốc khay đó ra khỏi ngăn xếp (xóa sổ)
            TrayItem emptyTray = _stackTrays.Pop();

            // Diễn animation Fade out mờ dần rồi tắt như lúc fill lên bếp
            CanvasGroup canvas = emptyTray.GetComponent<CanvasGroup>();
            if (canvas != null)
            {
                canvas.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    emptyTray.gameObject.SetActive(false); // Cất khay đi
                    canvas.alpha = 1f; // Reset độ mờ cho lần chơi sau
                });
            }
            else
            {
                emptyTray.gameObject.SetActive(false); // Fallback nếu quên add CanvasGroup
            }
        }
    }
    // Thuật toán kiểm tra 3 món giống nhau (Đã tối ưu CPU bằng cách so sánh Reference, ban dau la so bang string name)
    private bool CanMerge()
    {
        // Lấy tấm ảnh gốc ở ô đầu tiên làm hệ quy chiếu
        Sprite standardSprite = _totalSlots[0].GetSpriteFood;

        // Quét các ô còn lại (từ ô số 1)
        for (int i = 1; i < _totalSlots.Count; ++i)
        {
            // Nếu phát hiện có ô nào dùng ảnh khác với hệ quy chiếu -> Tạch!
            if (_totalSlots[i].GetSpriteFood != standardSprite)
            {
                return false;
            }
        }
        return true; // Lọt qua được vòng lặp -> 3 ô giống y hệt nhau
    }
    public void OnInitEmptyGrill()
    {
        foreach (var slot in _totalSlots)
        {
            slot.OnActiveFood(false);// Giấu hình ảnh đồ ăn đi
        }
        // 2. Dọn sạch Khay Chờ (Tray) dưới gầm bàn
        _stackTrays.Clear();
        foreach (var tray in _totalTrays)
        {
            tray.gameObject.SetActive(false);
        }
    }
}
