using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("--- CẤU HÌNH CƠ BẢN ---")]
    
    [Tooltip("Thời gian của màn chơi (tính bằng giây). VD: 120f = 2 phút")]
    public float levelTime = 120f;         
    
    [Tooltip("Tổng số BỘ 3 cần ghép (Ví dụ: 8 bộ = 24 miếng thức ăn trên mâm)")]
    public int allFoodSets = 8;           
    
    [Tooltip("Số LOẠI đồ ăn sẽ xuất hiện. Càng nhiều loại càng khó ghép!")]
    public int totalFoodTypes = 3;         

    [Header("--- CẤU HÌNH BẾP (GRILL) ---")]
    
    [Tooltip("Số lượng bếp mở sẵn, hoạt động bình thường lúc đầu game")]
    public int normalGrillCount = 4;       
    
    [Tooltip("Số lượng bếp bị đóng nắp (Người chơi phải ghép đúng món để mở khóa)")]
    public int lockedGrillCount = 0;
    [Header("--- CẤU HÌNH BOOSTER ---")]
    
    [Tooltip("Số lượng Booster sẽ bị khóa ngẫu nhiên trong màn này (Level 1: 0, Level 2: 1...)")]
    public int lockedBoosterCount = 0;       
}
