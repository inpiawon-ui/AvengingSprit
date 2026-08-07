using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Core.Module.Loading.Views
{
    /// <summary>진행률 바만 표시. Inspector에서 _root / _slider 연결.</summary>
    public class ProgressBarView : MonoBehaviour, ILoadingView
    {
        [SerializeField] protected GameObject _root;
        [SerializeField] protected Slider     _slider;

        public virtual UniTask ShowAsync()
        {
            if (_root != null) _root.SetActive(true);
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync()
        {
            if (_root != null) _root.SetActive(false);
            return UniTask.CompletedTask;
        }

        public virtual void UpdateProgress(float value)
        {
            if (_slider != null) _slider.value = value;
        }

        public virtual void UpdateMessage(string message) { }
    }
}
