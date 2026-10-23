using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 유령 성장(시안 Screens/ghost-growth) — 모든 호스트에 붙는 성장. 유령 레벨 · 레벨 보상 3칸 · 능력치 8칸(골드, 길게 누르면 연속) · 유령 경험치.
    /// 값 · 상한 · 보상은 IPlayerDataService(GhostStat* · Path*) 그대로. 위 프로필(유령 얼굴) · 도감 「성장 ›」 이 연다.
    /// </summary>
    public sealed class RbGhostPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _statIcons = Array.Empty<Sprite>();   // HostStat 순서
        [SerializeField] private Sprite _claimOn, _claimOff;
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();   // 보상 아이콘(rb_icon_*_96) — 이름으로 찾는다

        private static readonly HostStat[] StatOrder =
            { HostStat.Atk, HostStat.Hp, HostStat.Crit, HostStat.CritDamage, HostStat.Defense, HostStat.AtkSpeed, HostStat.Range, HostStat.MoveSpeed };
        private static readonly string[] StatKeys = { "hp", "atk", "crit", "atkspeed", "range", "movespeed", "critdmg", "def" };
        private const float ColPitch = (550f - 25f) * 2f / 3f, RowPitch = 142f * 2f / 3f;
        private const int PathShown = 3;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private readonly List<Transform> _tiles = new();
        private string _back = "Battle";

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("GhoBack", () => _shell.SelectTab(_back));
            _ui.OnClick("GhoHelp", () => Toast(Localize.Get("ui.growth.help.stats")));
            for (int i = 0; i < PathShown; i++)
            {
                int k = i;
                _ui.OnClick($"GhoPath{i}Claim", () => Claim(k));
            }
            var tpl = _ui.Find("GhoStatTemplate");
            if (tpl == null) return;
            var start = ((RectTransform)tpl).anchoredPosition;
            for (int i = 0; i < StatOrder.Length; i++)
            {
                var t = i == 0 ? tpl : Instantiate(tpl.gameObject, tpl.parent).transform;
                t.name = $"GhoStat{i}";
                ((RectTransform)t).anchoredPosition = start + new Vector2(ColPitch * (i % 2), -RowPitch * (i / 2));
                var stat = StatOrder[i];
                if (_ui.Find(t, "GhoStatIcon")?.GetComponent<Image>() is Image icon && (int)stat < _statIcons.Length) icon.sprite = _statIcons[(int)stat];
                _ui.Find(t, "GhoStatBuy")?.GetComponent<Button>()?.onClick.AddListener(() => Buy(stat));
                _tiles.Add(t);
            }
        }

        /// <summary>backTab — 뒤로를 누르면 돌아갈 탭(도감에서 왔으면 Host).</summary>
        public void Open(IPlayerDataService player, string backTab)
        {
            _player = player;
            _back = backTab;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            if (_player == null || !_player.IsReady) return;
            int lv = _player.GhostLevel;
            _ui.SetText("GhoLevel", $"Lv {lv}");
            bool max = lv >= _player.GhostLevelMax;
            _ui.SetText("GhoExpValue", max ? "MAX" : $"{_player.GhostExp:N0} /{_player.GhostExpMax:N0}");
            if (_ui.Find("GhoExpFill") is RectTransform f) f.anchorMax = new Vector2(max ? 1f : Mathf.Clamp01((float)_player.GhostExp / Mathf.Max(1, _player.GhostExpMax)), 1f);

            // 레벨 보상 — 아직 안 받은 것부터 셋
            int first = 0;
            while (first < _player.PathCount && _player.IsPathClaimed(first)) first++;
            first = Mathf.Clamp(first, 0, Mathf.Max(0, _player.PathCount - PathShown));
            int nextLv = -1;
            for (int i = 0; i < PathShown; i++)
            {
                int k = first + i;
                bool on = k < _player.PathCount;
                _ui.SetActive($"GhoPath{i}", on);
                if (!on) continue;
                int need = _player.PathLevel(k);
                bool claimed = _player.IsPathClaimed(k), reached = lv >= need;
                if (!reached && nextLv < 0) nextLv = need;
                _player.GetPathReward(k, out int gold, out int gem, out int core);
                string kind = gold > 0 ? "gold" : gem > 0 ? "gem" : "core";
                int amount = gold > 0 ? gold : gem > 0 ? gem : core;
                _ui.SetText($"GhoPath{i}Lv", $"Lv {need}");
                _ui.SetText($"GhoPath{i}Reward", $"{RbRewards.LabelOf(kind, null)} {amount:N0}");
                if (_ui.Get<Image>($"GhoPath{i}Icon") is Image icon) icon.sprite = RbRewards.IconOf(_icons, kind, null);
                _ui.SetActive($"GhoPath{i}Claim", reached && !claimed);
                _ui.SetActive($"GhoPath{i}State", !(reached && !claimed));
                _ui.SetText($"GhoPath{i}State", claimed ? "✓ " + Localize.Get("ui.growth5.path_done") : $"Lv {need}");
                if (_ui.Get<Image>($"GhoPath{i}") is Image bg) bg.sprite = reached && !claimed ? _claimOn : _claimOff;
            }
            _ui.SetText("GhoExpNote", nextLv > 0 ? string.Format(Localize.FromTable("ui.rb.ghost.exp_note", "챕터 클리어로 획득 · 다음 보상 Lv {0}"), nextLv)
                                                 : Localize.FromTable("ui.rb.ghost.exp_note_max", "챕터 클리어로 획득"));
            RefreshStats();
        }

        private void RefreshStats()
        {
            int cap = _player.GhostStatCap;
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                var stat = StatOrder[i];
                int lv = _player.GhostStatLevel(stat), cost = _player.GhostStatCost(stat);
                bool maxed = cost <= 0;
                TextIn(t, "GhoStatName", $"{Localize.Get($"ui.stat.{StatKeys[(int)stat]}")} <size=75%><color=#B3BDD6>Lv {lv}/{cap}</color></size>");
                TextIn(t, "GhoStatValue", maxed ? Pct(stat, lv) : $"{Pct(stat, lv)}<size=70%> › </size><color=#4AE08A>{Pct(stat, lv + 1)}</color>");
                var buy = _ui.Find(t, "GhoStatBuy");
                if (buy != null) buy.gameObject.SetActive(!maxed);
                bool levelGate = maxed && cap < _player.GhostStatMax;
                TextIn(t, "GhoStatBuyText", cost.ToString("N0"));
                if (_ui.Find(t, "GhoStatBuyText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct) ct.color = _player.Gold < cost ? new Color(1f, 0.42f, 0.42f) : Color.white;
                TextIn(t, "GhoStatMax", levelGate ? Localize.Format("ui.growth.need_level", _player.GhostLevel + 1) : maxed ? "MAX" : string.Empty);
            }
        }

        /// <summary>유령은 모든 몸에 붙어 사거리만 「가장 작은~가장 큰」(GrowthPanel 과 같은 규칙).</summary>
        private string Pct(HostStat stat, int lv)
        {
            var c = _player.Config;
            if (stat != HostStat.Range || c == null) return $"{_player.StatPercent(stat, lv, true):0.#}%";
            float share = c.StatShare(true);
            return $"{c.RangePercentPerLevel(HostStats.JobRanged) * lv * share:0.#}~{c.RangePercentPerLevel(HostStats.JobMelee) * lv * share:0.#}%";
        }

        private void Buy(HostStat stat)
        {
            if (_player.GhostStatCost(stat) <= 0) return;
            if (!_player.BuyGhostStat(stat)) { _shell.Notice.ShowGoldShort(_player.GhostStatCost(stat), _player.Gold, null, null, null); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 강화 한 번마다 저장
            RefreshStats();
            _shell.RefreshHeader();
        }

        private void Claim(int shown)
        {
            int first = 0;
            while (first < _player.PathCount && _player.IsPathClaimed(first)) first++;
            first = Mathf.Clamp(first, 0, Mathf.Max(0, _player.PathCount - PathShown));
            int k = first + shown;
            if (k >= _player.PathCount || _player.IsPathClaimed(k) || _player.GhostLevel < _player.PathLevel(k)) return;
            if (!_player.ClaimPath(k)) return;
            GameSound.Cue("run.gold");
            _player.SaveAsync().Forget();   // fire-and-forget: 받은 보상은 바로 남긴다
            Toast(Localize.Format("ui.growth.path_claimed", _player.PathLevel(k)));
            _shell.RefreshHeader();
            Refresh();
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
