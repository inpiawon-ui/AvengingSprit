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
    /// RE:BORN 유령 성장(시안 Screens/ghost-growth) — 모든 호스트에 붙는 성장. 유령 레벨 · 레벨 보상 3칸 · 강화 6줄(골드, 길게 누르면 연속) · 유령 경험치.
    /// 육성 개편(PD 2026-10-29) — 스탯은 유령이 들고 다닌다: 공격력 · 호스트 체력 · 치명타 · 공격 속도 · 이동 속도 · 받는 피해 감소. 상한 = 유령 레벨.
    /// 값 · 상한 · 보상은 IPlayerDataService(GhostStat* · Path*) 그대로. 위 프로필(유령 얼굴) · 도감 「성장 ›」 이 연다.
    /// </summary>
    public sealed class RbGhostPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _statIcons = Array.Empty<Sprite>();   // HostStat 순서
        [SerializeField] private Sprite _claimOn, _claimOff;
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();   // 보상 아이콘(rb_icon_*_96) — 이름으로 찾는다

        // 유령 HP 는 육성 항목이 아니다(PD) — 「호스트 체력」 · 「받는 피해 감소」(= 방어 %p)는 유령 쪽 이름을 쓴다
        // 치명타 피해 · 사거리를 되살렸다(PD 2026-10-29 「치명타 피해도 있지 않았어? 스탯 종류가 너무 적다」) — 예전 8칸 그대로
        private static readonly HostStat[] StatOrder =
            { HostStat.Atk, HostStat.Hp, HostStat.Crit, HostStat.CritDamage, HostStat.AtkSpeed, HostStat.MoveSpeed,
              HostStat.Range, HostStat.Defense, HostStat.SubPower };
        // 첨자는 `HostStat` 순서다
        private static readonly string[] StatKeys =
            { "ui.rb.ghost.stat.hp", "ui.stat.atk", "ui.stat.crit", "ui.stat.atkspeed", "ui.stat.range", "ui.stat.movespeed",
              "ui.stat.critdmg", "ui.rb.ghost.stat.def", "ui.rb.ghost.stat.subpow" };
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
            FitRows();
        }

        // 빌더는 4줄(8칸) 자리만 잡아 두었다 — 보조 위력(PD 2026-11-02)으로 늘어난 줄만큼 경험치 칸을 내리고 스크롤을 늘린다
        private const int BuiltRows = 4;

        private void FitRows()
        {
            float push = RowPitch * ((StatOrder.Length + 1) / 2 - BuiltRows);
            if (push <= 0f || !(_ui.Find("GhoExp") is RectTransform exp)) return;
            exp.anchoredPosition -= new Vector2(0f, push);
            if (exp.parent is RectTransform content) content.sizeDelta += new Vector2(0f, push);
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
                TextIn(t, "GhoStatName", $"{Localize.Get(StatKeys[(int)stat])} <size=75%><color=#B3BDD6>Lv {lv}/{cap}</color></size>");
                TextIn(t, "GhoStatValue", maxed ? Pct(stat, lv) : $"{Pct(stat, lv)}<size=70%> › </size><color=#4AE08A>{Pct(stat, lv + 1)}</color>");
                var buy = _ui.Find(t, "GhoStatBuy");
                if (buy != null) buy.gameObject.SetActive(!maxed);
                bool levelGate = maxed && cap < _player.GhostStatMax;
                TextIn(t, "GhoStatBuyText", cost.ToString("N0"));
                if (_ui.Find(t, "GhoStatBuyText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI ct) ct.color = _player.Gold < cost ? new Color(1f, 0.42f, 0.42f) : Color.white;
                TextIn(t, "GhoStatMax", levelGate ? Localize.Format("ui.growth.need_level", _player.GhostLevel + 1) : maxed ? "MAX" : string.Empty);
            }
        }

        /// <summary>그 레벨까지 오른 값 — 치명타 · 받는 피해 감소는 %p, 나머지는 %(둘 다 「x%」 로 적는다).</summary>
        private string Pct(HostStat stat, int lv)
        {
            // 사거리는 직업마다 다르게 붙는다(근거리가 가장 크다) — 「원거리 ~ 근거리」
            var c = _player.Config;
            // 보조 위력은 바탕 30% 에 더한 값 그대로 — 「보조 공격 30% › 30.1%」
            if (stat == HostStat.SubPower) return $"{Game.Module.InGame.PartyRule.SubPower * 100f + _player.GhostStatValue(stat, lv):0.#}%";
            if (stat != HostStat.Range || c == null) return $"{_player.GhostStatValue(stat, lv):0.#}%";
            return $"{c.RangePercentPerLevel(2) * lv:0.#}~{c.RangePercentPerLevel(0) * lv:0.#}%";
        }

        private void Buy(HostStat stat)
        {
            if (_player.GhostStatCost(stat) <= 0) return;
            if (!_player.BuyGhostStat(stat)) { _shell.Notice.ShowGoldShort(_player.GhostStatCost(stat), _player.Gold, null, null, null); return; }
            GameSound.Cue("run.shop");
            _player.SaveAsync().Forget();   // fire-and-forget: 강화 한 번마다 저장
            RefreshStats();
            _shell.RefreshHeader();
            // 호스트 강화와 같은 금빛 용암 · 불티 · 값 톡(PD 2026-10-30 「용암 이펙트가 사라졌다 — 여기도 적용」)
            int idx = Array.IndexOf(StatOrder, stat);
            if (_shell.Fx != null && idx >= 0 && idx < _tiles.Count && _tiles[idx] is RectTransform tile)
                _shell.Fx.UpgradeTick(tile, _ui.Find(tile, "GhoStatValue") as RectTransform);
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

        // ─────────────────────────────────────────
        // 능력치 칸 짚기 — 1챕터를 처음 깨고 왔을 때 한 번(RbLobbyUI.TryGhostStatCoach)
        // ─────────────────────────────────────────

        private const string StatCoachKey = "tut.lobby.ghost_stats";
        private const float CoachDim = 0.78f, CoachPad = 14f;
        private GameObject _coach;
        private float _coachSince;

        public static bool StatCoachSeen => PlayerPrefs.GetInt(StatCoachKey, 0) == 1;

        /// <summary>권유를 한 번 띄웠다 — 「나중에」를 눌러도 다시 묻지 않는다.</summary>
        public static void MarkStatCoachSeen()
        {
            PlayerPrefs.SetInt(StatCoachKey, 1);
            PlayerPrefs.Save();
        }

        public void ShowStatCoach()
        {
            if (_coach != null || _tiles.Count == 0) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Canvas.ForceUpdateCanvases();

            var root = (RectTransform)canvas.rootCanvas.transform;
            _coach = new GameObject("GhoStatCoach", typeof(RectTransform));
            var rt = (RectTransform)_coach.transform;
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            _coachSince = Time.unscaledTime;

            // 능력치 칸 전체를 감싸는 구멍(캔버스 좌표)
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            var corners = new Vector3[4];
            for (int i = 0; i < _tiles.Count; i++)
            {
                ((RectTransform)_tiles[i]).GetWorldCorners(corners);
                for (int c = 0; c < 4; c++)
                {
                    Vector2 p = root.InverseTransformPoint(corners[c]);
                    min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
            }
            min -= Vector2.one * CoachPad; max += Vector2.one * CoachPad;
            var r = root.rect;
            // 구멍 둘레 네 조각 + 구멍 — 어디를 눌러도 닫힌다(구멍 안 강화 단추는 안내를 닫은 뒤에 누른다)
            CoachPiece(rt, r.xMin, max.y, r.xMax, r.yMax, true);
            CoachPiece(rt, r.xMin, r.yMin, r.xMax, min.y, true);
            CoachPiece(rt, r.xMin, min.y, min.x, max.y, true);
            CoachPiece(rt, max.x, min.y, r.xMax, max.y, true);
            CoachPiece(rt, min.x, min.y, max.x, max.y, false);

            var label = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            label.SetParent(rt, false);
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            label.sizeDelta = new Vector2(640f, 110f);
            bool above = max.y + 140f < r.yMax;
            label.anchoredPosition = new Vector2((r.xMin + r.xMax) * 0.5f, above ? max.y + 75f : min.y - 75f);
            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (_ui.Get<TMP_Text>("GhoLevel") is TMP_Text like) { tmp.font = like.font; tmp.fontSharedMaterial = like.fontSharedMaterial; }
            tmp.text = Localize.Get("ui.tut.coach.ghost_stats");
            tmp.fontSize = 34f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 34f;
            tmp.fontSizeMin = 18f;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private void CoachPiece(RectTransform parent, float x0, float y0, float x1, float y1, bool dark)
        {
            var piece = new GameObject(dark ? "Dark" : "Hole", typeof(RectTransform)).GetComponent<RectTransform>();
            piece.SetParent(parent, false);
            piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f);
            piece.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            piece.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            piece.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, dark ? CoachDim : 0f);
            piece.gameObject.AddComponent<Button>().onClick.AddListener(CloseStatCoach);
        }

        private void CloseStatCoach()
        {
            if (Time.unscaledTime - _coachSince < 0.4f) return;   // 연 손가락이 바로 닫지 않게
            if (_coach != null) Destroy(_coach);
            _coach = null;
        }

        private void OnDisable()
        {
            if (_coach != null) Destroy(_coach);
            _coach = null;
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
