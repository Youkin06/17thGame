using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;
using TMPro;
using UnityEditor.SceneManagement;

/*外部スクリプトからほしいデータ*/
//最新ステージID(latestStageNumに入れたい)
//遷移するステージ名(stageNumとsceneNamgeに対応するもの)
//アクティブ化されているかどうか(StageButtonDataのisUnlockedを永続化したい、isUnlockedはステージ選択画面でのみ更新)
//クリア済みステージのスコア

//ボタンに持たせるデータのクラス
[System.Serializable]
public class StageButtonData
{
    public int stageNum;
    public string sceneName;//【TODO】ステージ管理用のクラスができたらそちらで管理する
    public Button buttonObj;
    public Image betweenLine;
    public TMP_Text scoreText;
    [HideInInspector] public bool isUnlocked;//開放済みかどうか
    [HideInInspector] public bool isClear;//クリア済みかどうか
    [HideInInspector] public int score;//クリア済みの場合のスコア
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

    [SerializeField] private int latestStageNum;
    [SerializeField] private GameObject selectMark_rect;
    private int selectStageNum;

    // Start is called before the first frame update
    void Start()
    {
        var progress = GameProgressManager.Instance;
        LoadProgressToStageButtons(progress.ProgressData);//進捗データをステージボタンのデータに反映させる

        cameraController.SetStageButtonsPos();//カメラ制御スクリプトにステージのボタンの位置を渡す
        SetStageNum(latestStageNum);//最初は最新ステージを選択している状態にする
        //ステージのボタンの初期化
        foreach (var buttonData in stageButtons)
        {
            Button button = buttonData.buttonObj;
            button.onClick.AddListener(() => SetStageNum(buttonData.stageNum));//メソッドの割り当て(引数はインデックスと一緒)
        }

        //ステージのボタンの状態を更新
        if (progress.JustClearedStageId.HasValue)
        {
            OnClearStageUpdate();
            progress.ClearStageSelectReturnFlags();
        }
        else
        {
            NormalStageUpdate();
        }

        //決定ボタンにメソッド割り当て
        decideButton.onClick.AddListener(DecideStage);
    }

    //進捗データ読み込みメソッド
    private void LoadProgressToStageButtons(GameProgressData progressData)
    {
        latestStageNum = progressData.latestUnlockedStage;//最新ステージIDをDBから取得して変数に入れる
        foreach (var buttonData in stageButtons)
        {
            var progress = GameProgressManager.Instance.GetStageProgress(buttonData.stageNum);
            if (progress == null) continue;

            buttonData.isUnlocked = progress.isUnlocked;
            buttonData.isClear = progress.isCleared;
            buttonData.score = progress.score;
        }
    }

    //ステージ番号を受け取って、カメラとUIを移動させるメソッド
    public void SetStageNum(int stageNum)
    {
        selectStageNum = stageNum;
        cameraController.MoveStagePos(stageNum);//カメラの移動
        selectMark_rect.transform.position = stageButtons[stageNum].buttonObj.transform.position;//選択UIの移動
        decideButton.transform.position = stageButtons[stageNum].buttonObj.transform.position + decideButtonXOffset;//決定ボタンの移動 
        Debug.Log($"ステージ:{stageNum}を選択");
    }

    //決定ボタンを押したときの処理
    public void DecideStage()
    {
        Debug.Log($"シーン:{SearchSceneName(selectStageNum)}に移動");
        //【TODO】ステージの名前が決定したらシーン遷移の処理を追加
        // SceneManager.LoadScene(SearchSceneName(selectStageNum));
    }

    //ステージクリア後にステージの状態を更新するメソッド
    public void OnClearStageUpdate()
    {
        foreach (StageButtonData buttonData in stageButtons)
        {
            ApplyTentativeStageState(buttonData);
        }
        //【TODO】クリア時に必要な演出があればここで記述
        PlayFillBetweenLine(latestStageNum, 1f, () =>//道のアニメーション再生
        {
            //【TODO】他に開放時に必要な演出があればここで記述
            ChangeLockedStageButton(latestStageNum, true);//最新ステージのボタンを開放
            ApplyFinalStageState(SearchStageButtonData(latestStageNum));//最終的な状態にする
        });
    }

    //ステージクリア後以外でステージの状態を更新するメソッド
    public void NormalStageUpdate()
    {
        foreach (StageButtonData buttonData in stageButtons)
        {
            ApplyFinalStageState(buttonData);
        }
    }


    //ステージ開放演出前の一時的な状態を適用するメソッド(ステージ開放演出前に呼び出す)
    public void ApplyTentativeStageState(StageButtonData buttonData)
    {
        if (buttonData.isClear)
        {
            ChangeLockedStageButton(buttonData.stageNum, true);
            ChangeButtonImage(buttonData.stageNum, clearStageSprite);
            SetBetweenLineActive(buttonData.stageNum, true);
            UpdateScoreText(buttonData.stageNum, true, buttonData.score);//仮のスコア値
        }
        else
        {
            ChangeLockedStageButton(buttonData.stageNum, false);
            ChangeButtonImage(buttonData.stageNum, lockedStageSprite);
            SetBetweenLineActive(buttonData.stageNum, false);
            UpdateScoreText(buttonData.stageNum, false, buttonData.score);//スコアの非表示
        }
    }

