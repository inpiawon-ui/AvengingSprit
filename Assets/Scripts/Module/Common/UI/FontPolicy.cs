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
    /// 픽셀 폰트는 원작 시트에서 구운 `OriginalPixel SDF` 다. ASCII 만 있고 한글이
    /// 없어서, 한글은 이 폰트의 대체(fallback)로 걸린 고딕이 받는다.
    /// </summary>
    public static class FontPolicy
    {
        /// <summary>
        /// 영문·숫자만 담는 요소. 픽셀 폰트 대상이다.
        ///
        /// ⚠️ 이 목록은 **프리팹의 현재 상태와 같아야 한다.** 두 곳이 갈라지면
        ///    `Tools/Game/Apply Fonts To UI Prefabs` 를 한 번 돌리는 것만으로
        ///    화면이 조용히 바뀐다. 실제로 한 번 겪었다 — 이 목록이 없는 폰트를
        ///    가리키고 있어서 도구를 돌리면 전부 고딕으로 되돌아갔다.
        ///
        /// 한글이 섞이는 요소는 넣지 않는다(챕터 이름·보스 한글명·"능력치" 등).
        /// 대체 폰트로 떨어지긴 하지만 한 낱말 안에서 서체가 갈려 보기 나쁘다.
        /// </summary>
        private static readonly HashSet<string> PixelElements = new()
        {
            // 재화·수치
            "StaminaText", "GoldText", "GemText", "GhostExpText", "GhostLevelText",
            "ProgressText", "StatValueText", "ContinueCostText", "OwnedHostCountText",
            "BattlePassExpText", "DailyLoginDayText", "EventTimerText",
            // 영문 라벨
            "GhostLabelText", "ChapterNumberText",
            "BossLabel", "ProgressLabel", "StatLabelText",
            "UltimateLabel", "HostNameEnText",
            "HostListTitleText", "TapToStartText", "VersionText", "CopyrightText",
            "SubtitleText",
            "MissionTabLabel", "AchievementTabLabel", "RankingTabLabel",
            "InventoryTabLabel", "FriendsTabLabel",
            "HostButtonTitleText", "ChapterButtonTitleText", "ShopButtonTitleText",
            "BattlePassTitleText", "BattlePassSeasonText", "EventTitleText", "DailyLoginTitleText",
            // 인게임 HUD — 전부 영문 라벨과 숫자다
            "GhostLabel", "GhostHpText", "LevelText",
            "HostLabel", "HostHpText", "MaintainText",
            "StageText", "BossHpText", "BuffTitleText",
            "PossessCooldownText", "PossessCostText",
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
            //
            // 볼드는 픽셀 대상만이 아니라 **전부**에 건다. 인게임 HUD 를 실기에서
            // 보니 한글 본문도 가늘어 읽히지 않았다. 자간은 아케이드 라벨 느낌이
            // 필요한 픽셀 대상에만 준다 — 한글은 자간을 벌리면 오히려 흩어진다.
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = role == FontRole.Pixel ? 1.5f : 0f;
        }
    }
}
