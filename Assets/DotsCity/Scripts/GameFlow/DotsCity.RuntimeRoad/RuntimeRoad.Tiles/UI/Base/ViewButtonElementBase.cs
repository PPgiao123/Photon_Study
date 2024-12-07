using Spirit604.Extensions;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class ViewButtonElementBase<T> : MonoBehaviour where T : ViewButtonElementBase<T>
    {
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        public Sprite Icon => icon.sprite;

        public event Action<T> OnClicked = delegate { };

        protected virtual void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClicked((T)this));
        }

        public void Init(Sprite spriteIcon)
        {
            icon.sprite = spriteIcon;
        }

        protected virtual void Reset()
        {
            icon = GetComponent<Image>();
            button = GetComponent<Button>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
