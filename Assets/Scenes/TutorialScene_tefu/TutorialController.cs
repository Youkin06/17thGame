using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    public Slider slider;
    public GameObject sceneHandler;
    public string ResultSceneName;
    public string GameOverSceneName;

    //ゲームオーバー処理を1度だけ行うための判定
    private bool isGameEnded = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(slider.value <= 0 && isGameEnded == false)
        {
            isGameEnded = true;
            OnPlayerDead();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れた対象がゴールかつ、エイムアシストではない自分自身のColliderが触れた場合にのみクリア処理
        if (collision.gameObject.tag == "Goal" && GetComponent<Collider2D>().IsTouching(collision))
        {
            OnGoalReached();
        }
    }

    //ゴール到達時の挙動
    public void OnGoalReached()
    {
        Debug.Log("Game Clear/Goal");
        if (sceneHandler != null)
        {
            // sceneHandlerからSceneNavigatorスクリプトを取得
            SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

            // スクリプトが見つかったら関数を実行
            if (navigator != null)
            {
                navigator.LoadScene(ResultSceneName);
            }
            else
            {
                Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
            }
        }
    }

    // プレイヤー死亡時の挙動
    public void OnPlayerDead()
    {
        Debug.Log("Game Over/Dead");
        if (sceneHandler != null)
        {
            // sceneHandlerからSceneNavigatorスクリプトを取得
            SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

            // スクリプトが見つかったら関数を実行
            if (navigator != null)
            {
                navigator.LoadScene(GameOverSceneName);
            }
            else
            {
                Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
            }
        }
    }

}
