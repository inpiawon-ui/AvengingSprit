using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 화면이 묻는 창구(PD 2026-10-29 확정 틀 S1b · 시안 `_exchange/in/mock_partyui_S1b.png` · `F_states`).
    /// 화면(`InGameMainUI.Party`)은 이것만 읽는다 — 파티 계산은 여기 있고, 그리는 쪽은 그림 · 자리만 맡는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>왼쪽 시너지 칸 하나 — 문장 그림 이름 · 이름 · 모은 수 · 다음 단계 수.</summary>
        public readonly struct PartySynCell
        {
            public readonly string Icon;
            public readonly string Name;
            public readonly int Count;
            public readonly int Next;
            public PartySynCell(string icon, string name, int count, int next)
            { Icon = icon; Name = name; Count = count; Next = next; }
        }

        public bool PartyEnabled => PartyOn;
        public int PartySubCount => _subs.Count;
        public string PartySubKey(int i) => i >= 0 && i < _subs.Count && _subs[i].U != null ? _subs[i].U.Key : null;
        public string PartyMainKey => _host != null ? _host.Key : null;
        public string PartyHostName(string key) => key != null ? HostName(key) : null;

        /// <summary>인게임 아틀라스의 그림을 이름으로(시너지 문장 · 칸 바탕).</summary>
        public Sprite PartySprite(string name) => GetSprite(name);

        /// <summary>파티 화면이 다시 그릴지 가늠하는 값 — 메인 · 보조 · 시너지 · 인연이 바뀌면 달라진다.</summary>
        public int PartyHudVersion
        {
            get
            {
                int h = _host != null ? _host.Key.GetHashCode() : 0;
                for (int i = 0; i < _subs.Count; i++) h = h * 31 + (_subs[i].U != null ? _subs[i].U.Key.GetHashCode() : 7);
                h = h * 31 + _synLines.Count;
                for (int i = 0; i < _synLines.Count; i++) h = h * 31 + _synLines[i].GetHashCode();
                return h * 31 + (_inyeonKey != null ? _inyeonKey.GetHashCode() : 3);
            }
        }

        /// <summary>보조 칸 비용(「+ −10」) — 지금 칸에 들일 때 드는 유령 HP.</summary>
        public int PartySubCost => PartyRule.CostSub;

        /// <summary>보조 칸을 눌렀다 — 그 보조와 메인을 바꿀지 묻는 창을 띄운다.</summary>
        public void PartyTapSub(int slot)
        {
            if (!PartyOn || _partyHold || slot < 0 || slot >= _subs.Count) return;
            OpenPartyPortraitSwap(slot);
        }

        private static string SynIcon(string tag) => tag switch
        {
            "파워" => "syn_icon_force", "무기" => "syn_icon_blade", "마법" => "syn_icon_magic",
            "갱단" => "syn_icon_gang", "코만도 부대" => "syn_icon_commando", "무도가" => "syn_icon_martial",
            "괴물" => "syn_icon_monster", "마도" => "syn_icon_mystic", _ => null,
        };

        private static string SynShortName(string tag) => tag == "코만도 부대" ? "코만도" : tag;

        /// <summary>
        /// 켜진 태그 시너지 칸(2명 이상)을 채운다 — 화면이 「무도가 2/3」 으로 그린다.
        /// 켜지지 않은 태그(1명)는 그리지 않는다(PD 「시너지 결정한 정보」 자리 — 최대한 작게).
        /// </summary>
        public void FillSynergyCells(List<PartySynCell> into)
        {
            into.Clear();
            if (!PartyOn || Unit.PartyKeys.Count < 2) return;
            int force = 0, blade = 0, magic = 0;
            _cellTagCount.Clear();
            foreach (var k in Unit.PartyKeys)
            {
                var a = AffinityRule.KindOf(k);
                if (a == Affinity.Force) force++; else if (a == Affinity.Blade) blade++; else if (a == Affinity.Magic) magic++;
                var o = OrgOf(k);
                if (o != null) _cellTagCount[o] = _cellTagCount.TryGetValue(o, out int n) ? n + 1 : 1;
            }
            AddCell(into, "파워", force);
            AddCell(into, "무기", blade);
            AddCell(into, "마법", magic);
            foreach (var kv in _cellTagCount) AddCell(into, kv.Key, kv.Value);
        }

        private static void AddCell(List<PartySynCell> into, string tag, int n)
        {
            if (n < 2) return;
            into.Add(new PartySynCell(SynIcon(tag), SynShortName(tag), n, 3));
        }

        private readonly Dictionary<string, int> _cellTagCount = new();

        /// <summary>지금 켜진 태그 시너지 효과 줄(시너지 설명 창이 띄운다).</summary>
        public IReadOnlyList<string> PartySynergyLines => _synLines;

        /// <summary>시너지 칸을 눌렀다 — 설명 창을 띄우고 게임을 멈춘다(PD 「눌렀을 때 띄운다」).</summary>
        public void PartyOpenSynergyDetail()
        {
            if (!PartyOn || _partyHold) return;
            var opts = new List<PartyOption>();
            if (_inyeonKey != null)
                opts.Add(new PartyOption($"인연 · {InyeonName(_inyeonKey)}", InyeonDesc(_inyeonKey), true, null));
            else if (_host != null && InyeonOf(_host.Key) != null)
            {
                var names = new List<string>();
                foreach (var p in InyeonPartners(_host.Key)) names.Add(HostName(p));
                opts.Add(new PartyOption($"인연 · {InyeonName(_host.Key)} (꺼짐)",
                                         $"짝 {string.Join(" · ", names)} 을(를) 보조로 데려오면 — {InyeonDesc(_host.Key)}", false, null));
            }
            for (int i = 0; i < _synLines.Count; i++) opts.Add(new PartyOption(_synLines[i], "메인에게 걸린다 · 보조는 공속만 절반", true, null));
            if (_synLines.Count == 0) opts.Add(new PartyOption("시너지 없음", "같은 속성 · 같은 조직 2명부터 켜진다 · 3명이면 강해진다", false, null));
            opts.Add(new PartyOption("닫기", string.Empty, true, null));
            ShowPartyPopup("시너지", "탭하면 닫힌다 — 그동안 게임은 멈춘다", opts, -1f);
        }

        // ── 빙의 가능 말풍선(PD 「빙의 가능일 때 어떤 시너지 효과가 나는지 적 호스트 머리 위에」) ──────────
        // 금빛 = 데려오면 인연 · 시너지가 켜짐 · 회색 = 시너지 없음. 빙의 가능한 적 하나에만(가장 가까운 것) 띄운다 —
        // 여럿 다 띄우면 화면이 글자로 덮인다(PD 「게임 하는 데 지장 없게」).
        private RectTransform _bubble;
        private UnityEngine.UI.Image _bubbleImg;
        private TMPro.TextMeshProUGUI _bubbleText;
        private Unit _bubbleFor;
        private string _bubbleSig;
        private const float BubbleLift = 96f;

        private void TickPossessBubble()
        {
            var target = PartyOn && _host != null && !_partyHold ? NearestPossessable() : null;
            if (target == null)
            {
                if (_bubble != null && _bubble.gameObject.activeSelf) _bubble.gameObject.SetActive(false);
                _bubbleFor = null;
                return;
            }
            EnsureBubble();
            if (_bubble == null) return;
            if (!_bubble.gameObject.activeSelf) _bubble.gameObject.SetActive(true);
            _bubble.anchoredPosition = target.Position + Vector2.up * BubbleLift;

            string sig = target.Key + "|" + Unit.PartyKeys.Count + "|" + _inyeonKey;
            if (target == _bubbleFor && sig == _bubbleSig) return;
            _bubbleFor = target;
            _bubbleSig = sig;
            string line = PartyHintFor(target.Key, out bool inyeon);
            bool on = line != null;
            _bubbleImg.sprite = GetSprite(on ? "possess_bubble_syn" : "possess_bubble_none");
            // 금빛 말풍선 안쪽은 크림색 — 글자는 짙게(부품 검수)
            _bubbleText.color = on ? new Color(0.29f, 0.18f, 0.05f) : new Color(0.86f, 0.86f, 0.9f);
            _bubbleText.text = on ? $"데려오면 ▶\n{line}" : "시너지 없음";
        }

        private Unit NearestPossessable()
        {
            if (_host == null) return null;
            Unit best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsPossessable || !e.IsHostBody) continue;
                float d = (e.Position - _host.Position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private void EnsureBubble()
        {
            if (_bubble != null || _shotLayer == null) return;
            var go = new GameObject("PartyPossessBubble", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _bubble = (RectTransform)go.transform;
            _bubble.SetParent(_shotLayer, false);
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0f, 1f);
            _bubble.pivot = new Vector2(0.5f, 0f);
            _bubble.sizeDelta = new Vector2(220f, 70f);
            _bubbleImg = go.GetComponent<UnityEngine.UI.Image>();
            _bubbleImg.type = UnityEngine.UI.Image.Type.Sliced;
            _bubbleImg.raycastTarget = false;
            // 두 줄(「데려오면 ▶」 / 효과) — 꼬리(아래 12)를 빼고 판 안쪽(테 16)에 들어가게
            _bubbleText = NewText(_bubble, string.Empty, 15, Color.white, new Vector2(0f, 6f), new Vector2(192f, 46f), true);
            if (GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady)
                lang.ApplyFonts(_bubble);
        }
    }
}
