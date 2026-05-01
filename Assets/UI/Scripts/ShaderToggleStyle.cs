using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShaderToggleStyle", menuName = "UI/Shader Toggle Style")]
public class ShaderToggleStyle : ScriptableObject
{
    [Serializable]
    public class FloatPropertySetting
    {
        [Tooltip("シェーダーのプロパティ名。例: _Progress")]
        public string propertyName = "_Progress";

        [Tooltip("OFF状態の値")]
        public float offValue = 0f;

        [Tooltip("ON状態の値")]
        public float onValue = 1f;
    }

    [Header("アニメーション時間")]
    [Min(0f)]
    public float duration = 0.15f;

    [Header("対象プロパティ")]
    public List<FloatPropertySetting> floatProperties = new();
}
