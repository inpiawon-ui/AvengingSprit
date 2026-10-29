using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 호스트 성장 — 탭 없는 한 화면(시안 Screens/growth.png, PD 2026-10-23).
    /// 히어로 판(몸 · 등급 · 상성 · 사거리 · 전투력 · 별) → 스킬 3칸 → 스킬 레벨 3칸 + 인연 짝 칸 → 성급 판 · 성급 올리기.
    /// 육성 개편(PD 2026-10-29) — 호스트는 **스킬**로 키운다. 능력치 강화 8칸은 없앴다(스탯은 유령 탭).
    ///   성급이 스킬 레벨 상한을 열고(액티브 = 성급 · 패시브 · 필살기 = 1 + 성급 ÷ 2), 레벨은 골드로 셋을 따로 올린다.
    /// 칸은 옛 능력치 칸 본보기(`GrwStatTemplate`)를 그대로 복제해 쓴다 — 새 그림 없음.
    /// </summary>
    public sealed class RbGrowthPage : MonoBehaviour
    {
        [Serializable]
        private struct SkillArt
        {
            public string Key;
            public Sprite Ult, Active, Passive;
        }

        [SerializeField] private SkillArt[] _arts = Array.Empty<SkillArt>();
        /// <summary>`HostStat` 순서의 아이콘.</summary>
        [SerializeField] private Sprite[] _statIcons = Array.Empty<Sprite>();
        [SerializeField] private Sprite _rankGold, _rankSilver;   // 성급 별 — 큰 금별(5성마다) · 작은 은별(RbHostPage.Stars)
        [SerializeField] private Sprite _tileNormal, _tileMax, _tilePoor;
        [SerializeField] private Sprite _btnReady, _btnShort;
        [SerializeField] private Sprite[] _stageBgs = Array.Empty<Sprite>();

        // 칸 순서 — 액티브 · 패시브 / 필살기 · 인연 짝. 앞 셋은 `HostSkillSlot` 그대로
        private static readonly HostSkillSlot[] SkillOrder = { HostSkillSlot.Active, HostSkillSlot.Passive, HostSkillSlot.Ult };
        private const int TileCount = 4, InyeonTile = 3;
        private const float ColPitch = (550f - 25f) * 2f / 3f, RowPitch = 162.6f * 2f / 3f;
        private const string NextColor = "#4AE08A", ShortColor = "#FF5A5A";

        // 창은 위 막대 · 아래 탭까지 딤으로 덮어야 해서 이 쪽이 아니라 화면 맨 뒤(RbLobbyUI 바로 아래)에 있다
        // 뒤로는 앞(위에 뜬) 것부터 — 정수 교환은 획득처 위에서 열린다
        private static readonly string[] Modals = { "GrwEssence", "GrwSources", "GrwSkillInfo", "GrwStarDone", "GrwStarAsk" };
        private const int EssenceMaxPick = 99;

        private UIBinder _ui;
        private UIBinder _modals;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private readonly List<Transform> _tiles = new();
        private string _key;
        private int _essCount = 1;
        private int _doneFromRank = -1, _doneFromCap = -1;

        private void Awake() => Init();

        /// <summary>버튼을 건다 — 한 번만. 화면(RbLobbyUI)이 먼저 부른다(이 쪽이 꺼져 있어도 창은 열린다).</summary>
        public void Init()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("GrwBack", () => _shell.SelectTab("Host"));
            _ui.OnClick("GrwHelp", () => _shell.ShowGuide("growth"));   // 시안 guide
            _ui.OnClick("GrwSkillUlt", () => ShowSkill(0));
            _ui.OnClick("GrwSkillAct", () => ShowSkill(1));
            _ui.OnClick("GrwSkillPas", () => ShowSkill(2));
            _ui.OnClick("GrwStarWhere", ShowSources);
            _ui.OnClick("GrwStarUp", StarUp);
            _modals = new UIBinder(_shell.transform);
            _modals.OnClick("GrwStarAskClose", () => _modals.SetActive("GrwStarAsk", false));
            _modals.OnClick("GrwStarAskNo", () => _modals.SetActive("GrwStarAsk", false));
            _modals.OnClick("GrwStarAskYes", ConfirmStarUp);
            _modals.OnClick("GrwStarDoneClose", () => _modals.SetActive("GrwStarDone", false));
            _modals.OnClick("GrwStarDoneOk", () => _modals.SetActive("GrwStarDone", false));
            _modals.OnClick("GrwStarDoneHelp", () => _shell.ShowGuide("growth"));
            _modals.OnClick("GrwSkillInfoClose", () => _modals.SetActive("GrwSkillInfo", false));
            _modals.OnClick("GrwSourcesClose", () => _modals.SetActive("GrwSources", false));
            _modals.OnClick("GrwSrc0Go", () => Leave(() => _shell.SelectTab("Summon")));
            _modals.OnClick("GrwSrc1Go", () => Leave(() => _shell.OpenChallenge("ChlTower")));
            _modals.OnClick("GrwSrc2Go", () => Leave(() => _shell.OpenChallenge("ChlNight")));
            _modals.OnClick("GrwSrc3Go", () => Leave(() => _shell.SelectTab("Battle")));
            _modals.OnClick("GrwSrc4Go", ShowEssence);
            _modals.OnClick("GrwEssenceClose", () => _modals.SetActive("GrwEssence", false));
            _modals.OnClick("GrwEssMinus", () => { _essCount = Mathf.Max(1, _essCount - 1); FillEssence(); });
            _modals.OnClick("GrwEssPlus", () => { _essCount = Mathf.Min(EssencePickMax(), _essCount + 1); FillEssence(); });
            _modals.OnClick("GrwEssGo", Exchange);
            BuildTiles();
        }

        public void Open(IPlayerDataService player, string hostKey)
        {
            _player = player;
            _key = hostKey;
            gameObject.SetActive(true);
            if (_ui.Find("GrwBody") is RectTransform view && view.TryGetComponent<ScrollRect>(out var sr)) sr.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public string HostKey => _key;

        private void BuildTiles()
        {
            var tpl = _ui.Find("GrwStatTemplate");
            if (tpl == null) return;
            var start = ((RectTransform)tpl).anchoredPosition;
            for (int i = 0; i < TileCount; i++)
            {
                var t = i == 0 ? tpl : Instantiate(tpl.gameObject, tpl.parent).transform;
                t.name = $"GrwStat{i}";
                ((RectTransform)t).anchoredPosition = start + new Vector2(ColPitch * (i % 2), -RowPitch * (i / 2));
                if (i < SkillOrder.Length)
                {
                    var slot = SkillOrder[i];
                    _ui.Find(t, "GrwStatBuy")?.GetComponent<Button>()?.onClick.AddListener(() => Buy(slot));
                }
                // 이름 · 값 줄은 능력치보다 길다(「액티브 Lv 3/6」 · 「Lv5 추가 옵션」, 3언어) — 칸 안에서 줄어들게 하한을 낮춘다
                foreach (var n in new[] { "GrwStatName", "GrwStatValue" })
                    if (_ui.Find(t, n)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI tx) tx.fontSizeMin = 12f;
                _tiles.Add(t);
            }
            _ui.Find(_tiles[InyeonTile], "GrwStatBuy")?.gameObject.SetActive(false);
            // 인연 칸은 버튼이 없다 — 이름 · 값 줄을 버튼 자리(오른쪽 끝)까지 늘린다(영어 「Bond · Unyielding Roar」 가 길다)
            float widen = 146f * 2f / 3f;   // 1080 기준 372 → 518
            foreach (var n in new[] { "GrwStatName", "GrwStatValue" })
                if (_ui.Find(_tiles[InyeonTile], n) is RectTransform r)
                {
                    r.sizeDelta += new Vector2(widen, 0f);
                    r.anchoredPosition += new Vector2(widen * r.pivot.x, 0f);   // 왼쪽 끝은 그대로
                }
            // 머리 · 안내 글자도 스킬 쪽으로(프리팹에 박힌 글자라 언어 키를 바꿔 끼운다)
            if (_ui.Find("GrwStatHeader")?.GetComponent<LocalizedText>() is LocalizedText head) head.SetKey("ui.rb.growth.skill_header");
            if (_ui.Find("GrwStatHint")?.GetComponent<LocalizedText>() is LocalizedText hint) hint.SetKey("ui.rb.growth.skill_hint");
        }

        // ── 칠하기 ──────────────────────────────────────────────

        public void Refresh()
        {
            if (_player == null || !_player.IsReady || string.IsNullOrEmpty(_key)) return;
            var e = _player.GetHost(_key);
            if (e == null) return;
            int m = _player.GetMastery(_key);
            RefreshHero(e, m);
            RefreshSkills(e);
            RefreshSkillLevels(e, m);
            RefreshInyeon();
            RefreshStar(m);
        }

        private void RefreshHero(HostEntry e, int m)
        {
            bool owned = m >= 1;
            var tint = owned ? Color.white : new Color(0.35f, 0.37f, 0.45f, 1f);
            if (_ui.Get<Image>("GrwHostArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.SetShadow(true);   // 발판 그림 대신 발밑 검은 그림자(PD 2026-10-26)
                show.Show(_key, tint);
            }
            if (_ui.Get<Image>("GrwHeroStage") is Image stage && _stageBgs.Length > 0)
                stage.sprite = _stageBgs[(Mathf.Max(1, _player.SelectedChapter) - 1) % _stageBgs.Length];
            _ui.SetText("GrwName", e.DisplayName);

            var kind = AffinityRule.KindOf(_key);
            string job = HostStats.JobIndex(e) switch
            {
                HostStats.JobMelee => "ui.job.melee",
                HostStats.JobMid => "ui.job.mid",
                _ => "ui.job.ranged",
            };
            string[] tags =
            {
                HostGradeLabel.Of(e.Grade),
                kind == Affinity.None ? null : Localize.Get($"ui.affinity.{kind.ToString().ToLowerInvariant()}"),
                Localize.Get(job),
            };
            LayoutTags(tags);

            _ui.SetText("GrwPowerValue", _player.PowerOf(_key).ToString("N0"));
            RbHostPage.Stars(_ui.Find("GrwStars"), "GrwStar", m, _rankGold, _rankSilver);
        }

        /// <summary>「R  무기  원거리」 칩 — 글자 폭만큼 칩을 늘려 이름 가운데 아래에 나란히 세운다(언어마다 길이가 다르다).</summary>
        private void LayoutTags(string[] tags)
        {
            const float Pad = 16f, Gap = 8f;
            var widths = new float[tags.Length];
            float total = 0f;
            int n = 0;
            for (int i = 0; i < tags.Length; i++)
            {
                var chip = _ui.Find($"GrwTag{i}") as RectTransform;
                bool on = chip != null && !string.IsNullOrEmpty(tags[i]);
                if (chip != null) chip.gameObject.SetActive(on);
                if (!on) continue;
                var t = _ui.Get<TextMeshProUGUI>($"GrwTag{i}Text");
                t.text = tags[i];
                widths[i] = t.GetPreferredValues(tags[i]).x + Pad * 2f;
                total += widths[i];
                n++;
            }
            total += Gap * Mathf.Max(0, n - 1);
            var name = _ui.Find("GrwName") as RectTransform;
            float x = (name != null ? name.anchoredPosition.x : 0f) - total * 0.5f;
            for (int i = 0; i < tags.Length; i++)
            {
                if (widths[i] <= 0f || !(_ui.Find($"GrwTag{i}") is RectTransform chip)) continue;
                chip.sizeDelta = new Vector2(widths[i], chip.sizeDelta.y);
                chip.anchoredPosition = new Vector2(x + widths[i] * 0.5f, chip.anchoredPosition.y);
                x += widths[i] + Gap;
            }
        }

        private void RefreshSkills(HostEntry e)
        {
            var art = ArtOf(_key);
            SetIcon("GrwSkillUltIcon", art.Ult);
            SetIcon("GrwSkillActIcon", art.Active);
            SetIcon("GrwSkillPasIcon", art.Passive);
            _ui.SetText("GrwSkillUltName", Localize.Get($"ult.{e.HostKey}.name"));
            var active = _player.GetActiveSkill(e.ActiveSkillKey);
            _ui.SetText("GrwSkillActName", active != null ? (string.IsNullOrEmpty(active.DisplayName) ? active.NameEn : active.DisplayName) : "—");
            var passive = _player.GetPassiveSkill(e.PassiveSkillKey);
            _ui.SetText("GrwSkillPasName", passive != null ? passive.DisplayName : "—");
        }

        private void RefreshSkillLevels(HostEntry e, int m)
        {
            bool locked = m < 1;
            var art = ArtOf(_key);
            Sprite[] icons = { art.Active, art.Passive, art.Ult };
            string[] kinds = { "ui.rb.growth.act", "ui.rb.growth.pas", "ui.rb.growth.ult" };
            string[] kindKo = { "액티브", "패시브", "필살기" };
            for (int i = 0; i < SkillOrder.Length && i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                var slot = SkillOrder[i];
                int lv = _player.SkillLevel(_key, slot), cap = _player.SkillLevelCap(_key, slot), max = _player.SkillLevelMax(slot);
                int cost = _player.SkillLevelCost(_key, slot);
                bool full = lv >= max, capped = !locked && !full && cost <= 0;
                bool poor = !locked && cost > 0 && _player.Gold < cost;
                if (_ui.Find(t, "GrwStatIcon")?.GetComponent<Image>() is Image icon && icons[i] != null)
                {
                    icon.sprite = icons[i];
                    icon.preserveAspect = true;
                }
                TextIn(t, "GrwStatName", string.Format(Localize.FromTable("ui.rb.growth.skill_lv", "{0} Lv {1}/{2}"),
                                                       Localize.FromTable(kinds[i], kindKo[i]), lv, locked ? max : cap));
                TextIn(t, "GrwStatValue", SkillLine(e, slot, lv, cap, max, locked));
                if (t.GetComponent<Image>() is Image bg) bg.sprite = full ? _tileMax : poor ? _tilePoor : _tileNormal;
                var buy = _ui.Find(t, "GrwStatBuy");
                bool showBuy = locked || cost > 0;
                if (buy != null) buy.gameObject.SetActive(showBuy && !capped);
                if (buy != null && buy.GetComponent<Button>() is Button b) b.interactable = !locked;
                TextIn(t, "GrwStatBuyCost", locked ? Localize.Get("ui.growth.locked") : cost.ToString("N0"));
                if (_ui.Find(t, "GrwStatBuyCost")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct)
                    ct.color = poor ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.79f, 0.24f);   // 모자람 빨강 · 평소 금색
                _ui.Find(t, "GrwStatBuyCoin")?.gameObject.SetActive(!locked);
                FitPrice(buy as RectTransform, _ui.Find(t, "GrwStatBuyCoin") as RectTransform,
                         _ui.Find(t, "GrwStatBuyCost")?.GetComponent<TextMeshProUGUI>(), !locked);
            }
        }

        /// <summary>
        /// 칸 아래 줄 — 다음 마디가 오면 마디를(Lv5 추가 옵션 · Lv10 인연 강화 · Lv5 강화형), 아니면 「지금 › 다음」 위력.
        /// 상한에 막혔으면 무엇이 필요한지(성급 n).
        /// </summary>
        private string SkillLine(HostEntry e, HostSkillSlot slot, int lv, int cap, int max, bool locked)
        {
            var c = _player.Config;
            if (lv >= max) return "MAX";
            int next = lv + 1;
            if (!locked && lv >= cap)
            {
                int needRank = slot == HostSkillSlot.Active ? next : next * 2 - 2;   // 패시브 · 필살기 상한 = 1 + 성급 ÷ 2
                return string.Format(Localize.FromTable("ui.rb.growth.skill_need_rank", "{0}성 필요"), needRank);
            }
            if (slot == HostSkillSlot.Active)
            {
                if (next == SkillScaling.SpecLevel) return $"<color={NextColor}>{Localize.Format("ui.rb.growth.node_opt", next)}</color>";
                if (c != null && next == c.ActiveInyeonLevel) return $"<color={NextColor}>{Localize.Format("ui.rb.growth.node_inyeon", next)}</color>";
                float per = c != null ? c.ActivePowerPerLevel : 0.06f;
                return PowerLine(per * (lv - 1), per * (next - 1));
            }
            if (slot == HostSkillSlot.Ult)
            {
                if (c != null && next == c.UltEnhancedLevel) return $"<color={NextColor}>{Localize.Format("ui.rb.growth.node_ult", next)}</color>";
                float per = c != null ? c.UltPowerPerLevel : 0.08f;
                return PowerLine(per * (lv - 1), per * (next - 1));
            }
            // 패시브 — 대표 수치(%)가 레벨을 따라 오른다. 오르지 않는 패시브면 레벨만
            var p = _player.GetPassiveSkill(e.PassiveSkillKey);
            if (p == null || !p.HasGrowth) return $"Lv {lv}<size=70%> › </size><color={NextColor}>Lv {next}</color>";
            return $"{p.GrowthAt(lv, max):0.#}%<size=70%> › </size><color={NextColor}>{p.GrowthAt(next, max):0.#}%</color>";
        }

        private static string PowerLine(float now, float next)
            => $"+{now * 100f:0.#}%<size=70%> › </size><color={NextColor}>+{next * 100f:0.#}%</color>";

        /// <summary>인연 짝 칸 — 짝 얼굴 · 짝 이름과 성급 · 지금 세기(두 성급 중 낮은 쪽).</summary>
        private void RefreshInyeon()
        {
            if (_tiles.Count <= InyeonTile) return;
            var t = _tiles[InyeonTile];
            string name = BattleDirector.InyeonName(_key);
            var partners = BattleDirector.InyeonPartners(_key);
            t.gameObject.SetActive(!string.IsNullOrEmpty(name) && partners.Count > 0);
            if (!t.gameObject.activeSelf) return;
            var cfg = _player.Config;
            // 짝이 둘이면 성급이 높은 짝을 보여 주고 「+1」 — 둘을 다 적으면 칸에 안 들어간다(3언어)
            int mine = _player.GetMastery(_key), bestAt = 0;
            for (int i = 1; i < partners.Count; i++)
                if (_player.GetMastery(partners[i]) > _player.GetMastery(partners[bestAt])) bestAt = i;
            string partner = partners[bestAt];
            int pm = _player.GetMastery(partner);
            if (_ui.Find(t, "GrwStatIcon")?.GetComponent<Image>() is Image icon && _shell.Face(partner) is Sprite face)
            {
                icon.sprite = face;
                icon.preserveAspect = true;
            }
            var ph = _player.GetHost(partner);
            string line = $"{(ph != null ? ph.DisplayName : partner)} ★{pm}" + (partners.Count > 1 ? $" <size=75%>+{partners.Count - 1}</size>" : string.Empty);
            int tier = cfg != null ? cfg.InyeonTierOf(Mathf.Min(mine, pm)) : 1;
            TextIn(t, "GrwStatName", string.Format(Localize.FromTable("ui.rb.growth.inyeon_tile", "인연 · {0} <size=75%><color=#B3BDD6>{1}단</color></size>"),
                                                   Localize.FromTable($"inyeon.{_key}.name", name), tier));
            TextIn(t, "GrwStatValue", line);
        }

        // 금화 + 값 묶음을 버튼 가운데로 — 값이 「1,200」 처럼 길면 글자를 줄인다(겹쳐서 금화를 덮었다, 2026-10-26)
        private static void FitPrice(RectTransform button, RectTransform coin, TextMeshProUGUI cost, bool withCoin)
        {
            if (button == null || cost == null) return;
            const float gap = 3f, pad = 6f;
            float cw = withCoin && coin != null ? coin.sizeDelta.x : 0f;
            float avail = button.rect.width - pad * 2f - (cw > 0f ? cw + gap : 0f);
            cost.enableAutoSizing = false;
            cost.fontSize = cost.fontSizeMax;
            float w = cost.GetPreferredValues(cost.text).x;
            if (w > avail && w > 0f)
            {
                cost.fontSize = cost.fontSizeMax * avail / w;
                w = cost.GetPreferredValues(cost.text).x;
            }
            float left = -(w + (cw > 0f ? cw + gap : 0f)) * 0.5f;
            if (cw > 0f) coin.anchoredPosition = new Vector2(left + cw * 0.5f, coin.anchoredPosition.y);
            var tr = cost.rectTransform;
            tr.sizeDelta = new Vector2(w + 2f, tr.sizeDelta.y);
            tr.anchoredPosition = new Vector2(left + (cw > 0f ? cw + gap : 0f) + tr.sizeDelta.x * 0.5f, tr.anchoredPosition.y);
            cost.alignment = TextAlignmentOptions.Left;
        }

        private void RefreshStar(int m)
        {
            int max = _player.MasteryMax;
            bool maxed = m >= max;
            int shards = _player.GetShards(_key), need = _player.MasteryCost(_key);
            int gold = _player.StarUpGoldCost(_key);
            _ui.SetActive("GrwStarWhere", !maxed);
            _ui.SetActive("GrwStarBar", !maxed);
            _ui.SetText("GrwStarRank", maxed ? string.Format(Localize.FromTable("ui.rb.growth.rank_max", "성급 {0} · 최대"), m)
                                     : m < 1 ? Localize.FromTable("ui.rb.growth.unlock_rank", "해금 › 1성")
                                     : string.Format(Localize.FromTable("ui.rb.growth.rank", "성급 {0} <size=70%>›</size> {1}"), m, m + 1));
            if (_ui.Find("GrwStarBarFill") is RectTransform fill)
                fill.anchorMax = new Vector2(maxed ? 1f : Mathf.Clamp01(need > 0 ? (float)shards / need : 0f), 1f);
            _ui.SetText("GrwStarInfo", maxed ? Localize.FromTable("ui.rb.growth.max_info", "남는 조각은 영혼 정수로 바뀌어요")
                      : string.Format(Localize.FromTable("ui.rb.growth.star_info", "{0}/{1} · <color=#4AE08A>체력·공격 +7%</color>"), shards, need));

            bool ready = !maxed && shards >= need && _player.Gold >= gold;
            if (_ui.Get<Image>("GrwStarUp") is Image btn) btn.sprite = ready ? _btnReady : _btnShort;
            if (_ui.Get<Button>("GrwStarUp") is Button b)
            {
                // 그림 바꾸기(누름 그림)는 바탕 그림에 맞춰 짝을 바꾼다 — 노란 버튼에 회색 누름 그림이 뜨지 않게
                b.transition = Selectable.Transition.ColorTint;
                b.interactable = !maxed;
            }
            var label = _ui.Get<TextMeshProUGUI>("GrwStarUpLabel");
            var sub = _ui.Get<TextMeshProUGUI>("GrwStarUpSub");
            if (label != null)
            {
                label.text = maxed ? Localize.Get("ui.growth.max")
                           : m < 1 ? Localize.FromTable("ui.rb.growth.unlock", "해금하기")
                           : Localize.FromTable("ui.rb.growth.starup", "성급 올리기");
                label.color = ready ? new Color(0.16f, 0.11f, 0.03f) : Color.white;
            }
            if (sub != null)
            {
                sub.text = maxed ? Localize.FromTable("ui.rb.growth.max_sub", "최대 성급")
                         : shards < need ? string.Format(Localize.FromTable("ui.rb.growth.shard_short", "조각 {0}개 부족"), need - shards)
                         : _player.Gold < gold ? string.Format(Localize.FromTable("ui.rb.growth.gold_short", "골드 {0:N0} 부족"), gold - _player.Gold)
                         : string.Format(Localize.FromTable("ui.rb.growth.cost", "골드 {0:N0}"), gold);
                sub.color = ready ? new Color(0.16f, 0.11f, 0.03f) : new Color(0.70f, 0.74f, 0.84f);
            }
        }

        // ── 행동 ────────────────────────────────────────────────

        private void Buy(HostSkillSlot slot)
        {
            int cost = _player.SkillLevelCost(_key, slot);
            if (cost <= 0) return;
            int powerBefore = _player.PowerOf(_key);
            if (!_player.BuySkillLevel(_key, slot)) { _shell.Notice.ShowGoldShort(cost, _player.Gold, null, null, null); return; }
            int powerGain = _player.PowerOf(_key) - powerBefore;
            if (powerGain > 0) PowerUpAsync(powerGain).Forget();   // fire-and-forget: 띠가 1.2초 뒤 스스로 걷힌다
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 올릴 때마다 저장 — 앱이 꺼져도 산 것은 남는다
            Refresh();
            _shell.RefreshHeader();
            // 올리기 성공 — 그 칸 아래 금빛 용암이 살짝 차오르고 불티 · 값이 톡(길게 누르면 차오른 채 머문다)
            int idx = Array.IndexOf(SkillOrder, slot);
            if (_shell.Fx != null && idx >= 0 && idx < _tiles.Count && _tiles[idx] is RectTransform tile)
                _shell.Fx.UpgradeTick(tile, _ui.Find(tile, "GrwStatValue") as RectTransform);
            // 액티브 Lv5 — 추가 옵션이 열렸다(예전 6성 각성 자리)
            if (slot == HostSkillSlot.Active && _player.SkillLevel(_key, slot) == SkillScaling.SpecLevel)
            {
                string opt = Localize.Get($"askill.{_key}.opt");
                if (opt != $"askill.{_key}.opt")
                    Toast(Localize.Format("ui.rb.growth.opt_lv_open", SkillScaling.SpecLevel) + "\n" + opt);
            }
        }

        // 「전투력 +n」 띠(시안 growth-success) — 톡 튀어나와 머물다 위로 사라진다. 길게 눌러 연속 강화하면 숫자만 갈아 끼운다
        private int _powerUpId;

        private async UniTaskVoid PowerUpAsync(int gain)
        {
            if (_ui.Find("GrwPowerUp") is not RectTransform up) return;
            int id = ++_powerUpId;
            _ui.SetText("GrwPowerUpText", string.Format(Localize.FromTable("ui.rb.growth.power_plus", "전투력 +{0}"), gain.ToString("N0")));
            up.gameObject.SetActive(true);
            var home = up.anchoredPosition;
            if (!up.TryGetComponent<CanvasGroup>(out var group)) group = up.gameObject.AddComponent<CanvasGroup>();
            var token = this.GetCancellationTokenOnDestroy();
            const float total = 1.2f;
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                if (id != _powerUpId) { up.anchoredPosition = home; return; }
                up.localScale = Vector3.one * (t < 0.2f ? Ease.OutBack(t / 0.2f) : 1f);
                float fade = t < total - 0.3f ? 0f : (t - (total - 0.3f)) / 0.3f;
                group.alpha = 1f - fade;
                up.anchoredPosition = home + new Vector2(0f, 18f * fade);
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            up.anchoredPosition = home;
            up.gameObject.SetActive(false);
        }

        private void StarUp()
        {
            int m = _player.GetMastery(_key);
            if (m >= _player.MasteryMax) { Toast(Localize.Get("ui.starup.maxed")); return; }
            int shards = _player.GetShards(_key), need = _player.MasteryCost(_key);
            int gold = _player.StarUpGoldCost(_key);
            // 모자라면 얼마나 모자란지 숫자로 — 「부족합니다」 만으로는 몇 판을 더 돌아야 하는지 모른다
            if (shards < need) { Toast(Localize.Format("ui.starup.shard_short", shards, need, need - shards)); return; }
            if (_player.Gold < gold) { _shell.Notice.ShowGoldShort(gold, _player.Gold, null, null, null); return; }
            ShowStarAsk(m, need, gold);
        }

        private void ShowStarAsk(int m, int need, int gold)
        {
            var host = _player.GetHost(_key);
            string name = host != null ? host.DisplayName : _key;
            SetFace("GrwStarAskFace");
            SetFace("GrwStarAskShardFace");
            _modals.SetText("GrwStarAskLine", string.Format(Localize.FromTable("ui.rb.growth.starup_line", "{0} 성급 {1} → {2}"), name, m, m + 1));
            _modals.SetText("GrwStarAskShardCount", need.ToString());
            _modals.SetText("GrwStarAskShardName", string.Format(Localize.FromTable("ui.rb.growth.shard_name", "{0} 조각"), name));
            _modals.SetText("GrwStarAskGoldCount", gold.ToString("N0"));
            _modals.SetText("GrwStarAskGain", Localize.FromTable("ui.rb.growth.star_gain", "체력·공격 +7%"));
            _modals.SetActive("GrwStarAsk", true);
        }

        private void ConfirmStarUp()
        {
            _modals.SetActive("GrwStarAsk", false);
            // 성공 화면의 「2성 → 3성 · 강화 상한 20 → 30」 — 올리기 전 값을 쥐어 둔다
            _doneFromRank = _player.GetMastery(_key);
            _doneFromCap = _player.SkillLevelCap(_key, HostSkillSlot.Active);
            if (!_player.TryStarUp(_key)) return;
            GameSound.Cue("run.card");
            _player.SaveAsync().Forget();   // fire-and-forget: 성급을 올리자마자 남긴다
            Refresh();
            _shell.RefreshHeader();
            // 각성(추가 옵션)은 성급이 아니라 액티브 Lv5 로 옮겼다(2026-10-29) — 성급은 늘 같은 성공 화면
            ShowStarDone();
        }

        // 성급 강화 성공(시안 star-success-detail, 전체 화면) — 성급은 스킬 레벨 **상한**을 연다(레벨은 골드로)
        private void ShowStarDone()
        {
            int m = _player.GetMastery(_key);
            int from = _doneFromRank >= 0 ? _doneFromRank : Mathf.Max(0, m - 1);
            int cap = _player.SkillLevelCap(_key, HostSkillSlot.Active);
            int capFrom = _doneFromCap >= 0 ? _doneFromCap : cap;
            _doneFromRank = _doneFromCap = -1;
            var host = _player.GetHost(_key);
            if (_modals.Get<Image>("GrwStarDoneArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.Show(_key, Color.white);
            }
            _modals.SetText("GrwStarDoneName", host != null ? host.DisplayName : _key);
            RbHostPage.Stars(_modals.Find("GrwStarDoneStars"), "GrwStarDoneStar", m, _rankGold, _rankSilver);
            _modals.SetText("GrwStarDoneGain", Localize.FromTable("ui.rb.growth.star_gain", "체력·공격 +7%"));
            _modals.SetText("GrwStarDoneRank", string.Format(Localize.FromTable("ui.rb.growth.done_line1", "{0}성 → {1}성 · 체력·공격 +7%"), from, m));
            _modals.SetText("GrwStarDoneLine2", string.Format(Localize.FromTable("ui.rb.growth.done_line2b", "스킬 상한 · 액티브 {0}→{1} · 패시브·필살기 {2}→{3}"),
                                                              capFrom, cap, 1 + from / 2, 1 + m / 2));
            _modals.SetActive("GrwStarDone", true);
            // 새로 찬 별(1~5성은 m-1 째, 6~10성은 각성 별로 다시 m-6 째)이 작게 터진다
            if (_shell.Fx != null && m > 0 && _modals.Find(_modals.Find("GrwStarDoneStars"), $"GrwStarDoneStar{(m - 1) % 5}") is RectTransform star)
                _shell.Fx.StarFill(star);
        }

        private void ShowSkill(int slot)
        {
            var e = _player != null ? _player.GetHost(_key) : null;
            if (e == null) return;
            var art = ArtOf(e.HostKey);
            string kind, name, desc;
            Sprite icon;
            switch (slot)
            {
                case 0:
                    kind = Localize.FromTable("ui.rb.growth.ult", "필살기");
                    name = Localize.Get($"ult.{e.HostKey}.name");
                    desc = Localize.Get($"ult.{e.HostKey}.desc");
                    icon = art.Ult;
                    break;
                case 1:
                    if (_player.GetActiveSkill(e.ActiveSkillKey) is not ActiveSkillEntry a) return;
                    kind = Localize.FromTable("ui.rb.growth.act", "액티브");
                    name = string.IsNullOrEmpty(a.DisplayName) ? a.NameEn : a.DisplayName;
                    desc = a.DisplayDescription;
                    icon = art.Active;
                    break;
                default:
                    if (_player.GetPassiveSkill(e.PassiveSkillKey) is not PassiveSkillEntry p) return;
                    kind = Localize.FromTable("ui.rb.growth.pas", "패시브");
                    name = p.DisplayName;
                    desc = p.DisplayDescription;
                    icon = art.Passive;
                    break;
            }
            if (_modals.Get<Image>("GrwSkillInfoIcon") is Image img && icon != null) img.sprite = icon;
            _modals.SetText("GrwSkillInfoKind", kind);
            _modals.SetText("GrwSkillInfoName", name);
            _modals.SetText("GrwSkillInfoDesc", desc);
            // 추가 옵션 — 액티브에만 있다(액티브 Lv5 에 열린다). 없으면 창을 줄인다
            string opt = slot == 1 ? Localize.Get($"askill.{e.HostKey}.opt") : null;
            bool hasOpt = !string.IsNullOrEmpty(opt) && opt != $"askill.{e.HostKey}.opt";
            _modals.SetActive("GrwSkillInfoAwakenTitle", hasOpt);
            _modals.SetActive("GrwSkillInfoAwaken", hasOpt);
            if (hasOpt)
            {
                // 액티브 Lv5 에 열린다(2026-10-29 — 예전 6성) · Lv10 은 인연이 켜지면 한 단계 더
                int lv = _player.SkillLevel(e.HostKey, HostSkillSlot.Active);
                bool open = lv >= SkillScaling.SpecLevel;
                int inyeonLv = _player.Config != null ? _player.Config.ActiveInyeonLevel : 10;
                _modals.SetText("GrwSkillInfoAwakenTitle", lv >= inyeonLv ? Localize.Format("ui.rb.growth.opt_lv_max", inyeonLv)
                    : open ? Localize.Format("ui.rb.growth.opt_lv_open", SkillScaling.SpecLevel)
                    : Localize.Format("ui.rb.growth.opt_lv_locked", SkillScaling.SpecLevel));
                _modals.SetText("GrwSkillInfoAwaken", opt);
                if (_modals.Get<TextMeshProUGUI>("GrwSkillInfoAwaken") is TextMeshProUGUI ot) ot.color = open ? new Color(0.29f, 0.88f, 0.54f) : new Color(0.70f, 0.74f, 0.84f);
            }
            if (_modals.Find("GrwSkillInfoBox") is RectTransform box)
            {
                float h = hasOpt ? 410f : 310f;   // 판 안 위 430 부터
                box.sizeDelta = new Vector2(box.sizeDelta.x, h);
                box.anchoredPosition = new Vector2(box.anchoredPosition.x, -(430f + h * 0.5f));
            }
            _modals.SetActive("GrwSkillInfo", true);
        }

        /// <summary>탭을 옮길 때 — 창은 이 쪽 밖(화면 맨 뒤)에 있어 쪽을 꺼도 안 닫힌다.</summary>
        public void CloseModals()
        {
            if (_modals == null) return;
            foreach (var n in Modals) _modals.SetActive(n, false);
        }

        /// <summary>뒤로 — 열린 창이 있으면 그것만 닫는다.</summary>
        public bool CloseModal()
        {
            if (_modals == null) return false;
            foreach (var n in Modals)
            {
                if (_modals.Find(n) is Transform t && t.gameObject.activeSelf) { t.gameObject.SetActive(false); return true; }
            }
            return false;
        }

        // ── 조각 획득처 · 정수 교환 ─────────────────────────────

        private string HostName => _player.GetHost(_key) is HostEntry h ? h.DisplayName : _key;

        /// <summary>미보유 쪽에서 — 그 몸의 조각 획득처.</summary>
        public void ShowSourcesFor(IPlayerDataService player, string hostKey)
        {
            Init();
            _player = player;
            _key = hostKey;
            ShowSources();
        }

        private void ShowSources()
        {
            if (_player == null) return;
            int shards = _player.GetShards(_key), need = _player.MasteryCost(_key);
            _modals.SetText("GrwSourcesName", string.Format(Localize.FromTable("ui.rb.growth.shard_name", "{0} 조각"), HostName));
            _modals.SetText("GrwSourcesOwned", need > 0
                ? string.Format(Localize.FromTable("ui.rb.src.owned", "보유 {0} /{1}"), shards, need)
                : string.Format(Localize.FromTable("ui.rb.src.owned_max", "보유 {0}"), shards));
            var cfg = _player.Config;
            _modals.SetText("GrwSrc3Sub", string.Format(Localize.FromTable("ui.rb.src.random_sub", "클리어 보너스 조각 +{0}"), cfg != null ? cfg.RandomLuckShards : 3));
            _modals.SetText("GrwSrc4Sub", string.Format(Localize.FromTable("ui.rb.src.essence_sub", "정수 {0}개로 조각 1개"), _player.EssencePriceOf(_key)));
            _modals.SetActive("GrwSources", true);
        }

        /// <summary>다른 화면으로 — 창을 다 닫고 간다(창은 쪽 밖에 있어 남는다).</summary>
        private void Leave(Action go)
        {
            CloseModals();
            go();
        }

        private void ShowEssence()
        {
            _essCount = 1;
            SetFace("GrwEssFace");
            _modals.SetText("GrwEssShardName", string.Format(Localize.FromTable("ui.rb.growth.shard_name", "{0} 조각"), HostName));
            FillEssence();
            _modals.SetActive("GrwEssence", true);
        }

        private int EssencePickMax()
        {
            int price = Mathf.Max(1, _player.EssencePriceOf(_key));
            return Mathf.Clamp(_player.Essence / price, 1, EssenceMaxPick);
        }

        private void FillEssence()
        {
            int price = _player.EssencePriceOf(_key), have = _player.Essence;
            int cost = price * _essCount;
            bool poor = have < cost;
            _modals.SetText("GrwEssRate", string.Format(Localize.FromTable("ui.rb.src.essence_sub", "정수 {0}개로 조각 1개"), price));
            _modals.SetText("GrwEssCount", _essCount.ToString());
            _modals.SetText("GrwEssNeed", poor
                ? string.Format(Localize.FromTable("ui.rb.ess.poor", "정수가 부족합니다 ({0}/{1})"), have, cost)
                : string.Format(Localize.FromTable("ui.rb.ess.need", "필요 정수 {0} · 보유 {1}"), cost, have));
            if (_modals.Get<TextMeshProUGUI>("GrwEssNeed") is TextMeshProUGUI need) need.color = poor ? new Color(1f, 0.35f, 0.35f) : new Color(0.70f, 0.74f, 0.84f);
            if (_modals.Get<Button>("GrwEssGo") is Button b) b.interactable = !poor;
            _modals.SetText("GrwEssGoText", poor
                ? string.Format(Localize.FromTable("ui.rb.ess.short", "정수 {0}개 부족"), cost - have)
                : Localize.FromTable("ui.rb.ess.go", "교환"));
            if (_modals.Get<TextMeshProUGUI>("GrwEssGoText") is TextMeshProUGUI go) go.color = poor ? new Color(0.70f, 0.74f, 0.84f) : new Color(0.16f, 0.11f, 0.03f);
        }

        private void Exchange()
        {
            if (!_player.ExchangeEssence(_key, _essCount)) { Toast(Localize.FromTable("ui.rb.ess.fail", "교환할 수 없어요")); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 바꾸자마자 남긴다
            _modals.SetActive("GrwEssence", false);
            _shell.RefreshHostPages();
            _shell.RefreshHeader();
            if (_modals.Find("GrwSources") is Transform s && s.gameObject.activeSelf) ShowSources();
        }

        private void SetFace(string node)
        {
            if (_modals.Get<Image>(node) is not Image img) return;
            var s = _shell.Face(_key);
            if (s != null) img.sprite = s;
        }

        // ── 도구 ────────────────────────────────────────────────

        private SkillArt ArtOf(string key)
        {
            for (int i = 0; i < _arts.Length; i++) if (_arts[i].Key == key) return _arts[i];
            return default;
        }

        private void SetIcon(string node, Sprite s)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = s;
            img.enabled = s != null;
            img.preserveAspect = true;
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t && t.text != value) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
