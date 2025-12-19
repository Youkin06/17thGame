using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    public bool isHijacking = false;
    public Slider slider;
    public float hpDecreaseRate = 0.1f;
    public float distanceDecreaseRate = 0.01f; // 移動距離1単位あたりの減少率

    void Start(){
        slider.maxValue = 1f;
        slider.minValue = 0f;
        slider.value = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (isHijacking)
        {
            slider.value -= Time.deltaTime * hpDecreaseRate;
        }

        if (slider.value <= 0)
        {
            //ゲームオーバー処理
            Debug.Log("GameOver");
        }
    }

    /// <summary>
    /// プレイヤーが移動した時に呼ばれるメソッド
    /// </summary>
    /// <param name="distanceDelta">このフレームでの移動距離</param>
    public void OnPlayerMoved(float distanceDelta)
    {
        if (!isHijacking)
        {
            slider.value -= distanceDelta * distanceDecreaseRate;
        }
    }
}
