using DG.Tweening;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class FoodSlot : MonoBehaviour 
{
    private Image _imgFood; // Hiển thị hình ảnh món ăn

    // Khai báo màu sắc để làm trạng thái (State)
    private Color normal = new Color(1, 1, 1, 1);   // Trắng tinh (Đồ ăn thật)
    private Color fade = new Color(1, 1, 1, 0.7f);  // Mờ 70% (Chỉ là bóng preview)

    private GrillStation _grillCtrl; // Lưu tham chiếu đến Bếp tổng quản lý nó
    public GrillStation GrillStation => _grillCtrl;
    public bool IsBeingMerged { get; private set; } = false;

    private void Awake()
    {
        _imgFood = this.transform.GetChild(0).GetComponent<Image>();
        _imgFood.gameObject.SetActive(false); // Ban đầu không có gì thì ẩn đi

        // (Đã Fix lỗi Brittle Code): Dùng GetComponentInParent thay cho parent.parent
        // Hàm này tự động leo lên các object cha tìm GrillStation, cực kỳ an toàn.
        _grillCtrl = this.GetComponentInParent<GrillStation>();
    }

    // Đặt món ăn thật vào ô
    public void OnSetSlot(Sprite spr) 
    {
        _imgFood.gameObject.SetActive(true);
        _imgFood.sprite = spr;
        _imgFood.SetNativeSize();
    }

    // Bật/Tắt hiển thị UI của ô này
    public void OnActiveFood(bool active)
    {
        _imgFood.gameObject.SetActive(active); 
        _imgFood.color = normal;
    }

    // Biến ô này thành "Bóng mờ" (Preview lúc kéo chuột qua)
    public void OnFadeFood()
    {
        this.OnActiveFood(true);// 
        _imgFood.color = fade; // Chuyển màu sang trong suốt 70%
    }

    // Hủy trạng thái Bóng mờ (Ẩn đi và đưa màu về chuẩn bị đón đồ ăn thật)
    public void OnHideFood()
    {
        this.OnActiveFood(false);
        _imgFood.color = normal;
    }
    public void OnCheckMerge()// Báo cáo lên Bếp Trưởng (GrillStation) để check xem ghép 3 được chưa
    {
        _grillCtrl?.OnCheckMerge();
    }
    // =========================================================
    // ANIMATION: ĐỒ ĂN BAY TỪ KHAY (TRAY) LÊN VỈ (SLOT)
    // =========================================================
    // Hàm này được GrillStation gọi khi nó quyết định dọn khay lên
    public void OnPrepareItem(Image img)
    {
        // 1. Cài đặt hình ảnh cho ô này bằng ảnh lấy từ dưới khay
        this.OnSetSlot(img.sprite);
        _imgFood.color = normal;

        // 2. THUẬT ĐỘN THỔ (Dịch chuyển tức thời)
        // Ép vị trí, kích thước và góc xoay của món ăn trên vỉ TRÙNG KHỚP với món ăn dưới khay
        _imgFood.transform.position = img.transform.position;
        _imgFood.transform.localScale = img.transform.localScale;
        _imgFood.transform.localEulerAngles = img.transform.localEulerAngles;

        // 3. DIỄN HOẠT (ANIMATION)
        // Yêu cầu món ăn bay ngược về tâm của cái Vỉ (Tọa độ Local 0,0,0 là tâm của object cha)
        _imgFood.transform.DOLocalMove(Vector3.zero, 0.2f);

        // Cùng lúc đó, phóng to từ từ kích thước từ dưới khay lên đúng kích thước chuẩn (Scale = 1,1,1)
        _imgFood.transform.DOScale(Vector3.one, 0.2f);
        // DÒNG NÀY ĐÃ FIX BUG: Xoay thẳng đứng lại (Rotation Z = 0) trong 0.2s
        _imgFood.transform.DOLocalRotate(Vector3.zero, 0.2f);
    }
    public void OnCheckPrepareTray() // Báo cáo lên Bếp Trưởng để check xem bếp rỗng thì gọi khay lên
    {
        _grillCtrl?.OnCheckPrepareTray();
    }
    public void OnFadeOut()
    {
        _imgFood.transform.DOLocalMoveY(100f, 0.6f).OnComplete(() =>
        {
            this.OnActiveFood(false);
            _imgFood.transform.localPosition = Vector3.zero;
            IsBeingMerged = false;
            
        });
        _imgFood.DOColor(new Color(1, 1, 0, 0), 0.6f); // 
        
    }
    public void DoShake()
    {
        _imgFood.transform.DOShakePosition(0.5f, 10f, 10, 180f);
    }
    // Hàm gọi để khóa ô lúc đang Fade Out
    public void MarkAsMerging(bool isMerging)
    {
        IsBeingMerged = isMerging;
    }
    // ------------------------------------------------------------------
    // CÁC HÀM GETTER (CUNG CẤP THÔNG TIN CHO DROPDRAGCTRL GỌI TỚI)
    // RẤT QUAN TRỌNG: Ô này ĐÃ CÓ ĐỒ ĂN THẬT khi 
    // 1. Ảnh đang được bật VÀ 2. Màu của ảnh là màu thật (normal, không phải bóng mờ fade)
    public bool HasFood => _imgFood.gameObject.activeInHierarchy && _imgFood.color == normal && !IsBeingMerged;

    // Lấy hình ảnh của món ăn đang nằm trong ô này
    public Sprite GetSpriteFood => _imgFood.sprite;

    // Hỏi cái Bếp (GrillStation) xem trong Bếp có ô nào đang trống không?
    public FoodSlot GetSlotNull => _grillCtrl.GetSlotNull();
}