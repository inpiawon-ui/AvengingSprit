using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Core.Module.UI
{
    /// <summary>모든 UI 패널의 베이스 클래스. Addressables 키 + 레이어로 관리된다.</summary>
    public abstract class UIPanel : MonoBehaviour
    {
        public UILayer Layer { get; internal set; } = UILayer.Default;

        /// <summary>패널이 열릴 때 호출 (애니메이션 등 비동기 처리 가능)</summary>
        public virtual UniTask OnOpenAsync()  => UniTask.CompletedTask;

        /// <summary>패널이 닫힐 때 호출</summary>
        public virtual UniTask OnCloseAsync() => UniTask.CompletedTask;

        /// <summary>위에 있던 패널이 닫혀 이 패널이 포커스를 받을 때</summary>
        public virtual void OnFocus() { }

        /// <summary>이 패널 위에 새 패널이 열릴 때</summary>
        public virtual void OnBlur()  { }
    }
}
