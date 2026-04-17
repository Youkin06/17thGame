using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [SerializeField] public GameProgressData ProgressData;
    public int? JustClearedStageId { get; private set; }
    public int? JustUnlockedStageId { get; private set; }

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

        ProgressData = new GameProgressData
        {
            latestUnlockedStage = 0,
            stages = new List<StageProgressData>()
        };

        TestSetData();//【テスト用】進捗データセットするメソッド
    }

    //【テスト用】進捗データをセットするメソッド
    public void TestSetData()
    {
        ProgressData.latestUnlockedStage = 3;
        ProgressData.stages.Add(new StageProgressData(0) { isUnlocked = true, isCleared = true, score = 1000 });
        ProgressData.stages.Add(new StageProgressData(1) { isUnlocked = true, isCleared = true, score = 2000 });
        ProgressData.stages.Add(new StageProgressData(2) { isUnlocked = true, isCleared = false, score = 0 });
        OnStageCleared(2, 1500);
        OnStageUnlocked(3);
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
        JustClearedStageId = stageId;
        ProgressData.stages[stageId].ClearStage(score);
    }

    //ステージ開放時に呼ぶメソッド
    public void OnStageUnlocked(int stageId)
    {
        JustUnlockedStageId = stageId;
    }

    //ステージ選択画面で開放後にフラグ解除するメソッド
    public void ClearStageSelectReturnFlags()
    {
        JustClearedStageId = null;
        JustUnlockedStageId = null;
    }

    //ステージIDに対応する進捗データを返すメソッド
    public StageProgressData GetStageProgress(int stageId)
    {
        foreach (var stage in ProgressData.stages)
        {
            if (stage.stageId == stageId)
            {
                return stage;
            }
        }
        return null;
    }
}

[System.Serializable]
public class GameProgressData
{
    public int latestUnlockedStage;
    public List<StageProgressData> stages;
}

[System.Serializable]
public class StageProgressData
{
    public int stageId;
    public bool isUnlocked;
    public bool isCleared;
    public int score;

    //初期化用コンストラクタ
    public StageProgressData(int stageId)
    {
        this.stageId = stageId;
        this.isUnlocked = false;
        this.isCleared = false;
        this.score = 0; 
    }

    //ステージクリア時に呼ぶメソッド
    public void ClearStage(int score)
    {
        isCleared = true;
        this.score = score;
    }
}
