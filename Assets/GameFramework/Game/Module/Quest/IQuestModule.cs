using static GameFramework.Game.Common.Enums;

namespace GameFramework.Game.Module.Quest
{
    /// <summary>
    /// 퀘스트 진행값을 관리하는 모듈 인터페이스.
    /// </summary>
    public interface IQuestModule
    {
        void Initialize();
        void Release();

        long GetQuest(QuestType inQuest);
        void SetQuest(QuestType inQuest, long inValue);
        void AddQuest(QuestType inQuest, long inValue);
    }
}
