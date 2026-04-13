using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CheckButton : MonoBehaviour
{
    [SerializeField] private GameObject check;
    [SerializeField] private ShaderToggleVisual shaderToggleVisual;

    private Button button;
    private Action<bool> pushButtonWork;
    private bool isOn;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (check == null)
        {
            Transform checkMark = transform.Find("Check");
            if (checkMark != null)
            {
                check = checkMark.gameObject;
            }
        }

        if (shaderToggleVisual == null)
        {
            shaderToggleVisual = GetComponent<ShaderToggleVisual>();
        }

        button.onClick.AddListener(SwitchCheck);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(SwitchCheck);
        }
    }

    /// <summary>
    /// ボタン押下時に実行する処理を登録する
    /// </summary>
    public void Setup(Action<bool> action)
    {
        pushButtonWork = action;
    }

    /// <summary>
    /// 状態を設定し、チェック表示とシェーダー見た目を同期する
    /// </summary>
    public void SetState(bool value, bool notify = false, bool instantVisual = false)
    {
        isOn = value;

        if (check != null)
        {
            check.SetActive(isOn);
        }

        shaderToggleVisual?.ApplyState(isOn, instantVisual);

        if (notify)
        {
            pushButtonWork?.Invoke(isOn);
        }
    }

    /// <summary>
    /// 現在の状態を返す
    /// </summary>
    public bool GetState()
    {
        return isOn;
    }

    private void SwitchCheck()
    {
        SetState(!isOn, notify: true, instantVisual: false);
    }
}
