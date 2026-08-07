using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    /// 요소 이름 → 폰트 역할 매핑. 화면 설계서의 요소명을 그대로 키로 쓴다.
    ///
    /// ⚠️ 픽셀 폰트 파일이 아직 없다. 확보 전까지 두 역할 모두 고딕으로 떨어진다
    ///    (`FontAssetProvider.Pixel` 이 null 이면 Gothic 으로 대체).
    ///    폰트를 넣으면 이 파일은 손대지 않아도 된다.
    /// </summary>
    public static class FontPolicy
    {
        /// <summary>영문·숫자만 담는 요소. 픽셀 폰트 대상이다.</summary>
        private static readonly HashSet<string> PixelElements = new()
        {
            // 재화·수치
            "StaminaText", "GoldText", "GemText", "GhostExpText", "GhostLevelText",
            "ProgressText", "StatValueText", "ContinueCostText", "OwnedHostCountText",
            // 영문 라벨
            "GhostLabelText", "ChapterNumberText", "ChapterNameText",
            "BossLabel", "ProgressLabel", "StatGroupLabel", "StatLabelText",
            "UltimateLabel", "UltimateNameText", "HostNameEnText",
            "HostListTitleText", "TapToStartText", "VersionText",
            "ContinueButtonText",
            "MissionTabLabel", "AchievementTabLabel", "RankingTabLabel",
            "InventoryTabLabel", "FriendsTabLabel",
            "HostButtonTitleText", "ChapterButtonTitleText", "ShopButtonTitleText",
        };

        public static FontRole RoleOf(string elementName)
            => PixelElements.Contains(elementName) ? FontRole.Pixel : FontRole.Gothic;

        /// <summary>
        /// 한 요소에 폰트를 적용한다.
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
                return;
            }

            tmp.font = gothic;
            // 픽셀 폰트 미확보 임시 보정 — 영문 라벨만 자간을 벌린다
            tmp.characterSpacing = role == FontRole.Pixel ? 4f : 0f;
        }
    }
}
