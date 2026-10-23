using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// RE:BORN UI 리뉴얼(2026-10-23) 스위치 — 2026-10-24 부터 **기본은 새 화면**이다(빌드도).
    /// 옛 화면은 PD 확인 전까지 남겨 두고, 메뉴 `Tools/Game/테스트 — 새 UI(RE:BORN)로 보기` 를 끄면 옛 화면으로 돌아간다.
    /// PD 가 새 화면을 확정하면 이 스위치와 옛 주소 · 옛 프리팹을 걷는다(Assets 밖 보관).
    /// </summary>
    public static class RebornUiSwitch
    {
        private const string Key = "dev.reborn_ui";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Key, 1) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>옛 화면 주소 → 새 화면 주소. 새 화면이 아직 없는 씬은 그대로 둔다.</summary>
        public static string Resolve(string address)
        {
            if (!Enabled) return address;
            return address switch
            {
                "UI/Title/TitleMainUI" => "UI/Title/RbTitleUI",
                "UI/Lobby/LobbyMainUI" => "UI/Lobby/RbLobbyUI",
                "UI/InGame/InGameMainUI" => "UI/InGame/RbInGameMainUI",
                _ => address,
            };
        }
    }
}
