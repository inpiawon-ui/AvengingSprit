using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Shop;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 작은 화면들 — 우편함(서버가 없어 빈 화면 · 시안 mail-empty) · 오늘 무료(시안 daily-free) · 설정(시안 settings) · 언어 선택(시안 language).
    /// 우편은 서버가 붙으면 목록을 넣는다(PD 2026-10-23 「화면만 · 빈 상태」). 설정의 계정 · 고객센터 · 약관은 준비 중.
    /// </summary>
    public sealed class RbMiscPages : MonoBehaviour
    {
        [SerializeField] private Sprite _toggleOn, _toggleOff;

        private static readonly Language[] Langs =
            { Language.Korean, Language.English, Language.Japanese, Language.ChineseTraditional, Language.ChineseSimplified };

        private UIBinder _ui;
        private UIBinder _page;
        private RbLobbyUI _shell;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _page = new UIBinder(_shell.transform);   // 우편함은 덮개 층 밖(쪽 층)에 있다
            _page.OnClick("MailBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("DailyClose", () => Close("RbDaily"));
            _ui.OnClick("DailyGoldClaim", ClaimGold);
            _ui.OnClick("DailySummonGo", () => { Close("RbDaily"); _shell.SelectTab("Summon"); });
            _ui.OnClick("SettingsClose", () => Close("RbSettings"));
            _ui.OnClick("SetMusic", () => { GameSound.MusicOn = !GameSound.MusicOn; FillSettings(); });
            _ui.OnClick("SetSfx", () => { GameSound.SfxOn = !GameSound.SfxOn; FillSettings(); });
            _ui.OnClick("SetVibrate", () => { GameSound.VibrateOn = !GameSound.VibrateOn; FillSettings(); });
            _ui.OnClick("SetLanguage", () => { _ui.SetActive("RbLanguage", true); FillLanguage(); });
            foreach (var n in new[] { "SetAccount", "SetSupport", "SetTerms" })
                _ui.OnClick(n, () => Toast(Localize.FromTable("ui.rb.soon", "준비 중이에요 · 업데이트로 열려요")));
            _ui.OnClick("LanguageClose", () => _ui.SetActive("RbLanguage", false));
            _ui.OnClick("ProfileClose", () => Close("RbProfile"));
            _ui.OnClick("ProfTabFace", () => { _profileType = "face"; FillProfile(); });
            _ui.OnClick("ProfTabTitle", () => { _profileType = "title"; FillProfile(); });
            _ui.OnClick("ProfGrow", () => { Close("RbProfile"); _shell.OpenRail("Ghost"); });
            for (int i = 0; i < Langs.Length; i++)
            {
                var l = Langs[i];
                _ui.OnClick($"Lang{i}", () => SetLanguage(l));
            }
        }

        public void ShowMail() { Hide(); _page.SetActive("RbMail", true); }

        // ── 프로필 — 대표 얼굴 · 칭호(꾸미기 전용) · 유령 성장 가기 ─────────────
        // 테두리 꾸미기는 옛 테두리 그림뿐이라 RE:BORN 화풍 그림이 오면 탭을 더한다

        private const int ProfileCells = 30;
        private string _profileType = "face";
        private readonly System.Collections.Generic.List<string> _profileKeys = new();
        private UnityEngine.U2D.SpriteAtlas _faces;
        private Sprite _ghostFace;

        public void ShowProfile(UnityEngine.U2D.SpriteAtlas faces)
        {
            _faces = faces;
            _ui.SetActive("RbProfile", true);
            FillProfile();
        }

        private void FillProfile()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady || !CoreModule.TryGet<IPlayerDataService>(out var p)) return;
            string face = s.Equipped("face");
            if (string.IsNullOrEmpty(face)) face = Game.Character.HostEntry.GhostKey;
            if (_ui.Get<Image>("ProfFace") is Image fi)
            {
                if (_ghostFace == null) _ghostFace = fi.sprite;   // 유령은 얼굴 아틀라스에 없다 — 프리팹의 유령 그림을 쓴다
                fi.sprite = FaceOf(face);
                fi.enabled = fi.sprite != null;
            }
            var host = p.GetHost(face);
            _ui.SetText("ProfName", host != null ? host.DisplayName : Localize.Get("host.ghost.name"));
            _ui.SetText("ProfLevel", $"Lv {p.GhostLevel}");
            string title = s.Equipped("title");
            _ui.SetText("ProfTitleName", string.IsNullOrEmpty(title) ? "—" : Localize.Get($"profile.{title}.name"));
            _ui.SetActive("ProfTabFaceOn", _profileType == "face");
            _ui.SetActive("ProfTabTitleOn", _profileType == "title");

            _profileKeys.Clear();
            if (_profileType == "face")
            {
                _profileKeys.Add(Game.Character.HostEntry.GhostKey);
                foreach (var e in p.PlayableHosts)
                    if (e != null && !e.IsGhost && !PlayerDataService.IsHiddenHost(e.HostKey) && p.IsHostOwned(e)) _profileKeys.Add(e.HostKey);
            }
            else foreach (var it in s.ProfileItems) if (it.Type == "title") _profileKeys.Add(it.Key);
            string equipped = _profileType == "face" ? face : title;
            for (int i = 0; i < ProfileCells; i++)
            {
                var cell = _ui.Find($"ProfCell{i}");
                if (cell == null) continue;
                bool on = i < _profileKeys.Count;
                cell.gameObject.SetActive(on);
                if (!on) continue;
                string key = _profileKeys[i];
                bool owned = _profileType == "face" || s.Owns(key);
                var img = _ui.Find(cell, $"ProfCell{i}Face")?.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = _profileType == "face" ? FaceOf(key) : null;
                    img.enabled = img.sprite != null;
                }
                if (_ui.Find(cell, $"ProfCell{i}Text")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t)
                    t.text = _profileType == "title" ? Localize.Get($"profile.{key}.name") : string.Empty;
                _ui.Find(cell, $"ProfCell{i}Check")?.gameObject.SetActive(key == equipped);
                _ui.Find(cell, $"ProfCell{i}Lock")?.gameObject.SetActive(!owned);
                var b = cell.GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.interactable = owned;
                string k = key;
                b.onClick.AddListener(() => { if (s.TryEquip(_profileType, k)) GameSound.Cue("ui.select"); FillProfile(); _shell.RefreshHeader(); });
            }
        }

        private Sprite FaceOf(string key)
        {
            var sprite = _faces != null ? _faces.GetSprite($"face_{key}") : null;
            return sprite != null ? sprite : _ghostFace;
        }

        public void ShowDaily() { _ui.SetActive("RbDaily", true); FillDaily(); }

        public void ShowSettings() { _ui.SetActive("RbSettings", true); _ui.SetActive("RbLanguage", false); FillSettings(); }

        public void Hide()
        {
            foreach (var n in new[] { "RbDaily", "RbSettings", "RbLanguage", "RbProfile" }) _ui.SetActive(n, false);
            _page?.SetActive("RbMail", false);
        }

        /// <summary>뒤로가기 — 맨 위 창 하나를 닫는다. 닫을 것이 없으면 false.</summary>
        public bool Back()
        {
            foreach (var n in new[] { "RbLanguage", "RbProfile", "RbSettings", "RbDaily", "RbMail" })
                if (_ui.Find(n) is Transform t && t.gameObject.activeSelf)
                {
                    if (n == "RbMail") _shell.SelectTab("Battle"); else Close(n);
                    return true;
                }
            return false;
        }

        private void Close(string n)
        {
            GameSound.Cue("ui.tap");
            _ui.SetActive(n, false);
        }

        // ── 오늘 무료 — 오늘의 상점 무료 골드 · 오늘 무료 소환 ─────────────

        private void FillDaily()
        {
            int slot = FreeGoldSlot(out var s);
            bool gold = slot >= 0 && !s.Bought;
            _ui.SetText("DailyGoldName", Localize.FromTable("ui.rb.daily.gold", "무료 골드"));
            _ui.SetText("DailyGoldSub", slot >= 0 ? Localize.Format("ui.store.price_gold", s.Count.ToString("N0")) : Localize.FromTable("ui.rb.daily.gold_sub", "오늘의 상점"));
            if (_ui.Get<Button>("DailyGoldClaim") is Button gb) gb.interactable = gold;
            _ui.SetText("DailyGoldClaimText", gold ? Localize.FromTable("ui.pass.claim", "받기") : Localize.FromTable("ui.pass.claimed", "받음"));
            bool summon = CoreModule.TryGet<IGachaService>(out var g) && g.IsReady && g.FreeAvailable;
            _ui.SetText("DailySummonName", Localize.FromTable("ui.rb.summon.free", "오늘 무료 1회"));
            _ui.SetText("DailySummonGoText", summon ? Localize.FromTable("ui.rb.daily.go_summon", "소환하러 가기 ›") : Localize.FromTable("ui.pass.claimed", "받음"));
            if (_ui.Get<Button>("DailySummonGo") is Button sb) sb.interactable = summon;
            if (CoreModule.TryGet<IDailyShopService>(out var d))
                _ui.SetText("DailyReset", Localize.Format("ui.shop.daily.reset", $"{(int)d.UntilReset.TotalHours:00}:{d.UntilReset.Minutes:00}:{d.UntilReset.Seconds:00}"));
        }

        private static int FreeGoldSlot(out DailyShopSlot slot)
        {
            slot = default;
            if (!CoreModule.TryGet<IDailyShopService>(out var d)) return -1;
            for (int i = 0; i < d.SlotCount; i++)
            {
                var s = d.Get(i);
                if (s.Kind == DailyShopKind.FreeGold) { slot = s; return i; }
            }
            return -1;
        }

        private void ClaimGold()
        {
            int i = FreeGoldSlot(out _);
            if (i < 0 || !CoreModule.TryGet<IDailyShopService>(out var d)) return;
            if (!d.TryBuy(i, out var fail)) { Toast(Localize.Get(fail)); return; }
            GameSound.Cue("run.gold");
            if (CoreModule.TryGet<IPlayerDataService>(out var p)) p.SaveAsync().Forget();   // fire-and-forget: 받은 것은 바로 남긴다
            _shell.RefreshHeader();
            FillDaily();
        }

        // ── 설정 · 언어 ────────────────────────────────────────

        private void FillSettings()
        {
            Toggle("SetMusic", GameSound.MusicOn);
            Toggle("SetSfx", GameSound.SfxOn);
            Toggle("SetVibrate", GameSound.VibrateOn);
            var lang = CoreModule.TryGet<ILanguageService>(out var ls) ? ls.Current : Language.Korean;
            _ui.SetText("SetLanguageValue", NameOf(lang) + " ›");
        }

        private void Toggle(string node, bool on)
        {
            if (_ui.Get<Image>(node + "Track") is Image img) img.sprite = on ? _toggleOn : _toggleOff;
            if (_ui.Find(node + "Knob") is RectTransform k) k.anchoredPosition = new Vector2(on ? 22f : -22f, 0f);
        }

        private void FillLanguage()
        {
            var cur = CoreModule.TryGet<ILanguageService>(out var ls) ? ls.Current : Language.Korean;
            for (int i = 0; i < Langs.Length; i++)
                _ui.SetText($"Lang{i}Text", NameOf(Langs[i]) + (Langs[i] == cur ? " ✓" : string.Empty));
        }

        private void SetLanguage(Language l)
        {
            if (CoreModule.TryGet<ILanguageService>(out var ls)) ls.SetLanguage(l);
            GameSound.Cue("ui.tap");
            FillLanguage();
            FillSettings();
        }

        /// <summary>언어 이름은 그 언어로 적는다(고르는 사람이 읽을 수 있게).</summary>
        private static string NameOf(Language l) => l switch
        {
            Language.English => "English",
            Language.Japanese => "日本語",
            Language.ChineseTraditional => "繁體中文",
            Language.ChineseSimplified => "简体中文",
            _ => "한국어",
        };

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
