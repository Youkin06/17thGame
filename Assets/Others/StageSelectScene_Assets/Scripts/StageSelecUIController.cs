using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;

/*外部スクリプトからほしいデータ*/
//最新ステージID(currentStageNumに入れたい)
//遷移するステージ名(stageNumとsceneNamgeに対応するもの)
//アクティブ化されているかどうか(StageButtonDataのisActiveを永続化したい、isActiveはステージ選択画面でのみ更新)
//クリア済みステージのスコア

[System.Serializable]
public class StageButtonData
{
    public int stageNum;
    public string sceneName;
    public Button buttonObj;
    public bool isActive;
    public Image betweenLine;
}

public class StageSelecUIController : MonoBehaviour
{
    [SerializeField] private List<StageButtonData> stageButtons;

    [SerializeField] private Sprite latestStageSprite;
    [SerializeField] private Sprite clearStageSprite;
    [SerializeField] private Sprite lockedStageSprite;
    [SerializeField] private float lineMoveDuration = 0.2f;

    [SerializeField] private Button decideButton;
    [SerializeField] private Vector3 decideButtonXOffset = new Vector3(-2f, 0f, 0f);
    [SerializeField] private StageSelectCameraController cameraController;
    
    [SerializeField] private int currentStageNum;
    [SerializeField] private GameObject selectMark_rect;
    private int selectStageNum;
    // Start is called before the first frame update
    void Start()
    {
        cameraController.SetStageButtonsPos();
        SetStageNum(currentStageNum);//最初は最新ステージを選択している状態にする
        //ステージのボタンの初期化
        foreach (var buttonData in stageButtons)
        {
            Button button = buttonData.buttonObj;
            button.onClick.AddListener(()=>SetStageNum(buttonData.stageNum));//メソッドの割り当て(引数はインデックスと一緒)
        }

        UpdateStageButtonState();//ステージのボタンの状態を更新

        //決定ボタンにメソッド割り当て
        decideButton.onClick.AddListener(DecideStage);
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
        decideButton.transform.position = stageButtons[stageNum].buttonObj.transform.position + decideButtonXOffset;//決定ボタンの移動 
        Debug.Log($"ステージ:{stageNum}を選択");
    }

    public void DecideStage()
    {
        Debug.Log($"シーン:{SearchSceneName(selectStageNum)}に移動");
        // SceneManager.LoadScene(sceneName[selectStageNum]);
    }

    //ステージのボタン状態を更新するメソッド
    public void UpdateStageButtonState()
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == currentStageNum)
            {
                if (buttonData.isActive)
                {
                   ChangeButtonImage(buttonData.stageNum, latestStageSprite);//現在のステージは最新ステージのスプライトに変更
                   ChangeLockedStageButton(buttonData.stageNum, true);//現在のステージを開放
                }
                else
                {
                    //ステージ解放演出を入れる
                    ChangeButtonImage(buttonData.stageNum, lockedStageSprite);//一度ロックステージのスプライトに変更
                    PlayUnlockAnimation(buttonData.stageNum);//ステージ解放演出再生
                    ChangeLockedStageButton(buttonData.stageNum, true);//現在のステージを開放
                    Debug.Log($"ステージ{buttonData.stageNum}を解放");//【DEBUG】ステージの状態をログに表示
                }
            }
            else if (buttonData.stageNum < currentStageNum)
            {
                ChangeLockedStageButton(buttonData.stageNum, true);//現在のステージより前のステージは開放
                ChangeButtonImage(buttonData.stageNum, clearStageSprite);//クリア済みのステージはクリアステージのスプライトに変更
                SetBetweenLineActive(buttonData.stageNum, true);//現在のステージより前のステージは道を表示
                Debug.Log($"ステージ{buttonData.stageNum}/isActive:{buttonData.isActive}");//【DEBUG】ステージの状態をログに表示
                //【NEXT】スコア表示処理を入れる
            }
            else
            {
                ChangeLockedStageButton(buttonData.stageNum, false);//現在のステージより後のステージはロック
                ChangeButtonImage(buttonData.stageNum, lockedStageSprite);//未開放のステージはロックステージのスプライトに変更
                SetBetweenLineActive(buttonData.stageNum, false);//現在のステージより後のステージは道を非表示
                Debug.Log($"ステージ{buttonData.stageNum}/isActive:{buttonData.isActive}");//【DEBUG】ステージの状態をログに表示
            }
        }
    }

    //ステージをロック/開放するメソッド
    public void ChangeLockedStageButton(int stageNum, bool isActive)
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum)
            {
                buttonData.isActive = isActive;
                buttonData.buttonObj.interactable = isActive;
                return;
            }
        }
    }

    //ステージ解放演出アニメーション
    public void PlayUnlockAnimation(int stageNum)
    {
        PlayFillBetweenLine(stageNum,1f,()=>//道のアニメーション再生
        {
           ChangeButtonImage(stageNum, latestStageSprite);//ステージのスプライトを最新ステージのスプライトに変更 
        });
        
        //【NEXT】その他、解放演出を入れる
    }

    //ステージ解放による道のアニメーション再生
    public void PlayFillBetweenLine(int stageNum,float targetFill,Action onComplete = null)
    {
        Image betweenLine = SearchBetweenLine(stageNum);//ステージ番号に対応するボタンから道のImageコンポーネントを取得
        betweenLine.fillAmount = 0;//道の初期状態を非表示にしてからアニメーション再生
        betweenLine.DOFillAmount(targetFill, lineMoveDuration)
              .SetEase(Ease.OutQuad)
              .OnComplete(() => onComplete?.Invoke());//アニメーション完了後にコールバックを呼び出す
    }

    //ステージ間の道の表示/非表示を切り替えるメソッド
    public void SetBetweenLineActive(int stageNum, bool isActive)
    {
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum)
            {
                if(buttonData.betweenLine != null)
                {
                    buttonData.betweenLine.fillAmount = isActive ? 1 : 0;//道の表示/非表示を切り替え
                    buttonData.betweenLine.gameObject.SetActive(isActive);
                }
                return;
            }
        }
    }

    //ステージボタンの画像を差し替えるメソッド
    public void ChangeButtonImage(int stageNum, Sprite newSprite)
    {        
        foreach(var buttonData in stageButtons)
        {
            if(buttonData.stageNum == stageNum){
                buttonData.buttonObj.image.sprite = newSprite;
                return;
            }
        }
    }

    //リストからステージ番号に対応するボタンを検索して返すメソッド
    public Button SearchButtonObj(int stageNum)
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

    //リストからステージ番号に対応するImageを検索して返すメソッド
    public Image SearchBetweenLine(int stageNum)
    {
        foreach(var buttonData in stageButtons){
            if(buttonData.stageNum == stageNum){
                return buttonData.betweenLine;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するシーン名を検索して返すメソッド
    public string SearchSceneName(int stageNum)
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
                buttonData.isActive = isActive;
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

