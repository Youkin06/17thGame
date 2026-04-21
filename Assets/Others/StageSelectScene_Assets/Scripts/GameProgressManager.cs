using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

/*使い方*/
    /// <summary>
    /// ゲームの進捗を管理するクラス。
    /// 初期化・リセットはInitializeStageData()を呼ぶ
    /// TestProgressDataはテスト用のデータクラスで、インスペクター上で進捗データをセットするためのもの。実装後は不要になる想定。
    /// 進捗の保存と読み込みの処理はSave()とLoad()に記述する。PlayerPrefsなどで保存することを想定している
    /// ステージクリア時はStageCleared()を呼び、ステージ開放時はStageUnlocked()を呼ぶ。最新ステージの更新はUpdateLatestUnlockedStage()を呼ぶ。
    /// 必ずStageUnlocked()を呼んでからUpdateLatestUnlockedStage()を呼ぶこと。ステージのスコアを更新する場合はUpdateStageScore()を呼ぶ。
    /// </summary>

/*今後実装してほしいこと*/
    /// テスト用のデータを本番用に置き換える
    /// ステージの名前とシーン名を紐付けるクラス(ScriptableObjectなど)を用意してTestProgressData.stageDatasの代わりにする
    /// 各ステージの進捗と最新ステージ番号が不整合になった場合に修正するためのメソッドを作る
    

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    public GameProgressState ProgressData { get; private set; }//各ステージのクリア状況を管理する進捗データ
    [Header("テスト用の進捗データ")]//【テスト用】インスペクターでセットするためのフィールド
    [SerializeField] private TestProgressData testProgressData;//【テスト用】インスペクターでテストデータをセットするためのフィールド
    public int? JustClearedStageId { get; private set; }//UIControllerでステージクリアの演出をするための、直近でクリアしたステージIDを保持するプロパティ。
    public int? JustUnlockedStageId { get; private set; }//UIControllerでステージ開放の演出をするための、直近で開放したステージIDを保持するプロパティ。

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();

        ProgressData = new GameProgressState
        {
            latestUnlockedStage = 0,
            stages = new List<StageProgressState>()
        };
        InitializeStageData(testProgressData.totalStages);//ステージデータを初期化するメソッド
        TestSetDataSet();//【テスト用】進捗データセットするメソッド
        
    }


    //ステージデータを初期化・リセットするメソッド
    public void InitializeStageData(int stageNum)
    {
        ProgressData.stages = new List<StageProgressState>();
        for (int i = 0; i < stageNum; i++)
        {
            ProgressData.stages.Add(new StageProgressState(i));
            ProgressData.stages[i].isUnlocked = (i == 0); //最初のステージだけ開放
            ProgressData.stages[i].isCleared = false;
            ProgressData.stages[i].score = 0;
        }
    }


    //【テスト用】進捗データをセットするメソッド
    public void TestSetDataSet()
    {
        //テスト用データの作成
        int totalstages = testProgressData.totalStages; //総ステージ数（例: 5）
        int latestUnlockedStage = testProgressData.latestUnlockedStage; //最新ステージID（例: 2）

        int clearStage = latestUnlockedStage - 1;//直近でクリアした、最新ステージの一つ前のステージID（例: 1）
        int clearScore = 3000;//直近でクリアしたステージのスコア（例: 3000）
    
        var statuses = testProgressData.stageStatuses; //ステージ進捗データの配列（例: 5ステージ分） --- IGNORE ---

        //初期化
        InitializeStageData(totalstages);

        //ステージ進捗情報の更新
        for(int i = 0; i < statuses.Length; i++)
        {
            ProgressData.stages[i].isUnlocked = statuses[i].unlocked;
            ProgressData.stages[i].isCleared = statuses[i].cleared;
            ProgressData.stages[i].score = statuses[i].score;
        }

        //【Test】ステージクリア時の処理
        // 本来はステージ開放時にそのステージ番号で呼ぶ
        OnStageCleared(latestUnlockedStage-1, clearScore);

        //【Test】ステージ開放時の処理
        // 本来はステージ開放時に次のステージ番号で呼ぶ
        OnStageUnlocked(latestUnlockedStage);

        //最新ステージ情報の更新
        UpdateLatestUnlockedStage(latestUnlockedStage);

    }

    public void Save()
    {
        //【TODO】進捗の保存処理をかく
    }

    public void Load()
    {
        //【TODO】進捗の読み込み処理
    }

    //ステージクリア時に呼ぶメソッド
    public void OnStageCleared(int stageId, int score)
    {
        bool cleared = ProgressData.OnClearStage(stageId, score);
        if (cleared)
        {
            JustClearedStageId = stageId;//必要ないかもしれない
            Debug.Log($"ステージ{stageId}がクリアされました。スコア: {score}");//【デバッグ用】
        }
    }

    //ステージ開放時に呼ぶメソッド
    public void OnStageUnlocked(int stageId)
    {
        bool unlock = ProgressData.OnUnlockStage(stageId);
        if (unlock)
        {
            JustUnlockedStageId = stageId;
            Debug.Log($"ステージ{stageId}が開放されました。");//【デバッグ用】
        }
    }

    //最新ステージ更新の際に呼ぶメソッド
    public void UpdateLatestUnlockedStage(int stageId)
    {
        if(ProgressData.stages[stageId-1].isCleared!= true)
        {
            Debug.LogError($"ステージ{stageId-1}がクリアされていないため、ステージ{stageId}を開放できません。");
            return;
        }

        ProgressData.latestUnlockedStage = stageId;
    }

    //ステージのスコアを更新するためのメソッド
    public void UpdateStageScore(int stageId, int score)
    {
        ProgressData.UpdateScore(stageId, score);
    }

    //ステージ選択画面で開放後にフラグ解除するメソッド
    public void ClearStageSelectReturnFlags()
    {
        JustClearedStageId = null;
        JustUnlockedStageId = null;
    }

    //ステージIDに対応するステージ進捗データを返すメソッド
    public StageProgressState GetStageProgress(int stageId)
    {
        var stage = ProgressData.GetStage(stageId);
        if(stage == null) return null;
        return stage;
    }
}

