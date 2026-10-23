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
    /// 히어로 판(몸 · 등급 · 상성 · 사거리 · 전투력 · 별) → 스킬 3칸 → 능력치 8칸(길게 누르면 연속 강화) → 성급 판 · 성급 올리기.
    /// 능력치 값은 이 게임 규칙대로 퍼센트(+x%)다 — 시안의 절대값(20 › 24)은 예시 숫자.
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
        [SerializeField] private Sprite _starOn, _starOff, _starAwaken;
        [SerializeField] private Sprite _tileNormal, _tileMax, _tilePoor;
        [SerializeField] private Sprite _btnReady, _btnShort;
        [SerializeField] private Sprite[] _stageBgs = Array.Empty<Sprite>();

        // 시안 순서 — 공격력 · 최대 HP / 크리 확률 · 크리 피해 / 방어력 · 공격 속도 / 사거리 · 이동 속도
        private static readonly HostStat[] StatOrder =
            { HostStat.Atk, HostStat.Hp, HostStat.Crit, HostStat.CritDamage, HostStat.Defense,
              HostStat.AtkSpeed, HostStat.Range, HostStat.MoveSpeed };
        // 첨자는 `HostStat` 순서다
        private static readonly string[] StatKeys = { "hp", "atk", "crit", "atkspeed", "range", "movespeed", "critdmg", "def" };
        private const float ColPitch = (550f - 25f) * 2f / 3f, RowPitch = 162.6f * 2f / 3f;
        private const string NextColor = "#4AE08A", ShortColor = "#FF5A5A";

        private UIBinder _ui;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private readonly List<Transform> _tiles = new();
        private string _key;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("GrwBack", () => _shell.SelectTab("Host"));
            _ui.OnClick("GrwHelp", () => Toast(Localize.FromTable("ui.rb.growth.help", "능력치는 골드로 · 성급은 조각과 골드로 올려요.\n성급이 오르면 강화 상한 · 스킬 위력이 함께 올라요.")));
            _ui.OnClick("GrwSkillUlt", () => ShowSkill(0));
            _ui.OnClick("GrwSkillAct", () => ShowSkill(1));
            _ui.OnClick("GrwSkillPas", () => ShowSkill(2));
            _ui.OnClick("GrwStarWhere", () => Toast(Localize.FromTable("ui.rb.growth.shard_where_body", "조각은 랜덤 출전 클리어 · 소환 · 상점에서 얻어요.")));
            _ui.OnClick("GrwStarUp", StarUp);
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
            for (int i = 0; i < StatOrder.Length; i++)
            {
                var t = i == 0 ? tpl : Instantiate(tpl.gameObject, tpl.parent).transform;
                t.name = $"GrwStat{i}";
                ((RectTransform)t).anchoredPosition = start + new Vector2(ColPitch * (i % 2), -RowPitch * (i / 2));
                var stat = StatOrder[i];
                if (_ui.Find(t, "GrwStatIcon")?.GetComponent<Image>() is Image icon && (int)stat < _statIcons.Length)
                    icon.sprite = _statIcons[(int)stat];
                _ui.Find(t, "GrwStatBuy")?.GetComponent<Button>()?.onClick.AddListener(() => Buy(stat));
                _tiles.Add(t);
            }
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
            RefreshStats(m);
            RefreshStar(m);
        }

        private void RefreshHero(HostEntry e, int m)
        {
            bool owned = m >= 1;
            var tint = owned ? Color.white : new Color(0.35f, 0.37f, 0.45f, 1f);
            if (_ui.Get<Image>("GrwHostArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
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
            RbHostPage.Stars(_ui.Find("GrwStars"), "GrwStar", m, _starOn, _starOff, _starAwaken);
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

        private void RefreshStats(int m)
        {
            bool locked = m < 1;
            int cap = _player.HostStatCap(_key);
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                var stat = StatOrder[i];
                int lv = _player.HostStatLevel(_key, stat);
                int cost = _player.HostStatCost(_key, stat);
                bool max = !locked && cost <= 0;
                bool poor = !locked && !max && _player.Gold < cost;
                TextIn(t, "GrwStatName", Localize.Get($"ui.stat.{StatKeys[(int)stat]}"));
                TextIn(t, "GrwStatLv", $"Lv {lv}/{cap}");
                TextIn(t, "GrwStatValue", max || locked ? Pct(stat, lv)
                                          : $"{Pct(stat, lv)}<size=70%> › </size><color={NextColor}>{Pct(stat, lv + 1)}</color>");
                if (t.GetComponent<Image>() is Image bg) bg.sprite = max ? _tileMax : poor ? _tilePoor : _tileNormal;
                var buy = _ui.Find(t, "GrwStatBuy");
                if (buy != null) buy.gameObject.SetActive(!max);
                if (buy != null && buy.GetComponent<Button>() is Button b) b.interactable = !locked;
                TextIn(t, "GrwStatBuyCost", locked ? Localize.Get("ui.growth.locked") : cost.ToString("N0"));
                if (_ui.Find(t, "GrwStatBuyCost")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct)
                    ct.color = poor ? new Color(1f, 0.35f, 0.35f) : Color.white;
                _ui.Find(t, "GrwStatBuyCoin")?.gameObject.SetActive(!locked);
            }
        }

        /// <summary>사거리만 직업마다 다르다(근 · 중 · 원거리) — 나머지는 표의 레벨당 퍼센트.</summary>
        private string Pct(HostStat stat, int lv)
        {
            var c = _player.Config;
            float v = stat == HostStat.Range && c != null
                ? c.RangePercentPerLevel(HostStats.JobIndex(_player.GetHost(_key))) * lv * c.StatShare(false)
                : _player.StatPercent(stat, lv, false);
            // 「+」 는 뺀다 — 칸이 좁아 「+0.4% › +0.7%」 가 강화 띠에 걸렸다(시안도 「150% › 160%」)
            return $"{v:0.#}%";
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

        private void Buy(HostStat stat)
        {
            int cost = _player.HostStatCost(_key, stat);
            if (cost <= 0) return;
            if (!_player.BuyHostStat(_key, stat)) { Toast(Localize.Get("ui.growth.gold_short")); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 강화 한 번마다 저장 — 앱이 꺼져도 산 것은 남는다
            Refresh();
            _shell.RefreshHeader();
        }

        private void StarUp()
        {
            int m = _player.GetMastery(_key);
            if (m >= _player.MasteryMax) { Toast(Localize.Get("ui.starup.maxed")); return; }
            int shards = _player.GetShards(_key), need = _player.MasteryCost(_key);
            int gold = _player.StarUpGoldCost(_key);
            // 모자라면 얼마나 모자란지 숫자로 — 「부족합니다」 만으로는 몇 판을 더 돌아야 하는지 모른다
            if (shards < need) { Toast(Localize.Format("ui.starup.shard_short", shards, need, need - shards)); return; }
            if (_player.Gold < gold) { Toast(Localize.Format("ui.starup.gold_short", _player.Gold, gold, gold - _player.Gold)); return; }
            string ask = string.Format(Localize.FromTable("ui.rb.growth.starup_ask", "{0}성 → {1}성\n조각 {2} · 골드 {3:N0} 을 써요"), m, m + 1, need, gold);
            SystemPopup.Show(ask, ConfirmStarUp, Localize.FromTable("ui.rb.growth.starup", "성급 올리기"), Localize.Get("ui.common.cancel"));
        }

        private void ConfirmStarUp()
        {
            if (!_player.TryStarUp(_key)) return;
            GameSound.Cue("run.card");
            _player.SaveAsync().Forget();   // fire-and-forget: 성급을 올리자마자 남긴다
            Toast(Localize.Get("ui.starup.done"));
            Refresh();
            _shell.RefreshHeader();
        }

        private void ShowSkill(int slot)
        {
            var e = _player != null ? _player.GetHost(_key) : null;
            if (e == null) return;
            string body = slot switch
            {
                0 => $"{Localize.Get($"ult.{e.HostKey}.name")}\n{Localize.Get($"ult.{e.HostKey}.desc")}",
                1 => _player.GetActiveSkill(e.ActiveSkillKey) is ActiveSkillEntry a ? $"{a.DisplayName}\n{a.DisplayDescription}" : string.Empty,
                _ => _player.GetPassiveSkill(e.PassiveSkillKey) is PassiveSkillEntry p ? $"{p.DisplayName}\n{p.DisplayDescription}" : string.Empty,
            };
            if (body.Length > 0) Toast(body);
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
