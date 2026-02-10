using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private SceneNavigator sceneNavigator; // SceneNavigatorをアタッチ

    [Header("Popups")]
    [SerializeField]
    private GameObject gameClearPanel; // GameClear_Panel

    [SerializeField]
    private GameObject gameOverPanel; // GameOver_Panel

    [Header("Display Text")]
    [SerializeField]
    private Text clearTimeText; // ClearTime_text

    [Header("Scene Names")]
    [SerializeField]
    private string stageSelectSceneName = "StageSelect"; // 戻る先のシーン名

    [SerializeField]
    private string nextStageSceneName = "Stage2"; // 次のステージ名

    void Start()
    {
        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// ゲームクリア画面を表示する
    /// <param name="clearTime">クリアにかかった時間(秒)</param>
    public void ShowGameClear(float clearTime)
    {
        gameClearPanel.SetActive(true);

        // タイム表示の更新
        if (clearTimeText != null)
        {
            // 時間(float)を "00:00.00" 形式の文字列に変換
            TimeSpan ts = TimeSpan.FromSeconds(clearTime);
            string timeString = string.Format(
                "{0:D2}:{1:D2}.{2:D2}",
                ts.Minutes,
                ts.Seconds,
                ts.Milliseconds / 10
            );

            clearTimeText.text = $"Clear Time: {timeString}";
        }
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void OnClickNextStage()
    {
        if (sceneNavigator != null)
        {
            sceneNavigator.LoadScene(nextStageSceneName);
        }
        else
        {
            Debug.LogWarning("SceneNavigatorがアタッチされていません。");
        }
    }

    public void OnClickRetry()
    {
        if (sceneNavigator != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            sceneNavigator.LoadScene(currentScene);
        }
    }

    public void OnClickToSelect()
    {
        if (sceneNavigator != null)
        {
            sceneNavigator.LoadScene(stageSelectSceneName);
        }
    }
}
