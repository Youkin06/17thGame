using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public GameObject Move;
    public GameObject Idle;
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolName = "IsMove";

    private bool isMove = false;

    void Start()
    {
        Move.SetActive(false);
        Idle.SetActive(false);
        if (animator == null)
            animator = GetComponent<Animator>();

        animator.SetBool(moveBoolName, isMove);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isMove = !isMove;
            animator.SetBool(moveBoolName, isMove);
        }
        if (isMove)
        {
            Move.SetActive(false); //なんか逆かもだけど気にしないで
            Idle.SetActive(true);
        }
        if (!isMove)
        {
            Move.SetActive(true);
            Idle.SetActive(false);
        }
    }
}
