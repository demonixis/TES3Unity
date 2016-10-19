﻿using System;
using TESUnity.ESM;
using UnityEngine;
using UnityEngine.UI;

namespace TESUnity.UI
{
    public class UIScroll : MonoBehaviour
    {
        private BOOKRecord _bookRecord;

        [SerializeField]
        private GameObject _container = null;
        [SerializeField]
        private Image _background = null;
        [SerializeField]
        private Text _content = null;

        public event Action<BOOKRecord> OnTake = null;
        public event Action<BOOKRecord> OnClosed = null;

        void Start()
        {
            var texture = TESUnity.instance.TextureManager.LoadTexture("scroll", true);
            _background.sprite = GUIUtils.CreateSprite(texture);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (_bookRecord == null)
                Close();
        }

        void Update()
        {
            if (Input.GetButtonDown("Use"))
                Take();
            else if (Input.GetButton("Menu"))
                Close();
        }

        public void Show(BOOKRecord book)
        {
            _bookRecord = book;

            var words = _bookRecord.TEXT.value;
            words = words.Replace("\r\n", "");
            words = words.Replace("<BR><BR>", "");
            words = words.Replace("<BR>", "\n");
            words = System.Text.RegularExpressions.Regex.Replace(words, @"<[^>]*>", string.Empty);

            _content.text = words;

            gameObject.SetActive(true);
        }

        public void Take()
        {
            if (OnTake != null)
                OnTake(_bookRecord);

            Close();
        }

        public void Close()
        {
            _container.SetActive(false);

            if (OnClosed != null)
                OnClosed(_bookRecord);
        }

        public static UIScroll Create(Transform parent)
        {
            var uiScrollAsset = Resources.Load<GameObject>("UI/Scroll");
            var uiScrollGO = (GameObject)GameObject.Instantiate(uiScrollAsset, parent);
            return uiScrollGO.GetComponent<UIScroll>();
        }
    }
}
