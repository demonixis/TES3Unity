using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Demonixis.ToolboxV2.UI
{
    [RequireComponent(typeof(Text))]
    public sealed class TypeWritterEffect : MonoBehaviour
    {
        private static readonly StringBuilder _stringBuilder = new StringBuilder();
        private Text _text;
        private int _size;
        private float _elaspedTime;
        private string _contentText = string.Empty;
        private bool _done = true;
        private int _index;

        [SerializeField] private float _cycleDuration = 0.05f;
        [SerializeField] private int _letterPerCycle = 1;
        [SerializeField] private bool _autoStart;
        [SerializeField] private bool _activeOnEnable;

        public bool AutoStart
        {
            get => _autoStart;
            set => _autoStart = value;
        }

        public bool IsActive => !_done;

        public float CycleDuration => _cycleDuration;

        public int LetterPerCycle => _letterPerCycle;

        public event Action Completed;

        private void OnEnable()
        {
            if (_activeOnEnable)
            {
                _autoStart = false;
                Begin();
            }
        }

        private void Start()
        {
            if (_autoStart)
            {
                Begin();
            }
        }

        private void Update()
        {
            if (!_done)
            {
                _elaspedTime += Time.deltaTime;
                UpdateText();
            }
        }

        public void Begin(string text = null)
        {
            if (_text == null)
            {
                _text = GetComponent<Text>();
            }

            _contentText = text == null ? _text.text : text;
            _size = _contentText.Length;
            _elaspedTime = _cycleDuration;
            _index = 0;
            _done = false;
            _stringBuilder.Length = 0;
            _text.text = string.Empty;
        }

        public void Stop()
        {
            _elaspedTime = _cycleDuration;
            _done = true;
        }

        private void UpdateText()
        {
            if (_elaspedTime >= _cycleDuration)
            {
                var limit = Mathf.Min(_index + _letterPerCycle, _size);

                for (int i = _index; i < limit; i++)
                {
                    _stringBuilder.Append(_contentText[i]);
                }

                _index = limit;
                _text.text = _stringBuilder.ToString();
                _elaspedTime = 0;

                if (_index >= _size)
                {
                    _done = true;

                    if (Completed != null)
                    {
                        Completed();
                    }
                }
            }
        }
    }
}