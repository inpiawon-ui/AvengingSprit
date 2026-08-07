using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Game.Common;
using GameFramework.Game.Events;
using System.Collections.Generic;
using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Module.Action
{
    /// <summary>
    /// 유저 행위(Action) 누적값을 관리하는 모듈 구현체.
    /// 값 변경 시 IEventBus로 ActionChangedEvent를 발행한다.
    /// </summary>
    public class ActionModule : IActionModule
    {
        // ─────────────────────────────────────────
        // 필드
        // ─────────────────────────────────────────
        private IEventBus _bus;
        private Dictionary<ActionType, long> _actions = new Dictionary<ActionType, long>();

        // ─────────────────────────────────────────
        // IActionModule
        // ─────────────────────────────────────────

        /// <summary>
        /// 모든 ActionType을 0으로 초기화하고 IEventBus 참조를 취득한다.
        /// </summary>
        public void Initialize()
        {
            // Initialize() 시점에는 모든 모듈 Register()가 완료됨이 보장됨
            _bus = CoreModule.Get<IEventBus>();

            _actions.Clear();

            var actionList = EnumUtil.GetEnumValues<ActionType>();
            for (var i = 0; i < actionList.Count; i++)
            {
                SetAction(actionList[i], 0);
            }
        }

        /// <summary>
        /// 내부 데이터를 초기화하고 IEventBus 참조를 해제한다.
        /// </summary>
        public void Release()
        {
            _actions.Clear();
            _bus = null;
        }

        /// <summary>
        /// 지정 ActionType의 현재 누적값을 반환한다.
        /// 미등록 키는 0으로 등록 후 반환한다.
        /// </summary>
        public long GetAction(ActionType inAction)
        {
            var result = 0L;

            if (_actions.ContainsKey(inAction))
            {
                result = _actions[inAction];
            }
            else
            {
                SetAction(inAction, result);
            }

            return result;
        }

        /// <summary>
        /// 지정 ActionType의 값을 inAccrue로 설정하고 이벤트를 발행한다.
        /// </summary>
        public void SetAction(ActionType inAction, long inAccrue)
        {
            if (_actions.ContainsKey(inAction))
            {
                _actions[inAction] = inAccrue;
            }
            else
            {
                _actions.Add(inAction, inAccrue);
            }

            // 초기화 이전(_bus == null)에는 이벤트 발행 생략
            if (_bus != null)
            {
                _bus.Publish(new ActionChangedEvent { ActionType = inAction, NewValue = inAccrue });
            }
        }

        /// <summary>
        /// 지정 ActionType의 값에 inAccrue를 더하고 이벤트를 발행한다.
        /// </summary>
        public void AddAction(ActionType inAction, long inAccrue)
        {
            if (_actions.ContainsKey(inAction))
            {
                _actions[inAction] += inAccrue;
            }
            else
            {
                _actions.Add(inAction, inAccrue);
            }

            // 초기화 이전(_bus == null)에는 이벤트 발행 생략
            if (_bus != null)
            {
                _bus.Publish(new ActionChangedEvent { ActionType = inAction, NewValue = _actions[inAction] });
            }
        }
    }
}
