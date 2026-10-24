using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// RE:BORN UI 리뉴얼(2026-10-23) 스위치 — 2026-10-24 부터 **기본은 새 화면**이다(빌드도).
    /// 타이틀 · 로비는 옛 프리팹을 Assets 밖(`Projects/AVSR/_archive/ui_old_1024/`)으로 옮겨 **언제나 새 화면**이다(PD 10-24 「정리해줘」).
    /// 인게임만 스위치를 따른다 — 새 인게임 프리팹이 옛 InGameMainUI.prefab 을 복사해 세우므로 옛 것이 아직 남아 있다.
    /// </summary>
    public static class RebornUiSwitch
    {
        private const string Key = "dev.reborn_ui";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Key, 1) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>옛 화면 주소 → 새 화면 주소. 씬은 아직 옛 주소 문자열을 들고 있다(SceneUILoader).</summary>
        public static string Resolve(string address)
        {
            switch (address)
            {
                case "UI/Title/TitleMainUI": return "UI/Title/RbTitleUI";      // 옛 프리팹 없음 — 스위치와 무관
                case "UI/Lobby/LobbyMainUI": return "UI/Lobby/RbLobbyUI";
            }
            if (!Enabled) return address;
            return address == "UI/InGame/InGameMainUI" ? "UI/InGame/RbInGameMainUI" : address;
        }
    }
}
