using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN UI 리뉴얼(2026-10-23) 로비 — **전투 탭 = 출전 준비 한 화면**(기획서 4-1).
    /// 출전 줄 순서: 랜덤(BEST) → 유령 → 오늘의 몸 → 보유 몸(등급 틀 · 값). 히든(사신)은 랜덤에서만(PlayerDataService.HiddenTrialHostKey).
    /// 프리팹은 빌더(`Tools/Game/RE:BORN 화면/로비 세우기`)가 세운다 — 노드 이름이 곧 키다.
    /// </summary>
    public sealed class RbLobbyUI : MonoBehaviour, IBackTarget
    {
        // 등급 틀(R · SR · SSR) · 랜덤 · 유령 틀 · 배지 · 카드 아이콘 · 챕터 무대(1~20) — 빌더가 꽂는다
        [SerializeField] private Sprite[] _gradeFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite _frameRandom, _frameGhost;
        [SerializeField] private Sprite _badgeBest, _badgeToday;
        [SerializeField] private Sprite[] _rarityTags = Array.Empty<Sprite>();
        [SerializeField] private Sprite _iconRandom, _iconGhost;
        [SerializeField] private Sprite[] _stageBgs = Array.Empty<Sprite>();
        [SerializeField] private Sprite _iconNext, _iconLock;

        private const float CardStep = 220f * 2f / 3f;   // 시안 카드 간격(1080 기준 220)
        private const float CardLeft = 30f * 2f / 3f;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();
        private readonly List<(Transform card, RunStartMode mode, string key)> _cards = new();
        private SpriteAtlas _faces;
        private RbHostPage _hostPage;
        private RbGrowthPage _growthPage;
        private string _tab = "Battle";

        private int _chapter = 1;
        private RunStartMode _mode = RunStartMode.Random;
        private string _host;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            gameObject.AddComponent<BackButtonRouter>();
            if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(transform);

            _ui.OnClick("ChapterPrev", () => StepChapter(-1));
            _ui.OnClick("ChapterNext", () => StepChapter(+1));
            _ui.OnClick("SortieButton", StartRun);
            _ui.OnClick("SortieAll", () => Soon("ui.rb.soon"));
            foreach (var r in new[] { "RailMail", "RailMission", "RailSettings", "RailPass", "RailEvent", "RailDaily", "HeaderProfile", "HeaderPlus" })
            {
                string node = r;
                _ui.OnClick(node, () => Soon("ui.rb.soon"));
            }
            foreach (var tab in new[] { "Shop", "Summon", "Challenge" })
                _ui.OnClick($"Tab{tab}", () => Soon("ui.rb.soon"));
            _ui.OnClick("TabHost", () => { GameSound.Cue("ui.tap"); SelectTab("Host"); });
            _ui.OnClick("TabBattle", () => { GameSound.Cue("ui.tap"); SelectTab("Battle"); });
            _hostPage = GetComponentInChildren<RbHostPage>(true);
            _growthPage = GetComponentInChildren<RbGrowthPage>(true);
            _ui.SetActive("SortieCardTemplate", false);
        }

        private void OnEnable()
        {
            if (CoreModule.TryGet<IEventBus>(out var bus))
            {
                _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshHeader()));
                _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => RefreshAll()));
            }
        }

        private async void Start()
        {
            _player = CoreModule.Get<IPlayerDataService>();
            try { _faces = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/hostface"); }
            catch (Exception e) { Debug.LogWarning($"[RbLobby] 얼굴 아틀라스: {e.Message}"); }
            if (this == null) return;
            _chapter = Mathf.Clamp(_player.SelectedChapter, 1, Mathf.Max(1, _player.ChapterCount));
            if (CoreModule.TryGet<IStaminaService>(out var st)) st.Sync();
            BuildCards();
            RefreshAll();
            SelectTab("Battle");
            LayoutRails();
        }

        // 화면 높이가 바뀌면(회전 · 창 크기) 양옆 아이콘 간격을 다시 잰다
        private void OnRectTransformDimensionsChange()
        {
            if (_ui != null) LayoutRails();
        }

        // ── 양옆 아이콘 — 16:9 는 20:9 시안보다 세로가 짧아 챕터 카드에 아래 아이콘이 가린다. 간격만 줄인다 ──────────

        private static readonly string[][] RailColumns =
        {
            new[] { "RailMail", "RailMission", "RailSettings" },
            new[] { "RailPass", "RailEvent", "RailDaily" },
        };
        private const float RailStep = 200f * 2f / 3f;   // 시안 아이콘 간격(1080 기준 200)
        private float _railFirstY = float.NaN;

        private void LayoutRails()
        {
            var root = (RectTransform)transform;
            var card = _ui.Find("ChapterCard") as RectTransform;
            var first = _ui.Find("RailMail") as RectTransform;
            if (card == null || first == null || root.rect.height <= 0f) return;
            if (float.IsNaN(_railFirstY)) _railFirstY = -first.anchoredPosition.y;   // 위에서 첫 아이콘 가운데까지
            float cardTop = root.rect.height - (card.anchoredPosition.y + card.sizeDelta.y * 0.5f);   // 위에서 카드 윗변까지
            float half = first.sizeDelta.y * 0.5f;
            float step = Mathf.Min(RailStep, (cardTop - 6f - half - _railFirstY) * 0.5f);
            foreach (var col in RailColumns)
                for (int i = 0; i < col.Length; i++)
                    if (_ui.Find(col[i]) is RectTransform r)
                        r.anchoredPosition = new Vector2(r.anchoredPosition.x, -(_railFirstY + step * i));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        // 뒤로가기 — 성장 → 도감 → 전투 탭 순으로 물러난다. 전투 탭이면 닫을 것이 없다(라우터가 종료 확인을 띄운다)
        public bool OnBackPressed()
        {
            if (_growthPage != null && _growthPage.gameObject.activeSelf) { SelectTab("Host"); return true; }
            if (_tab != "Battle") { SelectTab("Battle"); return true; }
            return false;
        }

        private void RefreshAll()
        {
            if (_player == null || !_player.IsReady) return;
            RefreshHeader();
            RefreshChapter();
            RefreshCards();
            RefreshSortie();
            if (_hostPage != null && _hostPage.gameObject.activeSelf) _hostPage.Refresh();
            if (_growthPage != null && _growthPage.gameObject.activeSelf) _growthPage.Refresh();
        }

        // ── 위 막대 ──────────────────────────────────────────

        public void RefreshHeader()
        {
            if (_player == null) return;
            int now = _player.Stamina, max = _player.StaminaMax;
            if (CoreModule.TryGet<IStaminaService>(out var st)) { now = st.Current; max = st.Max; }
            _ui.SetText("StaminaText", $"{now}/{max}");
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
            _ui.SetText("HeaderLevelText", _player.GhostLevel.ToString());
        }

        // ── 챕터 카드 ─────────────────────────────────────────

        private void StepChapter(int d)
        {
            int next = Mathf.Clamp(_chapter + d, 1, Mathf.Max(1, _player.ChapterCount));
            if (next == _chapter) return;
            _chapter = next;
            GameSound.Cue("ui.tap");
            RefreshChapter();
            RefreshSortie();
        }

        private bool ChapterOpen => _chapter <= _player.UnlockedChapter;

        private void RefreshChapter()
        {
            _ui.SetText("ChapterNoText", $"CH {_chapter}");
            _ui.SetText("ChapterNameText", Localize.FromTable($"stage.{_chapter}.1.name", _player.ChapterInfo(_chapter).Name ?? string.Empty));
            int rooms = 30;
            string best = _chapter < _player.UnlockedChapter || _chapter <= _player.ClearedChapter
                ? Localize.FromTable("ui.rb.lobby.cleared", "클리어")
                : ChapterOpen ? string.Format(Localize.FromTable("ui.rb.lobby.best", "최고 {0}/{1}방"), Mathf.Max(0, _player.ReachedStage), rooms)
                : Localize.FromTable("ui.rb.lobby.locked", "잠김");
            _ui.SetText("ChapterInfoText", string.Format(Localize.FromTable("ui.rb.lobby.chapter_info", "{0} · 권장 <color=#FFFFFF>{1:N0}</color>"),
                                                         best, _player.RecommendedPower(_chapter)));
            SetActiveButton("ChapterPrev", _chapter > 1);
            // 다음 챕터가 잠겼으면 자물쇠(시안) — 누르면 넘어가지만 출전은 막힌다
            var nextIcon = _ui.Get<Image>("ChapterNextIcon");
            if (nextIcon != null) nextIcon.sprite = _chapter + 1 > _player.UnlockedChapter ? _iconLock : _iconNext;
            SetActiveButton("ChapterNext", _chapter < _player.ChapterCount);

            var bg = _ui.Get<Image>("StageBg");
            if (bg != null && _stageBgs.Length > 0) bg.sprite = _stageBgs[Mathf.Clamp(_chapter - 1, 0, _stageBgs.Length - 1)];
        }

        private void SetActiveButton(string node, bool on)
        {
            var b = _ui.Get<Button>(node);
            if (b != null) b.interactable = on;
        }

        // ── 출전 줄 ──────────────────────────────────────────

        private void BuildCards()
        {
            var tpl = _ui.Find("SortieCardTemplate");
            var content = _ui.Find("SortieContent") as RectTransform;
            if (tpl == null || content == null) return;
            for (int i = 0; i < _cards.Count; i++) if (_cards[i].card != null) Destroy(_cards[i].card.gameObject);
            _cards.Clear();

            var list = new List<(RunStartMode, string)> { (RunStartMode.Random, null), (RunStartMode.Ghost, HostEntry.GhostKey) };
            var seen = new HashSet<string>();
            if (CoreModule.TryGet<ITodayHostService>(out var today))
                foreach (var k in today.Today)
                    if (seen.Add(k) && _player.GetHost(k) != null) list.Add((RunStartMode.Host, k));
            var owned = new List<HostEntry>();
            foreach (var e in _player.PlayableHosts)
                if (e != null && !e.IsGhost && _player.IsHostOwned(e) && !seen.Contains(e.HostKey)) owned.Add(e);
            // 등급 높은 것부터 — 같은 등급은 표 순서
            owned.Sort((a, b) => b.Grade.CompareTo(a.Grade));
            foreach (var e in owned) list.Add((RunStartMode.Host, e.HostKey));

            for (int i = 0; i < list.Count; i++)
            {
                var c = Instantiate(tpl.gameObject, content).transform;
                c.name = $"SortieCard{i}";
                c.gameObject.SetActive(true);
                ((RectTransform)c).anchoredPosition = new Vector2(CardLeft + i * CardStep, 0f);
                var (mode, key) = list[i];
                var btn = c.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => Pick(mode, key));
                _cards.Add((c, mode, key));
            }
            content.sizeDelta = new Vector2(CardLeft * 2f + list.Count * CardStep, content.sizeDelta.y);
            _ui = new UIBinder(transform);   // 복제한 카드도 이름으로 찾게
            if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(content);
        }

        private void Pick(RunStartMode mode, string key)
        {
            _mode = mode;
            _host = key;
            GameSound.Cue("ui.tap");
            RefreshCards();
            RefreshSortie();
        }

        private void RefreshCards()
        {
            foreach (var (card, mode, key) in _cards)
            {
                var e = key != null ? _player.GetHost(key) : null;
                bool sel = mode == _mode && (mode != RunStartMode.Host || key == _host);
                bool today = mode == RunStartMode.Host && IsToday(key);
                var frame = _ui.Find(card, "CardFrame")?.GetComponent<Image>();
                if (frame != null)
                    frame.sprite = mode == RunStartMode.Random ? _frameRandom : mode == RunStartMode.Ghost ? _frameGhost
                                 : e != null && (int)e.Grade < _gradeFrames.Length ? _gradeFrames[(int)e.Grade] : frame.sprite;
                _ui.Find(card, "CardSel")?.gameObject.SetActive(sel);

                // 배지: 랜덤 = BEST · 오늘의 몸 = 「오늘」 · 그 밖은 등급 태그(SR 이상만 — 시안)
                var badge = _ui.Find(card, "CardBadge")?.GetComponent<Image>();
                var badgeText = _ui.Find(card, "CardBadgeText")?.GetComponent<TextMeshProUGUI>();
                Sprite bs = null; string bt = string.Empty;
                if (mode == RunStartMode.Random) bs = _badgeBest;
                else if (today) { bs = _badgeToday; bt = Localize.FromTable("ui.rb.lobby.today", "오늘"); }
                else if (e != null && e.Grade != HostGrade.B && (int)e.Grade < _rarityTags.Length) bs = _rarityTags[(int)e.Grade];
                if (badge != null) { badge.sprite = bs; badge.gameObject.SetActive(bs != null); }
                if (badgeText != null) badgeText.text = bt;

                var icon = _ui.Find(card, "CardIcon")?.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = mode == RunStartMode.Random ? _iconRandom : mode == RunStartMode.Ghost ? _iconGhost : Face(key);

                string name = mode == RunStartMode.Random ? Localize.FromTable("ui.rb.lobby.random", "? 랜덤")
                            : mode == RunStartMode.Ghost ? Localize.FromTable("host.ghost.name", "유령")
                            : e != null ? e.DisplayName : key;
                TextIn(card, "CardName", name);

                int price = mode == RunStartMode.Host && !today && e != null ? _player.EntryPriceOf(e, _chapter) : 0;
                var priceText = _ui.Find(card, "CardPrice")?.GetComponent<TextMeshProUGUI>();
                if (priceText != null)
                {
                    priceText.text = price > 0 ? price.ToString("N0") : Localize.FromTable("ui.rb.lobby.free", "무료");
                    priceText.color = price > 0 ? (price > _player.Gold ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.79f, 0.24f))
                                                : new Color(0.29f, 0.88f, 0.54f);
                    priceText.alignment = price > 0 ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                }
                _ui.Find(card, "CardPriceIcon")?.gameObject.SetActive(price > 0);
            }
        }

        private Sprite Face(string key) => _faces != null && key != null ? _faces.GetSprite($"face_{key}") : null;

        private static bool IsToday(string hostKey)
            => !string.IsNullOrEmpty(hostKey) && CoreModule.TryGet<ITodayHostService>(out var t) && t.IsToday(hostKey);

        private void TextIn(Transform root, string node, string value)
        {
            var t = _ui.Find(root, node)?.GetComponent<TextMeshProUGUI>();
            if (t != null) t.text = value;
        }

        // ── 보너스 칩 · 안내 · 출전 버튼 ──────────────────────────

        private int Cost => _mode == RunStartMode.Host && !IsToday(_host) && _host != null
            ? _player.EntryPriceOf(_player.GetHost(_host), _chapter) : 0;

        private void RefreshSortie()
        {
            var cfg = _player.Config;
            string[] chips;
            string note;
            if (_mode == RunStartMode.Random)
            {
                chips = new[]
                {
                    Localize.FromTable("ui.rb.lobby.chip.hidden", "히든 등장 가능"),
                    string.Format(Localize.FromTable("ui.rb.lobby.chip.shards", "조각 +{0}"), cfg != null ? cfg.RandomLuckShards : 3),
                    string.Format(Localize.FromTable("ui.rb.lobby.chip.gold", "골드 +{0}%"), cfg != null ? cfg.RandomLuckGoldPct : 20),
                };
                note = Localize.FromTable("ui.rb.lobby.note.random", "클리어 보너스 · 히든은 이번 판 체험, 조각 미지급");
            }
            else if (_mode == RunStartMode.Ghost)
            {
                chips = new[] { string.Format(Localize.FromTable("ui.rb.lobby.chip.ghost_exp", "유령 경험치 +{0}%"), cfg != null ? cfg.GhostStartExpPct : 50) };
                note = Localize.FromTable("ui.rb.lobby.note.ghost", "몸 없이 시작 · 판 안에서 몸을 빼앗아 싸워요");
            }
            else
            {
                var e = _player.GetHost(_host);
                chips = e == null ? Array.Empty<string>() : new[]
                {
                    HostGradeLabel.Of(e.Grade),
                    string.Format(Localize.FromTable("ui.rb.lobby.chip.power", "전투력 {0:N0}"), _player.PowerOf(_host)),
                };
                note = IsToday(_host) ? Localize.FromTable("ui.rb.lobby.note.today", "오늘의 몸 · 골드 없이 출전")
                                      : Localize.FromTable("ui.rb.lobby.note.host", "고른 몸으로 바로 시작해요");
            }
            for (int i = 0; i < 3; i++)
            {
                bool on = i < chips.Length;
                _ui.SetActive($"BonusChip{i}", on);
                if (on) _ui.SetText($"BonusChip{i}Text", chips[i]);
            }
            _ui.SetText("BonusNote", note);

            int perRun = CoreModule.TryGet<IStaminaService>(out var st) ? st.PerRun : 5;
            int gold = Cost;
            string hostName = _host != null ? _player.GetHost(_host)?.DisplayName : null;
            string label = _mode == RunStartMode.Host && hostName != null
                ? string.Format(Localize.FromTable("ui.rb.lobby.sortie_with", "{0}{1} 출전"), hostName, KoreanRo(hostName))
                : _mode == RunStartMode.Ghost ? Localize.FromTable("ui.rb.lobby.sortie_ghost", "유령으로 출전")
                : Localize.FromTable("ui.rb.lobby.sortie", "출전");
            _ui.SetText("SortieLabel", label);
            _ui.SetText("SortieStaminaText", perRun.ToString());
            _ui.SetText("SortieGoldText", gold.ToString("N0"));
            _ui.SetActive("SortieGoldIcon", gold > 0);
            _ui.SetActive("SortieGoldText", gold > 0);
            LayoutSortie(gold > 0);
            SetActiveButton("SortieButton", ChapterOpen);
        }

        /// <summary>
        /// 「영매로 · 청룡으로」 — 받침이 없거나 ㄹ 받침이면 「로」, 아니면 「으로」. 한국어 문구만 이 자리를 쓴다(다른 언어는 {1} 이 없다).
        /// 괄호로 끝나는 이름(「코만도(기관총)」)은 괄호 안 마지막 글자로 본다.
        /// </summary>
        private static string KoreanRo(string name)
        {
            for (int i = name.Length - 1; i >= 0; i--)
            {
                char c = name[i];
                if (c < 0xAC00 || c > 0xD7A3) continue;
                int jong = (c - 0xAC00) % 28;
                return jong == 0 || jong == 8 ? "로" : "으로";
            }
            return "로";
        }

        /// <summary>「출전 ⚡5 🪙200」 한 줄을 글자 폭을 재서 버튼 가운데에 세운다 — 언어 · 자릿수마다 폭이 다르다.</summary>
        private void LayoutSortie(bool hasGold)
        {
            const float Gap = 10f;
            var label = _ui.Get<TextMeshProUGUI>("SortieLabel");
            var sIcon = _ui.Find("SortieStaminaIcon") as RectTransform;
            var sText = _ui.Get<TextMeshProUGUI>("SortieStaminaText");
            var gIcon = _ui.Find("SortieGoldIcon") as RectTransform;
            var gText = _ui.Get<TextMeshProUGUI>("SortieGoldText");
            if (label == null || sIcon == null || sText == null) return;
            float lw = label.GetPreferredValues(label.text).x, sw = sText.GetPreferredValues(sText.text).x;
            float gw = hasGold && gText != null ? gText.GetPreferredValues(gText.text).x : 0f;
            float total = lw + Gap * 2 + sIcon.rect.width + 4 + sw + (hasGold ? Gap + gIcon.rect.width + 4 + gw : 0f);
            float x = -total * 0.5f;
            Put(label.rectTransform, x, lw); x += lw + Gap * 2;
            Put(sIcon, x, sIcon.rect.width); x += sIcon.rect.width + 4;
            Put(sText.rectTransform, x, sw); x += sw;
            if (!hasGold) return;
            x += Gap;
            Put(gIcon, x, gIcon.rect.width); x += gIcon.rect.width + 4;
            Put(gText.rectTransform, x, gw);
        }

        private static void Put(RectTransform r, float left, float width)
        {
            r.sizeDelta = new Vector2(Mathf.Max(width + 2f, 4f), r.sizeDelta.y);
            r.anchoredPosition = new Vector2(left + r.sizeDelta.x * 0.5f, r.anchoredPosition.y);
        }

        // ── 출전 — 값을 둘 다 확인한 뒤에 낸다(골드가 모자란데 행동력만 빠지면 안 된다) ──────────

        private void StartRun()
        {
            if (!ChapterOpen) return;
            CoreModule.TryGet<IStaminaService>(out var stamina);
            if (stamina != null && !stamina.CanEnter) { StaminaPopup.Show(stamina, _player, true, RefreshHeader); return; }

            string key = _mode == RunStartMode.Random ? RollRandomHost()
                       : _mode == RunStartMode.Ghost ? HostEntry.GhostKey : _host;
            var e = _player.GetHost(key);
            if (e == null) return;
            int gold = PlayerDataService.FreeHostsForTest ? 0 : Cost;
            if (gold > 0 && _player.Gold < gold)
            {
                // 골드 부족 — 대안 「랜덤으로 출전 · 골드 없음」(묶음 규칙)
                SystemPopup.Show(string.Format(Localize.Get("ui.hostselect.gold_short"), e.DisplayName, HostGradeLabel.Of(e.Grade), _player.Gold, gold),
                                 () => Pick(RunStartMode.Random, null), Localize.FromTable("ui.rb.lobby.gold_short_random", "랜덤으로 출전 · 골드 없음"),
                                 Localize.Get("ui.common.ok"));
                return;
            }
            if (stamina != null && !stamina.TryPayEntry()) { StaminaPopup.Show(stamina, _player, true, RefreshHeader); return; }
            if (gold > 0) _player.TrySpendGold(gold);

            _player.SelectedChapter = _chapter;
            _player.SelectHost(key);
            _player.RunStartMode = _mode;
            _player.RunHostKey = key;
            _player.StartAsGhost = e.IsGhost;
            GameSound.Cue("ui.play");
            EnterGameAsync().Forget();   // fire-and-forget: 씬 전환 완료를 기다릴 화면이 없다
        }

        /// <summary>랜덤 — 히든(사신) 아주 낮은 확률 → 안 가진 S급 낮은 확률 → 가진 몸(ChapterHostPanel 과 같은 규칙).</summary>
        private string RollRandomHost()
        {
            var cfg = _player.Config;
            if (cfg != null && UnityEngine.Random.Range(0f, 100f) < cfg.RandomHiddenChancePct
                && _player.GetHost(PlayerDataService.HiddenTrialHostKey) != null)
                return PlayerDataService.HiddenTrialHostKey;
            var owned = new List<string>();
            var legend = new List<string>();
            foreach (var e in _player.PlayableHosts)
            {
                if (e == null || e.IsGhost) continue;
                if (_player.IsHostOwned(e)) owned.Add(e.HostKey);
                else if (e.Grade == HostGrade.S || (cfg != null && cfg.RandomIncludesUnowned)) legend.Add(e.HostKey);
            }
            if (legend.Count > 0 && UnityEngine.Random.Range(0, 100) < LegendChancePercent)
                return legend[UnityEngine.Random.Range(0, legend.Count)];
            if (owned.Count > 0) return owned[UnityEngine.Random.Range(0, owned.Count)];
            return HostEntry.GhostKey;
        }

        private const int LegendChancePercent = 5;

        private async UniTaskVoid EnterGameAsync()
        {
            await _player.SaveAsync();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.InGame,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }

        // ── 하단 탭 ──────────────────────────────────────────

        private static readonly Color TabOn = new(1f, 0.79f, 0.24f), TabOff = new(0.70f, 0.74f, 0.84f);

        /// <summary>아래 탭 — 쪽을 바꾼다. 위 막대 · 아래 탭은 모든 쪽이 같이 쓴다.</summary>
        public void SelectTab(string tab)
        {
            _tab = tab;
            foreach (var t in new[] { "Shop", "Host", "Battle", "Summon", "Challenge" })
            {
                var label = _ui.Get<TextMeshProUGUI>($"Tab{t}Label");
                if (label != null) label.color = t == tab ? TabOn : TabOff;
            }
            _ui.SetActive("PageBattle", tab == "Battle");
            if (_growthPage != null) _growthPage.gameObject.SetActive(false);
            if (_hostPage != null)
            {
                if (tab == "Host" && _player != null) _hostPage.Open(_player, _faces);
                else _hostPage.gameObject.SetActive(false);
            }
            if (tab == "Battle" && _player != null && _player.IsReady) { RefreshCards(); RefreshSortie(); }
            RefreshHeader();
        }

        /// <summary>도감 카드 → 호스트 성장 한 화면. 아래 탭은 「호스트」 그대로다.</summary>
        public void OpenGrowth(string hostKey)
        {
            if (_growthPage == null || _player == null) return;
            if (_hostPage != null) _hostPage.gameObject.SetActive(false);
            _growthPage.Open(_player, hostKey);
        }

        /// <summary>유령 성장 — 다음 차례에 새 화면(시안 ghost-growth)으로 바꾼다.</summary>
        public void OpenGhostGrowth() => Soon("ui.rb.soon");

        private static void Soon(string key)
            => SystemPopup.Show(Localize.FromTable(key, "준비 중이에요 · 업데이트로 열려요"), null, Localize.Get("ui.common.ok"), null);
    }
}
