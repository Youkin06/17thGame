using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class CheckButton : MonoBehaviour
{
    private Button button;
    private GameObject check;

    private Action<bool> pushButtonWork;

    // Start is called before the first frame update
    void Awake()
    {
        button = GetComponent<Button>();
        if(check == null)
        {
            var checkMark = transform.Find("Check");
            check = checkMark.gameObject;
        }

        button.onClick.AddListener(SwichCheck);
    }

    //初期化関数（初期値とボタンを押したときの処理設定）
    public void Setup(bool initialBool, Action<bool> action)
    {
        check.SetActive(initialBool);
        pushButtonWork = action;
    }

    void SwichCheck()
    {
        bool onCheck = check.activeSelf;
        check.SetActive(!onCheck);
        //保存しておいた処理を呼び出し
        pushButtonWork?.Invoke(!onCheck);
    }
}
