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
            /// <summary>무리 내부 이름(설명 창을 열 때 쓴다).</summary>
            public readonly string Tag;
            public PartySynCell(string icon, string name, int count, int next, string tag)
            { Icon = icon; Name = name; Count = count; Next = next; Tag = tag; }
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

        // 화면 칸 「무도가 2/3」 의 짧은 이름 — 언어팩 `ui.party.tag.{id}`(내부 태그는 그대로)
        private static string SynShortName(string tag) => TagId(tag) is string id ? Tx($"ui.party.tag.{id}") : tag;

        /// <summary>
        /// 태그 시너지 칸을 채운다 — 화면이 「무도가 2/3」 으로 그린다.
        /// 메인만 있어도(1명) 그 태그를 「1/2」 로 보여 준다(PD 2026-10-29 「메인만 빙의하면 보여야 어떤 애를 얻어야 할지 목표를 정하지」).
        /// 켜진 것(2명 이상)을 앞에, 아직인 것(1명)을 뒤에 — 칸이 모자라면 아직인 것부터 빠진다.
        /// </summary>
        public void FillSynergyCells(List<PartySynCell> into)
        {
            into.Clear();
            SyncPartyKeys();   // 화면이 전투 틱보다 먼저 물으면 지난 몸의 무리가 남았다(2026-10-29 검수)
            if (!PartyOn || Unit.PartyKeys.Count < 1) return;
            _cellTagCount.Clear();
            foreach (var k in Unit.PartyKeys)
            {
                var o = OrgOf(k);
                if (o != null) _cellTagCount[o] = _cellTagCount.TryGetValue(o, out int n) ? n + 1 : 1;
            }
            for (int pass = 0; pass < 2; pass++)
            {
                bool on = pass == 0;
                foreach (var kv in _cellTagCount) AddCell(into, kv.Key, kv.Value, on);
            }
        }

        private static void AddCell(List<PartySynCell> into, string tag, int n, bool on)
        {
            if (n < 1 || (n >= 2) != on) return;
            into.Add(new PartySynCell(SynIcon(tag), SynShortName(tag), n, n >= 2 ? 3 : 2, tag));
        }

        private readonly Dictionary<string, int> _cellTagCount = new();

        /// <summary>지금 켜진 태그 시너지 효과 줄(시너지 설명 창이 띄운다).</summary>
        public IReadOnlyList<string> PartySynergyLines => _synLines;

        // 시너지 칸을 누르면 그 칸 옆 설명 말풍선 — BattleDirector.PartyPopup.PartyOpenSynergyDetail(시안 F_syndetail)

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
            // 시안 S1b — 밝은 말풍선은 무엇이 켜질 때만. 없으면 꼬리표 아래 회색 「시너지 없음」 한 줄(F_possess)
            _bubbleImg.gameObject.SetActive(on);
            _bubbleText.text = on ? Tf("ui.party.bubble.bring", line) : string.Empty;
            _bubbleNone.text = on ? string.Empty : Tx("ui.party.syn.none");
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

        private TMPro.TextMeshProUGUI _bubbleNone;

        /// <summary>
        /// 시안 S1b 그대로 — 머리 위 보라 꼬리표 「👻 빙의 가능」 + 그 오른쪽 아래 밝은 말풍선 「데려오면 ▶ / 무리 2/3 · 효과」.
        /// 묶음 판(240 × 100)의 아래 가운데가 적 머리. 좌표는 시안 px(판 위 왼쪽 기준).
        /// </summary>
        private void EnsureBubble()
        {
            if (_bubble != null || _shotLayer == null) return;
            var go = new GameObject("PartyPossessBubble", typeof(RectTransform));
            _bubble = (RectTransform)go.transform;
            _bubble.SetParent(_shotLayer, false);
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0f, 1f);
            _bubble.pivot = new Vector2(0.3f, 0f);
            _bubble.sizeDelta = new Vector2(240f, 100f);
            if (_popLike == null && _unitLayer != null && _unitLayer.GetComponentInParent<Canvas>() is Canvas cv)
                foreach (var t in cv.rootCanvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (t.name == "RbNoBodyText") { _popLike = t; break; }
            var box = new MBox(0, 0, 240, 100);
            // 꼬리표 — 시안 (362,792)-(494,826) · 묶음 판 기준 (0,0)-(132,34)
            // 꼬리표는 늘리지 않는다 — 왼쪽 유령 얼굴이 9분할 세로 칸에 걸려 눌렸다(2026-10-29 검수). 그림 크기(137 × 42) 그대로
            var pill = MImg("Pill", _bubble, box, new MBox(0, 0, 137, 42), "possess_pill");
            if (pill.sprite == null) { pill.sprite = GetSprite("possess_bubble_syn"); pill.type = UnityEngine.UI.Image.Type.Sliced; pill.color = new Color(0.55f, 0.3f, 0.95f); }
            MTxt(_bubble, box, new MBox(42, 2, 132, 32), Tx("ui.party.p2.possess_tag"), 21, Color.white,
                 TMPro.TextAlignmentOptions.Center);
            // 「시너지 없음」 — 꼬리표 바로 아래 회색 한 줄
            _bubbleNone = MTxt(_bubble, box, new MBox(0, 44, 137, 66), string.Empty, 16, new Color(0.86f, 0.87f, 0.92f),
                               TMPro.TextAlignmentOptions.Center);
            // 밝은 말풍선 — 시안 (428,832)-(596,886) · 묶음 판 기준 (66,40)-(234,94)
            _bubbleImg = MImg("Bubble", _bubble, box, new MBox(66, 44, 236, 100), "possess_bubble_light", true);
            if (_bubbleImg.sprite == null) { _bubbleImg.sprite = GetSprite("possess_bubble_none"); _bubbleImg.type = UnityEngine.UI.Image.Type.Sliced; }
            var bb = new MBox(66, 44, 236, 100);
            _bubbleText = MTxt(_bubbleImg.rectTransform, bb, new MBox(78, 48, 230, 92), string.Empty, 18, new Color(0.1f, 0.11f, 0.16f),
                               TMPro.TextAlignmentOptions.MidlineLeft);
            _bubbleText.lineSpacing = -8f;
            if (GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady)
                lang.ApplyFonts(_bubble);
        }
    }
}
