using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StageSelectCameraController : MonoBehaviour
{
    [SerializeField] private StageSelecUIController uiController;
    private Vector3[] stageButtonsPos;
    [SerializeField] private int currentStage;
    [SerializeField] private int selectStage;
    [SerializeField] private GameObject targetObj;
    [SerializeField] private int zPos_offset = 2;

    // Start is called before the first frame update
    void Start()
    {
        SetStageButtonsPos();
        MoveStagePos(currentStage);//現在いるステージのボタンの位置に移動
    }

    //指定のインデックスのボタンまで移動するメソッド
    public void MoveStagePos(int stageNum)
    {
        Vector3 stagePos = stageButtonsPos[stageNum];//選択しているステージのインデックスのボタンの位置を取得
        Vector3 targetPos = new Vector3(stagePos.x,stagePos.y,stagePos.z - zPos_offset);
        targetObj.transform.DOMove(targetPos,1f).SetEase(Ease.OutQuart);//ボタンの位置まで滑らかに移動

        // targetObj.transform.position = new Vector3(stagePos.x,stagePos.y,stagePos.z-zPos_offset);
    }

    public void SetStageButtonsPos()
    {
        this.stageButtonsPos = uiController.GetStageButtonsPos();//UIコントローラーからステージのボタンの位置を取得
    }
}
