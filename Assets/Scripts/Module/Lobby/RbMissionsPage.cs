using System;
using System.Collections.Generic;
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
    /// RE:BORN 미션(시안 Screens/missions · mission-complete) — 일일 · 주간 · 업적 탭. 업적은 미션 안의 탭(PD 2026-10-23) — 지금은 준비 중.
    /// 진행 · 받기는 ISeasonService 그대로. 「전부 완료」 미션은 맨 위 큰 줄로 따로 그린다.
    /// </summary>
    public sealed class RbMissionsPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _icons = Array.Empty<Sprite>();
        [SerializeField] private Sprite _btnClaim, _btnIdle;

        private const int Rows = 8;
        private static readonly Color TabOn = Color.white, TabOff = new(0.70f, 0.74f, 0.84f);

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private int _tab;   // 0 일일 · 1 주간 · 2 업적
        private readonly List<Transform> _rows = new();

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("MisBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("MisHelp", () => Toast(Localize.FromTable("ui.rb.mis.help", "미션을 끝내면 보상과 패스 경험치를 받아요.\n일일은 매일 · 주간은 매주 초기화돼요.")));
            for (int i = 0; i < 3; i++)
            {
                int t = i;
                _ui.OnClick($"MisTab{i}", () => { _tab = t; GameSound.Cue("ui.tap"); Refresh(); });
            }
            _ui.OnClick("MisClaimAll", ClaimAll);
            var tpl = _ui.Find("MisRowTemplate");
            if (tpl == null) return;
            var start = ((RectTransform)tpl).anchoredPosition;
            float step = ((RectTransform)tpl).sizeDelta.y + 14f;
            for (int i = 0; i < Rows; i++)
            {
                var r = i == 0 ? tpl : Instantiate(tpl.gameObject, tpl.parent).transform;
                r.name = $"MisRow{i}";
                ((RectTransform)r).anchoredPosition = start + new Vector2(0f, -step * i);
                _rows.Add(r);
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < 3; i++)
            {
                _ui.SetActive($"MisTab{i}On", i == _tab);
                if (_ui.Get<TextMeshProUGUI>($"MisTab{i}Text") is TextMeshProUGUI t) t.color = i == _tab ? TabOn : TabOff;
            }
            bool ach = _tab == 2;
            _ui.SetActive("MisSoon", ach);
            _ui.SetActive("MisAll", !ach);
            _ui.SetActive("MisClaimAll", !ach);
            foreach (var r in _rows) r.gameObject.SetActive(false);
            if (ach || !CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady) return;

            var list = s.Missions(_tab == 0 ? MissionPeriod.Daily : MissionPeriod.Weekly);
            int shown = 0, done = 0, total = 0;
            MissionState? all = null;
            foreach (var m in list)
            {
                if (m.Def.Type.EndsWith("_all_done")) { all = m; continue; }
                total++;
                if (m.Done) done++;
                if (shown >= _rows.Count) continue;
                FillRow(_rows[shown++], m);
            }
            // 맨 위 — 「오늘 미션 전부 완료 3/6개 달성」 + 보상
            var t2 = s.UntilDayReset;
            _ui.SetText("MisAllTitle", Localize.FromTable(_tab == 0 ? "ui.rb.mis.all_daily" : "ui.rb.mis.all_weekly", _tab == 0 ? "◆ 오늘 미션 전부 완료" : "◆ 이번 주 미션 전부 완료"));
            _ui.SetText("MisAllCount", string.Format(Localize.FromTable("ui.rb.mis.count", "{0}/{1}개 달성"), done, total));
            if (_ui.Find("MisAllFill") is RectTransform f) f.anchorMax = new Vector2(total > 0 ? (float)done / total : 0f, 1f);
            string allReward = all.HasValue ? all.Value.Def.Rewards : string.Empty;
            var parsed = RbRewards.Parse(allReward);
            if (_ui.Get<Image>("MisAllIcon") is Image ai) { ai.sprite = parsed.Count > 0 ? RbRewards.IconOf(_icons, parsed[0].kind, parsed[0].key) : null; ai.enabled = ai.sprite != null; }
            _ui.SetText("MisAllReward", parsed.Count > 0 ? $"{RbRewards.LabelOf(parsed[0].kind, parsed[0].key)} {parsed[0].amount}" : string.Empty);
            var allBtn = _ui.Get<Button>("MisAllClaim");
            if (allBtn != null)
            {
                bool can = all.HasValue && all.Value.Claimable;
                allBtn.gameObject.SetActive(can);
                allBtn.onClick.RemoveAllListeners();
                if (can) { string id = all.Value.Def.Id; allBtn.onClick.AddListener(() => ClaimOne(id)); }
            }
            _ui.SetText("MisReset", string.Format(Localize.FromTable("ui.pass.reset_in", "초기화까지 {0}"),
                _tab == 0 ? $"{(int)t2.TotalHours:00}:{t2.Minutes:00}:{t2.Seconds:00}"
                          : string.Format(Localize.FromTable("ui.rb.mis.days", "{0}일"), Math.Max(1, (int)Math.Ceiling(s.UntilWeekReset.TotalDays)))));
            if (_ui.Get<Button>("MisClaimAll") is Button ca) ca.interactable = s.HasClaimableMission;
        }

        private void FillRow(Transform row, MissionState m)
        {
            row.gameObject.SetActive(true);
            TextIn(row, "MisRowName", Localize.Get($"mission.{m.Def.Type}"));
            TextIn(row, "MisRowCount", $"{Math.Min(m.Progress, m.Def.Target)}/{m.Def.Target}");
            if (_ui.Find(row, "MisRowFill") is RectTransform f) f.anchorMax = new Vector2(m.Def.Target > 0 ? Mathf.Clamp01((float)m.Progress / m.Def.Target) : 0f, 1f);
            var parsed = RbRewards.Parse(m.Def.Rewards);
            string kind = parsed.Count > 0 ? parsed[0].kind : "passxp";
            int amount = parsed.Count > 0 ? parsed[0].amount : m.Def.PassXp;
            if (_ui.Find(row, "MisRowRewardIcon")?.GetComponent<Image>() is Image ri) { ri.sprite = RbRewards.IconOf(_icons, kind, parsed.Count > 0 ? parsed[0].key : null); ri.enabled = ri.sprite != null; }
            TextIn(row, "MisRowReward", amount.ToString("N0"));
            var b = _ui.Find(row, "MisRowClaim");
            if (b == null) return;
            var btn = b.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.interactable = m.Claimable;
            if (b.GetComponent<Image>() is Image img) img.sprite = m.Claimable ? _btnClaim : _btnIdle;
            TextIn(b, "MisRowClaimText", m.Claimed ? Localize.FromTable("ui.pass.claimed", "받음")
                                       : m.Claimable ? Localize.FromTable("ui.pass.claim", "받기")
                                       : m.Locked ? Localize.FromTable("ui.pass.locked", "잠김")
                                       : Localize.FromTable("ui.pass.doing", "진행 중"));
            if (m.Claimable) { string id = m.Def.Id; btn.onClick.AddListener(() => ClaimOne(id)); }
        }

        private void ClaimOne(string id)
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.TryClaimMission(id, out var r)) return;
            GameSound.Cue("run.gold");
            Toast(RbRewards.Received(r));
            _shell.RefreshHeader();
            Refresh();
        }

        private void ClaimAll()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            bool any = false;
            foreach (var m in s.Missions(_tab == 0 ? MissionPeriod.Daily : MissionPeriod.Weekly))
                if (m.Claimable && s.TryClaimMission(m.Def.Id, out _)) any = true;
            if (any) { GameSound.Cue("run.gold"); _shell.RefreshHeader(); }
            Refresh();
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string message) => SystemPopup.Show(message, null, Localize.Get("ui.common.ok"), null);
    }
}