//進捗データクラス
[System.Serializable]
public class GameProgressState
{
    public int latestUnlockedStage;
    public List<StageProgressState> stages;
    // ステージクリア時にデータをチェック&更新するメソッド
    public bool OnClearStage(int stageId, int score)
    {
        var stage = GetStage(stageId);
        
        if (stage ==null) return false;
        if(!stage.isUnlocked) return false;//解放されているかのチェック

        stage.Clear(score);
        return true;
    }

    public bool OnUnlockStage(int stageId)
    {
        var stage = GetStage(stageId);
        var preStage = GetStage(stageId - 1);

        if (stage == null) return false;
        if(preStage==null || !preStage.isCleared) return false;//【NEXT】前提条件のチェック 条件を変える場合はここを修正

        stage.Unlock();
        return true;
    }

    public void UpdateScore(int stageId, int score)
    {
        var stage = GetStage(stageId);
        if (stage == null) return;
        if (!stage.isCleared) return;//クリアしていないステージのスコアは更新しない

        stage.score = score;
    }

    public StageProgressState GetStage(int stageId)
    {
        foreach (var stage in stages)
        {
            if (stage.stageId == stageId)
            {
                return stage;
            }
        }
        return null;
    }
}

//各ステージごとの進捗データクラス
//【Check】ステージごとに必要なデータがあればここに追加する、ステージデータを外部から参照する際はこのクラスを経由してアクセスする想定
[System.Serializable]
public class StageProgressState
{
    public int stageId;
    public bool isUnlocked;
    public bool isCleared;
    public int score;

    //初期化用コンストラクタ
    public StageProgressState(int stageId)
    {
        this.stageId = stageId;
        this.isUnlocked = false;
        this.isCleared = false;
        this.score = 0;
    }

    //ステージクリア時に呼ぶメソッド
    public void Clear(int score)
    {
        isCleared = true;
        this.score = score;
    }

    public void Unlock()
    {
        isUnlocked = true;
    }
}
