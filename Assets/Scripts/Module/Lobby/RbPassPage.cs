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

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private readonly List<Transform> _rows = new();
        private float _rowStep;
        private bool _busy;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("PasBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("PasHelp", () => Toast(Localize.FromTable("ui.rb.pass.help", "미션을 끝내 패스 경험치를 모으면 단계마다 보상을 받아요.\n프리미엄을 열면 오른쪽 보상도 받을 수 있어요.")));
            _ui.OnClick("PasMissions", () => _shell.OpenRail("Missions"));
            _ui.OnClick("PasClaimAll", ClaimAll);
            _ui.OnClick("PasPremium", BuyPremium);
            var tpl = _ui.Find("PasRowTemplate");
            if (tpl != null) { _rowStep = ((RectTransform)tpl).sizeDelta.y + 10f; tpl.gameObject.SetActive(false); }
        }

        public void Open()
        {
            gameObject.SetActive(true);
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
                _ui.SetText("PasPremiumText", $"{Localize.FromTable("ui.pass.premium", "프리미엄 열기")}  {store.PriceLabel(p)}");
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
                // 「현재」 표시 — 다음에 열릴 단계(다 열었으면 마지막)
                _ui.Find(_rows[i], "PasRowNow")?.gameObject.SetActive(level == Math.Min(s.Tier + 1, s.TierMax));
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
            TextIn(row, p + "State", claimed ? "✓ " + Localize.FromTable("ui.pass.claimed", "받음")
                                   : can ? Localize.FromTable("ui.pass.claim", "받기")
                                   : unlocked ? string.Empty : Localize.FromTable("ui.pass.locked", "잠김"));
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
            _shell.Notice.ShowConfirm("rb_icon_pass_xp_96", string.Format(Localize.FromTable("ui.rb.notice.buy_name", "{0} 구매"), Localize.Get(p.NameKey)), store.PriceLabel(p),
                Localize.FromTable("ui.rb.notice.store_pay", "결제는 스토어 계정으로 진행됩니다"),
                () => BuyAsync(store, p.Id).Forget());   // fire-and-forget: 결과는 다시 그린다
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

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
