using GameFramework.Core.Common;

namespace Game.Module.Events
{
    /// <summary>타이틀에서 전면 탭이 눌렸다. 로비로 전환하는 신호.</summary>
    public struct TitleStartRequestedEvent : IEvent
    {
    }

    /// <summary>호스트 선택 화면을 열어달라. 진입 경로 2종을 모드로 구분한다.</summary>
    public struct HostSelectRequestedEvent : IEvent
    {
        /// <summary>true = CHAPTER/CONTINUE(스테이지 진입) · false = HOST(조회·강화)</summary>
        public bool IsChapterStart;
    }

    /// <summary>
    /// 챕터 선택을 다시 열어달라. 호스트 선택에서 뒤로 가면 챕터 선택으로 돌아간다
    /// (PLAY → 챕터 선택 → 호스트 선택 순서라 한 칸 앞이 챕터 선택이다).
    /// </summary>
    public struct ChapterSelectRequestedEvent : IEvent
    {
    }

    /// <summary>빙의 시작. 선택 호스트로 인게임에 진입한다.</summary>
    public struct PossessStartRequestedEvent : IEvent
    {
        public string HostKeyToPossess;
    }

    /// <summary>1차 범위 밖 기능이 눌렸다. 준비중 안내를 띄우는 용도.</summary>
    public struct NotImplementedFeatureEvent : IEvent
    {
        public string FeatureLabel;
    }
}
