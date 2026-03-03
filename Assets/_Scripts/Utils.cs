using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Utils
{
    // Hàm này giúp lấy tất cả component T nằm trong các object con của parent
    // Ví dụ: Lấy tất cả các "Bếp" (GrillStation) nằm trong "Khu vực bếp" (_gridGrill)
    public static List<T> GetListInChild<T>(Transform parent)
    {
        List<T> result = new List<T>();
        // Duyệt qua từng đứa con của object cha
        for (int i = 0; i < parent.childCount; i++)
        {
            var component = parent.GetChild(i).GetComponent<T>();
            // Nếu đứa con đó có component mình cần thì thêm vào list
            if (component != null)
            {
                result.Add(component);
            }
        }
        return result;
    }

    // --- HÀM QUAN TRỌNG: Rút bài ngẫu nhiên ---
    // Input: Một danh sách gốc (source) và số lượng cần lấy (n)
    // Output: Một danh sách mới chứa n phần tử ngẫu nhiên
    // LƯU Ý: Hàm này sẽ XÓA phần tử đã lấy khỏi danh sách gốc (source)
    public static List<T> TakeAndRemoveRandom<T>(List<T> source, int n)
    {
        List<T> result = new List<T>(); // Tạo cái giỏ mới để đựng đồ lấy được

        // Đảm bảo không lấy quá số lượng hiện có (Tránh lỗi Index Out of Range)
        n = Mathf.Min(n, source.Count);

        for (int i = 0; i < n; i++)
        {
            // Bước 1: Chọn bừa 1 vị trí (index) trong danh sách gốc
            int randIndex = Random.Range(0, source.Count);

            // Bước 2: Bỏ món đồ đó vào giỏ mới (result)
            result.Add(source[randIndex]);

            // Bước 3: XÓA món đồ đó khỏi danh sách gốc ngay lập tức
            // Để lần lặp sau không bao giờ bốc trúng món này nữa (tránh trùng lặp)
            source.RemoveAt(randIndex);
        }
        return result;
    }
    public static T GetRayCastUI<T>(Vector2 pos) where T : MonoBehaviour 
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = pos;
        List<RaycastResult> list = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, list);
        if (list.Count > 0) 
        {
            for (int i = 0; i < list.Count; i++) 
            {
                T component = list[i].gameObject.GetComponent<T>();
                if (component != null) 
                {
                    return component;
                }
            }
        }
        return null;
    }
}