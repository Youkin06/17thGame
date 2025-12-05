using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    public float speed;
    public DynamicJoystick dynamicJoystick;
    public Rigidbody2D rb;

    public void FixedUpdate()
    {
        // ジョイスティック入力値を取得
        Vector3 direction = GetJoystickInput(dynamicJoystick);

        // 入力がある場合のみ処理
        if (direction.sqrMagnitude > 0.01f) 
        {
            // 1. 移動処理
            rb.AddForce(direction * speed, ForceMode2D.Force);
            
            // 2. 回転処理
            // 第1引数: Z軸をどこに向けるか（画面の奥 = Vector3.forward）
            // 第2引数: Y軸（頭のてっぺん）をどこに向けるか（進行方向 = direction）
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);

            // 3. スムーズに回転
            float turnSpeed = 5f; 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    public Vector3 GetJoystickInput(DynamicJoystick dynamicJoystick)
    {
        Vector3 direction = Vector3.up * dynamicJoystick.Vertical + Vector3.right * dynamicJoystick.Horizontal;
        return direction.normalized; // 正規化して方向ベクトルに
    }
}