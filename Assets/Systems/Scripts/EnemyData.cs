using UnityEngine;

public enum EnemyType {
    Normal,     // ただ歩くだけ

    Fast,       // 早く移動する
    Dasher,     // プレイヤーを見たら突進する
    Shooter     // 弾を撃つ
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("敵の種類")]
    public EnemyType enemyType; // ← ここで「種類」を選べるようにする

    [Header("基本設定")]
    public float moveSpeed;       

    [Header("乗っ取り時の設定")]
    public Color bodyColor;       
    public float hijackDuration;  
}