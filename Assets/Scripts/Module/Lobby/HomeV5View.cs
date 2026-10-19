using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 로비 홈 v5 — 혼합 컨셉 「픽셀 연대기 × 쇼케이스」(PD 2026-10-17, 시안 `mock_uiv5_home`).
    /// 진열 칸(유령 + 고른 호스트 · 등급 · 속성 · 떠 있는 PASS · 미션 · 업적) → 시나리오 카드 → 망령의 탑 · 망령의 밤 카드.
    /// 노드는 `LobbyHomeV5Builder` 가 세운다. 누르기는 `LobbyMainUI` 가 묶는다(창을 여는 쪽이 거기 있다).
    /// </summary>
    public sealed class HomeV5View : MonoBehaviour
    {
        [Serializable]
        public struct KeyedSprite
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private KeyedSprite[] _portraits = Array.Empty<KeyedSprite>();   // 호스트 키 → 진열 그림
        [SerializeField] private Sprite[] _chapterArt = Array.Empty<Sprite>();            // ch_art_1.. (챕터 표 Art 칸)
        [SerializeField] private Sprite[] _gradeBadges = Array.Empty<Sprite>();           // R · SR · SSR (HostGrade 순)
        [SerializeField] private Sprite _kindBlade, _kindForce, _kindMagic;
        [SerializeField] private Sprite _passIcon, _passIconBought;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();
        private readonly Dictionary<string, Sprite> _portraitMap = new();

        private void Awake()
        {
            _ui = new UIBinder(transform);
            for (int i = 0; i < _portraits.Length; i++)
                if (!string.IsNullOrEmpty(_portraits[i].Key)) _portraitMap[_portraits[i].Key] = _portraits[i].Sprite;
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<HostSelectedEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<SeasonChangedEvent>(_ => RefreshPass()));
            _tokens.Add(bus.Subscribe<ModeProgressChangedEvent>(_ => RefreshModes()));
            _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => Refresh()));
            Refresh();
            Layout();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        private void OnRectTransformDimensionsChange() => Layout();

        public void Refresh()
        {
            if (_ui == null) return;
            if (_player == null) CoreModule.TryGet(out _player);
            if (_player == null || !_player.IsReady) return;
            RefreshShowcase();
            RefreshPass();
            RefreshScenario();
            RefreshModes();
        }

        // ── 진열 칸 ──────────────────────────────────────────

        private void RefreshShowcase()
        {
            string key = _player.SelectedHostId;
            var e = string.IsNullOrEmpty(key) ? null : _player.GetHost(key);
            bool ghost = e == null || e.IsGhost;
            SetImage("HomeHost", ghost ? null : PortraitOf(key));
            ShowUnit("HomeHost", ghost ? null : key);

            var grade = _ui.Get<Image>("HomeGrade");
            if (grade != null)
            {
                int g = ghost ? -1 : (int)e.Grade;
                grade.sprite = g >= 0 && g < _gradeBadges.Length ? _gradeBadges[g] : null;
                grade.enabled = grade.sprite != null;
            }
            var kind = ghost ? InGame.Affinity.None : InGame.AffinityRule.KindOf(key);
            SetImage("HomeKind", kind switch
            {
                InGame.Affinity.Blade => _kindBlade,
                InGame.Affinity.Force => _kindForce,
                InGame.Affinity.Magic => _kindMagic,
                _ => null,
            });
        }

        private Sprite PortraitOf(string key) => _portraitMap.TryGetValue(key, out var s) ? s : null;

        /// <summary>안 샀으면 「PASS」 티켓 + 「구매」 꼬리표, 샀으면 왕관 티켓 + 「Lv N」. 받을 것이 있으면 빨간 점.</summary>
        private void RefreshPass()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady)
            {
                _ui.SetActive("HomePassButton", false);
                return;
            }
            _ui.SetActive("HomePassButton", true);
            SetImage("HomePassIcon", s.HasPremium ? _passIconBought : _passIcon);
            _ui.SetActive("HomePassTag", !s.HasPremium);
            _ui.SetActive("HomePassLv", s.HasPremium);
            _ui.SetText("HomePassTagText", Localize.Get("ui.home.pass_buy"));
            _ui.SetText("HomePassLv", $"Lv {s.Tier}");
            _ui.SetActive("HomePassDot", s.HasClaimable || s.HasClaimableMission);
        }

        // ── 시나리오 ─────────────────────────────────────────

        private void RefreshScenario()
        {
            int ch = Mathf.Clamp(_player.SelectedChapter, 1, Mathf.Max(1, _player.ChapterCount));
            var row = _player.ChapterInfo(ch);
            _ui.SetText("HomeScenarioTitle", Localize.Get("ui.home.scenario"));
            _ui.SetText("HomeChapterPillText", Localize.Format("ui.home.chapter", ch));
            _ui.SetText("HomeChapterName", Localize.FromTable($"stage.{ch}.1.name", row.Name ?? string.Empty));
            _ui.SetText("HomeContinueText", Localize.Get("ui.home.continue"));
            if (_chapterArt.Length > 0)
            {
                // 챕터가 그림보다 많으면(라이브 100챕터) 돌려 쓴다 — 빈 칸보다 낫다
                int art = (row.Art > 0 ? row.Art : ch) - 1;
                SetImage("HomeScenarioArt", _chapterArt[art % _chapterArt.Length]);
            }
        }

        // ── 탑 · 밤 ──────────────────────────────────────────

        private static readonly string[] Days = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

        private void RefreshModes()
        {
            if (!CoreModule.TryGet<IModeProgress>(out var m)) return;
            m.Refresh();
            int free = m.Table != null ? m.Table.FreeEntriesPerDay : 3;
            string day = Days[Mathf.Clamp(m.TodayIndex, 0, 6)];

            _ui.SetText("HomeTowerTitle", Localize.Get("ui.tower.title"));
            _ui.SetText("HomeTowerSub", Localize.Format("ui.tower.floor", m.TowerNextFloor));
            _ui.SetText("HomeTowerFoot", Localize.Format("ui.home.entries", m.TowerEntries, free));
            _ui.SetActive("HomeTowerDot", m.TowerEntries > 0 || (!m.TowerSweptToday && m.SweepNow != null));

            _ui.SetText("HomeNightTitle", Localize.Get("ui.night.title"));
            _ui.SetText("HomeNightSub", Localize.Format("ui.home.night_sub",
                Localize.Get($"ui.night.day.{day}"), Localize.Get($"ui.night.day_short.{day}")));
            _ui.SetText("HomeNightFoot", Localize.Format("ui.home.best", m.NightBest(m.TodayIndex)));
            _ui.SetActive("HomeNightDot", m.NightEntries > 0);
        }

        // ── 화면 채우기 ──────────────────────────────────────

        /// <summary>9:16 에서 진열 · 시나리오 · 모드 칸의 높이(위 막대 · 아래 바 사이 1048 을 나눈 값).</summary>
        /// <summary>막대 높이(배율 1) — 빌더 LobbyHomeV5Builder 의 TopBar · NavBar 와 같다.</summary>
        private const float TopBar = 120f, NavBar = 112f;
        private const float BaseBody = 1048f, BaseShow = 418f, BaseScenario = 284f, BaseMode = 326f, Gap = 10f;

        /// <summary>
        /// 화면을 꽉 채운다(PD 2026-10-17 「위치만 말고 스케일링으로 꽉 차게」).
        /// 가로는 판이 앵커로 늘고(태블릿 = 넓게), 세로는 남는 높이를 9:16 비율 그대로 세 칸에 나눠 키운다(긴 폰 = 높게).
        /// 진열 칸의 두 캐릭터도 칸이 커진 만큼 같이 키운다 — 칸만 크고 사람이 그대로면 빈 무대가 된다.
        /// </summary>
        private void Layout()
        {
            var body = _ui?.Find("HomeBody") as RectTransform;
            var cards = _ui?.Find("HomeCards") as RectTransform;
            var show = _ui?.Find("HomeShowcase") as RectTransform;
            if (body == null || cards == null || show == null) return;
            // 위 막대가 태블릿에서 커진 만큼(BarZoom) 위 여백도 같이. 아래 바는 높이가 그대로다(폭만 넓힌다)
            float bar = body.parent is RectTransform home ? BarZoom.ScaleFor(home.rect.width) : 1f;
            body.offsetMin = new Vector2(body.offsetMin.x, NavBar);
            body.offsetMax = new Vector2(body.offsetMax.x, -TopBar * bar);
            float k = Mathf.Max(0.85f, body.rect.height / BaseBody);
            // 시나리오 카드는 기본 높이 밑으로 줄이지 않는다 — 줄이면 챕터 이름이 이어 하기 버튼에 덮였다(4:3)
            float scen = BaseScenario * Mathf.Max(1f, k), mode = BaseMode * k;
            float cardsH = scen + Gap + mode;
            SetHeight(cards, 0f, cardsH);
            if (_ui.Find("HomeScenarioCard") is RectTransform sc)
            {
                sc.offsetMin = new Vector2(sc.offsetMin.x, -scen);
                sc.offsetMax = new Vector2(sc.offsetMax.x, 0f);
            }
            foreach (var n in ModeCards)
                if (_ui.Find(n) is RectTransform m) m.offsetMax = new Vector2(m.offsetMax.x, mode);
            float showH = Mathf.Max(BaseShow * 0.85f, body.rect.height - cardsH - Gap);
            SetHeight(show, cardsH + Gap, showH);

            float w = Mathf.Max(1f, show.rect.width) / (720f - 24f);
            float c = Mathf.Clamp(Mathf.Min(showH / BaseShow, w), 0.85f, 1.6f);
            foreach (var n in StageActors)
                if (_ui.Find(n) is RectTransform a) a.localScale = new Vector3(c, c, 1f);
        }

        private static readonly string[] ModeCards = { "HomeTowerCard", "HomeNightCard" };
        private static readonly string[] StageActors = { "HomeGhost", "HomeHost" };

        private static void SetHeight(RectTransform r, float y, float h)
        {
            r.offsetMin = new Vector2(r.offsetMin.x, y);
            r.offsetMax = new Vector2(r.offsetMax.x, y + h);
        }

        /// <summary>인게임과 같은 몸 그림(큰 칸)을 띄운다 — 큰 그림이 없는 몸은 초상이 그대로 선다.</summary>
        private void ShowUnit(string node, string key)
        {
            var t = _ui.Find(node);
            if (t == null || t.GetComponent<Image>() == null) return;
            if (!t.TryGetComponent<UnitShowcase>(out var show)) show = t.gameObject.AddComponent<UnitShowcase>();
            show.Show(key);
        }

        private void SetImage(string node, Sprite sprite)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }
    }
}
