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
    public string[] sceneName;
    // Start is called before the first frame update
    void Start()
    {
        int i = 0;
        foreach (Button button in stageButtons)
        {
            int num = i;
            button.onClick.AddListener(()=>SetStageNum(num));
            i++;
        }
        decideButton.onClick.AddListener(DecideStage);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStageNum(int stageNum)
    {
        selectStageNum = stageNum;
        cameraController.MoveStagePos(stageNum);
        Debug.Log($"ステージ:{stageNum}を選択");
    }

    public void DecideStage()
    {
        Debug.Log($"シーン:{sceneName[selectStageNum]}に移動");
        // SceneManager.LoadScene(sceneName[selectStageNum]);
    }
}
