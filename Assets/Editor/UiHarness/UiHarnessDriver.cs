using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary>
    /// 상태를 만드는 손 — 사람 손과 같은 경로(이벤트 시스템 레이캐스트 → 포인터 다운·업·클릭)로 누른다.
    /// 그 자리를 다른 부품이 가리고 있으면 누르지 않고 실패한다(«보이는데 눌리지 않는» 사고를 같이 잡는다).
    /// </summary>
    public sealed class UiHarnessDriver
    {
        public float SettleSeconds = 1f;

        public async UniTask Click(string name, float settle = -1f)
        {
            var target = Require(name);
            var hit = HitAt(target, out var data);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, data, ExecuteEvents.pointerClickHandler);
            await Seconds(settle < 0f ? SettleSeconds : settle);
        }

        public void PressDown(string name)
        {
            var target = Require(name);
            var hit = HitAt(target, out var data);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerDownHandler);
        }

        public void PressUp(string name)
        {
            var target = Require(name);
            var hit = HitAt(target, out var data);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerExitHandler);
        }

        public static UniTask Seconds(float seconds) => UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true);

        public static async UniTask Frames(int count)
        {
            for (int i = 0; i < count; i++) await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        }

        /// <summary> 대상 가운데를 쏴서 맞은 «누를 수 있는» 부품을 돌려준다 — 대상(또는 그 자손)이 아니면 실패. </summary>
        private static GameObject HitAt(GameObject target, out PointerEventData data)
        {
            var system = EventSystem.current;
            if (system == null) throw new InvalidOperationException("EventSystem 이 없다");

            data = new PointerEventData(system)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position),
            };
            var hits = new List<RaycastResult>();
            system.RaycastAll(data, hits);
            if (hits.Count == 0) throw new InvalidOperationException(target.name + " 자리에 누를 수 있는 것이 없다");

            var top = hits[0].gameObject;
            if (top != target && !top.transform.IsChildOf(target.transform))
                throw new InvalidOperationException($"{target.name} 을 {top.name} 이(가) 가리고 있다");

            data.pointerCurrentRaycast = hits[0];
            data.pointerPressRaycast = hits[0];
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            data.pointerPress = handler;
            return handler != null ? handler : top;
        }

        private static GameObject Require(string name)
        {
            var target = GameObject.Find(name);
            if (target == null) throw new InvalidOperationException(name + " 이 화면에 없다");
            return target;
        }
    }
}
