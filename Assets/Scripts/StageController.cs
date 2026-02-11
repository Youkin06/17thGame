using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageController : MonoBehaviour
{
    public Slider slider;
    public GameObject sceneHandler;
    public GameObject gameOverPopUp;
    public GameObject gameClearPopUp;
    //public string ResultSceneName;
    //public string GameOverSceneName;

    private bool isGoaled = false;
    private bool isOver = false;
    // Start is called before the first frame update
    void Start()
    {
        //時間停止を解除(リトライ時用)
        Time.timeScale = 1;

        gameClearPopUp.SetActive(false);
        gameOverPopUp.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(slider.value <= 0 && isGoaled == false && isOver == false)
        {
            //1秒後にゲームを停止
            Invoke(nameof(TimeStop),1f);
            //ゲームオーバーポップを表示
            gameOverPopUp.SetActive(true);
            isGoaled = true;
            //OnPlayerDead();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れた対象がゴールかつ、エイムアシストではない自分自身のColliderが触れた場合かつ、まだゴールしていない場合のみクリア処理
        if (collision.gameObject.tag == "Goal" && GetComponent<Collider2D>().IsTouching(collision) && isGoaled == false &&isOver==false)
        {
            //1秒後にゲームを停止
            Invoke(nameof(TimeStop),1f);
            //ゲームクリアポップを表示
            gameClearPopUp.SetActive(true);
            //OnGoalReached();
        }
    }

    //時間停止
    public void TimeStop()
    {
        Time.timeScale = 0;
    }
    
    // //ゴール到達時の挙動
    // public void OnGoalReached()
    // {
    //     Debug.Log("Game Clear/Goal");
    //     if (sceneHandler != null)
    //     {
    //         // sceneHandlerからSceneNavigatorスクリプトを取得
    //         SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

    //         // スクリプトが見つかったら関数を実行
    //         if (navigator != null)
    //         {
    //             navigator.LoadScene(ResultSceneName);
    //         }
    //         else
    //         {
    //             Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
    //         }
    //     }
    // }
    // // プレイヤー死亡時の挙動
    // public void OnPlayerDead()
    // {
    //     Debug.Log("Game Over/Dead");
    //     if (sceneHandler != null)
    //     {
    //         // sceneHandlerからSceneNavigatorスクリプトを取得
    //         SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

    //         // スクリプトが見つかったら関数を実行
    //         if (navigator != null)
    //         {
    //             navigator.LoadScene(GameOverSceneName);
    //         }
    //         else
    //         {
    //             Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
    //         }
    //     }
    // }

}
