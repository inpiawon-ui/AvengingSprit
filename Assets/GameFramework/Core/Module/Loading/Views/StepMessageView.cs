using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Core.Module.Loading.Views
{
    /// <summary>단계 메시지만 표시. Inspector에서 _root / _messageText 연결.</summary>
    public class StepMessageView : MonoBehaviour, ILoadingView
    {
        [SerializeField] protected GameObject _root;
        [SerializeField] protected Text       _messageText;

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

        public virtual void UpdateProgress(float value) { }

        public virtual void UpdateMessage(string message)
        {
            if (_messageText != null) _messageText.text = message;
        }
    }
}
