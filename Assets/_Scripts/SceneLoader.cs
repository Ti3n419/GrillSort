using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    public static string TargetSceneName;

    public static void LoadSceneWithLoadingScreen(string targetScene, string loadingScene)
    {
        TargetSceneName = targetScene;// 1. Nhớ tên Scene cần đến
        SceneManager.LoadScene(loadingScene);// 2. Nhảy sang Scene Loading
    }
}
