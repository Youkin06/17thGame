using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformCancel : MonoBehaviour
{
    [Header("打ち消し倍率")]
    [SerializeField] private float multiplier = 1f;

    [Header("基準位置")]
    [SerializeField] private bool useInitialParentPosition = true;

    private Transform _parentTransform;
    private Vector3 _initialLocalPosition;
    private Vector3 _initialParentPosition;

    private void Start()
    {
        _parentTransform = transform.parent;

        if (_parentTransform == null)
        {
            Debug.LogWarning("親オブジェクトがありません。", this);
            enabled = false;
            return;
        }

        _initialLocalPosition = transform.localPosition;
        _initialParentPosition = _parentTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 parentMove;

        if (useInitialParentPosition)
        {
            // 親が開始時からどれだけ動いたか
            parentMove = _parentTransform.position - _initialParentPosition;
        }
        else
        {
            // 親の現在位置そのものを使う
            parentMove = _parentTransform.position;
        }

        Vector3 cancelMove = new Vector3(
            -parentMove.x * multiplier,
            -parentMove.y * multiplier,
            0f
        );

        transform.localPosition = _initialLocalPosition + cancelMove;
    }
}
