using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    // ReSharper disable Unity.PerformanceAnalysis
    public void LoadScene(string sceneName)
    {
        // 現在開いているシーン名を取得しておく。
        // sceneName が未設定だった場合や、指定したシーンが読み込めない場合のフォールバック先として使う。
        string currentSceneName = SceneManager.GetActiveScene().name;

        // sceneName が null / 空文字 / 空白のみの場合は、現在のシーンを読み込み対象にする。
        string targetSceneName = string.IsNullOrWhiteSpace(sceneName) ? currentSceneName : sceneName;

        // 指定されたシーンが Build Settings に存在しない場合は、現在のシーンにフォールバックする。
        // LoadScene 実行後の try-catch ではUnity側のエラーログを防げないため、事前に確認する。
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"Scene '{targetSceneName}' could not be loaded. Loading current scene instead.");
            targetSceneName = currentSceneName;
        }

        Debug.Log("Loading Scene: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Game Exit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
