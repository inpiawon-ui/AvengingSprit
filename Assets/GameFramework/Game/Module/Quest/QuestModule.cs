using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Game.Common;
using GameFramework.Game.Events;
using System.Collections.Generic;
using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Module.Quest
{
    /// <summary>
    /// 퀘스트 진행값을 관리하는 모듈 구현체.
    /// 값 변경 시 IEventBus로 QuestChangedEvent를 발행한다.
    /// </summary>
    public class QuestModule : IQuestModule
    {
        // ─────────────────────────────────────────
        // 필드
        // ─────────────────────────────────────────
        private IEventBus _bus;
        private Dictionary<QuestType, long> _repository = new Dictionary<QuestType, long>();

        // ─────────────────────────────────────────
        // IQuestModule
        // ─────────────────────────────────────────

        /// <summary>
        /// 모든 QuestType을 0으로 초기화하고 IEventBus 참조를 취득한다.
        /// </summary>
        public void Initialize()
        {
            // Initialize() 시점에는 모든 모듈 Register()가 완료됨이 보장됨
            _bus = CoreModule.Get<IEventBus>();

            _repository.Clear();

            var questList = EnumUtil.GetEnumValues<QuestType>();
            for (var i = 0; i < questList.Count; i++)
            {
                SetQuest(questList[i], 0);
            }
        }

        /// <summary>
        /// 내부 데이터를 초기화하고 IEventBus 참조를 해제한다.
        /// </summary>
        public void Release()
        {
            _repository.Clear();
            _bus = null;
        }

        /// <summary>
        /// 지정 QuestType의 현재 진행값을 반환한다.
        /// 미등록 키는 0으로 등록 후 반환한다.
        /// </summary>
        public long GetQuest(QuestType inQuest)
        {
            var result = 0L;

            if (_repository.ContainsKey(inQuest))
            {
                result = _repository[inQuest];
            }
            else
            {
                SetQuest(inQuest, result);
            }

            return result;
        }

        /// <summary>
        /// 지정 QuestType의 값을 inValue로 설정하고 이벤트를 발행한다.
        /// </summary>
        public void SetQuest(QuestType inQuest, long inValue)
        {
            if (_repository.ContainsKey(inQuest))
            {
                _repository[inQuest] = inValue;
            }
            else
            {
                _repository.Add(inQuest, inValue);
            }

            // 초기화 이전(_bus == null)에는 이벤트 발행 생략
            if (_bus != null)
            {
                _bus.Publish(new QuestChangedEvent { QuestType = inQuest, NewValue = inValue });
            }
        }

        /// <summary>
        /// 지정 QuestType의 값에 inValue를 더하고 이벤트를 발행한다.
        /// </summary>
        public void AddQuest(QuestType inQuest, long inValue)
        {
            if (_repository.ContainsKey(inQuest))
            {
                _repository[inQuest] += inValue;
            }
            else
            {
                _repository.Add(inQuest, inValue);
            }

            // 초기화 이전(_bus == null)에는 이벤트 발행 생략
            if (_bus != null)
            {
                _bus.Publish(new QuestChangedEvent { QuestType = inQuest, NewValue = _repository[inQuest] });
            }
        }
    }
}
