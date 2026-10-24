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
        [SerializeField] private Sprite _starOn, _starOff, _starAwaken;

        private const float CardW = 328f * 2f / 3f;
        private static readonly float[] ColX = { (30f + 164f - 540f) * 2f / 3f, (375f + 164f - 540f) * 2f / 3f, (722f + 164f - 540f) * 2f / 3f };
        private const float RowStep = 405f * 2f / 3f, FirstY = -(862f + 193f - 850f) * 2f / 3f;
        private static readonly Color LockedTint = new(0.22f, 0.24f, 0.32f, 1f);
        // 등급(R · SR · SSR) 안쪽 빛 — 시안 카드 바탕색
        private static readonly Color[] GlowColors =
        {
            new(0.30f, 0.58f, 1f, 0.75f), new(0.66f, 0.42f, 1f, 0.8f), new(1f, 0.74f, 0.28f, 0.8f),
        };
        private static readonly Color ChipOn = new(0.24f, 0.90f, 1f, 1f), ChipOff = new(0.70f, 0.74f, 0.84f, 1f);

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
                if (_ui.Get<TextMeshProUGUI>($"ColFilter{i}Text") is TextMeshProUGUI t) t.color = i == _filter ? ChipOn : ChipOff;
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
                frame.color = owned ? Color.white : new Color(0.55f, 0.58f, 0.68f, 1f);
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

            TextIn(card, "ColCardName", e.DisplayName);
            ApplyStars(_ui.Find(card, "ColCardStars"), "ColCardStar", m);
            if (owned)
            {
                TextIn(card, "ColCardLv", $"Lv {StatLevelSum(key)}");
                TextIn(card, "ColCardPower", _player.PowerOf(key).ToString("N0"));
            }
            else
            {
                TextIn(card, "ColCardLv", string.Format(Localize.FromTable("ui.rb.host.shards", "조각 {0}/{1}"), _player.GetShards(key), need));
                TextIn(card, "ColCardPower", Localize.FromTable("ui.rb.host.locked", "미보유"));
            }
            _ui.Find(card, "ColCardPowerIcon")?.gameObject.SetActive(owned);
            CenterRun(_ui.Find(card, "ColCardPowerIcon") as RectTransform, _ui.Find(card, "ColCardPower")?.GetComponent<TextMeshProUGUI>(), owned);
        }

        private int StatLevelSum(string key)
        {
            int sum = 0;
            foreach (HostStat s in Enum.GetValues(typeof(HostStat))) sum += _player.HostStatLevel(key, s);
            return sum;
        }

        /// <summary>
        /// 별 다섯 칸 — 1~5성은 금별을 왼쪽부터 채우고, 6~10성(각성)은 금별을 각성 별로 바꿔 간다(키트 star_awaken).
        /// </summary>
        public static void Stars(Transform root, string prefix, int rank, Sprite on, Sprite off, Sprite awaken)
        {
            if (root == null) return;
            int awakened = Mathf.Clamp(rank - 5, 0, 5), lit = Mathf.Clamp(rank, 0, 5);
            for (int i = 0; i < 5; i++)
                if (root.Find($"{prefix}{i}")?.GetComponent<Image>() is Image img)
                    img.sprite = i < awakened ? awaken : i < lit ? on : off;
        }

        private void ApplyStars(Transform root, string prefix, int rank) => Stars(root, prefix, rank, _starOn, _starOff, _starAwaken);

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
