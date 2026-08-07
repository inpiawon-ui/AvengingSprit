using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Core.Module.Loading.Views
{
    /// <summary>진행률 바 + 단계 메시지를 모두 표시하는 뷰.</summary>
    public class FullLoadingView : MonoBehaviour, ILoadingView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider     _slider;
        [SerializeField] private Text       _messageText;

        /// <summary>런타임에서 기본 뷰를 코드로 생성할 때 사용.</summary>
        public static FullLoadingView CreateDefault()
        {
            var go = new GameObject("[LoadingView]");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<FullLoadingView>();
        }

        public virtual UniTask ShowAsync()
        {
            (_root != null ? _root : gameObject).SetActive(true);
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync()
        {
            (_root != null ? _root : gameObject).SetActive(false);
            return UniTask.CompletedTask;
        }

        public virtual void UpdateProgress(float value)
        {
            if (_slider != null) _slider.value = value;
        }

        public virtual void UpdateMessage(string message)
        {
            if (_messageText != null) _messageText.text = message;
        }
    }
}
