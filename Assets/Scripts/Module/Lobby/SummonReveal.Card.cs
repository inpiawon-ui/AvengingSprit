using System;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 소환 결과 카드 — 호스트 목록 카드와 같은 틀(PD 2026-10-27 「기존 카드 프레임 정보처럼 정리」 · 시안 `_exchange/mock_summon_card`).
    /// 1회: 아래 정보 판 = 도감 카드 한 장(그대로) + 오른쪽에 이름 · 등급 · 속성 · 별 · 조각 진행 막대(가진 것 → 이번에 받은 것).
    /// 10회: 배양조마다 밑에 작은 카드 = 틀 · 속성 · 등급 · 이름 · 조각 ×n · NEW. 얼굴 · 별은 없다(바로 위에 몸이 서 있고 칸이 좁다).
    /// 자리 숫자는 시안 판(540 폭 · 1200 높이)을 720 · 1280 으로 옮긴 값이다.
    /// </summary>
    public sealed partial class SummonReveal
    {
        /// <summary>등급(R · SR · SSR) 틀 — 호스트 목록과 같은 그림(SSR 은 은빛 틀 × 금색).</summary>
        [SerializeField] private Sprite[] _cardFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _cardTags = Array.Empty<Sprite>();
        /// <summary>속성 아이콘 — 무기 · 파워 · 마법(Affinity 값 − 1).</summary>
        [SerializeField] private Sprite[] _affIcons = Array.Empty<Sprite>();
        [SerializeField] private Sprite _rankGold, _rankSilver, _shardIcon, _badgeNew, _barTrack, _barFill;

        // 시안 카드 빛 색(R #4fa3ff · SR #b67cff · SSR #ffc846) — 호스트 목록 GlowColors 와 같다
        private static readonly Color[] CardLine = { new(0.31f, 0.64f, 1f), new(0.71f, 0.49f, 1f), new(1f, 0.78f, 0.27f) };
        private static readonly Color CardFill = Color.white;   // 판 그림 제 남색 그대로 · 불투명(시안) — 어둡게 곱하고 0.97 로 두니 뒤 배양실이 비쳤다
        private static readonly Color AffBox = new(0.06f, 0.1f, 0.2f, 1f);
        private static readonly Color SubText = new(0.73f, 0.78f, 0.9f);
        private static readonly Color AddText = new(0.37f, 0.94f, 0.79f);
        private const float MockToGame = 720f / 540f;

        // ── 1회 정보 판 ─────────────────────────────────────────

        /// <summary>1회 아래 정보 판 — 공개 0.6초 뒤 왼쪽에서 OutBack 으로 들어온다(옛 이름 판 자리 · 박자).</summary>
        private void MakeDock(Slot s)
        {
            int tier = Mathf.Clamp(s.E.Grade, 0, 2);
            float w = U - M(32f), h = P(232f * 1280f / 1200f);
            var dock = Layer("Dock", s.Root, false);
            dock.anchorMin = dock.anchorMax = new Vector2(0.5f, 0.5f);
            dock.sizeDelta = new Vector2(w, h);
            dock.gameObject.AddComponent<CanvasGroup>();
            OverParticles(dock);
            s.Dock = dock;
            Framed(dock, tier);

            // 왼쪽 = 도감 카드 그대로
            var lobby = GetComponentInParent<RbLobbyUI>();
            CoreModule.TryGet<IPlayerDataService>(out var player);
            var host = FindHost(player, s.E.HostKey);
            var card = lobby != null && lobby.HostPage != null ? lobby.HostPage.NewCard(dock, host, player, lobby.Faces) : null;
            float cardW = M(166f);
            if (card != null)
            {
                card.anchorMin = card.anchorMax = new Vector2(0f, 0.5f);
                card.pivot = new Vector2(0f, 0.5f);
                // 시안 카드 166 × 194 — 판 위아래에 여백이 남게 높이도 맞춘다
                float k = Mathf.Min(cardW / Mathf.Max(1f, card.sizeDelta.x), M(186f) / Mathf.Max(1f, card.sizeDelta.y));
                card.localScale = Vector3.one * k;
                card.anchoredPosition = new Vector2(M(16f), 0f);
            }

            // 위 꼬리표 「새 몸! · 획득!」 + NEW
            var head = Box(dock, "Head", M(18f), -M(16f), M(92f), M(32f));
            Framed(head, tier);
            var headText = Label(head, "Text", 0f, 0f, head.sizeDelta.x, head.sizeDelta.y, M(15f), CardLine[tier], TextAlignmentOptions.Center);
            headText.text = Localize.Get(s.E.TagKey);
            if (s.E.IsNew && _badgeNew != null)
            {
                var nb = Box(dock, "New", w - M(64f), -M(20f), M(72f), M(40f));
                Pic(nb, _badgeNew);
            }

            // 오른쪽 = 이름 · 등급 · 속성 · 별 · 조각
            float x = M(198f), right = w - x - M(16f);
            var name = Label(dock, "Name", x, M(22f), right, M(38f), M(30f), Color.white, TextAlignmentOptions.Left);
            name.text = s.E.Name;
            var tag = Box(dock, "Grade", x, M(66f), M(38f), M(24f));
            Pic(tag, tier < _cardTags.Length ? _cardTags[tier] : null);
            int aff = (int)AffinityRule.KindOf(s.E.HostKey) - 1;
            var affRt = Box(dock, "Aff", x + M(46f), M(67f), M(22f), M(22f));
            Pic(affRt, aff >= 0 && aff < _affIcons.Length ? _affIcons[aff] : null);
            var affText = Label(dock, "AffText", x + M(74f), M(64f), right - M(74f), M(28f), M(15f), SubText, TextAlignmentOptions.Left);
            affText.text = string.Format(Localize.FromTable("ui.rb.summon.card.aff", "{0}형"), Localize.Get(AffKey(aff)));
            var stars = Box(dock, "Stars", x, M(98f), M(160f), M(24f));
            for (int i = 0; i < 5; i++) Box(stars, $"Star{i}", 0f, 0f, 1f, 1f).gameObject.AddComponent<Image>().raycastTarget = false;
            StarRank.Apply(stars, "Star", player != null ? player.GetMastery(s.E.HostKey) : 0, _rankGold, _rankSilver,
                           new Vector2(-M(80f) + StarRowWidth(player, s.E.HostKey) * 0.5f, 0f), M(20f), M(3f));

            float y = M(134f);
            if (s.E.IsNew) Chip(dock, x, y, Localize.FromTable("ui.rb.summon.card.unlock", "새 호스트 해금! · 이제 출전할 수 있어요"), true);
            else if (s.E.Essence > 0 || player == null || player.MasteryCost(s.E.HostKey) <= 0)
                Chip(dock, x, y, string.Format(Localize.FromTable("ui.rb.summon.card.essence", "최대 성급 · 정수 +{0}"), s.E.Essence), false);
            else ShardBar(dock, x, y, right, player.GetShards(s.E.HostKey), s.E.Shards, player.MasteryCost(s.E.HostKey));
            dock.gameObject.SetActive(false);
        }

        private float StarRowWidth(IPlayerDataService player, string key)
        {
            int rank = player != null ? player.GetMastery(key) : 0, golds = rank / StarRank.PerGold, silvers = rank % StarRank.PerGold;
            return golds * M(20f) * StarRank.BigScale + silvers * M(20f) + Mathf.Max(0, golds + silvers - 1) * M(3f);
        }

        /// <summary>「조각 +5 · 17 / 30」 막대 — 가진 것(진한 색) 위에 이번에 받은 것(옅은 색)이 이어진다.</summary>
        private void ShardBar(RectTransform dock, float x, float y, float w, int total, int add, int need)
        {
            int before = Mathf.Max(0, total - add);
            var lbl = Label(dock, "ShardLabel", x, y, w * 0.6f, M(20f), M(14f), SubText, TextAlignmentOptions.Left);
            lbl.text = string.Format(Localize.FromTable("ui.rb.summon.card.shards_add", "조각 <color=#5FF0C9>+{0}</color>"), add);
            var num = Label(dock, "ShardNum", x + w * 0.4f, y, w * 0.6f, M(20f), M(15f), Color.white, TextAlignmentOptions.Right);
            num.text = $"{total} / {need}";
            var track = Box(dock, "ShardTrack", x, y + M(25f), w, M(12f));
            Pic(track, _barTrack, true);
            Fill(track, "Add", Mathf.Clamp01((float)total / need), new Color(0.56f, 0.95f, 1f));
            Fill(track, "Have", Mathf.Clamp01((float)before / need), Color.white);
            var next = Label(dock, "ShardNext", x, y + M(42f), w, M(20f), M(13f), SubText, TextAlignmentOptions.Left);
            next.text = total >= need ? Localize.FromTable("ui.rb.summon.card.ready", "성급을 올릴 수 있어요!")
                                      : string.Format(Localize.FromTable("ui.rb.summon.card.next", "다음 성급까지 {0}개"), need - total);
        }

        private void Fill(RectTransform track, string name, float ratio, Color c)
        {
            var rt = Layer(name, track, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(ratio, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = Pic(rt, _barFill, true);
            img.color = c;
            img.enabled = ratio > 0f && _barFill != null;
        }

        /// <summary>새 몸 해금(금) · 다 키운 몸(남색) 한 줄 칩.</summary>
        private void Chip(RectTransform dock, float x, float y, string text, bool gold)
        {
            var t = Label(dock, "Chip", x + M(10f), y + M(4f), 0f, M(24f), M(14f), gold ? CardLine[2] : SubText, TextAlignmentOptions.Left);
            t.enableAutoSizing = false;
            t.text = text;
            float tw = t.GetPreferredValues(text, 9999f, 0f).x;   // 칸 폭 0 으로 재면 첫 낱말에서 끊긴 폭이 나온다
            t.rectTransform.sizeDelta = new Vector2(tw + 2f, t.rectTransform.sizeDelta.y);
            var plate = Box(dock, "ChipPlate", x, y, tw + M(20f), M(32f));
            plate.SetSiblingIndex(t.rectTransform.GetSiblingIndex());
            // 금 칩 = SSR 틀(금) · 남색 칩 = R 틀 — 금 테 판(rb_cell_line_gold)은 테 선이 가늘어 칩 높이에선 안 보였다
            Framed(plate, gold ? 2 : 0);
        }

        /// <summary>정보 판 — 공개 0.6초 뒤부터 0.45초 OutBack 으로 왼쪽에서 들어온다.</summary>
        private void DrawDock(Slot s, float u)
        {
            if (s.Dock == null) return;
            bool on = u >= 0f;
            s.Dock.gameObject.SetActive(on);
            if (!on) return;
            float k = Ease.OutBack(Seg(u, 0f, 0.45f));
            float y = H * 0.5f - P((880f + 116f) * 1280f / 1200f);
            s.Dock.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-U, 0f, k), y);
            if (s.Dock.TryGetComponent<CanvasGroup>(out var g)) g.alpha = Seg(u, 0f, 0.2f);
            FitChip(s.Dock);
        }

        /// <summary>
        /// 칩 판 폭을 글자에 맞춘다 — 만들 때 재면 아직 글꼴에 안 들어온 글자(「해금…」)가 0 폭이라 판이 「새 몸」만 감쌌다.
        /// 글자가 화면에 그려진 뒤의 선호 폭으로 매 프레임 맞춘다.
        /// </summary>
        private void FitChip(RectTransform dock)
        {
            if (dock.Find("Chip") is not RectTransform chip || dock.Find("ChipPlate") is not RectTransform plate) return;
            if (!chip.TryGetComponent<TextMeshProUGUI>(out var t)) return;
            float tw = t.preferredWidth;
            chip.sizeDelta = new Vector2(tw + 2f, chip.sizeDelta.y);
            plate.sizeDelta = new Vector2(tw + M(20f), plate.sizeDelta.y);
        }

        /// <summary>유리 조각 파티클(배양실 캔버스 + 1)보다 위에 그린다 — 조각이 정보 판 · 작은 카드 위를 지나가 판이 비쳐 보였다.</summary>
        private void OverParticles(RectTransform rt)
        {
            var c = rt.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingLayerID = _canvas.sortingLayerID;
            c.sortingOrder = _canvas.sortingOrder + 2;
        }

        // ── 10회 작은 카드 ──────────────────────────────────────

        /// <summary>10회 칸 밑 작은 카드(시안 100 × 72) — 틀 · 속성 · 등급 · 이름 · 조각 ×n(정수 ×n · 새 몸) · NEW.</summary>
        private void MakeMini(Slot s)
        {
            int tier = Mathf.Clamp(s.E.Grade, 0, 2);
            float w = M(100f), h = M(72f);
            var mini = Layer("Label", s.Root, false);
            mini.anchorMin = mini.anchorMax = new Vector2(0.5f, 0.5f);
            mini.sizeDelta = new Vector2(w, h);
            s.Label = mini;
            OverParticles(mini);
            Framed(mini, tier);
            var affBack = Box(mini, "AffBox", M(4f), M(4f), M(18f), M(18f));
            Solid(affBack, AffBox);
            int aff = (int)AffinityRule.KindOf(s.E.HostKey) - 1;
            Pic(Box(affBack, "Aff", M(1f), M(1f), M(16f), M(16f)), aff >= 0 && aff < _affIcons.Length ? _affIcons[aff] : null);
            Pic(Box(mini, "Grade", w - M(34f), M(4f), M(30f), M(19f)), tier < _cardTags.Length ? _cardTags[tier] : null);
            s.LabelName = Label(mini, "Name", M(3f), M(25f), w - M(6f), M(18f), M(12.5f), Color.white, TextAlignmentOptions.Center);
            s.LabelName.text = s.E.Name;
            s.LabelCount = Label(mini, "Count", M(3f), M(46f), w - M(6f), M(19f), M(13f), Color.white, TextAlignmentOptions.Center);
            if (s.E.IsNew)
            {
                s.LabelCount.color = CardLine[2];
                s.LabelCount.text = Localize.Get("ui.summon.reveal.new");
            }
            else
            {
                string count = s.E.Essence > 0 ? string.Format(Localize.FromTable("ui.rb.summon.card_essence", "정수 ×{0}"), s.E.Essence) : $"×{s.E.Shards}";
                s.LabelCount.text = count;
                if (s.E.Essence == 0 && _shardIcon != null)
                {
                    // 조각 아이콘 + 「×5」 — 묶음을 가운데로
                    s.LabelCount.enableAutoSizing = false;
                    float tw = s.LabelCount.GetPreferredValues(count, 9999f, 0f).x, iw = M(15f), gap = M(3f);
                    float left = (w - tw - iw - gap) * 0.5f;
                    Pic(Box(mini, "Shard", left, M(48f), iw, iw), _shardIcon);
                    var tr = s.LabelCount.rectTransform;
                    tr.sizeDelta = new Vector2(tw + 2f, tr.sizeDelta.y);
                    tr.anchoredPosition = new Vector2(left + iw + gap, tr.anchoredPosition.y);
                    s.LabelCount.alignment = TextAlignmentOptions.Left;
                }
            }
            if (s.E.IsNew && _badgeNew != null) s.LabelNew = Pic(Box(mini, "New", w * 0.5f + M(6f), -M(14f), M(52f), M(29f)), _badgeNew);
            mini.gameObject.SetActive(false);
        }

        // ── 도구 ───────────────────────────────────────────────

        /// <summary>시안 판(540 폭) px → 설계 폭(720) 기준 지금 판.</summary>
        private float M(float mockPx) => mockPx * MockToGame * U / 720f;

        private static HostEntry FindHost(IPlayerDataService player, string key)
        {
            if (player == null) return null;
            foreach (var e in player.PlayableHosts) if (e != null && e.HostKey == key) return e;
            return null;
        }

        private static string AffKey(int aff) => aff switch { 0 => "ui.affinity.blade", 1 => "ui.affinity.force", 2 => "ui.affinity.magic", _ => "ui.affinity.none" };

        /// <summary>어두운 바탕 + 등급 틀(9분할) — 정보 판 · 꼬리표 · 작은 카드가 같이 쓴다.</summary>
        private void Framed(RectTransform rt, int tier)
        {
            var back = Layer("Back", rt, true);
            Pic(back, _labelPlate, true).color = CardFill;
            var line = Layer("Frame", rt, true);
            var img = Pic(line, tier < _cardFrames.Length ? _cardFrames[tier] : null, true);
            img.color = tier == 2 ? RbFrameFit.SsrTint : Color.white;
        }

        /// <summary>부모 왼쪽 위 기준 상자(x 오른쪽 · y 아래가 +).</summary>
        private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var rt = Layer(name, parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
            return rt;
        }

        private static Image Pic(RectTransform rt, Sprite sprite, bool sliced = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = sprite;
            img.preserveAspect = !sliced;
            img.type = sliced && sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.enabled = sprite != null;
            return img;
        }

        private static void Solid(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = c;
        }

        private TextMeshProUGUI Label(RectTransform parent, string name, float x, float y, float w, float h, float size, Color c,
                                      TextAlignmentOptions align)
        {
            var rt = Box(parent, name, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = t.fontSizeMax = size;
            t.fontSizeMin = size * 0.55f;
            t.enableAutoSizing = true;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<HeavyText>();
            return t;
        }
    }
}
