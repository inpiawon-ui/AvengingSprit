using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 육성 화면 — 하단 바 HOST 칸(시안 growth_ui_v4). 유령 탭 · 호스트 탭 두 쪽이다(기획 2026-09-21).
    ///
    ///   유령 탭   유저(유령) 레벨 · 경험치, 능력치 골드 강화(**모든 몸**에 붙는다), 성장 경로 보상
    ///   호스트 탭 고른 몸의 숙련도(Lv x/10, 별 = 2단계마다 1개, 조각 막대), 능력치 골드 강화(그 몸에만),
    ///            스킬(액티브 · 패시브), 호스트 목록
    ///
    /// 화면은 시안 픽셀 그대로다 — `GrowthBuilder` 가 세운다. 노드 이름 = 여기서 찾는 이름.
    /// </summary>
    public sealed class GrowthPanel : MonoBehaviour
    {
        [Serializable]
        private struct HostArt
        {
            public string Key;
            public Sprite Portrait;   // 카드 큰 그림 — 초상(발주본)이 있으면 그것, 없으면 유닛 그림(동쪽 · 대기)
            public Sprite Thumb;      // 목록 칸 — 초상이 있으면 같은 그림, 없으면 유닛 그림(남동 · 대기)
            public Sprite Skill;      // 액티브 스킬 아이콘
            public bool IsPortrait;   // 초상인가 — 유닛 그림은 투명 여백이 있어 칸을 키워 쓴다
        }

        /// <summary>
        /// 그림 칸 — 초상 · 유닛 그림 두 벌. 초상은 잉크에 딱 맞게 잘려 있어 시안 칸 그대로,
        /// 유닛 그림(96px)은 위 · 옆 투명 여백만큼 칸을 키운다. (x, y, w, h) — 부모 왼쪽 위 기준, 아래로 +.
        /// </summary>
        [SerializeField] private Rect _portraitRect, _portraitRectUnit, _thumbRect, _thumbRectUnit;

        [SerializeField] private HostArt[] _hostArts = Array.Empty<HostArt>();
        /// <summary>`HostStat` 순서의 아이콘.</summary>
        [SerializeField] private Sprite[] _statIcons = Array.Empty<Sprite>();
        [SerializeField] private Sprite _starBigOn, _starBigOff, _starSmallOn, _starSmallOff;
        [SerializeField] private Sprite _nodeDone, _nodeNext, _nodeLock;
        [SerializeField] private Color _labelDone = Color.cyan, _labelNow = Color.white,
                                       _labelNext = Color.yellow, _labelLock = Color.white;
        /// <summary>유령 탭은 「능력치 · 스킬」 탭 줄이 없어 능력치 판이 이만큼 위로 늘었다(px).</summary>
        [SerializeField] private float _ghostStatLift = 50f;
        /// <summary>스킬 탭에서는 호스트 목록이 이만큼 아래에 있다(시안 px).</summary>
        [SerializeField] private float _skillListDrop = 48f;
        /// <summary>목록 칸 간격(가로 · 세로)과 한 줄 칸 수.</summary>
        [SerializeField] private Vector2 _cardPitch = new(174.3f, 162f);
        [SerializeField] private int _cardColumns = 4;
        [SerializeField] private float _rowPitch = 50.6f;

        // 목록 순서 — 시안 첫 줄이 공격력이다
        private static readonly HostStat[] StatOrder =
            { HostStat.Atk, HostStat.Hp, HostStat.Crit, HostStat.AtkSpeed, HostStat.Range, HostStat.MoveSpeed };
        private static readonly string[] StatKeys = { "hp", "atk", "crit", "atkspeed", "range", "movespeed" };

        private enum Page { Ghost, Host }

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();
        private readonly List<Transform> _rows = new();
        private readonly List<Transform> _cards = new();
        private readonly List<string> _cardKeys = new();
        private Page _page = Page.Ghost;
        private bool _skillTab;
        private string _hostKey;
        private bool _built;
        private Vector2 _statSectionPos, _statViewportSize;
        private Vector2 _hostListPos;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("TabGhostButton", () => ShowPage(Page.Ghost));
            _ui.OnClick("TabHostButton", () => ShowPage(Page.Host));
            _ui.OnClick("StatTabButton", () => { _skillTab = false; RefreshAll(); });
            _ui.OnClick("SkillTabButton", () => { _skillTab = true; RefreshAll(); });
            _ui.OnClick("StatHelpButton", () => Toast(Localize.Get("ui.growth.help.stats")));
            _ui.OnClick("PathHelpButton", () => Toast(Localize.Get("ui.growth.help.path")));
            _ui.OnClick("PathBoxButton", () => ClaimNext());
            for (int i = 0; i < 5; i++) _ui.OnClick($"PathNode{i}", () => ClaimNext());
            _ui.OnClick("ShardBarButton", UpgradeMastery);
            foreach (var (node, label) in new[] { ("GrowthGoldPlusButton", "상점"), ("GrowthMailButton", "우편"),
                                                  ("GrowthSettingsButton", "설정"), ("SortButton", "정렬") })
            {
                var captured = label;
                _ui.OnClick(node, () => NotifyNotReady(captured));
            }

            var section = _ui.Find("StatSection") as RectTransform;
            if (section != null) _statSectionPos = section.anchoredPosition;
            var viewport = _ui.Find("StatViewport") as RectTransform;
            if (viewport != null) _statViewportSize = viewport.sizeDelta;
            var list = _ui.Find("HostList") as RectTransform;
            if (list != null) _hostListPos = list.anchoredPosition;
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshGoldAndRows()));
            _tokens.Add(bus.Subscribe<GhostProgressChangedEvent>(_ => RefreshAll()));
            _tokens.Add(bus.Subscribe<LanguageChangedEvent>(_ => RefreshAll()));
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => RefreshAll()));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        public void Open()
        {
            // ⚠ 맨 앞으로 올리지 마라 — 공통 하단 바(`BottomNav`)가 뒤 형제라 그 위를 덮는다(2026-09-21).
            //   순서는 빌더가 잡는다: 로비 판 < 육성 화면 < 하단 바 < 창들.
            gameObject.SetActive(true);
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;
            BuildOnce();
            RefreshAll();
        }

        public void Close() => gameObject.SetActive(false);

        // ── 세우기 — 줄 · 칸을 표만큼 복제 ───────────────────────

        private void BuildOnce()
        {
            if (_built) return;
            _built = true;

            var rowTemplate = _ui.Find("StatRow");
            if (rowTemplate != null)
            {
                var content = rowTemplate.parent as RectTransform;
                for (int i = 0; i < StatOrder.Length; i++)
                {
                    var row = i == 0 ? rowTemplate : Instantiate(rowTemplate.gameObject, content).transform;
                    row.name = $"StatRow{i}";
                    var rt = (RectTransform)row;
                    rt.anchoredPosition = ((RectTransform)rowTemplate).anchoredPosition + new Vector2(0f, -_rowPitch * i);
                    var stat = StatOrder[i];
                    var icon = _ui.Find(row, "RowIcon")?.GetComponent<Image>();
                    if (icon != null && (int)stat < _statIcons.Length)
                    {
                        icon.sprite = _statIcons[(int)stat];
                        icon.preserveAspect = true;
                    }
                    var button = _ui.Find(row, "RowButton")?.GetComponent<Button>();
                    if (button != null) button.onClick.AddListener(() => BuyStat(stat));
                    _rows.Add(row);
                }
                if (content != null)
                    content.sizeDelta = new Vector2(content.sizeDelta.x, _rowPitch * StatOrder.Length + 4f);
            }

            var cardTemplate = _ui.Find("HostCard");
            if (cardTemplate != null && _player != null)
            {
                var content = cardTemplate.parent as RectTransform;
                var hosts = _player.PlayableHosts;
                int n = 0;
                for (int i = 0; i < hosts.Count; i++)
                {
                    var e = hosts[i];
                    if (e == null || e.IsGhost) continue;
                    var card = n == 0 ? cardTemplate : Instantiate(cardTemplate.gameObject, content).transform;
                    card.name = $"HostCard_{e.HostKey}";
                    var rt = (RectTransform)card;
                    rt.anchoredPosition = ((RectTransform)cardTemplate).anchoredPosition +
                                          new Vector2(_cardPitch.x * (n % _cardColumns), -_cardPitch.y * (n / _cardColumns));
                    string key = e.HostKey;
                    card.GetComponent<Button>()?.onClick.AddListener(() => SelectHost(key));
                    _cards.Add(card);
                    _cardKeys.Add(key);
                    n++;
                }
                if (content != null)
                {
                    int rows = (n + _cardColumns - 1) / _cardColumns;
                    content.sizeDelta = new Vector2(content.sizeDelta.x, _cardPitch.y * rows + 8f);
                }
                if (string.IsNullOrEmpty(_hostKey) && _cardKeys.Count > 0) _hostKey = _cardKeys[0];
            }
        }

        // ── 칠하기 ──────────────────────────────────────────────

        private void ShowPage(Page page)
        {
            _page = page;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_player == null || !_player.IsReady || !_built) return;
            bool ghost = _page == Page.Ghost;

            SetActive("BgGhost", ghost);
            SetActive("BgHost", !ghost && !_skillTab);
            SetActive("BgSkill", !ghost && _skillTab);
            SetActive("TabGhostOn", ghost); SetActive("TabGhostOff", !ghost);
            SetActive("TabHostOn", !ghost); SetActive("TabHostOff", ghost);
            SetTabText("TabGhostText", ghost);
            SetTabText("TabHostText", !ghost);

            SetActive("HostPortrait", !ghost);
            SetActive("ShardGroup", !ghost);
            SetActive("ExpGroup", ghost);
            SetActive("HostTabs", !ghost);
            SetActive("StatTabOn", !_skillTab); SetActive("StatTabOff", _skillTab);
            SetActive("SkillTabOn", _skillTab); SetActive("SkillTabOff", !_skillTab);
            SetTabText("StatTabText", !_skillTab);
            SetTabText("SkillTabText", _skillTab);
            SetActive("StatSection", ghost || !_skillTab);
            SetActive("SkillSection", !ghost && _skillTab);
            SetActive("HostList", !ghost);
            SetActive("GhostPath", ghost);

            // 유령 탭은 탭 줄이 없어 능력치 판이 위로 늘었다 — 판 · 창을 올리고 늘린다
            var section = _ui.Find("StatSection") as RectTransform;
            if (section != null) section.anchoredPosition = _statSectionPos + new Vector2(0f, ghost ? _ghostStatLift : 0f);
            var viewport = _ui.Find("StatViewport") as RectTransform;
            if (viewport != null) viewport.sizeDelta = _statViewportSize + new Vector2(0f, ghost ? _ghostStatLift : 0f);
            var list = _ui.Find("HostList") as RectTransform;
            if (list != null) list.anchoredPosition = _hostListPos + new Vector2(0f, _skillTab ? -_skillListDrop : 0f);

            RefreshGoldAndRows();
            if (ghost) { RefreshGhostCard(); RefreshPath(); }
            else { RefreshHostCard(); RefreshSkills(); RefreshCards(); }
        }

        private void SetTabText(string node, bool on)
        {
            var t = _ui.Find(node)?.GetComponent<TabTextColors>();
            if (t != null) t.Apply(on);
        }

        private void RefreshGoldAndRows()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GrowthGoldText", _player.Gold.ToString("N0"));
            bool ghost = _page == Page.Ghost;
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var stat = StatOrder[i];
                int lv = ghost ? _player.GhostStatLevel(stat) : _player.HostStatLevel(_hostKey, stat);
                // 호스트는 성급이 상한을 연다 — 「/ 10」 이 「/ 20」 으로 바뀌는 것이 성급의 보상이다
                int max = ghost ? _player.GhostStatMax : _player.HostStatCap(_hostKey);
                int cost = ghost ? _player.GhostStatCost(stat) : _player.HostStatCost(_hostKey, stat);
                TextIn(row, "RowNameText", Localize.Get($"ui.stat.{StatKeys[(int)stat]}"));
                TextIn(row, "RowLvLabel", "Lv.");
                TextIn(row, "RowLvNum", lv.ToString());
                TextIn(row, "RowLvMax", $"/ {max}");
                TextIn(row, "RowPctText", $"+{_player.StatPercent(stat, lv):0.0}%");
                TextIn(row, "RowCostText", cost > 0 ? cost.ToString("N0") : Localize.Get("ui.growth.max"));
                var coin = _ui.Find(row, "RowCoin");
                if (coin != null) coin.gameObject.SetActive(cost > 0);
                row.GetComponentInChildren<InkRun>(true)?.Apply();
            }
        }

        private void RefreshGhostCard()
        {
            _ui.SetText("CardNameEnText", "GHOST");
            _ui.SetText("CardNameText", Localize.Get("ui.growth.ghost.name"));
            _ui.SetText("CardDescText", Localize.Get("ui.growth.ghost.desc"));
            int lv = _player.GhostLevel, max = _player.GhostLevelMax;
            SetLevel(lv, max);
            // 유령 별 — 10레벨마다 하나(Lv 28 → ★★★)
            SetStars("CardStar", Mathf.Clamp(Mathf.CeilToInt(lv / 10f), 0, 5), _starBigOn, _starBigOff);
            SetBar("ExpBarFill", _player.GhostExp, _player.GhostExpMax);
            _ui.SetText("ExpValueText", $"{_player.GhostExp:N0} / {_player.GhostExpMax:N0}");
        }

        private void RefreshHostCard()
        {
            var e = _player.GetHost(_hostKey);
            if (e == null) return;
            _ui.SetText("CardNameEnText", ShortName(e).ToUpperInvariant());
            _ui.SetText("CardNameText", e.DisplayName);
            _ui.SetText("CardDescText", Localize.Get($"host.{e.HostKey}.desc"));
            int mastery = _player.GetMastery(_hostKey);
            SetLevel(mastery, _player.MasteryMax);
            SetStars("CardStar", _player.StarsOf(_hostKey), _starBigOn, _starBigOff);
            var portrait = _ui.Get<Image>("HostPortrait");
            if (portrait != null)
            {
                var art = ArtOf(_hostKey);
                portrait.sprite = art.Portrait;
                portrait.enabled = portrait.sprite != null;
                portrait.preserveAspect = true;
                PlaceArt(portrait.rectTransform, art.IsPortrait ? _portraitRect : _portraitRectUnit);
            }
            int shards = _player.GetShards(_hostKey);
            int cost = _player.MasteryCost(_hostKey);
            SetBar("ShardBarFill", cost > 0 ? shards : 1, cost > 0 ? cost : 1);
            _ui.SetText("ShardValueText", cost > 0 ? $"{shards} / {cost}" : Localize.Get("ui.growth.max"));
        }

        private void SetLevel(int lv, int max)
        {
            _ui.SetText("CardLvLabel", "Lv.");
            _ui.SetText("CardLvNum", lv.ToString());
            _ui.SetText("CardLvMax", $"/ {max}");
            _ui.Find("CardLvRun")?.GetComponent<InkRun>()?.Apply();
        }

        private void SetStars(string prefix, int filled, Sprite on, Sprite off)
        {
            for (int i = 0; i < 5; i++)
            {
                var img = _ui.Get<Image>($"{prefix}{i}");
                if (img != null) img.sprite = i < filled ? on : off;
            }
        }

        private void SetBar(string fillName, int value, int max)
        {
            var fill = _ui.Get<Image>(fillName);
            if (fill != null) fill.fillAmount = Mathf.Clamp01(max > 0 ? (float)value / max : 0f);
        }

        private void RefreshSkills()
        {
            var e = _player.GetHost(_hostKey);
            if (e == null) return;
            var active = _player.GetActiveSkill(e.ActiveSkillKey);
            _ui.SetText("SkillActiveName", active != null
                ? (string.IsNullOrEmpty(active.DisplayName) ? active.NameEn : active.DisplayName) : "—");
            _ui.SetText("SkillActiveDesc", active?.DisplayDescription ?? string.Empty);
            var icon = _ui.Get<Image>("SkillActiveIcon");
            if (icon != null)
            {
                icon.sprite = ArtOf(_hostKey).Skill;
                icon.enabled = icon.sprite != null;
            }
            var passive = _player.GetPassiveSkill(e.PassiveSkillKey);
            _ui.SetText("SkillPassiveName", passive != null ? passive.DisplayName : "—");
            _ui.SetText("SkillPassiveDesc", passive != null ? passive.DisplayDescription : string.Empty);
        }

        private void RefreshCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                string key = _cardKeys[i];
                var e = _player.GetHost(key);
                bool sel = key == _hostKey;
                var frameOn = _ui.Find(card, "CardFrameSel");
                if (frameOn != null) frameOn.gameObject.SetActive(sel);
                var frameOff = _ui.Find(card, "CardFrame");
                if (frameOff != null) frameOff.gameObject.SetActive(!sel);
                var thumb = _ui.Find(card, "CardThumb")?.GetComponent<Image>();
                if (thumb != null)
                {
                    var art = ArtOf(key);
                    thumb.sprite = art.Thumb;
                    thumb.enabled = thumb.sprite != null;
                    thumb.preserveAspect = true;
                    PlaceArt(thumb.rectTransform, art.IsPortrait ? _thumbRect : _thumbRectUnit);
                }
                // 영문 몸 이름은 겹친다(코만도 넷 · 갱스터 둘) — 목록은 현지 이름으로 가른다
                TextIn(card, "CardName", e != null ? e.DisplayName : key);
                int stars = _player.StarsOf(key);
                for (int s = 0; s < 5; s++)
                {
                    var img = _ui.Find(card, $"CardStar{s}")?.GetComponent<Image>();
                    if (img != null) img.sprite = s < stars ? _starSmallOn : _starSmallOff;
                }
            }
        }

        private void RefreshPath()
        {
            int lv = _player.GhostLevel;
            int count = Mathf.Min(_player.PathCount, 5);
            var track = _ui.Find("PathTrack") as RectTransform;
            var xs = new float[count];
            for (int i = 0; i < count; i++)
            {
                var node = _ui.Find($"PathNode{i}") as RectTransform;
                xs[i] = node != null ? node.anchoredPosition.x : 0f;
            }
            if (count == 0) return;

            // 지금 레벨 자리 — 앞뒤 칸 사이를 레벨로 나눈다
            float markX;
            int next = -1;
            for (int i = 0; i < count; i++) if (lv < _player.PathLevel(i)) { next = i; break; }
            if (next == 0)
            {
                float t = Mathf.Clamp01((float)(lv - 1) / Mathf.Max(1, _player.PathLevel(0) - 1));
                markX = Mathf.Lerp(xs[0] - 40f, xs[0], t);
            }
            else if (next < 0) markX = xs[count - 1];
            else
            {
                int a = _player.PathLevel(next - 1), b = _player.PathLevel(next);
                markX = Mathf.Lerp(xs[next - 1], xs[next], (float)(lv - a) / Mathf.Max(1, b - a));
            }

            for (int i = 0; i < count; i++)
            {
                int need = _player.PathLevel(i);
                bool reached = lv >= need;
                bool claimed = _player.IsPathClaimed(i);
                var img = _ui.Get<Image>($"PathNode{i}");
                if (img != null) img.sprite = claimed ? _nodeDone : (reached || i == next) ? _nodeNext : _nodeLock;
                var label = _ui.Get<TextMeshProUGUI>($"PathLabel{i}");
                if (label != null)
                {
                    label.text = $"Lv. {need}";
                    // 받음 = 하늘색, 받을 수 있음 · 다음 목표 = 노랑, 그 뒤 = 흰색(시안)
                    label.color = claimed ? _labelDone : (reached || i == next) ? _labelNext : _labelLock;
                }
            }
            PlaceX("PathMarker", markX);
            PlaceX("PathMarkerLabel", markX);
            _ui.SetText("PathMarkerLabel", $"Lv. {lv}");
            // 표시가 칸 위에 겹치면 칸 글자와 겹친다 — 그때는 표시 글자를 끈다
            bool onNode = false;
            for (int i = 0; i < count; i++) if (Mathf.Abs(xs[i] - markX) < 50f) onNode = true;
            SetActive("PathMarkerLabel", !onNode);

            float nextX = next >= 0 ? xs[next] : xs[count - 1];
            Span("PathLineDone", xs[0], markX);
            Span("PathLineNow", markX, nextX);
            Span("PathLineLock", nextX, xs[count - 1]);

            // 아래 상자 — 받을 수 있거나 다음에 받을 보상
            int show = -1;
            for (int i = 0; i < count; i++) if (!_player.IsPathClaimed(i)) { show = i; break; }
            SetActive("PathRewards", show >= 0);
            if (show >= 0)
            {
                _player.GetPathReward(show, out int gold, out int gem, out int core);
                _ui.SetText("PathRewardGold", $"×{gold:N0}");
                _ui.SetText("PathRewardGem", $"×{gem:N0}");
                _ui.SetText("PathRewardCore", $"×{core:N0}");
            }
            _ui.SetText("BoxLvLabel", "Lv.");
            _ui.SetText("BoxLvNum", lv.ToString());
            _ui.SetText("BoxLvMax", $"/ {_player.GhostLevelMax}");
            _ui.Find("BoxLvRun")?.GetComponent<InkRun>()?.Apply();
            SetBar("BoxBarFill", _player.GhostExp, _player.GhostExpMax);
            _ui.SetText("BoxValueText", $"{_player.GhostExp:N0} / {_player.GhostExpMax:N0}");
        }

        private void PlaceX(string node, float x)
        {
            var rt = _ui.Find(node) as RectTransform;
            if (rt == null) return;
            var p = rt.anchoredPosition;
            p.x = x;
            rt.anchoredPosition = p;
        }

        /// <summary>진행 줄 한 토막 — 가운데 기준(pivot 0.5)으로 두 점 사이를 채운다.</summary>
        private void Span(string node, float from, float to)
        {
            var rt = _ui.Find(node) as RectTransform;
            if (rt == null) return;
            float w = Mathf.Max(0f, to - from);
            rt.gameObject.SetActive(w > 1f);
            var p = rt.anchoredPosition;
            p.x = (from + to) * 0.5f;
            rt.anchoredPosition = p;
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        }

        // ── 행동 ────────────────────────────────────────────────

        private void SelectHost(string key)
        {
            _hostKey = key;
            RefreshAll();
        }

        private void BuyStat(HostStat stat)
        {
            bool ghost = _page == Page.Ghost;
            int cost = ghost ? _player.GhostStatCost(stat) : _player.HostStatCost(_hostKey, stat);
            if (cost <= 0) return;
            bool ok = ghost ? _player.BuyGhostStat(stat) : _player.BuyHostStat(_hostKey, stat);
            if (!ok) { Toast(Localize.Get("ui.growth.gold_short")); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 강화 한 번마다 저장 — 앱이 꺼져도 산 것은 남는다
            RefreshGoldAndRows();
        }

        private void UpgradeMastery()
        {
            int cost = _player.MasteryCost(_hostKey);
            if (cost <= 0) return;
            var e = _player.GetHost(_hostKey);
            string name = e != null ? e.DisplayName : _hostKey;
            int have = _player.GetShards(_hostKey);
            if (have < cost)
            {
                Toast(Localize.Format("ui.hostselect.mastery.short", name, have, cost, cost - have));
                return;
            }
            int lv = _player.GetMastery(_hostKey);
            SystemPopup.Show(Localize.Format("ui.hostselect.mastery.confirm", name, $"Lv {lv} → {lv + 1}", cost), () =>
            {
                if (!_player.SpendShards(_hostKey, cost)) return;
                GameSound.Cue("run.card");
                _player.SaveAsync().Forget();   // fire-and-forget: 숙련도를 올리자마자 남긴다
                RefreshAll();
            });
        }

        private void ClaimNext()
        {
            for (int i = 0; i < _player.PathCount; i++)
            {
                if (_player.IsPathClaimed(i)) continue;
                if (_player.GhostLevel < _player.PathLevel(i))
                {
                    Toast(Localize.Format("ui.growth.path_locked", _player.PathLevel(i)));
                    return;
                }
                if (_player.ClaimPath(i))
                {
                    GameSound.Cue("run.gold");
                    Toast(Localize.Format("ui.growth.path_claimed", _player.PathLevel(i)));
                    _player.SaveAsync().Forget();   // fire-and-forget: 받은 보상은 바로 남긴다
                    RefreshAll();
                }
                return;
            }
        }

        // ── 도구 ────────────────────────────────────────────────

        /// <summary>
        /// 영문 이름의 앞부분 — 표의 영문 이름은 「GANGSTER — GUN」 처럼 무기까지 붙어 있어 카드 칸을 넘는다.
        /// 시안처럼 몸 이름만 쓴다. 같은 몸 이름이 여럿(코만도 넷)이라도 아래 현지 이름 줄이 가른다.
        /// </summary>
        private static string ShortName(HostEntry e)
        {
            string n = string.IsNullOrEmpty(e.NameEn) ? e.HostKey : e.NameEn;
            int cut = n.IndexOf(" — ", StringComparison.Ordinal);
            if (cut < 0) cut = n.IndexOf(" - ", StringComparison.Ordinal);
            return (cut > 0 ? n.Substring(0, cut) : n).Trim();
        }

        private static void PlaceArt(RectTransform rt, Rect r)
        {
            if (r.size == Vector2.zero) return;
            rt.anchoredPosition = new Vector2(r.x, -r.y);
            rt.sizeDelta = r.size;
        }

        private HostArt ArtOf(string key)
        {
            for (int i = 0; i < _hostArts.Length; i++)
                if (_hostArts[i].Key == key) return _hostArts[i];
            return default;
        }

        private void SetActive(string node, bool on)
        {
            var t = _ui.Find(node);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        private void TextIn(Transform root, string node, string value)
        {
            var t = _ui.Find(root, node)?.GetComponent<TextMeshProUGUI>();
            if (t != null && t.text != value) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);

        private static void NotifyNotReady(string label)
            => CoreModule.Get<IEventBus>().Publish(new NotImplementedFeatureEvent { FeatureLabel = label });
    }
}
