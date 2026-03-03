// --- FILE TRAYITEM.CS ---
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrayItem : MonoBehaviour
{
    private List<Image> _foodList; // Danh sách các slot hình ảnh TRONG cái đĩa này
    public List<Image> FoodList => _foodList;

    void Awake()
    {
        // Lấy tất cả component Image con
        _foodList = Utils.GetListInChild<Image>(this.transform);
        // Tắt hết đi (ẩn) lúc đầu
        for (int i = 0; i < _foodList.Count; i++)
        {
            _foodList[i].gameObject.SetActive(false);
        }
    }

    // Hàm nhận dữ liệu từ GrillStation để hiển thị
    public void OnSetFood(List<Sprite> items)
    {
        // Chỉ hiển thị nếu số lượng món không vượt quá số slot hình ảnh có sẵn
        if (items.Count <= _foodList.Count)
        {
            for (int i = 0; i < items.Count; i++)
            {
                // Chọn ngẫu nhiên 1 vị trí trong đĩa để hiện món này lên (cho nó tự nhiên)
                Image slot = this.RandomSlot();

                slot.gameObject.SetActive(true); // Bật lên
                slot.sprite = items[i]; // Gán ảnh
                slot.SetNativeSize(); // Chỉnh kích thước về đúng chuẩn ảnh gốc
            }
        }
    }

    // Tìm slot trống trong đĩa (Logic giống bên GrillStation)
    private Image RandomSlot()
    {
    rerand: int n = Random.Range(0, _foodList.Count);
        // Nếu slot này đang bật (tức là đã có món ăn) -> quay lại tìm slot khác
        if (_foodList[n].gameObject.activeInHierarchy) goto rerand;
        return _foodList[n];
    }
}

