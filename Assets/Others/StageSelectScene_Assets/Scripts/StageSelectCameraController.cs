using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StageSelectCameraController : MonoBehaviour
{
    [Header("ステージ選択画面UI制御コントローラ")]
    [SerializeField] private StageSelecUIController uiController;
    private Vector3[] stageButtonsPos;
    [Header("動かすカメラオブジェクト")]
    [SerializeField] private GameObject targetObj;
    [Header("カメラの配置オフセット")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -3f);

    // Start is called before the first frame update
    void Start()
    {
        // MoveStagePos(currentStage);//現在いるステージのボタンの位置に移動
    }

    //指定のインデックスのボタンまで移動するメソッド
    public void MoveStagePos(int stageNum)
    {
        Vector3 stagePos = stageButtonsPos[stageNum];//選択しているステージのインデックスのボタンの位置を取得
        Vector3 targetPos = new Vector3(stagePos.x,stagePos.y,stagePos.z) + cameraOffset;
        targetObj.transform.DOMove(targetPos,1f).SetEase(Ease.OutQuart);//ボタンの位置まで滑らかに移動

        // targetObj.transform.position = new Vector3(stagePos.x,stagePos.y,stagePos.z-zPos_offset);
    }

    public void SetStageButtonsPos()
    {
        this.stageButtonsPos = uiController.GetStageButtonsPos();//UIコントローラーからステージのボタンの位置を取得
    }
}
