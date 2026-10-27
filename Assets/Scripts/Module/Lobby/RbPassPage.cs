using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 시즌 패스(시안 Screens/pass · pass-options) — 시즌 배너 · 레벨 막대 · 단계 줄(무료 | 레벨 | 프리미엄) · 모두 받기 · 프리미엄 열기.
    /// 단계 · 받기 · 사기는 ISeasonService · IStoreService(pass_premium) 그대로. 단계 줄은 본보기를 단계 수만큼 복제한다.
    /// </summary>
    public sealed class RbPassPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();
        [SerializeField] private Sprite _cellClaim, _cellIdle, _cellDone;
        // 레벨 줄 — 마디(현재 금빛 · 지나온 하늘색 · 아직 회색) · 줄 토막(지나온 하늘색 · 아직 회색). 시안 mock_pass_track
        [SerializeField] private Sprite _nodeNow, _nodeOn, _nodeOff, _lineOn, _lineOff;

        // 시안 pass — 받음 초록 · 받기 노랑 · 아직이면 보상 이름(흐린 글자) · 「현재」 줄 숫자는 노랗게 위로
        private static readonly Color StateDone = new(0.36f, 0.86f, 0.55f, 1f);
        private static readonly Color StateClaim = new(1f, 0.79f, 0.24f, 1f);
        private static readonly Color StateDim = new(0.62f, 0.66f, 0.78f, 1f);
        private static readonly Color LvOn = new(0.24f, 0.90f, 1f, 1f);
        private static readonly Color LvOff = new(0.55f, 0.60f, 0.70f, 1f);
        private const float NowGap = 34f;   // 「현재」 글자만큼 아래 줄 토막을 줄인다(게임 px)
        private const string RewardHost = "vampire";   // 이번 시즌 프리미엄 Lv 30 보상 호스트(배너 얼굴)

        private UIBinder _ui;
        private float _downBaseY = float.NaN, _downBaseH;
        private RbLobbyUI _shell;
        private readonly List<Transform> _rows = new();
        private float _rowStep;
        private bool _busy;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("PasBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("PasHelp", () => _shell.ShowGuide("pass"));   // 시안 guide
            _ui.OnClick("PasMissions", () => _shell.OpenRail("Missions"));
            _ui.OnClick("PasClaimAll", ClaimAll);
            _ui.OnClick("PasPremium", BuyPremium);
            var tpl = _ui.Find("PasRowTemplate");
            if (tpl != null) { _rowStep = ((RectTransform)tpl).sizeDelta.y + 10f; tpl.gameObject.SetActive(false); }
        }

        public void Open(UnityEngine.U2D.SpriteAtlas faces)
        {
            gameObject.SetActive(true);
            if (faces != null && _ui.Get<Image>("PasFace") is Image face && faces.GetSprite($"face_{RewardHost}") is Sprite fs) face.sprite = fs;
            Refresh();
        }

        private void Refresh()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady) return;
            var t = s.UntilEnd;
            _ui.SetText("PasLeft", string.Format(Localize.FromTable("ui.rb.pass.left", "남은 기간 {0}일"), Math.Max(0, t.Days)));
            bool max = s.Tier >= s.TierMax;
            _ui.SetText("PasLevel", max ? $"Lv {s.Tier} · MAX" : $"Lv {s.Tier} · {s.XpInTier}/{s.XpPerTier}");
            if (_ui.Find("PasXpFill") is RectTransform f) f.anchorMax = new Vector2(max ? 1f : Mathf.Clamp01((float)s.XpInTier / Mathf.Max(1, s.XpPerTier)), 1f);
            _ui.SetActive("PasMissionsDot", s.HasClaimableMission);
            _ui.SetActive("PasPremium", !s.HasPremium);
            if (CoreModule.TryGet<IStoreService>(out var store) && store.Find("pass_premium") is StoreProduct p)
                _ui.SetText("PasPremiumPrice", store.PriceLabel(p));   // 글자(「프리미엄 열기」)는 칸을 나눠 따로 둔다(시안)
            if (_ui.Get<Button>("PasClaimAll") is Button ca) ca.interactable = s.HasClaimable;

            var tiers = s.Tiers;
            var tpl = _ui.Find("PasRowTemplate");
            var content = tpl != null ? tpl.parent as RectTransform : null;
            if (tpl == null || content == null) return;
            while (_rows.Count < tiers.Count)
            {
                var r = Instantiate(tpl.gameObject, content).transform;
                r.name = $"PasRow{_rows.Count}";
                ((RectTransform)r).anchoredPosition = ((RectTransform)tpl).anchoredPosition + new Vector2(0f, -_rowStep * _rows.Count);
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(r);
                _rows.Add(r);
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, -((RectTransform)tpl).anchoredPosition.y + _rowStep * tiers.Count + 20f);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool on = i < tiers.Count;
                _rows[i].gameObject.SetActive(on);
                if (!on) continue;
                int level = i + 1;
                TextIn(_rows[i], "PasRowLv", level.ToString());
                // 레벨 마디 — 현재(다음에 열릴 단계, 다 열었으면 마지막) 금빛 · 지나온 하늘색 · 아직 회색. 「현재」 는 마디 아래
                int cur = Math.Min(s.Tier + 1, s.TierMax);
                bool now = level == cur, passed = level < cur, next = level == cur + 1;   // 시안 — 바로 다음 레벨까지 하늘색
                _ui.Find(_rows[i], "PasRowNow")?.gameObject.SetActive(now);
                SetSprite(_rows[i], "PasRowNode", now ? _nodeNow : passed || next ? _nodeOn : _nodeOff);
                SetSprite(_rows[i], "PasRowLineUp", level <= cur + 1 ? _lineOn : _lineOff);   // 앞 줄에서 여기까지(다음 레벨까지 이어진다)
                SetSprite(_rows[i], "PasRowLineDown", level <= cur ? _lineOn : _lineOff);      // 여기서 다음 레벨로
                if (_ui.Find(_rows[i], "PasRowLineDown") is RectTransform down)
                {
                    if (float.IsNaN(_downBaseY)) { _downBaseY = down.anchoredPosition.y; _downBaseH = down.sizeDelta.y; }
                    // 「현재」 글자 자리만큼 아래 토막 위쪽을 줄인다(중심을 내려 위만 잘리게)
                    float cut = now ? NowGap : 0f;
                    down.sizeDelta = new Vector2(down.sizeDelta.x, _downBaseH - cut);
                    down.anchoredPosition = new Vector2(down.anchoredPosition.x, _downBaseY - cut * 0.5f);
                }
                if (_ui.Find(_rows[i], "PasRowLv")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI lt)
                    lt.color = now ? StateClaim : passed || next ? LvOn : LvOff;
                Cell(_rows[i], "PasRowFree", tiers[i].Free, level, false, s, true);
                Cell(_rows[i], "PasRowPrem", tiers[i].Premium, level, true, s, s.HasPremium);
            }
        }

        private void Cell(Transform row, string p, string rewards, int level, bool premium, ISeasonService s, bool unlocked)
        {
            var parsed = RbRewards.Parse(rewards);
            if (_ui.Find(row, p + "Icon")?.GetComponent<Image>() is Image ic) { ic.sprite = parsed.Count > 0 ? RbRewards.IconOf(_icons, parsed[0].kind, parsed[0].key) : null; ic.enabled = ic.sprite != null; }
            TextIn(row, p + "Amount", parsed.Count > 0 ? parsed[0].amount.ToString("N0") : string.Empty);
            bool claimed = s.IsClaimed(level, premium), can = s.CanClaim(level, premium);
            string label = parsed.Count > 0 ? RbRewards.LabelOf(parsed[0].kind, parsed[0].key) : string.Empty;
            TextIn(row, p + "State", claimed ? "✓ " + Localize.FromTable("ui.pass.claimed", "받음")
                                   : can ? Localize.FromTable("ui.pass.claim", "받기") : label);
            if (_ui.Find(row, p + "State")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI st)
                st.color = claimed ? StateDone : can ? StateClaim : StateDim;
            _ui.Find(row, p + "Lock")?.gameObject.SetActive(!unlocked);
            var cell = _ui.Find(row, p);
            if (cell != null && cell.GetComponent<Image>() is Image bg) bg.sprite = claimed ? _cellDone : can ? _cellClaim : _cellIdle;
            if (cell != null && cell.GetComponent<Button>() is Button b)
            {
                b.onClick.RemoveAllListeners();
                b.interactable = can;
                if (can) b.onClick.AddListener(() => Claim(level, premium));
            }
        }

        private void Claim(int level, bool premium)
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.TryClaim(level, premium, out var r)) return;
            GameSound.Cue("run.gold");
            _shell.Notice.ShowReward(r);
            _shell.RefreshHeader();
            Refresh();
        }

        private void ClaimAll()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.TryClaimAll(out var r)) return;
            GameSound.Cue("run.gold");
            _shell.Notice.ShowReward(r);
            _shell.RefreshHeader();
            Refresh();
        }

        private void BuyPremium()
        {
            if (_busy || !CoreModule.TryGet<IStoreService>(out var store) || store.Find("pass_premium") is not StoreProduct p) return;
            // 시안 pass-options — 「프리미엄 열기」 · 「프리미엄 + 단계」 둘 중 고른다(묶음 상품이 있으면)
            var options = new List<(string, string, Action)>
            {
                (Localize.Get(p.NameKey), store.PriceLabel(p), () => BuyAsync(store, p.Id).Forget()),   // fire-and-forget: 결과는 다시 그린다
            };
            if (store.Find("pass_bundle") is StoreProduct bundle)
                options.Add((Localize.Get(bundle.NameKey), store.PriceLabel(bundle), () => BuyAsync(store, bundle.Id).Forget()));   // fire-and-forget: 결과는 다시 그린다
            _shell.Notice.ShowOptions(Localize.FromTable("ui.rb.pass.open_premium", "프리미엄 열기"),
                                      Localize.FromTable("ui.rb.pass.premium_line", "지금, 복수의 시간이 시작된다."), options.ToArray());
        }

        private async UniTaskVoid BuyAsync(IStoreService store, string id)
        {
            _busy = true;
            try
            {
                var (outcome, _) = await store.BuyAsync(id);
                if (this == null) return;
                if (outcome == PurchaseOutcome.Granted) GameSound.Cue("run.card");
                Refresh();
            }
            finally { _busy = false; }
        }

        private void SetSprite(Transform root, string node, Sprite sprite)
        {
            if (sprite != null && _ui.Find(root, node)?.GetComponent<Image>() is Image img) img.sprite = sprite;
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
