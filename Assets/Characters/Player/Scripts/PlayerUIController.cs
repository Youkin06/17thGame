using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    public Slider slider;
    public float timeDecreaseRate = 0.1f;
    public float distanceDecreaseRate = 0.01f; // 移動距離1単位あたりの減少率

    void Start(){
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        slider.maxValue = 1f;
        slider.minValue = 0f;
        slider.value = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController != null && playerController.isHijacking)
        {
            slider.value -= Time.deltaTime * timeDecreaseRate;
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
        if (playerController == null || !playerController.isHijacking)
        {
            slider.value -= distanceDelta * distanceDecreaseRate;
        }
    }
    
    /// <summary>
    /// スライダー値を全回復（1.0）にリセット
    /// </summary>
    public void ResetHijackTimer()
    {
        slider.value = 1f;
    }
}
