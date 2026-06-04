using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

[CreateAssetMenu(menuName = "TestProgressData", order = 1)]
public class TestProgressData : ScriptableObject
{
    [Header("総ステージ数")]
    [Header("(ここを更新したらstageDatasとstageStatusの内容も更新すること)")]
    public int totalStages = 5; //総ステージ数（例: 5）
    [Header("一番最新のステージ")]
    [Header("(ここを更新したらstageStatusの内容も更新すること)")]
    public int latestUnlockedStage = 2; //最新ステージID（例: 2
    [Header("存在するステージのデータ")]
    public StageData[] stageDatas = new StageData[5]; //ステージデータの配列
    [Header("ステージ進捗データ")]
    public StageStatus[] stageStatuses = new StageStatus[5]; //ステージ状態の配列

}

[System.Serializable]
public class StageData//【テスト用】本番では別のスクリプタブルオブジェクトなどから取得する想定
{
    public int id;
    public string name;
}

[System.Serializable]
public class StageStatus//【テスト用】本番ではGameProgressStateのstagesの内容をセットする想定
{
    public bool unlocked;
    public bool cleared;
    public int score;
}