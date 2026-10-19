using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 탭 v6 — 시안 `mock_growth_v6_page` · `mock_growth_v6_starup` (PD 2026-10-16, 합친 안 `_exchange/plan_growth_v6.md`).
    ///
    /// 성급(1~10성) 하나가 원인이다 — 오르면 몸 체력 · 공격 ×1.07, 스킬 Lv, 액티브 · 필살기 위력, 패시브 값, 강화 상한이 함께 오르고
    /// 6성에 각성 옵션이 열린다(10성 효과 ×2). 화면은 그 관계를 같은 말 · 같은 순서로 세 곳(요약 띠 · 스킬 줄 · 성급 창)에 보인다.
    /// 성급 표기는 은별 1~4 · 5성마다 큰 금별(<see cref="StarRank"/>). 반 칸 별은 없다.
    ///
    /// 노드는 `GrowthV6Builder` 가 세운다(H6…).
    /// </summary>
    public sealed partial class GrowthPanel
    {
        [SerializeField] private float _h6RowPitch = 27.9f;
        [SerializeField] private float _h6CardPitch = 134f;
        [SerializeField] private Sprite _starGold, _starSilver;
        [SerializeField] private Sprite[] _gradeBadges = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _gradeFrames = System.Array.Empty<Sprite>();

        /// <summary>안 가진 몸의 그림 색 — 잠겨 있다는 것이 한눈에 읽혀야 한다.</summary>
        private static readonly Color LockedTint = new(0.32f, 0.34f, 0.4f, 1f);
        /// <summary>창의 「다음 값」 색 — 오르는 것은 초록(시안).</summary>
        /// <summary>v6 은 어두운 판이라 연두, v5 는 양피지라 짙은 초록(연두는 양피지 위에서 안 읽힌다).</summary>
        private static string NextColor = "#5CFF7A";
        private const string ShortColor = "#FF6B6B";
        private const int OptionLevel = SkillScaling.SpecLevel;

        // ── UI v5(PD 2026-10-17 시안 mock_uiv4_growth_stats · mock_uiv5_growth_skill · mock_uiv3_growth_star) ──
        // 쇼케이스 진열 칸 + 탭 셋(기본 능력 · 스킬 · 성급). 노드는 `UiV5Screens.RunGrowth` 가 세운다. 꺼져 있으면 v6 그대로다.
        [SerializeField] private bool _h6V5;
        /// <summary>능력치 칸 열 수(v5 = 2열 큰 카드) · 열 간격.</summary>
        [SerializeField] private int _h6Cols = 1;
        [SerializeField] private float _h6ColPitch;
        /// <summary>속성 아이콘 — 무기 · 파워 · 마법(Affinity 1~3 순).</summary>
        [SerializeField] private Sprite[] _kindIcons = System.Array.Empty<Sprite>();
        private int _h6Tab = 1;
        private static readonly string[] H6Groups = { "H6GroupStats", "H6GroupSkill", "H6GroupStar" };

        private readonly List<Transform> _h6Rows = new();
        private readonly List<Transform> _h6Cards = new();
        private readonly List<string> _h6CardKeys = new();

        private void AwakeHostPage()
        {
            _ui.OnClick("H6StarUpButton", OpenStarUp);
            _ui.OnClick("H6StatHelp", () => Toast(Localize.Get("ui.growth.help.stats")));
            _ui.OnClick("H6SortButton", () => NotifyNotReady("정렬"));
            _ui.OnClick("H6UltHit", () => ShowSkillDetail(SkillSlot.Ult));
            _ui.OnClick("H6ActHit", () => ShowSkillDetail(SkillSlot.Active));
            _ui.OnClick("H6PasHit", () => ShowSkillDetail(SkillSlot.Passive));
            _ui.OnClick("H6Dim", CloseStarUp);
            _ui.OnClick("H6WinCancel", CloseStarUp);
            _ui.OnClick("H6WinConfirm", ConfirmStarUp);
            if (_h6V5) NextColor = "#1E8E3E";
            for (int i = 0; i < H6Groups.Length; i++)
            {
                int tab = i;
                _ui.OnClick($"H6V5Tab{i}", () => SelectH6Tab(tab));
            }
        }

        /// <summary>v5 탭 — 그룹 하나만 켠다. 성급 탭은 「성급 올리기」 창 내용을 그 자리에 채운다(팝업 없이).</summary>
        private void SelectH6Tab(int tab)
        {
            if (!_h6V5) return;
            _h6Tab = tab;
            for (int i = 0; i < H6Groups.Length; i++)
            {
                SetActive(H6Groups[i], i == tab);
                SetActive($"H6V5Tab{i}On", i == tab);
                var label = _ui.Get<TextMeshProUGUI>($"H6V5Tab{i}Text");
                if (label != null) label.color = i == tab ? new Color(1f, 0.9f, 0.4f) : new Color(0.86f, 0.88f, 0.95f);
            }
            if (tab == 2) FillStarTab();
        }

        private void FillStarTab()
        {
            if (string.IsNullOrEmpty(_hostKey) || _player == null) return;
            int m = _player.GetMastery(_hostKey);
            bool maxed = m >= _player.MasteryMax;
            if (maxed)
            {
                _ui.SetText("H6WinRank", Localize.Get("ui.starup.maxed"));
                _ui.SetText("H6WinRises", string.Empty);
                LayoutWindow(new List<(Sprite icon, string label, string value)>());
                SetActive("H6WinFoot", false);
            }
            else
            {
                SetActive("H6WinFoot", true);
                FillStarUpWindow(m);
            }
            RefreshLadder(m);
        }

        /// <summary>성급 사다리 1~10 — 은별 · 5성마다 큰 금별, 얻은 칸은 밝게, 다음 칸에 테.</summary>
        private void RefreshLadder(int m)
        {
            int max = _player.MasteryMax;
            for (int i = 1; i <= 10; i++)
            {
                var star = _ui.Get<Image>($"H6V5LadderStar{i}");
                if (star == null) continue;
                bool gold = i % StarRank.PerGold == 0;
                star.sprite = gold ? _starGold : _starSilver;
                star.preserveAspect = true;
                star.color = i <= m ? Color.white : new Color(0.3f, 0.32f, 0.4f, 1f);
                var num = _ui.Get<TextMeshProUGUI>($"H6V5LadderNum{i}");
                if (num != null) num.color = i == m + 1 ? new Color(0.1f, 0.55f, 0.35f) : new Color(0.36f, 0.24f, 0.14f);
            }
            var sel = _ui.Find("H6V5LadderSel") as RectTransform;
            var target = _ui.Find($"H6V5LadderStar{Mathf.Clamp(m + 1, 1, 10)}") as RectTransform;
            if (sel != null)
            {
                sel.gameObject.SetActive(target != null && m < max);
                if (target != null) sel.anchoredPosition = target.anchoredPosition;
            }
        }

        private void BuildHostPageOnce()
        {
            var rowTemplate = _ui.Find("H6Row");
            if (rowTemplate != null)
            {
                var content = rowTemplate.parent as RectTransform;
                for (int i = 0; i < StatOrder.Length; i++)
                {
                    var row = i == 0 ? rowTemplate : Instantiate(rowTemplate.gameObject, content).transform;
                    row.name = $"H6Row{i}";
                    int cols = Mathf.Max(1, _h6Cols);
                    ((RectTransform)row).anchoredPosition =
                        ((RectTransform)rowTemplate).anchoredPosition
                        + new Vector2(_h6ColPitch * (i % cols), -_h6RowPitch * (i / cols));
                    var stat = StatOrder[i];
                    var icon = _ui.Find(row, "H6RowIcon")?.GetComponent<Image>();
                    if (icon != null && (int)stat < _statIcons.Length)
                    {
                        icon.sprite = _statIcons[(int)stat];
                        icon.preserveAspect = true;
                    }
                    _ui.Find(row, "H6RowButton")?.GetComponent<Button>()?.onClick.AddListener(() => BuyStat(stat));
                    _h6Rows.Add(row);
                }
                int rowsDown = (StatOrder.Length + Mathf.Max(1, _h6Cols) - 1) / Mathf.Max(1, _h6Cols);
                if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, _h6RowPitch * rowsDown + 4f);
            }

            var cardTemplate = _ui.Find("H6HostCard");
            if (cardTemplate == null || _player == null) return;
            var listContent = cardTemplate.parent as RectTransform;
            var hosts = _player.PlayableHosts;
            int n = 0;
            for (int i = 0; i < hosts.Count; i++)
            {
                var e = hosts[i];
                if (e == null || e.IsGhost || Game.User.PlayerDataService.IsHiddenHost(e.HostKey)) continue;
                var card = n == 0 ? cardTemplate : Instantiate(cardTemplate.gameObject, listContent).transform;
                card.name = $"H6HostCard_{e.HostKey}";
                ((RectTransform)card).anchoredPosition =
                    ((RectTransform)cardTemplate).anchoredPosition + new Vector2(_h6CardPitch * n, 0f);
                string key = e.HostKey;
                card.GetComponent<Button>()?.onClick.AddListener(() => SelectHost(key));
                _h6Cards.Add(card);
                _h6CardKeys.Add(key);
                n++;
            }
            if (listContent != null)
                listContent.sizeDelta = new Vector2(((RectTransform)cardTemplate).anchoredPosition.x * 2f + _h6CardPitch * n,
                                                    listContent.sizeDelta.y);
            if (string.IsNullOrEmpty(_hostKey) && _h6CardKeys.Count > 0) _hostKey = _h6CardKeys[0];
        }

        // ── 칠하기 ──────────────────────────────────────────────

        private void RefreshHostPage()
        {
            var e = _player.GetHost(_hostKey);
            if (e == null) return;
            int m = _player.GetMastery(_hostKey);
            var art = ArtOf(_hostKey);

            _ui.SetText("H6Name", e.DisplayName);
            _ui.SetText("H6Concept", Localize.Get($"host.{e.HostKey}.concept"));
            _ui.SetText("H6PowerText", Localize.Format("ui.growth6.power", _player.PowerOf(_hostKey).ToString("N0")));
            var portrait = _ui.Get<Image>("H6Portrait");
            if (portrait != null)
            {
                portrait.sprite = art.Portrait;
                portrait.enabled = portrait.sprite != null;
                portrait.preserveAspect = true;
                portrait.color = m >= 1 ? Color.white : LockedTint;
                // 인게임과 같은 몸 그림(큰 칸) — 큰 그림이 없는 몸은 초상이 그대로 선다
                if (!portrait.TryGetComponent<Game.Module.Common.UI.UnitShowcase>(out var show))
                    show = portrait.gameObject.AddComponent<Game.Module.Common.UI.UnitShowcase>();
                show.Show(_hostKey, m >= 1 ? Color.white : LockedTint);
            }
            var grade = _ui.Get<Image>("H6Grade");
            if (grade != null)
            {
                int g = (int)e.Grade;
                grade.sprite = g < _gradeBadges.Length ? _gradeBadges[g] : null;
                grade.enabled = grade.sprite != null;
                grade.preserveAspect = true;
            }

            _ui.SetText("H6RankLabel", Localize.Format("ui.growth6.rank", m));
            _ui.SetText("H6StatHeader", Localize.Get("ui.growth.stat_header"));
            if (!_h6V5) PlaceAfterText("H6StatHeader", "H6StatHelp", 8f);   // v5 는 빌더가 상한 띠 오른쪽 끝에 둔다
            _ui.SetText("H6ListHeader", Localize.Get("ui.growth.host_list"));
            _ui.SetText("H6SortText", Localize.Get("ui.growth.sort.default"));
            var stars = _ui.Find("H6Stars");
            StarRank.Apply(stars, "H6Star", m, _starGold, _starSilver, Vector2.zero, _h6V5 ? 34f : 26f, 3f);

            if (_h6V5)
            {
                var kind = Game.Module.InGame.AffinityRule.KindOf(_hostKey);
                int ki = (int)kind - 1;
                SetIcon("H6V5Kind", ki >= 0 && ki < _kindIcons.Length ? _kindIcons[ki] : null);
                SelectH6Tab(_h6Tab);   // 그룹 표시 · 성급 탭이면 내용까지(몸을 바꾸거나 올린 뒤)
            }

            RefreshSummary(m);
            RefreshStarUpBox();
            RefreshHostRows();
            RefreshHostSkills(e, m, art);
            RefreshHostCards();
        }

        /// <summary>다음 성급 요약 띠 — 「3성 → 4성 · 체력·공격 ×1.07 · 스킬 Lv 3→4 · 강화 상한 15→20」.</summary>
        private void RefreshSummary(int m)
        {
            int max = _player.MasteryMax;
            string text;
            if (m >= max) text = Localize.Get("ui.growth6.summary_max");
            else if (m < 1) text = Localize.Get("ui.growth6.summary_unlock");
            else
            {
                var config = _player.Config;
                int capNow = _player.HostStatCap(_hostKey);
                int capNext = config != null ? config.HostStatCap(m + 1) : capNow;
                text = Localize.Format("ui.growth6.summary", m, m + 1, capNow, capNext);
            }
            _ui.SetText("H6SummaryText", text);
        }

        /// <summary>성급 칸 — 조각 막대 · 성급 UP 버튼(값). 조각 · 골드가 바뀔 때마다 다시 칠한다.</summary>
        private void RefreshStarUpBox()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            int m = _player.GetMastery(_hostKey);
            bool maxed = m >= _player.MasteryMax;
            int shards = _player.GetShards(_hostKey), need = _player.MasteryCost(_hostKey);
            int gold = _player.StarUpGoldCost(_hostKey);
            var fill = _ui.Get<Image>("H6ShardFill");
            if (fill != null) fill.fillAmount = maxed ? 1f : Mathf.Clamp01(need > 0 ? (float)shards / need : 0f);
            _ui.SetText("H6ShardText", maxed ? Localize.Get("ui.growth.max") : $"{shards} / {need}");
            _ui.SetText("H6StarUpText", maxed ? Localize.Get("ui.growth.max")
                                       : m < 1 ? Localize.Get("ui.growth.unlock") : Localize.Get("ui.starup.button"));
            _ui.SetText("H6StarUpCost", maxed ? "-" : gold.ToString("N0"));
            _ui.SetText("H6CapText", Localize.Format("ui.growth6.cap", m, _player.HostStatCap(_hostKey)));
        }

        private void RefreshHostRows()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            bool locked = _player.GetMastery(_hostKey) < 1;
            int cap = _player.HostStatCap(_hostKey);
            for (int i = 0; i < _h6Rows.Count; i++)
            {
                var row = _h6Rows[i];
                var stat = StatOrder[i];
                int lv = _player.HostStatLevel(_hostKey, stat);
                int cost = _player.HostStatCost(_hostKey, stat);
                TextIn(row, "H6RowName", Localize.Get($"ui.stat.{StatKeys[(int)stat]}"));
                // 「/ 15」 가 「/ 20」 으로 바뀌는 것이 성급의 보상이다
                TextIn(row, "H6RowLv", Localize.Format("ui.growth6.row_lv", lv, cap));
                TextIn(row, "H6RowPct", PctText(stat, lv, false));
                TextIn(row, "H6RowCost", locked ? Localize.Get("ui.growth.locked")
                                       : cost > 0 ? cost.ToString("N0") : Localize.Get("ui.growth.max"));
                var coin = _ui.Find(row, "H6RowCoin");
                if (coin != null) coin.gameObject.SetActive(!locked && cost > 0);
            }
        }

        private void RefreshHostSkills(HostEntry e, int m, HostArt art)
        {
            int max = _player.MasteryMax;
            bool maxed = m >= max;
            int now = Mathf.Max(1, m), next = Mathf.Min(max, now + (m < 1 ? 0 : 1));
            string Next(int lv) => lv.ToString();

            // 필살기
            SetIcon("H6UltIcon", art.Ult != null ? art.Ult : art.Skill);
            _ui.SetText("H6UltTagText", Localize.Get(_h6V5 ? "ui.sortie.tag_ult" : "ui.growth6.ult_tag"));
            _ui.SetText("H6UltName", Localize.Get($"ult.{e.HostKey}.name"));
            _ui.SetText("H6UltCond", Localize.Get("ui.growth6.ult_cond"));
            _ui.SetText("H6UltDesc", Localize.Get($"ult.{e.HostKey}.short"));
            _ui.SetText("H6UltPower", maxed
                ? Localize.Format("ui.growth6.power_max", Pct(SkillPower.Ult(m)))
                : Localize.Format("ui.growth6.power_line", Pct(SkillPower.Ult(now)), Next(next),
                                  Pct(SkillPower.Ult(next))));

            // 액티브
            var active = _player.GetActiveSkill(e.ActiveSkillKey);
            SetIcon("H6ActIcon", art.Skill);
            _ui.SetText("H6ActTagText", Localize.Get(_h6V5 ? "ui.sortie.tag_act" : "ui.growth6.act_tag"));
            _ui.SetText("H6ActName", active != null
                ? (string.IsNullOrEmpty(active.DisplayName) ? active.NameEn : active.DisplayName) : "—");
            _ui.SetText("H6ActCond", Localize.Get("ui.growth6.act_cond"));
            _ui.SetText("H6ActDesc", Localize.Get($"askill.{e.HostKey}.short"));
            _ui.SetText("H6ActPower", maxed
                ? Localize.Format("ui.growth6.power_max", Pct(SkillPower.Active(m)))
                : Localize.Format("ui.growth6.power_line", Pct(SkillPower.Active(now)), Next(next),
                                  Pct(SkillPower.Active(next))));
            // 보조값(지속 · 개수 · 범위) — 다음 성급에 실제로 달라질 때만 상자를 보인다
            bool step = !maxed && m >= 1 && active != null && active.GrowthUnit != SkillGrowthUnit.None
                        && !Mathf.Approximately(active.GrowthAt(now), active.GrowthAt(next));
            SetActive("H6ActStep", step);
            if (step)
            {
                _ui.SetText("H6ActStepTitle", Localize.Format("ui.growth6.step", next));
                _ui.SetText("H6ActStepValue", $"{active.GrowthLabel} " +
                    Arrow(Unit(active.GrowthUnit, active.GrowthAt(now)), Unit(active.GrowthUnit, active.GrowthAt(next))));
            }

            // 6성 각성 옵션 — 잠김 / 열림 / 10성 ×2
            bool open = m >= OptionLevel;
            SetActive("H6OptLock", !open);
            _ui.SetText("H6OptTagText", m >= max ? Localize.Get("ui.growth6.opt_max")
                                       : open ? Localize.Format("ui.growth6.opt_open", OptionLevel)
                                       : Localize.Format("ui.growth6.opt_locked", OptionLevel));
            var opt = _ui.Get<TextMeshProUGUI>("H6OptText");
            if (opt != null)
            {
                opt.text = Localize.Get($"askill.{e.HostKey}.opt");
                opt.color = open ? Color.white : new Color(0.62f, 0.66f, 0.76f);
            }

            // 패시브
            var passive = _player.GetPassiveSkill(e.PassiveSkillKey);
            SetIcon("H6PasIcon", art.Passive);
            _ui.SetText("H6PasTagText", Localize.Get(_h6V5 ? "ui.sortie.tag_pas" : "ui.growth6.pas_tag"));
            _ui.SetText("H6PasName", passive != null ? passive.DisplayName : "—");
            _ui.SetText("H6PasDesc", Localize.Get($"pskill.{e.HostKey}.short"));
            string pas = string.Empty;
            if (passive != null && passive.HasGrowth)
                pas = maxed
                    ? Localize.Format("ui.growth6.pas_max", passive.GrowthLabel, $"{passive.GrowthAt(m, max):0.#}%")
                    : Localize.Format("ui.growth6.pas_line", passive.GrowthLabel, $"{passive.GrowthAt(now, max):0.#}%",
                                      $"{passive.GrowthAt(next, max):0.#}%");
            _ui.SetText("H6PasPower", pas);
        }

        private void RefreshHostCards()
        {
            for (int i = 0; i < _h6Cards.Count; i++)
            {
                var card = _h6Cards[i];
                string key = _h6CardKeys[i];
                var e = _player.GetHost(key);
                int m = _player.GetMastery(key);
                _ui.Find(card, "CardFrameSel")?.gameObject.SetActive(key == _hostKey);
                int g = e != null ? (int)e.Grade : 0;
                var frame = _ui.Find(card, "CardFrame")?.GetComponent<Image>();
                if (frame != null && g < _gradeFrames.Length && _gradeFrames[g] != null) frame.sprite = _gradeFrames[g];
                var badge = _ui.Find(card, "CardGrade")?.GetComponent<Image>();
                if (badge != null)
                {
                    badge.sprite = g < _gradeBadges.Length ? _gradeBadges[g] : null;
                    badge.enabled = badge.sprite != null;
                    badge.preserveAspect = true;
                }
                var thumb = _ui.Find(card, "CardThumb")?.GetComponent<Image>();
                if (thumb != null)
                {
                    thumb.sprite = ArtOf(key).Thumb;
                    thumb.enabled = thumb.sprite != null;
                    thumb.preserveAspect = true;
                    thumb.color = m >= 1 ? Color.white : LockedTint;
                }
                TextIn(card, "CardName", e != null ? e.DisplayName : key);
                TextIn(card, "CardRank", m >= 1 ? Localize.Format("ui.growth6.card_rank", m) : string.Empty);
                var rect = card as RectTransform;
                float h = rect != null ? rect.sizeDelta.y : 113f;
                // 「N성」 오른쪽에 별 줄 — 카드 아래 줄
                StarRank.Apply(card, "H6CardStar", m, _starGold, _starSilver, new Vector2(10f, -h * 0.5f + 13f), 13f, 1f);
            }
        }

        // ── 스킬 자세히 ─────────────────────────────────────────

        private enum SkillSlot { Ult, Active, Passive }

        /// <summary>줄을 누르면 긴 설명(화면 줄은 한 줄 요약).</summary>
        private void ShowSkillDetail(SkillSlot slot)
        {
            var e = _player != null ? _player.GetHost(_hostKey) : null;
            if (e == null) return;
            string body = slot switch
            {
                SkillSlot.Ult => $"{Localize.Get($"ult.{e.HostKey}.name")}\n{Localize.Get($"ult.{e.HostKey}.desc")}",
                SkillSlot.Active => _player.GetActiveSkill(e.ActiveSkillKey) is ActiveSkillEntry a
                    ? $"{a.DisplayName}\n{a.DisplayDescription}" : string.Empty,
                _ => _player.GetPassiveSkill(e.PassiveSkillKey) is PassiveSkillEntry p
                    ? $"{p.DisplayName}\n{p.DisplayDescription}" : string.Empty,
            };
            if (body.Length > 0) Toast(body);
        }

        // ── 성급 올리기 창 ──────────────────────────────────────────

        private void OpenStarUp()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            int m = _player.GetMastery(_hostKey);
            if (m >= _player.MasteryMax) { Toast(Localize.Get("ui.starup.maxed")); return; }
            if (_h6V5) { SelectH6Tab(2); return; }   // v5 — 창 대신 성급 탭이 같은 내용을 보인다
            FillStarUpWindow(m);
            SetActive("H6StarUpWindow", true);
        }

        private void CloseStarUp()
        {
            if (_h6V5) return;   // v5 성급 탭은 닫는 창이 아니다
            SetActive("H6StarUpWindow", false);
        }

        private void FillStarUpWindow(int m)
        {
            var e = _player.GetHost(_hostKey);
            var art = ArtOf(_hostKey);
            int to = m + 1, max = _player.MasteryMax;
            SetIcon("H6WinFace", art.Thumb);
            _ui.SetText("H6WinTitle", Localize.Get("ui.starup.title"));
            _ui.SetText("H6WinRank", Localize.Format("ui.growth6.win_rank", m, to));
            _ui.SetText("H6WinRises", Localize.Get("ui.starup.rises"));
            StarRank.Apply(_ui.Find("H6WinStarsNow"), "WStar", m, _starGold, _starSilver, Vector2.zero, 22f, 2f);
            StarRank.Apply(_ui.Find("H6WinStarsNext"), "WStar", to, _starGold, _starSilver, Vector2.zero, 22f, 2f);

            // 함께 오르는 것 — 메인 화면과 같은 순서 · 같은 말(몸 → 스킬 Lv · 위력 → 각성 → 강화 상한)
            int now = Mathf.Max(1, m);
            var lines = new List<(Sprite icon, string label, string value)>
            {
                ((int)HostStat.Hp < _statIcons.Length ? _statIcons[(int)HostStat.Hp] : null,
                 Localize.Get("ui.growth6.w.body"), Localize.Get("ui.growth6.w.body_val")),
                (_starGold, Localize.Get("ui.growth6.w.skill_lv"), Arrow(m.ToString(), to.ToString())),
                (art.Skill, Localize.Get("ui.growth6.w.act"),
                 Arrow($"{Pct(SkillPower.Active(now))}%", $"{Pct(SkillPower.Active(to))}%")),
            };
            var active = e != null ? _player.GetActiveSkill(e.ActiveSkillKey) : null;
            if (m >= 1 && active != null && active.GrowthUnit != SkillGrowthUnit.None
                && !Mathf.Approximately(active.GrowthAt(now), active.GrowthAt(to)))
                lines.Add((art.Skill, Localize.Get("ui.growth6.w.act_step"),
                           $"{active.GrowthLabel} " + Arrow(Unit(active.GrowthUnit, active.GrowthAt(now)),
                                                            Unit(active.GrowthUnit, active.GrowthAt(to)))));
            lines.Add((art.Ult != null ? art.Ult : art.Skill, Localize.Get("ui.growth6.w.ult"),
                       Arrow($"{Pct(SkillPower.Ult(now))}%", $"{Pct(SkillPower.Ult(to))}%")));
            var passive = e != null ? _player.GetPassiveSkill(e.PassiveSkillKey) : null;
            if (passive != null && passive.HasGrowth)
                lines.Add((art.Passive, $"{Localize.Get("ui.growth6.w.pas")} {passive.GrowthLabel}",
                           Arrow($"{passive.GrowthAt(now, max):0.#}%", $"{passive.GrowthAt(to, max):0.#}%")));
            if (to == OptionLevel)
                lines.Add((art.Skill, Localize.Get("ui.growth6.w.opt"), Localize.Get("ui.growth6.w.opt_open")));
            if (to == max)
                lines.Add((art.Skill, Localize.Get("ui.growth6.w.opt_max"), Localize.Get("ui.growth6.w.opt_double")));
            var config = _player.Config;
            int capNow = _player.HostStatCap(_hostKey);
            int capNext = config != null ? config.HostStatCap(to) : capNow;
            lines.Add(((int)HostStat.Atk < _statIcons.Length ? _statIcons[(int)HostStat.Atk] : null,
                       Localize.Get("ui.growth6.w.cap"), Arrow($"Lv {capNow}", $"Lv {capNext}")));
            LayoutWindow(lines);

            int shards = _player.GetShards(_hostKey), need = _player.MasteryCost(_hostKey);
            int gold = _player.StarUpGoldCost(_hostKey);
            _ui.SetText("H6WinShardText", shards >= need ? $"{shards} / {need}" : $"<color={ShortColor}>{shards}</color> / {need}");
            _ui.SetText("H6WinGoldText", _player.Gold >= gold ? gold.ToString("N0") : $"<color={ShortColor}>{gold:N0}</color>");
            _ui.SetText("H6WinCancelText", Localize.Get("ui.common.cancel"));
            _ui.SetText("H6WinConfirmText", m < 1 ? Localize.Get("ui.growth.unlock") : Localize.Get("ui.starup.button"));
            _ui.SetText("H6WinNext", NextMilestone(to));
        }

        /// <summary>줄 수만큼 창 높이 · 세로선 · 아래 묶음을 맞춘다(빌더 값: 줄 50 + 사이 4, 첫 줄 262).</summary>
        /// <summary>v5 성급 탭 — 첫 줄 · 줄 간격(창 위 변 기준). 창은 탭 칸에 고정이라 높이 · 자리를 바꾸지 않는다.</summary>
        [SerializeField] private float _h6V5RowFirst = 176f, _h6V5RowPitch = 34f;

        private void LayoutWindow(List<(Sprite icon, string label, string value)> lines)
        {
            if (_h6V5) { LayoutStarTab(lines); return; }
            const float first = 262f, pitch = 54f, rowH = 50f, footH = 150f;
            int shown = 0;
            for (int i = 0; ; i++)
            {
                var row = _ui.Find($"H6WinRow{i}");
                if (row == null) break;
                bool on = i < lines.Count;
                row.gameObject.SetActive(on);
                if (!on) continue;
                var (icon, label, value) = lines[i];
                var img = _ui.Find(row, "Icon")?.GetComponent<Image>();
                if (img != null) { img.sprite = icon; img.enabled = icon != null; img.preserveAspect = true; }
                TextIn(row, "Label", label);
                TextIn(row, "Value", value);
                shown++;
            }
            float rowsBottom = first + shown * pitch;
            if (_ui.Find("H6WinLine") is RectTransform line)
            {
                float len = Mathf.Max(0f, shown * pitch - pitch + rowH * 0.5f);
                line.sizeDelta = new Vector2(line.sizeDelta.x, len);
                line.anchoredPosition = new Vector2(line.anchoredPosition.x, -(first + rowH * 0.5f + len * 0.5f));
            }
            if (_ui.Find("H6WinFoot") is RectTransform foot)
                foot.anchoredPosition = new Vector2(foot.anchoredPosition.x, -(rowsBottom + 8f + foot.sizeDelta.y * 0.5f));
            if (_ui.Find("H6Window") is RectTransform win)
            {
                float h = rowsBottom + 8f + footH;
                win.sizeDelta = new Vector2(win.sizeDelta.x, h);
                win.anchoredPosition = new Vector2(win.anchoredPosition.x, h * 0.5f);   // 화면 가운데
            }
        }

        private void LayoutStarTab(List<(Sprite icon, string label, string value)> lines)
        {
            for (int i = 0; ; i++)
            {
                var row = _ui.Find($"H6WinRow{i}");
                if (row == null) break;
                bool on = i < lines.Count;
                row.gameObject.SetActive(on);
                if (!on) continue;
                if (row is RectTransform rr) rr.anchoredPosition = new Vector2(rr.anchoredPosition.x, -(_h6V5RowFirst + _h6V5RowPitch * i));
                var (icon, label, value) = lines[i];
                var img = _ui.Find(row, "Icon")?.GetComponent<Image>();
                if (img != null) { img.sprite = icon; img.enabled = icon != null; img.preserveAspect = true; }
                TextIn(row, "Label", label);
                TextIn(row, "Value", value);
            }
        }

        /// <summary>창 아래 「다음 이정표」 — 바로 다음에 오는 큰 일 하나(5 · 10성 큰 금별, 6성 각성, 보조값 계단).</summary>
        private string NextMilestone(int from)
        {
            int max = _player.MasteryMax;
            for (int lv = from + 1; lv <= max; lv++)
            {
                string what = lv == OptionLevel ? Localize.Get("ui.growth6.mile_opt")
                            : lv == max ? Localize.Get("ui.growth6.mile_max")
                            : lv % StarRank.PerGold == 0 ? Localize.Get("ui.growth6.mile_gold")
                            : null;
                if (what != null) return Localize.Format("ui.growth6.next_mile", lv, what);
            }
            return string.Empty;
        }

        private void ConfirmStarUp()
        {
            int shards = _player.GetShards(_hostKey), need = _player.MasteryCost(_hostKey);
            int gold = _player.StarUpGoldCost(_hostKey);
            // 모자라면 얼마나 모자란지 숫자로 — 「부족합니다」 만으로는 몇 판을 더 돌아야 하는지 모른다
            if (shards < need) { Toast(Localize.Format("ui.starup.shard_short", shards, need, need - shards)); return; }
            if (_player.Gold < gold) { Toast(Localize.Format("ui.starup.gold_short", _player.Gold, gold, gold - _player.Gold)); return; }
            if (!_player.TryStarUp(_hostKey)) return;
            GameSound.Cue("run.card");
            _player.SaveAsync().Forget();   // fire-and-forget: 성급을 올리자마자 남긴다
            CloseStarUp();
            Toast(Localize.Get("ui.starup.done"));
            RefreshAll();
        }

        /// <summary>배율 1.08 → 「108」. 화면은 배율 대신 퍼센트로 보인다(PD 2026-10-17).</summary>
        private static string Pct(float multiplier) => (multiplier * 100f).ToString("0.#");

        private static string Arrow(string now, string next) => $"{now}  →  <color={NextColor}>{next}</color>";

        private static string Unit(SkillGrowthUnit unit, float v) => unit switch
        {
            SkillGrowthUnit.Seconds => Localize.Format("ui.unit.seconds", v),
            SkillGrowthUnit.Multiplier => $"{Pct(v)}%",
            SkillGrowthUnit.Percent => $"{v:0.#}%",
            _ => "—",
        };

        // ── 도구 ────────────────────────────────────────────────

        /// <summary>제목 글자 끝 바로 뒤에 아이콘을 둔다 — 언어마다 제목 길이가 달라 고정 자리면 글자를 덮는다(영어).</summary>
        private void PlaceAfterText(string textNode, string iconNode, float gap)
        {
            var t = _ui.Get<TextMeshProUGUI>(textNode);
            if (t == null || _ui.Find(iconNode) is not RectTransform icon) return;
            var r = t.rectTransform;
            float left = r.anchoredPosition.x - r.pivot.x * r.rect.width;
            float width = Mathf.Min(t.GetPreferredValues(t.text).x, r.rect.width);
            icon.anchoredPosition = new Vector2(left + width + gap + icon.rect.width * icon.pivot.x, icon.anchoredPosition.y);
        }

        private void SetIcon(string node, Sprite sprite)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
            img.preserveAspect = true;
        }
    }
}
