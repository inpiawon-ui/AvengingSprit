using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 탭 v5 — 한 화면(몸 정보 + 성급 · 능력치 8종 · 스킬 둘 · 가로 목록) + 「성급 올리기」 창 (2026-10-06).
    ///
    /// 성급은 **조각 + 골드**로 올린다. 한 단계 = 별 반 칸 = 스킬 Lv 하나 = 강화 상한 한 칸.
    /// 성급이 오르면 무엇이 함께 오르는지를 창이 한 줄씩 보여 준다(스킬 Lv · 액티브 값 · 패시브 값 · 강화 상한) —
    /// 「성급 하나가 셋을 올린다」가 이 화면의 요점이다(기획 2026-10-06).
    ///
    /// 노드는 `GrowthBuilder.HostPage` 가 세운다(H5…).
    /// </summary>
    public sealed partial class GrowthPanel
    {
        [SerializeField] private float _h5RowPitch = 39.4f;
        [SerializeField] private float _h5CardPitch = 144f;

        /// <summary>안 가진 몸의 그림 색 — 잠겨 있다는 것이 한눈에 읽혀야 한다.</summary>
        private static readonly Color LockedTint = new(0.32f, 0.34f, 0.4f, 1f);
        /// <summary>창의 「다음 값」 색 — 오르는 것은 초록(시안).</summary>
        private const string NextColor = "#5CFF7A";
        private const string ShortColor = "#FF6B6B";

        private readonly List<Transform> _h5Rows = new();
        private readonly List<Transform> _h5Cards = new();
        private readonly List<string> _h5CardKeys = new();

        private void AwakeHostPage()
        {
            _ui.OnClick("H5StarUpButton", OpenStarUp);
            _ui.OnClick("H5StatHelpButton", () => Toast(Localize.Get("ui.growth.help.stats")));
            _ui.OnClick("H5SortButton", () => NotifyNotReady("정렬"));
            _ui.OnClick("H5Dim", CloseStarUp);
            _ui.OnClick("H5WinCancel", CloseStarUp);
            _ui.OnClick("H5WinConfirm", ConfirmStarUp);
        }

        private void BuildHostPageOnce()
        {
            var rowTemplate = _ui.Find("H5Row");
            if (rowTemplate != null)
            {
                var content = rowTemplate.parent as RectTransform;
                for (int i = 0; i < StatOrder.Length; i++)
                {
                    var row = i == 0 ? rowTemplate : Instantiate(rowTemplate.gameObject, content).transform;
                    row.name = $"H5Row{i}";
                    ((RectTransform)row).anchoredPosition =
                        ((RectTransform)rowTemplate).anchoredPosition + new Vector2(0f, -_h5RowPitch * i);
                    var stat = StatOrder[i];
                    var icon = _ui.Find(row, "H5RowIcon")?.GetComponent<Image>();
                    if (icon != null && (int)stat < _statIcons.Length) icon.sprite = _statIcons[(int)stat];
                    _ui.Find(row, "H5RowButton")?.GetComponent<Button>()?.onClick.AddListener(() => BuyStat(stat));
                    _h5Rows.Add(row);
                }
                if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, _h5RowPitch * StatOrder.Length);
            }

            var cardTemplate = _ui.Find("H5Card");
            if (cardTemplate == null || _player == null) return;
            var listContent = cardTemplate.parent as RectTransform;
            var hosts = _player.PlayableHosts;
            int n = 0;
            for (int i = 0; i < hosts.Count; i++)
            {
                var e = hosts[i];
                if (e == null || e.IsGhost) continue;
                var card = n == 0 ? cardTemplate : Instantiate(cardTemplate.gameObject, listContent).transform;
                card.name = $"H5Card_{e.HostKey}";
                ((RectTransform)card).anchoredPosition =
                    ((RectTransform)cardTemplate).anchoredPosition + new Vector2(_h5CardPitch * n, 0f);
                string key = e.HostKey;
                card.GetComponent<Button>()?.onClick.AddListener(() => SelectHost(key));
                _h5Cards.Add(card);
                _h5CardKeys.Add(key);
                n++;
            }
            if (listContent != null)
                listContent.sizeDelta = new Vector2(((RectTransform)cardTemplate).anchoredPosition.x * 2f + _h5CardPitch * n + 16f,
                                                    listContent.sizeDelta.y);
            if (string.IsNullOrEmpty(_hostKey) && _h5CardKeys.Count > 0) _hostKey = _h5CardKeys[0];
        }

        // ── 칠하기 ──────────────────────────────────────────────

        private void RefreshHostPage()
        {
            var e = _player.GetHost(_hostKey);
            if (e == null) return;
            int m = _player.GetMastery(_hostKey);
            var art = ArtOf(_hostKey);

            _ui.SetText("H5NameEnText", ShortName(e).ToUpperInvariant());
            _ui.SetText("H5NameText", e.DisplayName);
            _ui.SetText("H5DescText", Localize.Get($"host.{e.HostKey}.desc"));
            var portrait = _ui.Get<Image>("H5Portrait");
            if (portrait != null)
            {
                portrait.sprite = art.Portrait;
                portrait.enabled = portrait.sprite != null;
                portrait.color = m >= 1 ? Color.white : LockedTint;
            }

            SetHalfStars(_ui.Find("HostPage"), "H5Star", m);
            _ui.SetText("H5StarValue", StarText(m));
            RefreshStarUpBox();
            RefreshHostRows();
            RefreshHostSkills(e, m, art);
            RefreshHostCards();
        }

        /// <summary>성급 상자 — 조각 막대 · 성급 UP 버튼(값). 조각 · 골드가 바뀔 때마다 다시 칠한다.</summary>
        private void RefreshStarUpBox()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            int m = _player.GetMastery(_hostKey);
            bool maxed = m >= _player.MasteryMax;
            int shards = _player.GetShards(_hostKey), need = _player.MasteryCost(_hostKey);
            int gold = _player.StarUpGoldCost(_hostKey);
            var fill = _ui.Get<Image>("H5ShardFill");
            if (fill != null) fill.fillAmount = maxed ? 1f : Mathf.Clamp01(need > 0 ? (float)shards / need : 0f);
            _ui.SetText("H5ShardValue", maxed ? Localize.Get("ui.growth.max") : $"{shards} / {need}");
            _ui.SetText("H5StarUpText", maxed ? Localize.Get("ui.growth.max")
                                       : m < 1 ? Localize.Get("ui.growth.unlock") : Localize.Get("ui.starup.button"));
            _ui.SetText("H5StarUpCost", maxed ? "-" : gold.ToString("N0"));
            _ui.SetText("H5CapText", Localize.Format("ui.starup.cap_short", _player.HostStatCap(_hostKey)));
        }

        private void RefreshHostRows()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            bool locked = _player.GetMastery(_hostKey) < 1;
            int cap = _player.HostStatCap(_hostKey);
            for (int i = 0; i < _h5Rows.Count; i++)
            {
                var row = _h5Rows[i];
                var stat = StatOrder[i];
                int lv = _player.HostStatLevel(_hostKey, stat);
                int cost = _player.HostStatCost(_hostKey, stat);
                TextIn(row, "H5RowName", Localize.Get($"ui.stat.{StatKeys[(int)stat]}"));
                TextIn(row, "H5RowLvLabel", "Lv.");
                TextIn(row, "H5RowLvNum", lv.ToString());
                // 「/ 5」 가 「/ 10」 으로 바뀌는 것이 성급의 보상이다
                TextIn(row, "H5RowLvMax", $"/ {cap}");
                TextIn(row, "H5RowPct", PctText(stat, lv, false));
                TextIn(row, "H5RowCost", locked ? Localize.Get("ui.growth.locked")
                                       : cost > 0 ? cost.ToString("N0") : Localize.Get("ui.growth.max"));
                row.GetComponentInChildren<InkRun>(true)?.Apply();
            }
        }

        private void RefreshHostSkills(HostEntry e, int m, HostArt art)
        {
            var active = _player.GetActiveSkill(e.ActiveSkillKey);
            _ui.SetText("H5SkillNameA", active != null
                ? (string.IsNullOrEmpty(active.DisplayName) ? active.NameEn : active.DisplayName) : "—");
            _ui.SetText("H5SkillDescA", active?.DisplayDescription ?? string.Empty);
            SetIcon("H5SkillIconA", art.Skill);
            var passive = _player.GetPassiveSkill(e.PassiveSkillKey);
            _ui.SetText("H5SkillNameP", passive != null ? passive.DisplayName : "—");
            _ui.SetText("H5SkillDescP", passive != null ? passive.DisplayDescription : string.Empty);
            SetIcon("H5SkillIconP", art.Passive);

            // 스킬 Lv = 성급 단계. 배지가 성급 별과 같은 노란 별을 달고 있어 「성급이 올린 값」으로 읽힌다
            string lvText = $"Lv.{Mathf.Max(0, m)}";
            _ui.SetText("H5BadgeAText", lvText);
            _ui.SetText("H5BadgePText", lvText);
            // 특수 효과는 Lv5 에 열린다(SkillScaling.SpecLevel) — 열리면 꼬리표를 뗀다
            SetActive("H5LockA", m < SkillScaling.SpecLevel);
            _ui.SetText("H5LockText", Localize.Format("ui.starup.spec_lock", SkillScaling.SpecLevel));
        }

        private void RefreshHostCards()
        {
            for (int i = 0; i < _h5Cards.Count; i++)
            {
                var card = _h5Cards[i];
                string key = _h5CardKeys[i];
                var e = _player.GetHost(key);
                bool sel = key == _hostKey;
                int m = _player.GetMastery(key);
                _ui.Find(card, "H5CardFrameSel")?.gameObject.SetActive(sel);
                _ui.Find(card, "H5CardFrame")?.gameObject.SetActive(!sel);
                var thumb = _ui.Find(card, "H5CardThumb")?.GetComponent<Image>();
                if (thumb != null)
                {
                    thumb.sprite = ArtOf(key).Thumb;
                    thumb.enabled = thumb.sprite != null;
                    thumb.color = m >= 1 ? Color.white : LockedTint;
                }
                // 영문 몸 이름은 겹친다(코만도 넷 · 갱스터 둘) — 목록은 현지 이름으로 가른다
                TextIn(card, "H5CardName", e != null ? e.DisplayName : key);
                SetHalfStars(card, "H5CardStar", m);
            }
        }

        // ── 성급 올리기 창 ──────────────────────────────────────────

        private void OpenStarUp()
        {
            if (string.IsNullOrEmpty(_hostKey)) return;
            int m = _player.GetMastery(_hostKey);
            if (m >= _player.MasteryMax) { Toast(Localize.Get("ui.starup.maxed")); return; }
            FillStarUpWindow(m);
            SetActive("H5StarUpWindow", true);
        }

        private void CloseStarUp() => SetActive("H5StarUpWindow", false);

        private void FillStarUpWindow(int m)
        {
            var e = _player.GetHost(_hostKey);
            var art = ArtOf(_hostKey);
            SetIcon("H5WinFace", art.Thumb);

            _ui.SetText("H5WinStarNum0", (m * 0.5f).ToString("0.#"));
            _ui.SetText("H5WinStarNum1", ((m + 1) * 0.5f).ToString("0.#"));
            var win = _ui.Find("H5Window");
            for (int i = 0; i < 5; i++)
            {
                SetHalfFill(_ui.Find(win, $"H5WinStar0_{i}"), m, i);
                SetHalfFill(_ui.Find(win, $"H5WinStar1_{i}"), m + 1, i);
            }

            // 함께 오르는 넷 — 스킬 Lv · 액티브 값 · 패시브 값 · 강화 상한
            WinRow(0, _starBigOn, Localize.Get("ui.starup.skill_lv"), Arrow(m.ToString(), (m + 1).ToString()));

            var active = e != null ? _player.GetActiveSkill(e.ActiveSkillKey) : null;
            string aName = active != null ? active.DisplayName : "—";
            // 시안처럼 첫 줄은 스킬 이름, 둘째 줄은 「값 이름  지금 → 다음」
            if (active != null && active.GrowthUnit != SkillGrowthUnit.None)
                WinRow(1, art.Skill, aName,
                       $"{active.GrowthLabel}  " + Arrow(Unit(active.GrowthUnit, active.GrowthAt(Mathf.Max(1, m))),
                                                         Unit(active.GrowthUnit, active.GrowthAt(m + 1))));
            else WinRow(1, art.Skill, aName, "—");

            var passive = e != null ? _player.GetPassiveSkill(e.PassiveSkillKey) : null;
            string pName = passive != null ? passive.DisplayName : "—";
            if (passive != null && passive.HasGrowth)
            {
                int max = _player.MasteryMax;
                WinRow(2, art.Passive, pName,
                       $"{passive.GrowthLabel}  " + Arrow($"{passive.GrowthAt(Mathf.Max(1, m), max):0.#}%",
                                                          $"{passive.GrowthAt(m + 1, max):0.#}%"));
            }
            else WinRow(2, art.Passive, pName, "—");

            var config = _player.Config;
            int capNow = _player.HostStatCap(_hostKey);
            int capNext = config != null ? config.HostStatCap(m + 1) : capNow;
            WinRow(3, (int)HostStat.Atk < _statIcons.Length ? _statIcons[(int)HostStat.Atk] : null,
                   Localize.Get("ui.starup.stat_cap"), Arrow($"Lv {capNow}", $"Lv {capNext}"));

            int shards = _player.GetShards(_hostKey), need = _player.MasteryCost(_hostKey);
            int gold = _player.StarUpGoldCost(_hostKey);
            _ui.SetText("H5WinShardText", shards >= need ? $"{shards} / {need}" : $"<color={ShortColor}>{shards}</color> / {need}");
            _ui.SetText("H5WinGoldText", _player.Gold >= gold ? gold.ToString("N0") : $"<color={ShortColor}>{gold:N0}</color>");
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

        private void WinRow(int i, Sprite icon, string label, string value)
        {
            var row = _ui.Find($"H5WinRow{i}");
            if (row == null) return;
            var img = _ui.Find(row, "Icon")?.GetComponent<Image>();
            if (img != null) { img.sprite = icon; img.enabled = icon != null; }
            TextIn(row, "Label", label);
            TextIn(row, "Value", value);
        }

        private static string Arrow(string now, string next) => $"{now}  →  <color={NextColor}>{next}</color>";

        private static string Unit(SkillGrowthUnit unit, float v) => unit switch
        {
            SkillGrowthUnit.Seconds => Localize.Format("ui.unit.seconds", v),
            SkillGrowthUnit.Multiplier => $"×{v:0.0#}",
            SkillGrowthUnit.Percent => $"{v:0.#}%",
            _ => "—",
        };

        // ── 도구 ────────────────────────────────────────────────

        /// <summary>성급 표기 — 반 칸 단위(Lv3 → 「1.5 / 5」).</summary>
        private string StarText(int mastery) => $"{mastery * 0.5f:0.#} / {_player.MasteryMax / 2}";

        /// <summary>별 다섯 칸에 성급 단계를 칠한다 — 한 단계 = 반 칸.</summary>
        private void SetHalfStars(Transform root, string prefix, int halfSteps)
        {
            if (root == null) return;
            for (int i = 0; i < 5; i++) SetHalfFill(_ui.Find(root, $"{prefix}{i}"), halfSteps, i);
        }

        private void SetHalfFill(Transform star, int halfSteps, int index)
        {
            if (star == null) return;
            var on = _ui.Find(star, "On")?.GetComponent<Image>();
            if (on != null) on.fillAmount = Mathf.Clamp01((halfSteps - index * 2) * 0.5f);
        }

        private void SetIcon(string node, Sprite sprite)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }
    }
}
