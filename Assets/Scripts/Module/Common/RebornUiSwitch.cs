using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// RE:BORN UI 리뉴얼(2026-10-23) 동안 새 화면을 미리 보는 스위치 — 다 바꾸기 전에는 새 화면에 없는 기능(상점 · 육성 …)이 사라지므로
    /// 기본은 꺼 둔다. 메뉴 `Tools/Game/테스트 — 새 UI(RE:BORN)로 보기` 로 켠다. 전부 바꾸면 이 스위치와 옛 주소를 지운다.
    /// </summary>
    public static class RebornUiSwitch
    {
        private const string Key = "dev.reborn_ui";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
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
