using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectCameraController : MonoBehaviour
{
    public Button[] stageButtons;
    public int currentStage;
    public int selectStage;
    public Camera mainCamera;
    public GameObject targetObj;
    int zPos_offset = 2;

    // Start is called before the first frame update
    void Start()
    {
        MoveStagePos(currentStage);
    }

    // Update is called once per frame
    void Update()
    {
    
    }

    public void MoveStagePos(int stageNum)
    {
        Vector3 stagePos = stageButtons[stageNum].transform.position;
        targetObj.transform.position = new Vector3(stagePos.x,stagePos.y,stagePos.z-zPos_offset);
    }
}
