using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestStageController : MonoBehaviour
{
    public Slider slider;

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
        if (collision.gameObject.tag == "Goal")
        {
            OnGoalReached();
        }
    }

    //ゴール到達時の挙動
    public void OnGoalReached()
    {
        Debug.Log("Game Clear/Goal");
    }

    // プレイヤー死亡時の挙動
    public void OnPlayerDead()
    {
        Debug.Log("Game Over/Dead");
    }

}
