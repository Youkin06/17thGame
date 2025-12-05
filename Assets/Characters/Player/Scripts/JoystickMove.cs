using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    public float speed;
    public FixedJoystick fixedJoystick;
    public Rigidbody2D rb;

    public void FixedUpdate()
    {
        Vector3 direction = Vector3.up * fixedJoystick.Vertical + Vector3.right * fixedJoystick.Horizontal;
        rb.AddForce(direction * speed, ForceMode2D.Force);
    }
}