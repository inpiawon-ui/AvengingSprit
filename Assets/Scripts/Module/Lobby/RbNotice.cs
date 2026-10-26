using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 로비 공용 알림 창 — 보상(REWARD) · 모자라요(행동력 · 젬) · 골드가 모자라요(얻는 곳) · 구매 확인.
    /// 시안 Screens/reward · energy-short · energy-poor · gem-short · gold-short · purchase-confirm.png. 전에는 시스템 알림(글자뿐)이었다.
    /// 화면 맨 앞(위 막대 · 아래 탭 · 다른 창 위)에 있다. 확인 · 닫기 · 뒤로로 닫는다.
    /// </summary>
    public sealed class RbNotice : MonoBehaviour
    {
        private const int RewardCells = 4;
        private const float CellPitch = 132f, CellWidth = 120f, CellsCenter = 360f;
        private static readonly string[] Panels = { "RbNotInfo", "RbNotConfirm", "RbNotOptions", "RbNotShort", "RbNotGold", "RbNotReward" };
        private static readonly Color Ink = new(0.16f, 0.11f, 0.03f), Dim = new(0.70f, 0.74f, 0.84f);

        /// <summary>키트 아이콘(rb_icon_*_96) — 보상 · 재화 그림을 이름으로 찾는다.</summary>
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private Action _onGo, _onYes;
        private readonly Action[] _onOption = new Action[2];
        private readonly List<(string kind, string key, int amount)> _lines = new();
        private readonly List<RectTransform> _fxCells = new();

        private void Awake()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("NotRewardClose", () => Close("RbNotReward"));
            _ui.OnClick("NotRewardOk", () => Close("RbNotReward"));
            _ui.OnClick("NotShortClose", () => Close("RbNotShort"));
            _ui.OnClick("NotShortGo", () => { Close("RbNotShort"); _onGo?.Invoke(); });
            _ui.OnClick("NotGoldClose", () => Close("RbNotGold"));
            _ui.OnClick("NotGoldGo", () => { Close("RbNotGold"); _onGo?.Invoke(); });
            _ui.OnClick("NotGoldRow0Go", () => Leave(() => _shell.SelectTab("Battle")));
            _ui.OnClick("NotGoldRow1Go", () => Leave(() => { _shell.SelectTab("Shop"); _shell.ShowShopTab("today"); }));
            _ui.OnClick("NotGoldRow2Go", () => Leave(() => _shell.OpenRail("Mail")));
            _ui.OnClick("NotConfirmClose", () => Close("RbNotConfirm"));
            _ui.OnClick("NotConfirmNo", () => Close("RbNotConfirm"));
            _ui.OnClick("NotConfirmYes", () => { Close("RbNotConfirm"); _onYes?.Invoke(); });
            _ui.OnClick("NotOptionsClose", () => Close("RbNotOptions"));
            _ui.OnClick("NotInfoClose", () => Close("RbNotInfo"));
            _ui.OnClick("NotInfoOk", () => Close("RbNotInfo"));
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                _ui.OnClick($"NotOption{i}", () => { Close("RbNotOptions"); _onOption[k]?.Invoke(); });
            }
        }

        // ── 보상 ───────────────────────────────────────────

        public void ShowReward(RewardResult r)
        {
            _lines.Clear();
            if (r != null)
                for (int i = 0; i < r.Lines.Count; i++)
                    _lines.Add((r.Lines[i].Kind.ToString().ToLowerInvariant(), r.Lines[i].Key, r.Lines[i].Amount));
            ShowReward(_lines);
        }

        /// <summary>받은 것 줄(kind · key · 수). 칸은 넷 — 넘치는 줄은 마지막 칸에 「+n」 으로 묶지 않고 앞의 넷만 보인다.</summary>
        public void ShowReward(List<(string kind, string key, int amount)> lines)
        {
            Awake();
            int n = Mathf.Min(lines.Count, RewardCells);
            float left = CellsCenter - (n * CellPitch - (CellPitch - CellWidth)) * 0.5f;
            for (int i = 0; i < RewardCells; i++)
            {
                var cell = _ui.Find($"NotRewardCell{i}") as RectTransform;
                if (cell == null) continue;
                cell.gameObject.SetActive(i < n);
                if (i >= n) continue;
                var (kind, key, amount) = lines[i];
                cell.anchoredPosition = new Vector2(left + i * CellPitch + CellWidth * 0.5f, cell.anchoredPosition.y);
                if (_ui.Find(cell, $"NotRewardCell{i}Icon")?.GetComponent<Image>() is Image icon) icon.sprite = RbRewards.IconOf(_icons, kind, key);
                SetIn(cell, $"NotRewardCell{i}Count", amount.ToString("N0"));
                SetIn(cell, $"NotRewardCell{i}Label", RbRewards.LabelOf(kind, key));
            }
            Open("RbNotReward");
            GameSound.Cue("run.card");
            // 칸이 차례로 열리고 금화는 위 막대 골드 칩으로(시안 reward_reveal · coin_burst)
            if (_shell != null && _shell.Fx != null)
            {
                _fxCells.Clear();
                RectTransform goldCell = null;
                for (int i = 0; i < n; i++)
                {
                    if (_ui.Find($"NotRewardCell{i}") is not RectTransform c) continue;
                    c.localScale = Vector3.one;   // 앞 연출이 덜 끝난 채 다시 열려도 제 크기에서 시작
                    _fxCells.Add(c);
                    if (goldCell == null && (lines[i].kind == "gold" || lines[i].kind == "goldrun")) goldCell = c;
                }
                _shell.Fx.RewardCells(_fxCells, goldCell, _shell.GoldChip);
            }
        }

        /// <summary>
        /// 상자를 샀다 — 열린 상자가 있으면 상자 열기 쪽(RbChestOpen, 시안 chest-opening · chest-open), 없으면 보상 창.
        /// 여러 개를 샀으면 내용물을 합쳐 한 번에 보인다(상자 그림은 첫 상자 등급).
        /// </summary>
        public void ShowPurchase(RewardResult r)
        {
            Awake();
            var chest = _shell != null ? _shell.GetComponentInChildren<RbChestOpen>(true) : null;
            if (r == null || r.OpenedChests == null || r.OpenedChests.Count == 0 || chest == null) { ShowReward(r); return; }
            _lines.Clear();
            int gold = 0;
            var shards = new Dictionary<string, int>();
            for (int i = 0; i < r.OpenedChests.Count; i++)
            {
                var c = r.OpenedChests[i];
                gold += c.Gold;
                for (int k = 0; k < c.ShardHostKeys.Length; k++)
                {
                    shards.TryGetValue(c.ShardHostKeys[k], out int had);
                    shards[c.ShardHostKeys[k]] = had + (k < c.ShardCounts.Length ? c.ShardCounts[k] : 1);
                }
            }
            if (gold > 0) _lines.Add(("gold", null, gold));
            foreach (var kv in shards) _lines.Add(("shard", kv.Key, kv.Value));
            chest.Show(r.OpenedChests[0].ChestKey, _lines, _icons);
        }

        // ── 모자라요 ───────────────────────────────────────

        /// <summary>행동력 — 부족(출전에서 막힘) 또는 채우기(위 막대). 젬 채우기를 다 썼거나 젬이 모자라면 버튼이 잿빛.</summary>
        public void ShowStamina(IStaminaService st, IPlayerDataService player, bool shortOnly, Action onRefilled)
        {
            if (st == null || player == null) return;
            var until = st.UntilNext;
            string next = until > TimeSpan.Zero ? $"{(int)until.TotalMinutes:00}:{until.Seconds:00}" : "—";
            int left = st.RefillLeftToday;
            int perDay = player.Config != null ? player.Config.StaminaRefillPerDay : 6;
            bool can = left > 0 && player.Gem >= st.RefillGemCost;
            string title = shortOnly ? Localize.FromTable("ui.rb.notice.stamina_title", "행동력이 부족해요") : Localize.FromTable("ui.rb.notice.stamina_fill", "행동력 채우기");
            string have = st.Current < st.PerRun ? $"<color=#FF5A5A>{st.Current}</color>" : st.Current.ToString();   // 모자란 쪽만 빨갛게
            string line1 = string.Format(Localize.FromTable("ui.rb.notice.need_have", "필요 {0} · 보유 {1}"), st.PerRun, have);
            string line2 = string.Format(Localize.FromTable("ui.rb.notice.next", "다음 회복 {0}"), next);
            string button = left <= 0 ? Localize.FromTable("ui.stamina.refill_none", "오늘 젬 채우기를 다 썼어요")
                          : string.Format(Localize.FromTable("ui.stamina.refill_btn", "젬 {0} 채우기"), st.RefillGemCost);
            string foot = string.Format(Localize.FromTable("ui.stamina.refill_line", "젬 {0} 으로 {1} 채우기 · 오늘 {2}/{3}"),
                                        st.RefillGemCost, st.Max, perDay - left, perDay);
            if (left > 0 && player.Gem < st.RefillGemCost) foot = Localize.FromTable("ui.stamina.fail.gem", "젬이 모자라요");
            ShowShort("rb_icon_currency_energy_96", title, line1, line2, button, can, foot, () =>
            {
                if (st.TryRefillWithGem(out _)) GameSound.Cue("ui.select");
                onRefilled?.Invoke();
            });
        }

        /// <summary>젬이 모자라요 — 「젬 사러 가기」 → 상점 재화 탭.</summary>
        public void ShowGemShort()
        {
            ShowShort("rb_icon_currency_gem_96", Localize.FromTable("ui.rb.notice.gem_title", "젬이 모자라요"),
                      Localize.FromTable("ui.rb.notice.gem_line", "젬이 모자라요. 젬 묶음을 볼까요?"), string.Empty,
                      Localize.FromTable("ui.rb.notice.gem_go", "젬 사러 가기"), true, string.Empty, () => _shell.OpenRail("Gems"));
        }

        private void ShowShort(string icon, string title, string line1, string line2, string button, bool canGo, string foot, Action onGo)
        {
            Awake();
            _onGo = onGo;
            if (_ui.Get<Image>("NotShortIcon") is Image img) img.sprite = RbRewards.Find(_icons, icon);
            _ui.SetText("RbNotShortTitle", title);
            _ui.SetText("NotShortLine1", line1);
            _ui.SetText("NotShortLine2", line2);
            _ui.SetText("NotShortGoText", button);
            _ui.SetText("NotShortFoot", foot);
            if (_ui.Get<Button>("NotShortGo") is Button b) b.interactable = canGo;
            if (_ui.Get<TextMeshProUGUI>("NotShortGoText") is TextMeshProUGUI t) t.color = canGo ? Ink : Dim;
            Open("RbNotShort");
        }

        /// <summary>
        /// 골드가 모자라요 — 필요 / 보유 · 메모 · 대안 버튼(없으면 숨김) · 골드 얻는 곳(챕터 클리어 · 오늘의 상점 · 우편).
        /// 출전(대안 「랜덤으로 출전 · 골드 없음」)과 성장(대안 없음)이 같이 쓴다.
        /// </summary>
        public void ShowGoldShort(int need, int have, string note, string button, Action onGo)
        {
            Awake();
            _onGo = onGo;
            _ui.SetText("NotGoldLine", string.Format(Localize.FromTable("ui.rb.notice.gold_line", "필요 {0:N0} / 보유 <color=#FF5A5A>{1:N0}</color>"), need, have));
            _ui.SetText("NotGoldNote", note ?? string.Empty);
            _ui.SetActive("NotGoldGo", !string.IsNullOrEmpty(button));
            if (!string.IsNullOrEmpty(button)) _ui.SetText("NotGoldGoText", button);
            Open("RbNotGold");
        }

        // ── 구매 확인 ───────────────────────────────────────

        /// <summary>상품 이름 · 값 · 메모(실제 돈이면 스토어 결제 안내). 구매를 누르면 onBuy.</summary>
        public void ShowConfirm(string iconName, string name, string price, string note, Action onBuy)
        {
            Awake();
            _onYes = onBuy;
            if (_ui.Get<Image>("NotConfirmIcon") is Image img)
            {
                var s = RbRewards.Find(_icons, iconName);
                if (s != null) img.sprite = s;
            }
            _ui.SetText("NotConfirmName", name);
            _ui.SetText("NotConfirmPrice", price);
            _ui.SetText("NotConfirmNote", note ?? string.Empty);
            Open("RbNotConfirm");
        }

        /// <summary>안내 창 — 아이콘 · 이름 · 여러 줄 본문 · 확인(상점 상자 확률 보기).</summary>
        public void ShowInfo(string iconName, string name, string body)
        {
            Awake();
            if (_ui.Get<Image>("NotInfoIcon") is Image img)
            {
                var s = RbRewards.Find(_icons, iconName);
                if (s != null) img.sprite = s;
            }
            _ui.SetText("NotInfoName", name);
            _ui.SetText("NotInfoBody", body);
            Open("RbNotInfo");
        }

        /// <summary>상품 고르기(둘까지) — 패스 프리미엄 · 프리미엄 + 단계. 누르면 그 상품의 onBuy.</summary>
        public void ShowOptions(string title, string line, (string name, string price, Action onBuy)[] options)
        {
            Awake();
            _ui.SetText("RbNotOptionsTitle", title);
            _ui.SetText("NotOptionsLine", line);
            for (int i = 0; i < 2; i++)
            {
                bool on = options != null && i < options.Length;
                _ui.SetActive($"NotOption{i}", on);
                _onOption[i] = on ? options[i].onBuy : null;
                if (!on) continue;
                _ui.SetText($"NotOption{i}Name", options[i].name);
                _ui.SetText($"NotOption{i}Price", options[i].price);
            }
            Open("RbNotOptions");
        }

        // ── 열고 닫기 ───────────────────────────────────────

        public bool Back()
        {
            if (_ui == null) return false;
            foreach (var p in Panels)
                if (_ui.Find(p) is Transform t && t.gameObject.activeSelf) { t.gameObject.SetActive(false); return true; }
            return false;
        }

        public void HideAll()
        {
            if (_ui == null) return;
            foreach (var p in Panels) _ui.SetActive(p, false);
        }

        private void Open(string panel)
        {
            _ui.SetActive(panel, true);
            if (_ui.Find(panel) is Transform t) t.SetAsLastSibling();   // 나중에 연 창이 앞
        }

        private void Close(string panel) => _ui.SetActive(panel, false);

        private void Leave(Action go)
        {
            HideAll();
            go();
        }

        private void SetIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }
    }
}
