#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class DebugTools
{
    // Dòng này sẽ tạo ra một menu mới trên thanh công cụ trên cùng của Unity
    [MenuItem("Tools/Xóa toàn bộ Save Game (Reset Level)")]
    public static void ClearSave()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("<b><color=green>ĐÃ XÓA SẠCH SAVE!</color></b> Bấm Play để chơi lại từ Level 1 nhé.");
    }
}
#endif