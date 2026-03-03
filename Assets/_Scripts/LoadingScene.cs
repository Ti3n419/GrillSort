using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _loadingFillImage;

    [Header("Startup Config (Dành cho Scene Loading đầu tiên)")]
    [SerializeField] private string _fallbackTargetScene = "MainMenu";// Nếu Target rỗng thì mặc định về Menu

    private void Start()
    {
        // Kiểm tra xem SceneLoader có được giao nhiệm vụ không, nếu không thì lấy mặc định
        string sceneToLoad = string.IsNullOrEmpty(SceneLoader.TargetSceneName) ? _fallbackTargetScene : SceneLoader.TargetSceneName;
        StartCoroutine(LoadSceneAsync(sceneToLoad));
    }
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Bắt đầu tải Scene ngầm dưới nền
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Chặn không cho tự động chuyển Scene ngay khi tải xong (để làm mượt thanh fill)
        operation.allowSceneActivation = false;

        float visualProgress = 0f;
        // Đảm bảo thanh Fill bắt đầu từ 0
        if (_loadingFillImage != null) _loadingFillImage.fillAmount = 0f;

        while (!operation.isDone)
        {
            // [MẸO UNITY]: operation.progress chỉ chạy từ 0 đến 0.9 là dừng (0.9 là tải xong)
            // Chia cho 0.9f để quy chuẩn nó về mốc 0.0 -> 1.0
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            // Dùng MoveTowards để thanh trượt chạy mượt mà lên chứ không bị giật cục
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, 3f * Time.deltaTime);

            if (_loadingFillImage != null)
            {
                _loadingFillImage.fillAmount = visualProgress;
            }
            // Khi đã tải xong (progress tới 1) và thanh UI cũng đã chạy tới mốc 1
            if (operation.progress >= 0.9f && visualProgress >= 1f)
            {
                // Cho phép Unity hiển thị Scene mới lên
                operation.allowSceneActivation = true;
            }
            yield return null;// Chờ đến khung hình tiếp theo
        }
    }
}
