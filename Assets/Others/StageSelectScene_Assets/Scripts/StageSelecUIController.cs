using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StageButtonData
{
    public int stageNum;
    public string sceneName;
    public Button buttonObj;
    public bool isButtonActive;
    public Image betweenLine;
}

public class StageSelecUIController : MonoBehaviour
{
    [SerializeField] private List<StageButtonData> stageButtons;
    public Button decideButton;
    public StageSelectCameraController cameraController;
    int selectStageNum;
    public int currentStageNum;
    public GameObject selectMark_rect;
    // Start is called before the first frame update
    void Start()
    {
        //ステージのボタンの初期化
        int i = 0;
        foreach (var buttonData in stageButtons)
        {
            int num = i;
            Button button = buttonData.buttonObj;
            button.onClick.AddListener(()=>SetStageNum(num));//メソッドの割り当て(引数はインデックスと一緒)
            if(num >= currentStageNum)
            {
                button.interactable = false;//現在のステージより後ならボタンを無効化
            }
            i++;
        }

        //決定ボタンにメソッド割り当て
        decideButton.onClick.AddListener(DecideStage);
        
        selectMark_rect.transform.position = stageButtons[selectStageNum].buttonObj.transform.position;//選択UIの初期位置移動
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStageNum(int stageNum)
    {
        selectStageNum = stageNum;
        cameraController.MoveStagePos(stageNum);//カメラの移動
        selectMark_rect.transform.position = stageButtons[stageNum].buttonObj.transform.position;//選択UIの移動
        Debug.Log($"ステージ:{stageNum}を選択");
    }

    public void DecideStage()
    {
        Debug.Log($"シーン:{searchSceneName(selectStageNum)}に移動");
        // SceneManager.LoadScene(sceneName[selectStageNum]);
    }

    //リストからステージ番号に対応するボタンを検索して返すメソッド
    public Button searchButton(int stageNum)
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum)
            {
                return buttonData.buttonObj;
            }
        }
        return null;
    } 

    //リストからステージ番号に対応するシーン名を検索して返すメソッド
    public string searchSceneName(int stageNum)
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum)
            {
                return buttonData.sceneName;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するボタンを検索して、ボタンの有効/無効を切り替えるメソッド
    public void SetButtonIsActive(int stageNum, bool isActive)
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum)
            {
                buttonData.isButtonActive = isActive;
                buttonData.buttonObj.interactable = isActive;
                return;
            }
        }
    }

    //リストからボタンの座標配列を取得するメソッド
    public Vector3[] GetStageButtonsPos()
    {
        Vector3[] stageButtonsPos = new Vector3[stageButtons.Count];
        for(int i=0; i<stageButtons.Count; i++)
        {
            stageButtonsPos[i] = stageButtons[i].buttonObj.transform.position;
        }
        return stageButtonsPos;
    }
}

