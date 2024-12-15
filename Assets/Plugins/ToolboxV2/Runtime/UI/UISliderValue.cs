using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Demonixis.ToolboxV2.UI
{
    [ExecuteInEditMode]
    public sealed class UISliderValue : MonoBehaviour
    {
        [FormerlySerializedAs("_slider")] [SerializeField] private Slider slider;
        [FormerlySerializedAs("_label")] [SerializeField] private Text label;
        [FormerlySerializedAs("_unit")] [SerializeField] private string unit = string.Empty;
        [FormerlySerializedAs("_truncate")] [FormerlySerializedAs("m_Truncate")] [SerializeField] private int truncate;
        [FormerlySerializedAs("m_MinMax")] [SerializeField] private Vector2 minMax = Vector2.zero;

        public event Action<float> ValueChanged;

        public bool WholeNumbers
        {
            get => slider.wholeNumbers;
            set => slider.wholeNumbers = value;
        }

        public float Value
        {
            get => slider.value;
            set
            {
                slider.SetValueWithoutNotify(value);
                ShowValue(value);
            }
        }

        private void Awake()
        {
            slider.onValueChanged.AddListener(OnValueChanged);

            if (minMax != Vector2.zero)
            {
                SetMinMax(minMax.x, minMax.y);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            OnValidate();
#endif
        }

        public void SetMinMax(float min, float max)
        {
            slider.minValue = min;
            slider.maxValue = max;
        }

        private void OnValueChanged(float value)
        {
            ShowValue(value);
            ValueChanged?.Invoke(value);
        }

        private void ShowValue(float value)
        {
            if (truncate > 0)
            {
                value = Mathf.Round(value * truncate) / truncate;
            }

            label.text = $"{value}{unit}";
        }

        private void OnValidate()
        {
            if (label == null)
            {
                return;
            }

            label.text = $"{slider.value}{unit}";
        }
    }
}