    //最終的なステージの状態を適用するメソッド(ステージ開放演出後に呼び出す)
    public void ApplyFinalStageState(StageButtonData buttonData)
    {
        if (buttonData.isClear)
        {
            ChangeLockedStageButton(buttonData.stageNum, true);
            ChangeButtonImage(buttonData.stageNum, clearStageSprite);
            SetBetweenLineActive(buttonData.stageNum, true);
            UpdateScoreText(buttonData.stageNum, true, buttonData.score);//仮のスコア値
        }
        else if (buttonData.isUnlocked)
        {
            ChangeLockedStageButton(buttonData.stageNum, true);
            ChangeButtonImage(buttonData.stageNum, latestStageSprite);
            SetBetweenLineActive(buttonData.stageNum, true);
            UpdateScoreText(buttonData.stageNum, false, buttonData.score);//スコアの非表示
        }
        else
        {
            ChangeLockedStageButton(buttonData.stageNum, false);
            ChangeButtonImage(buttonData.stageNum, lockedStageSprite);
            SetBetweenLineActive(buttonData.stageNum, false);
            UpdateScoreText(buttonData.stageNum, false, buttonData.score);//スコアの非表示
        }
    }


    //ステージをロック/開放するメソッド
    public void ChangeLockedStageButton(int stageNum, bool isUnlocked)
    {
        StageButtonData buttonData = SearchStageButtonData(stageNum);
        buttonData.isUnlocked = isUnlocked;
        buttonData.buttonObj.interactable = isUnlocked;
        return;
    }

    //ステージ解放による道のアニメーション再生
    public void PlayFillBetweenLine(int stageNum, float targetFill, Action onComplete = null)
    {
        Image betweenLine = SearchBetweenLine(stageNum);//ステージ番号に対応するボタンから道のImageコンポーネントを取得
        betweenLine.fillAmount = 0;//道の初期状態を非表示にしてからアニメーション再生
        betweenLine.gameObject.SetActive(true);//道のオブジェクトをアクティブにする

        betweenLine.DOFillAmount(targetFill, lineMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());//アニメーション完了後にコールバックを呼び出す
    }

    //ステージ間の道の表示/非表示を切り替えるメソッド
    public void SetBetweenLineActive(int stageNum, bool isUnlocked)
    {
        StageButtonData buttonData = SearchStageButtonData(stageNum);
        if (buttonData != null && buttonData.betweenLine != null)
        {
            buttonData.betweenLine.fillAmount = isUnlocked ? 1 : 0;//道の表示/非表示を切り替え
            buttonData.betweenLine.gameObject.SetActive(isUnlocked);
        }
        return;
    }


    //ステージボタンの画像を差し替えるメソッド
    public void ChangeButtonImage(int stageNum, Sprite newSprite)
    {
        StageButtonData buttonData = SearchStageButtonData(stageNum);
        buttonData.buttonObj.image.sprite = newSprite;
        return;
    }

    //ステージボタンのスコアテキストを更新するメソッド
    public void UpdateScoreText(int stageNum, bool isClear, float score)
    {
        StageButtonData buttonData = SearchStageButtonData(stageNum);
        if (buttonData != null && buttonData.scoreText != null)
        {
            if (isClear)
            {
                buttonData.scoreText.text = $"Score:{score}";
                buttonData.scoreText.gameObject.SetActive(true);
            }
            else
            {
                buttonData.scoreText.text = "";
                buttonData.scoreText.gameObject.SetActive(false);
            }
        }
        return;
    }

    //リストからステージ番号に対するデータ自体を検索して返すメソッド
    public StageButtonData SearchStageButtonData(int stageNum)
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.stageNum == stageNum)
            {
                return buttonData;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するボタンを検索して返すメソッド
    public Button SearchButtonObj(int stageNum)
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.stageNum == stageNum)
            {
                return buttonData.buttonObj;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するImageを検索して返すメソッド
    public Image SearchBetweenLine(int stageNum)
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.stageNum == stageNum)
            {
                return buttonData.betweenLine;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するシーン名を検索して返すメソッド
    public string SearchSceneName(int stageNum)
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.stageNum == stageNum)
            {
                return buttonData.sceneName;
            }
        }
        return null;
    }

    //リストからステージ番号に対応するボタンを検索して、ボタンの有効/無効を切り替えるメソッド
    public void SetButtonisUnlocked(int stageNum, bool isUnlocked)
    {
        foreach (var buttonData in stageButtons)
        {
            if (buttonData.stageNum == stageNum)
            {
                buttonData.isUnlocked = isUnlocked;
                buttonData.buttonObj.interactable = isUnlocked;
                return;
            }
        }
    }

    //リストからボタンの座標配列を取得するメソッド
    public Vector3[] GetStageButtonsPos()
    {
        Vector3[] stageButtonsPos = new Vector3[stageButtons.Count];
        for (int i = 0; i < stageButtons.Count; i++)
        {
            stageButtonsPos[i] = stageButtons[i].buttonObj.transform.position;
        }
        return stageButtonsPos;
    }
}

