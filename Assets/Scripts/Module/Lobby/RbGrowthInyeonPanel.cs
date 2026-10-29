using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.InGame;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 성장 화면 오른쪽 인연 판 + 무리 판 — PD 시안 `_exchange/in/mock1029_growth_syn.png`(2026-10-29
    /// 「인연이랑 우리가 추가하는 시너지도 · 2마리일 때 3마리일 때 효과 · 둘 다 저기서 설명」).
    ///   인연 판: 사슬 · 「인연 – 이름」 · 단계 / 내 얼굴 ─사슬→ 짝 얼굴 · 켜짐 스위치 / 액티브 → 짝 · 효과 두 줄 / 단계 조건
    ///   무리 판: 문장 · 「무리 – 이름」 · 가진 수/3 / 설명 / 2명 · 3명 효과 / 무리 호스트 얼굴(가진 것 밝게 · 없는 것 자물쇠) / 켜는 법
    /// 좌표는 한 화면 시안(`mock1029_growth_onescreen.png`) 720 판 px 그대로 — 인연 · 무리 두 판이 가로로 나란히(PD 「가로로 정리해서 스크롤 없이」).
    /// </summary>
    public sealed class RbGrowthInyeonPanel : MonoBehaviour
    {
        [Serializable]
        public struct Art
        {
            public Sprite Panel, Box, Step, Link, FaceCyan, FaceGold, Chain, Toggle, Arrow, Star, Lock;
            public Sprite[] OrgIcons;   // rb_syn_icon_{gang,commando,martial,monster,mystic}
        }

        private const float SliceScale = 2.6f;
        private static readonly Box Area = new(15, 780, 705, 1043);   // 두 판을 묶는 틀
        private static readonly Box IyBox = new(15, 780, 357, 1043);
        private static readonly Box OrgBox = new(365, 780, 705, 1043);

        private Art _art;
        private TMP_Text _like;
        private RectTransform _iy, _org, _members;
        private Image _myFace, _partnerFace, _skillIcon, _effectFace, _orgIcon;
        private TextMeshProUGUI _title, _step, _hint, _desc, _rule, _partnerLabel;
        private TextMeshProUGUI _orgTitle, _orgCount, _orgDesc, _orgTier2, _orgTier3, _orgRule;

        /// <summary>판을 세운다 — parent 는 스킬 칸이 놓인 GrwBodyContent, toContent 는 시안 px → 그 안 자리.</summary>
        public static RbGrowthInyeonPanel Create(RectTransform parent, Func<float, float, Vector2> toContent, Art art, TMP_Text like)
        {
            var go = new GameObject("GrwInyeonPanel", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Area.W, Area.H);
            rt.anchoredPosition = toContent((Area.X0 + Area.X1) * 0.5f, (Area.Y0 + Area.Y1) * 0.5f);
            var p = go.AddComponent<RbGrowthInyeonPanel>();
            p._art = art;
            p._like = like;
            p.Build();
            return p;
        }

        /// <summary>인연 판(누르면 짝 · 켜는 법 창).</summary>
        public RectTransform InyeonRect => _iy;

        private void Build()
        {
            var root = (RectTransform)transform;
            // ── 인연 판(왼쪽) ──
            _iy = New("Inyeon", root, Area, IyBox);
            Img(_iy, IyBox, IyBox, _art.Panel, true).raycastTarget = true;
            Img(_iy, IyBox, new Box(26, 789, 52, 813), _art.Link, false).preserveAspect = true;
            _title = Txt(_iy, IyBox, new Box(58, 787, 270, 815), 19, Color.white, TextAlignmentOptions.MidlineLeft);
            Img(_iy, IyBox, new Box(276, 791, 346, 811), _art.Step, true);
            _step = Txt(_iy, IyBox, new Box(276, 791, 346, 811), 15, new Color(0.24f, 0.13f, 0.02f), TextAlignmentOptions.Center);
            Img(_iy, IyBox, new Box(24, 820, 348, 940), _art.Box, true);
            _partnerLabel = Txt(_iy, IyBox, new Box(232, 822, 344, 834), 11, new Color(0.7f, 0.74f, 0.84f), TextAlignmentOptions.Center);
            Img(_iy, IyBox, new Box(36, 834, 120, 904), _art.FaceCyan, false);
            _myFace = Img(_iy, IyBox, new Box(43, 840, 113, 898), null, false);
            Img(_iy, IyBox, new Box(138, 856, 222, 882), _art.Chain, false).preserveAspect = true;
            Img(_iy, IyBox, new Box(248, 834, 332, 904), _art.FaceGold, false);
            _partnerFace = Img(_iy, IyBox, new Box(255, 840, 325, 898), null, false);
            Img(_iy, IyBox, new Box(36, 912, 66, 928), _art.Toggle, false).preserveAspect = true;
            _hint = Txt(_iy, IyBox, new Box(72, 908, 344, 934), 14, new Color(0.62f, 0.92f, 1f), TextAlignmentOptions.MidlineLeft);
            _hint.fontSizeMin = 8f;   // 영어 「Active when the partner is a Sub」 가 가장 길다
            Img(_iy, IyBox, new Box(24, 946, 348, 1036), _art.Box, true);
            _skillIcon = Img(_iy, IyBox, new Box(78, 950, 116, 988), null, false);
            Img(_iy, IyBox, new Box(150, 961, 176, 977), _art.Arrow, false).preserveAspect = true;
            _effectFace = Img(_iy, IyBox, new Box(204, 950, 242, 988), null, false);
            _desc = Txt(_iy, IyBox, new Box(32, 990, 340, 1034), 15, Color.white, TextAlignmentOptions.Center, wrap: true);
            _rule = null;   // 한 화면 시안에서 뺐다(인연 창을 누르면 단계 조건이 나온다)

            // ── 무리 판(오른쪽) ──
            _org = New("Org", root, Area, OrgBox);
            Img(_org, OrgBox, OrgBox, _art.Panel, true);
            _orgIcon = Img(_org, OrgBox, new Box(376, 787, 404, 815), null, false);
            _orgTitle = Txt(_org, OrgBox, new Box(410, 787, 614, 815), 19, Color.white, TextAlignmentOptions.MidlineLeft);
            Img(_org, OrgBox, new Box(620, 791, 694, 811), _art.Step, true);
            _orgCount = Txt(_org, OrgBox, new Box(620, 791, 694, 811), 15, new Color(0.24f, 0.13f, 0.02f), TextAlignmentOptions.Center);
            _orgDesc = Txt(_org, OrgBox, new Box(378, 819, 692, 843), 14, new Color(0.78f, 0.82f, 0.9f), TextAlignmentOptions.MidlineLeft);
            Img(_org, OrgBox, new Box(376, 849, 694, 907), _art.Box, true);
            _orgTier2 = Txt(_org, OrgBox, new Box(390, 852, 684, 878), 16, Color.white, TextAlignmentOptions.MidlineLeft);
            _orgTier3 = Txt(_org, OrgBox, new Box(390, 878, 684, 904), 16, Color.white, TextAlignmentOptions.MidlineLeft);
            _members = New("Members", _org, OrgBox, new Box(376, 912, 694, 1008));
            _orgRule = Txt(_org, OrgBox, new Box(370, 1012, 700, 1034), 12, new Color(0.7f, 0.74f, 0.84f), TextAlignmentOptions.Center);
        }

        /// <summary>그 호스트의 인연 · 무리를 채운다. 인연이 없으면 인연 판만, 무리가 없으면 무리 판만 숨긴다.</summary>
        public void Fill(IPlayerDataService player, string key, RbLobbyUI shell, Sprite activeIcon)
        {
            FillInyeon(player, key, shell, activeIcon);
            FillOrg(player, key, shell);
        }

        private void FillInyeon(IPlayerDataService player, string key, RbLobbyUI shell, Sprite activeIcon)
        {
            string name = BattleDirector.InyeonName(key);
            var partners = BattleDirector.InyeonPartners(key);
            bool on = !string.IsNullOrEmpty(name) && partners.Count > 0;
            _iy.gameObject.SetActive(on);
            if (!on) return;
            int mine = player.GetMastery(key), bestAt = 0;
            for (int i = 1; i < partners.Count; i++)
                if (player.GetMastery(partners[i]) > player.GetMastery(partners[bestAt])) bestAt = i;
            string partner = partners[bestAt];
            int pm = player.GetMastery(partner);
            int tier = player.Config != null ? player.Config.InyeonTierOf(Mathf.Min(mine, pm)) : 1;

            _title.text = string.Format(Localize.FromTable("ui.rb.iy.title", "인연 – {0}"), name);
            _step.text = string.Format(Localize.FromTable("ui.rb.iy.step", "{0}단"), tier);
            _partnerLabel.text = Localize.FromTable("ui.rb.iy.partner", "짝 호스트");
            SetFace(_myFace, shell.Face(key));
            SetFace(_partnerFace, shell.Face(partner));
            SetFace(_effectFace, shell.Face(partner));
            SetFace(_skillIcon, activeIcon);
            _hint.text = Localize.FromTable("ui.rb.iy.hint", "짝 호스트를 보조로 편성하면 활성");
            _desc.text = BattleDirector.InyeonDesc(key);
            if (_rule != null) _rule.text = Localize.FromTable("ui.rb.iy.rule", "<color=#FFD66B>★★</color> 단계 조건: 두 호스트 중 낮은 성급");
        }

        private readonly List<string> _orgMembers = new();

        private void FillOrg(IPlayerDataService player, string key, RbLobbyUI shell)
        {
            string org = BattleDirector.HostOrg(key);
            _org.gameObject.SetActive(org != null);
            if (org == null) return;
            string id = BattleDirector.OrgId(org);
            _orgIcon.sprite = OrgIcon(id);
            _orgIcon.enabled = _orgIcon.sprite != null;
            _orgIcon.preserveAspect = true;

            _orgMembers.Clear();
            int owned = 0;
            foreach (var h in player.PlayableHosts)
            {
                if (h == null || h.IsGhost || PlayerDataService.IsHiddenHost(h.HostKey) || BattleDirector.HostOrg(h.HostKey) != org) continue;
                _orgMembers.Add(h.HostKey);
                if (player.IsHostOwned(h)) owned++;
            }
            // 이 호스트를 맨 앞에, 가진 것 → 없는 것 순서
            _orgMembers.Sort((a, b) => Rank(player, key, a).CompareTo(Rank(player, key, b)));

            _orgTitle.text = string.Format(Localize.FromTable("ui.rb.org.title", "무리 – {0}"), Localize.Get($"ui.party.tag.{id}"));
            _orgCount.text = $"{Mathf.Min(owned, 3)}/3";
            _orgDesc.text = Localize.Get($"ui.party.tag.{id}.desc");
            _orgTier2.text = TierLine(org, id, 2, owned >= 2);
            _orgTier3.text = TierLine(org, id, 3, owned >= 3);
            _orgRule.text = Localize.FromTable("ui.rb.org.rule", "같은 무리를 메인 · 보조로 함께 데려가면 켜진다");

            for (int i = _members.childCount - 1; i >= 0; i--) Destroy(_members.GetChild(i).gameObject);
            int n = Mathf.Min(_orgMembers.Count, 5);
            float cell = n >= 5 ? 52f : 62f, pitch = n >= 5 ? 62f : 78f;
            float x0 = -(pitch * (n - 1)) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                string k = _orgMembers[i];
                bool have = player.IsHostOwned(player.GetHost(k));
                var at = new Vector2(x0 + pitch * i, 10f);
                var frame = Child(_members, at, cell, k == key ? _art.FaceCyan : have ? _art.FaceGold : _art.FaceCyan);
                if (!have) frame.color = new Color(0.55f, 0.58f, 0.66f);
                var face = Child(_members, at, cell - 12f, shell.Face(k));
                face.preserveAspect = true;
                if (!have) face.color = new Color(0.32f, 0.34f, 0.4f);
                if (!have && _art.Lock != null) Child(_members, at + new Vector2(0f, -4f), 26f, _art.Lock).preserveAspect = true;
                var nm = Label(_members, cell + 12f, 12, have ? Color.white : new Color(0.62f, 0.66f, 0.76f));
                nm.rectTransform.anchoredPosition = at + new Vector2(0f, -(cell * 0.5f + 10f));
                nm.text = player.GetHost(k)?.DisplayName ?? k;
            }
        }

        private static int Rank(IPlayerDataService player, string self, string k)
            => k == self ? 0 : player.IsHostOwned(player.GetHost(k)) ? 1 : 2;

        private static string TierLine(string org, string id, int need, bool on)
        {
            string eff = Localize.Format($"ui.party.p2.eff.{id}", BattleDirector.OrgTierPct(org, need >= 3));
            string head = string.Format(Localize.FromTable("ui.rb.org.need", "{0}명"), need);
            return on ? $"<color=#FFD66B>{head}</color>  {eff}" : $"<color=#9AA3B8>{head}  {eff}</color>";
        }

        private Sprite OrgIcon(string id)
        {
            if (_art.OrgIcons == null) return null;
            foreach (var s in _art.OrgIcons) if (s != null && s.name == $"rb_syn_icon_{id}") return s;
            return null;
        }

        private static void SetFace(Image img, Sprite s)
        {
            img.sprite = s;
            img.enabled = s != null;
            img.preserveAspect = true;
        }

        // ── 도구 ──────────────────────────────────────────

        private readonly struct Box
        {
            public readonly float X0, Y0, X1, Y1;
            public Box(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
            public float W => X1 - X0;
            public float H => Y1 - Y0;
        }

        private static RectTransform New(string name, RectTransform parent, Box pb, Box b)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(b.W, b.H);
            rt.anchoredPosition = new Vector2((b.X0 + b.X1 - pb.X0 - pb.X1) * 0.5f, (pb.Y0 + pb.Y1 - b.Y0 - b.Y1) * 0.5f);
            return rt;
        }

        private static Image Img(RectTransform parent, Box pb, Box b, Sprite s, bool sliced)
        {
            var img = New("Img", parent, pb, b).gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = sliced && s != null && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            // 부품 원본이 판보다 2~3배 크게 와서 9분할 테가 시안의 3배 굵기로 나왔다(검수 2026-10-29) — 테만 줄인다
            if (img.type == Image.Type.Sliced) img.pixelsPerUnitMultiplier = SliceScale;
            img.enabled = s != null;
            img.raycastTarget = false;
            return img;
        }

        private static Image Child(RectTransform parent, Vector2 at, float size, Sprite s)
        {
            var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = at;
            var img = go.GetComponent<Image>();
            img.sprite = s;
            img.enabled = s != null;
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI Txt(RectTransform parent, Box pb, Box b, float size, Color c, TextAlignmentOptions align, bool wrap = false)
        {
            var t = New("Text", parent, pb, b).gameObject.AddComponent<TextMeshProUGUI>();
            Style(t, size, c, align, wrap);
            return t;
        }

        private TextMeshProUGUI Label(RectTransform parent, float width, float size, Color c)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(width, 18f);
            var t = go.AddComponent<TextMeshProUGUI>();
            Style(t, size, c, TextAlignmentOptions.Center, false);
            return t;
        }

        private void Style(TextMeshProUGUI t, float size, Color c, TextAlignmentOptions align, bool wrap)
        {
            if (_like != null) { t.font = _like.font; t.fontSharedMaterial = _like.fontSharedMaterial; }
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = 9f;
            t.color = c;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.fontStyle = FontStyles.Bold;
        }
    }
}
