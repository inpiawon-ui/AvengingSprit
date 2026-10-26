using System;
using System.Collections.Generic;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 호스트 탭 — 도감(시안 Screens/collection.png). 유령 카드 · 거르기(전체 · 파워 · 무기 · 마법) · 정렬 · 3열 카드.
    /// 카드를 누르면 호스트 성장 한 화면(<see cref="RbGrowthPage"/>)을 연다. 히든(사신)은 보이지 않는다.
    /// 카드의 「Lv」 는 능력치 강화 레벨의 합이다 — 이 게임에는 몸 레벨이 따로 없다(성급은 별 줄).
    /// </summary>
    public sealed class RbHostPage : MonoBehaviour
    {
        [SerializeField] private Sprite[] _gradeFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _rarityTags = Array.Empty<Sprite>();
        // 속성 아이콘 — 무기 · 파워 · 마법(Affinity 값 − 1)
        [SerializeField] private Sprite[] _affIcons = Array.Empty<Sprite>();
        // 틀 그림마다 몸통(테 선) 상자 — 빌더가 그림에서 잰다(RbFrameFit)
        [SerializeField] private Sprite[] _fitSprites = Array.Empty<Sprite>();
        [SerializeField] private Vector4[] _fitBodies = Array.Empty<Vector4>();
        // 성급 별 — 큰 금별(5성마다) · 작은 은별(StarRank)
        [SerializeField] private Sprite _rankGold, _rankSilver;

        private const float CardW = 328f * 2f / 3f;
        private static readonly float[] ColX = { (30f + 164f - 540f) * 2f / 3f, (375f + 164f - 540f) * 2f / 3f, (722f + 164f - 540f) * 2f / 3f };
        private const float RowStep = 405f * 2f / 3f, FirstY = -(862f + 193f - 850f) * 2f / 3f;
        private static readonly Color LockedTint = new(0.22f, 0.24f, 0.32f, 1f);
        // 등급(R · SR · SSR) 안쪽 빛 — 시안 카드 바탕색
        // 시안 카드 빛 색(묶음 v6.css --card-glow: R #4fa3ff · SR #b67cff · SSR #ffc846, 알파 0x66 = 0.4)
        private static readonly Color[] GlowColors =
        {
            new(0.31f, 0.64f, 1f, 0.4f), new(0.71f, 0.49f, 1f, 0.4f), new(1f, 0.78f, 0.27f, 0.4f),
        };
        private static readonly Color ChipOn = new(0.24f, 0.90f, 1f, 1f), ChipOff = new(0.70f, 0.74f, 0.84f, 1f);
        private static readonly Color ChipIconOff = new(0.45f, 0.48f, 0.58f, 1f);

        private enum Sort { Power, Grade }

        private UIBinder _ui;
        private IPlayerDataService _player;
        private RbLobbyUI _shell;
        private SpriteAtlas _faces;
        private readonly List<Transform> _cards = new();
        private readonly List<HostEntry> _shown = new();
        private int _filter;            // 0 전체 · 1 파워 · 2 무기 · 3 마법
        private Sort _sort = Sort.Power;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>();
            _ui.OnClick("ColBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("ColHelp", () => _shell.ShowGuide("growth", "summon"));   // 시안 guide
            _ui.OnClick("ColGhostGrow", () => _shell.OpenGhostGrowth());
            for (int i = 0; i < 4; i++)
            {
                int f = i;
                _ui.OnClick($"ColFilter{i}", () => { _filter = f; GameSound.Cue("ui.tap"); Refresh(); });
            }
            _ui.OnClick("ColFilter4", () => { _sort = _sort == Sort.Power ? Sort.Grade : Sort.Power; GameSound.Cue("ui.tap"); Refresh(); });
            _ui.SetActive("ColCardTemplate", false);
        }

        /// <summary>쪽이 열릴 때 — 얼굴 아틀라스는 로비가 이미 들고 있다.</summary>
        public void Open(IPlayerDataService player, SpriteAtlas faces)
        {
            _player = player;
            _faces = faces;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            if (_player == null || !_player.IsReady) return;
            RefreshGhost();
            RefreshFilters();
            Collect();
            LayCards();
            for (int i = 0; i < _shown.Count; i++) FillCard(_cards[i], _shown[i]);
        }

        // ── 유령 카드 · 보유 수 ──────────────────────────────────

        private void RefreshGhost()
        {
            int lv = _player.GhostLevel;
            _ui.SetText("ColGhostLv", string.Format(Localize.FromTable("ui.rb.host.ghost_lv", "유령 Lv <size=120%>{0}</size>"), lv));
            bool max = lv >= _player.GhostLevelMax;
            SetBar("ColGhostExpFill", max ? 1f : (float)_player.GhostExp / Mathf.Max(1, _player.GhostExpMax));

            int owned = 0, all = 0;
            foreach (var e in _player.PlayableHosts)
            {
                if (e == null || e.IsGhost || PlayerDataService.IsHiddenHost(e.HostKey)) continue;
                all++;
                if (_player.IsHostOwned(e)) owned++;
            }
            _ui.SetText("ColOwned", string.Format(Localize.FromTable("ui.rb.host.owned", "보유 <color=#FFFFFF>{0}</color> /{1}"), owned, all));
        }

        private void RefreshFilters()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_ui.Get<TextMeshProUGUI>($"ColFilter{i}Text") is not TextMeshProUGUI t) continue;
                t.color = i == _filter ? ChipOn : ChipOff;
                if (i == 0) continue;
                // 속성 칩 — 고른 칩만 아이콘 제 색, 나머지는 잿빛으로 눌러 고른 칸이 한눈에 보이게(PD 2026-10-26 C안)
                var affIcon = _ui.Find($"ColFilter{i}Icon") as RectTransform;
                if (affIcon != null && affIcon.GetComponent<Image>() is Image img) img.color = i == _filter ? Color.white : ChipIconOff;
                FitChipRun(affIcon, t, (_ui.Find($"ColFilter{i}") as RectTransform)?.rect.width ?? 0f);
            }
            _ui.SetText("ColFilter4Text", Localize.FromTable(_sort == Sort.Power ? "ui.rb.host.sort_power" : "ui.rb.host.sort_grade",
                                                             _sort == Sort.Power ? "전투력 순" : "등급 순"));
            CenterRun(null, _ui.Get<TextMeshProUGUI>("ColFilter4Text"), false);
            if (_ui.Find("ColFilter4Icon") is RectTransform icon && _ui.Find("ColFilter4Text") is RectTransform text)
            {
                // 「전투력 순 ⇅」 — 글자와 아이콘 묶음을 칩 가운데로
                float w = text.sizeDelta.x + 4f + icon.sizeDelta.x;
                text.anchoredPosition = new Vector2(-w * 0.5f + text.sizeDelta.x * 0.5f, text.anchoredPosition.y);
                icon.anchoredPosition = new Vector2(w * 0.5f - icon.sizeDelta.x * 0.5f, icon.anchoredPosition.y);
            }
        }

        // ── 카드 ────────────────────────────────────────────────

        private void Collect()
        {
            _shown.Clear();
            foreach (var e in _player.PlayableHosts)
            {
                if (e == null || e.IsGhost || PlayerDataService.IsHiddenHost(e.HostKey)) continue;
                if (_filter > 0 && AffinityRule.KindOf(e.HostKey) != FilterKind(_filter)) continue;
                _shown.Add(e);
            }
            // 가진 몸이 먼저 · 그 안에서 전투력(또는 등급) 높은 순
            _shown.Sort((a, b) =>
            {
                bool oa = _player.IsHostOwned(a), ob = _player.IsHostOwned(b);
                if (oa != ob) return oa ? -1 : 1;
                int c = _sort == Sort.Power ? _player.PowerOf(b.HostKey).CompareTo(_player.PowerOf(a.HostKey))
                                            : b.Grade.CompareTo(a.Grade);
                return c != 0 ? c : string.CompareOrdinal(a.HostKey, b.HostKey);
            });
        }

        private static Affinity FilterKind(int filter) => filter switch
        {
            1 => Affinity.Force,
            2 => Affinity.Blade,
            _ => Affinity.Magic,
        };

        private void LayCards()
        {
            var tpl = _ui.Find("ColCardTemplate");
            var content = _ui.Find("ColGridContent") as RectTransform;
            if (tpl == null || content == null) return;
            while (_cards.Count < _shown.Count)
            {
                var c = Instantiate(tpl.gameObject, content).transform;
                c.name = $"ColCard{_cards.Count}";
                int index = _cards.Count;
                c.GetComponent<Button>()?.onClick.AddListener(() => OpenCard(index));
                _cards.Add(c);
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.ApplyFonts(c);
            }
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = i < _shown.Count;
                _cards[i].gameObject.SetActive(on);
                if (on) ((RectTransform)_cards[i]).anchoredPosition = new Vector2(ColX[i % 3], FirstY - RowStep * (i / 3));
            }
            int rows = (_shown.Count + 2) / 3;
            content.sizeDelta = new Vector2(content.sizeDelta.x, 12f * 2f / 3f + RowStep * rows);
        }

        private void OpenCard(int index)
        {
            if (index < 0 || index >= _shown.Count) return;
            GameSound.Cue("ui.tap");
            _shell.OpenGrowth(_shown[index].HostKey);
        }

        private void FillCard(Transform card, HostEntry e)
        {
            string key = e.HostKey;
            bool owned = _player.IsHostOwned(e);
            int m = _player.GetMastery(key);
            int g = (int)e.Grade;
            if (_ui.Find(card, "ColCardFrame")?.GetComponent<Image>() is Image frame && g < _gradeFrames.Length)
            {
                frame.sprite = _gradeFrames[g];
                var tint = e.Grade == HostGrade.S ? RbFrameFit.SsrTint : Color.white;   // SSR 은 은빛 틀에 금색을 곱한다(왕관 없음)
                frame.color = owned ? tint : tint * new Color(0.55f, 0.58f, 0.68f, 1f);
                RbFrameFit.Apply(frame.rectTransform, ((RectTransform)card).sizeDelta, frame.sprite, _fitSprites, _fitBodies);
            }
            if (_ui.Find(card, "ColCardRarity")?.GetComponent<Image>() is Image tag && g < _rarityTags.Length) tag.sprite = _rarityTags[g];
            if (_ui.Find(card, "ColCardGlow")?.GetComponent<Image>() is Image glow)
            {
                var c = g < GlowColors.Length ? GlowColors[g] : GlowColors[0];
                glow.color = owned ? c : new Color(c.r, c.g, c.b, c.a * 0.35f);
            }
            if (_ui.Find(card, "ColCardFace")?.GetComponent<Image>() is Image face)
            {
                face.sprite = _faces != null ? _faces.GetSprite($"face_{key}") : null;
                face.enabled = face.sprite != null;
                face.color = owned ? Color.white : LockedTint;
            }
            _ui.Find(card, "ColCardLock")?.gameObject.SetActive(!owned);
            // 빨간 점 — 지금 성급을 올릴(해금할) 수 있다
            int need = _player.MasteryCost(key);
            bool ready = m < _player.MasteryMax && need > 0 && _player.GetShards(key) >= need && _player.Gold >= _player.StarUpGoldCost(key);
            _ui.Find(card, "ColCardDot")?.gameObject.SetActive(ready);

            if (_ui.Find(card, "ColCardAff")?.GetComponent<Image>() is Image aff)
            {
                int a = (int)AffinityRule.KindOf(key) - 1;
                aff.sprite = a >= 0 && a < _affIcons.Length ? _affIcons[a] : null;
                _ui.Find(card, "ColCardAffBox")?.gameObject.SetActive(aff.sprite != null);
                aff.color = owned ? Color.white : new Color(0.55f, 0.58f, 0.68f, 1f);   // 틀과 같은 잿빛 — 얼굴처럼 검게 누르면 무엇인지 안 보인다
            }
            RbCardName.Fit(_ui.Find(card, "ColCardName")?.GetComponent<TextMeshProUGUI>(), e.DisplayName);
            ApplyStars(_ui.Find(card, "ColCardStars"), "ColCardStar", m);
            // 「Lv」 줄은 없다(PD 2026-10-26 「호스트 레벨은 필요 없다」 — 강화 단계 합이었다). 안 가진 몸은 전투력 자리에 조각 진행
            TextIn(card, "ColCardPower", owned ? _player.PowerOf(key).ToString("N0")
                                               : string.Format(Localize.FromTable("ui.rb.host.shards", "조각 {0}/{1}"), _player.GetShards(key), need));
            _ui.Find(card, "ColCardPowerIcon")?.gameObject.SetActive(owned);
            CenterRun(_ui.Find(card, "ColCardPowerIcon") as RectTransform, _ui.Find(card, "ColCardPower")?.GetComponent<TextMeshProUGUI>(), owned);
        }

        /// <summary>
        /// 성급 별 줄 — 1~4성은 작은 은별을 하나씩 더하고 5성마다 큰 금별 하나(3성 = 은 3 · 7성 = 금 1 + 은 2 · 10성 = 금 2).
        /// PD 2026-10-16 결정(StarRank)으로 되돌렸다 — 리뉴얼 때 「금별 5 + 각성 별」로 바뀌어 있었다(PD 2026-10-26 「전에 정리한 양식대로」).
        /// 별 크기는 별 줄 칸 높이에 맞춘다(은별 0.75 · 금별은 그 1.6배) — 화면마다 칸 크기가 달라서다.
        /// (0.62 는 도감 카드에서 은별이 점처럼 작았다 — 2026-10-26 캡처)
        /// </summary>
        public static void Stars(Transform root, string prefix, int rank, Sprite gold, Sprite silver)
        {
            if (root is not RectTransform r) return;
            float h = r.rect.height > 0f ? r.rect.height : r.sizeDelta.y;
            StarRank.Apply(root, prefix, rank, gold, silver, Vector2.zero, h * 0.75f, h * 0.08f);
        }

        private void ApplyStars(Transform root, string prefix, int rank) => Stars(root, prefix, rank, _rankGold, _rankSilver);

        /// <summary>「⚔ 3,230」 — 아이콘 + 글자 한 줄을 글자 폭을 재서 가운데에 세운다.</summary>
        private static void CenterRun(RectTransform icon, TextMeshProUGUI text, bool withIcon)
        {
            if (text == null) return;
            var tr = text.rectTransform;
            float w = text.GetPreferredValues(text.text).x;
            float iw = withIcon && icon != null ? icon.sizeDelta.x + 4f : 0f;
            float left = -(w + iw) * 0.5f;
            if (withIcon && icon != null) icon.anchoredPosition = new Vector2(left + icon.sizeDelta.x * 0.5f, icon.anchoredPosition.y);
            tr.sizeDelta = new Vector2(w + 2f, tr.sizeDelta.y);
            tr.anchoredPosition = new Vector2(left + iw + tr.sizeDelta.x * 0.5f, tr.anchoredPosition.y);
            text.alignment = TextAlignmentOptions.Left;
        }

        // 칩 안 「아이콘 + 글자」 묶음을 가운데로. 칩 폭(양옆 8)을 넘으면 글자를 먼저 최소 크기까지, 그래도 넘으면 아이콘을 줄인다
        // (영어 「Weapon」 이 한국어보다 길다)
        private static void FitChipRun(RectTransform icon, TextMeshProUGUI text, float chipWidth)
        {
            if (icon == null || text == null || chipWidth <= 0f) return;
            const float gap = 4f, pad = 8f;
            float avail = chipWidth - pad * 2f;
            const float iconMax = 60f * 2f / 3f;   // 빌더 값(시안 60) — sizeDelta 는 줄였다 되돌리므로 거기서 읽지 않는다
            text.enableAutoSizing = false;
            text.fontSize = text.fontSizeMax;
            float w = text.GetPreferredValues(text.text).x;
            if (w + iconMax + gap > avail)
            {
                text.fontSize = Mathf.Max(text.fontSizeMin, text.fontSizeMax * (avail - iconMax - gap) / w);
                w = text.GetPreferredValues(text.text).x;
            }
            float iw = Mathf.Clamp(avail - gap - w, iconMax * 0.6f, iconMax);
            icon.sizeDelta = new Vector2(iw, iw);
            float left = -(w + iw + gap) * 0.5f;
            icon.anchoredPosition = new Vector2(left + iw * 0.5f, icon.anchoredPosition.y);
            var tr = text.rectTransform;
            tr.sizeDelta = new Vector2(w + 2f, tr.sizeDelta.y);
            tr.anchoredPosition = new Vector2(left + iw + gap + tr.sizeDelta.x * 0.5f, tr.anchoredPosition.y);
            text.alignment = TextAlignmentOptions.Left;
        }

        private void SetBar(string fill, float ratio)
        {
            if (_ui.Find(fill) is RectTransform r) r.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        private void TextIn(Transform root, string node, string value)
        {
            if (_ui.Find(root, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private static void Toast(string key)
            => SystemPopup.Show(Localize.FromTable(key, key), null, Localize.Get("ui.common.ok"), null);
    }
}
