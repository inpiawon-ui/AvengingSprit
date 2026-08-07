using System.Collections.Generic;
using TMPro;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 폰트 역할. 확정 사항 9 — 하이브리드.
    ///   Pixel  : 영문·숫자·타이틀. 1991 아케이드 감성 담당
    ///   Gothic : 한글 본문. 가독성 담당
    /// </summary>
    public enum FontRole
    {
        Pixel,
        Gothic,
    }

    /// <summary>
    /// 요소 이름 → 폰트 **페이스** 매핑.
    ///
    /// ⚠️ 크기·정렬·색·줄바꿈은 여기서 다루지 않는다. 그 값들은 목업 실측 레이아웃
    ///    (`UISpec/_layout_{화면}.json`, 생성기 `Projects/AVSR/_layout_{화면}.py`)이
    ///    단일 출처다. 두 곳에서 같은 속성을 쓰면 실행 순서에 따라 결과가 뒤집힌다.
    ///
    /// ⚠️ 픽셀 폰트 파일이 아직 없다. 확보 전까지 두 역할 모두 고딕으로 떨어진다.
    /// </summary>
    public static class FontPolicy
    {
        /// <summary>영문·숫자만 담는 요소. 픽셀 폰트 대상이다.</summary>
        private static readonly HashSet<string> PixelElements = new()
        {
            // 재화·수치
            "StaminaText", "GoldText", "GemText", "GhostExpText", "GhostLevelText",
            "ProgressText", "StatValueText", "ContinueCostText", "OwnedHostCountText",
            "BattlePassExpText", "DailyLoginDayText", "EventTimerText",
            // 영문 라벨
            "GhostLabelText", "ChapterNumberText", "ChapterNameText",
            "BossLabel", "BossNameText", "ProgressLabel", "StatGroupLabel", "StatLabelText",
            "UltimateLabel", "UltimateNameText", "HostNameEnText",
            "HostListTitleText", "TapToStartText", "VersionText",
            "ContinueButtonText",
            "MissionTabLabel", "AchievementTabLabel", "RankingTabLabel",
            "InventoryTabLabel", "FriendsTabLabel",
            "HostButtonTitleText", "ChapterButtonTitleText", "ShopButtonTitleText",
            "BattlePassTitleText", "BattlePassSeasonText", "EventTitleText", "DailyLoginTitleText",
        };

        public static FontRole RoleOf(string elementName)
            => PixelElements.Contains(elementName) ? FontRole.Pixel : FontRole.Gothic;

        /// <summary>
        /// 한 요소에 폰트 페이스를 적용한다.
        /// 픽셀 폰트가 없으면 고딕으로 대체하되, 픽셀 대상은 자간을 조금 벌려
        /// 아케이드 라벨 느낌을 살린다(임시 보정).
        /// </summary>
        public static void Apply(TextMeshProUGUI tmp, TMP_FontAsset pixel, TMP_FontAsset gothic)
        {
            if (tmp == null) return;
            var role = RoleOf(tmp.gameObject.name);

            if (role == FontRole.Pixel && pixel != null)
            {
                tmp.font = pixel;
                tmp.characterSpacing = 0f;
                tmp.fontStyle = FontStyles.Normal;
                return;
            }

            tmp.font = gothic;
            // 픽셀 폰트 미확보 임시 보정 — 목업 라벨은 굵은 아케이드체다.
            // Noto 는 가늘어 그대로 두면 화면이 흐리게 보인다. 볼드 + 자간으로 근사한다.
            tmp.fontStyle = role == FontRole.Pixel ? FontStyles.Bold : FontStyles.Normal;
            tmp.characterSpacing = role == FontRole.Pixel ? 1.5f : 0f;
        }
    }
}
