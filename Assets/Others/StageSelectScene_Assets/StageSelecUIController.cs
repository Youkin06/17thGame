using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class StageSelecUIController : MonoBehaviour
{
    public Button[] stageButtons;
    public Button decideButton;
    public StageSelectCameraController cameraController;
    int selectStageNum;
    public int currentStageNum;
    public string[] sceneName;
    public GameObject selectMark_rect;
    // Start is called before the first frame update
    void Start()
    {
        //ステージのボタンの初期化
        int i = 0;
        foreach (Button button in stageButtons)
        {
            int num = i;
            button.onClick.AddListener(()=>SetStageNum(num));//メソッドの割り当て(引数はインデックスと一緒)
            if(num >= currentStageNum)
            {
                button.interactable = false;//現在のステージより後ならボタンを無効化
            }
            i++;
        }

        //決定ボタンにメソッド割り当て
        decideButton.onClick.AddListener(DecideStage);
        
        selectMark_rect.transform.position = stageButtons[selectStageNum].transform.position;//選択UIの初期位置移動
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStageNum(int stageNum)
    {
        selectStageNum = stageNum;
        cameraController.MoveStagePos(stageNum);//カメラの移動
        selectMark_rect.transform.position = stageButtons[stageNum].transform.position;//選択UIの移動
        Debug.Log($"ステージ:{stageNum}を選択");
    }

    public void DecideStage()
    {
        Debug.Log($"シーン:{sceneName[selectStageNum]}に移動");
        // SceneManager.LoadScene(sceneName[selectStageNum]);
    }
}
