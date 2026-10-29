using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 빙의 연출 (PD 2026-10-29 — A안 「분혼」 · 판 `Projects/AVSR/_exchange/ref/possess_fx_1029/board_*.png`).
    ///   · **보조 빙의** — 메인 가슴에서 작은 혼(HD 유령을 줄인 것)이 혼줄을 달고 나가 적에게 쏙 들어간다.
    ///     적은 원작 빙의 자세로 떨고 발밑에 작은 물결 → 보랏빛 보조가 된다. **메인은 계속 싸운다.**
    ///   · **상시 혼줄** — 메인과 보조 사이에 얇고 옅은 혼줄이 플레이 내내 이어진다(「빙의되면 플레이에서도 저런 식으로 유지」).
    ///   · **놓아주기** — 메인 교체 · 보조 교체로 떠나는 호스트는 해골로 쓰러지지 않는다(죽음이 아니다).
    ///     원작 빙의 자세로 옅어지며 흩어짐 그림과 함께 사라진다.
    ///
    /// ⚠ PD 「너무 과하지 않게」 · 「지금 게임에 있는 것에 맞춰」 — 새 그림 없이 지금 빙의 부품(혼줄 · 물결 · 흩어짐)만 쓴다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>작은 혼 = HD 유령을 이만큼 줄인 것. 원작 축소 그림(96 칸)은 거칠어서 쓰지 않는다(PD 「HD 유령 사용 중」).</summary>
        private const float FragScale = 0.42f;
        private const float FragFlySeconds = 0.35f;
        /// <summary>꽂힌 뒤 빙의 자세로 떠는 시간 — 0.3초면 자세가 안 읽힌다(PD 2026-10-29 「자기만의 애니가 안 보인다」).</summary>
        private const float FragSuckSeconds = 0.5f;
        /// <summary>날아가는 동안 위로 솟는 높이 — 곧게 날면 탄처럼 보인다.</summary>
        private const float FragArc = 36f;
        private static readonly Color FragTint = new(0.88f, 0.80f, 1f, 0.95f);

        /// <summary>상시 혼줄 — 전투를 가리지 않게 얇고 옅게, 캐릭터 뒤(바닥 층)에.</summary>
        private const float LinkThickness = 6f;
        private const float LinkAlpha = 0.42f;
        private const float ReleaseSeconds = 0.5f;

        private sealed class PartyLink
        {
            public Unit Target;
            public int Slot;
            /// <summary>혼이 나온 곳. 보조 교체면 떠나는 옛 보조 자리(고정), 아니면 메인 가슴(따라감).</summary>
            public Vector2 From;
            public bool FromMain;
            public float Age;
            public RectTransform FragRect;
            public Image Frag;
            public RectTransform CordRect;
            public Image Cord;
            public bool Rippled;
        }

        private sealed class PartyRelease
        {
            public Unit U;
            public CanvasGroup Group;
            public float Age;
        }

        private readonly List<PartyLink> _partyLinks = new();
        private readonly List<PartyRelease> _partyReleases = new();
        private readonly List<RectTransform> _linkRects = new();
        private readonly List<Image> _linkImages = new();

        /// <summary>혼이 날아가는 중이면 새 빙의를 받지 않는다 — 빈 칸을 둘이 동시에 노린다.</summary>
        private bool PartyLinking => _partyLinks.Count > 0;

        private Vector2 ChestOf(Unit u) => u.Position + Vector2.up * SoulChestLift;

        /// <summary>적 하나를 보조로 데려오는 연출을 시작한다. 끝나면 그 자리에 보조가 선다.</summary>
        private void BeginSubPossess(Unit target, int slot, Unit fromSub)
        {
            var link = new PartyLink { Target = target, Slot = slot, FromMain = fromSub == null };
            link.From = fromSub != null ? ChestOf(fromSub) : ChestOf(_host);

            var ghost = UnitGet("ghost", "s");
            var go = new GameObject("PartyFrag", typeof(RectTransform), typeof(Image));
            link.FragRect = (RectTransform)go.transform;
            link.FragRect.SetParent(_shotLayer, false);
            link.FragRect.anchorMin = link.FragRect.anchorMax = new Vector2(0f, 1f);
            link.FragRect.pivot = new Vector2(0.5f, 0.5f);
            link.Frag = go.GetComponent<Image>();
            link.Frag.raycastTarget = false;
            link.Frag.sprite = ghost;
            link.Frag.color = FragTint;
            float side = ghost != null ? ghost.rect.width : 128f;
            link.FragRect.sizeDelta = Vector2.one * side * FragScale;
            link.FragRect.anchoredPosition = link.From;

            (link.CordRect, link.Cord) = NewCordImage("PartyFragCord", _shotLayer);

            // 빼앗기는 몸은 그 자리에 선다 — 적 목록에서는 이미 빠졌다(TakeFromEnemies)
            target.SetHpBarVisible(false);
            _partyLinks.Add(link);
        }

        /// <summary>떠나는 호스트를 놓아준다 — 원작 빙의 자세로 옅어지며 흩어진다. 쓰러짐(해골) 그림은 쓰지 않는다.</summary>
        private void BeginRelease(Unit u)
        {
            if (u == null) return;
            u.SetHpBarVisible(false);
            u.SetSpriteOverride(PossessedFrame(u.Key, 0f));
            var group = u.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = u.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            PlayFx("souldeath", u.Position, SoulDeathFxSize, loop: false);
            PlaySoulRipple(u.Position, 0f, PossessRippleScale, PossessRippleAlpha);
            _partyReleases.Add(new PartyRelease { U = u, Group = group });
        }

        private void TickPartyFx(float dt)
        {
            for (int i = _partyReleases.Count - 1; i >= 0; i--)
            {
                var r = _partyReleases[i];
                r.Age += dt;
                if (r.U == null) { _partyReleases.RemoveAt(i); continue; }
                r.U.SetSpriteOverride(PossessedFrame(r.U.Key, r.Age));
                r.Group.alpha = 1f - Mathf.Clamp01(r.Age / ReleaseSeconds);
                if (r.Age < ReleaseSeconds) continue;
                Destroy(r.U.gameObject);
                _partyReleases.RemoveAt(i);
            }

            for (int i = _partyLinks.Count - 1; i >= 0; i--)
            {
                var l = _partyLinks[i];
                if (l.Target == null) { EndLink(l); _partyLinks.RemoveAt(i); continue; }
                l.Age += dt;
                if (l.FromMain && _host != null) l.From = ChestOf(_host);
                var to = ChestOf(l.Target);
                // 빼앗기는 호스트는 혼이 출발한 순간부터 제 빙의 자세(원작 정면 2 장)로 떤다 — 지금 빙의 채널과 같다(PD 2026-10-29)
                l.Target.SetSpriteOverride(PossessedFrame(l.Target.Key, l.Age));

                if (l.Age < FragFlySeconds)
                {
                    float t = l.Age / FragFlySeconds;
                    float k = t * t * (3f - 2f * t);
                    var p = Vector2.Lerp(l.From, to, k) + Vector2.up * Mathf.Sin(Mathf.PI * k) * FragArc;
                    l.FragRect.anchoredPosition = p;
                    DrawCord(l.CordRect, l.Cord, l.From, p, LinkThickness, 0.9f);
                    continue;
                }

                // 꽂힘 — 원작 빙의 자세 · 혼은 쏙 빨려 든다 · 발밑 작은 물결
                if (!l.Rippled)
                {
                    l.Rippled = true;
                    PlaySoulRipple(l.Target.Position, 0f, PossessRippleScale, PossessRippleAlpha);
                }
                float s = Mathf.Clamp01((l.Age - FragFlySeconds) / FragSuckSeconds);
                l.FragRect.anchoredPosition = to;
                l.FragRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, s * s);
                DrawCord(l.CordRect, l.Cord, l.From, to, LinkThickness, 0.9f * (1f - s * 0.5f));
                if (s < 1f) continue;

                FinishSubPossess(l);
                EndLink(l);
                _partyLinks.RemoveAt(i);
            }

            DrawPartyLinks();
        }

        private void FinishSubPossess(PartyLink l)
        {
            var at = l.Target.Position;
            string key = l.Target.Key;
            Destroy(l.Target.gameObject);
            var sub = MakeSub(key, at);
            int slot = Mathf.Clamp(l.Slot, 0, _subs.Count);
            _subs.Insert(slot, sub);
            Debug.Log($"[빙의 파티] 보조 {slot + 1} = {key} · 공격 {sub.U.Atk}");
        }

        private static void EndLink(PartyLink l)
        {
            if (l.FragRect != null) Destroy(l.FragRect.gameObject);
            if (l.CordRect != null) Destroy(l.CordRect.gameObject);
        }

        /// <summary>메인 ↔ 보조 상시 혼줄. 메인이 없으면(유령 · 빙의 중) 감춘다.</summary>
        private void DrawPartyLinks()
        {
            int need = _host != null && PartyOn ? _subs.Count : 0;
            while (_linkRects.Count < need)
            {
                var (rt, img) = NewCordImage("PartyLinkCord", _fieldLayer);
                _linkRects.Add(rt);
                _linkImages.Add(img);
            }
            for (int i = 0; i < _linkRects.Count; i++)
            {
                var u = i < need ? _subs[i].U : null;
                bool show = u != null;
                if (_linkRects[i].gameObject.activeSelf != show) _linkRects[i].gameObject.SetActive(show);
                if (!show) continue;
                // 혼줄이 숨쉬듯 아주 조금 짙어졌다 옅어진다 — 멈춘 선이면 바닥 무늬로 읽힌다
                float a = LinkAlpha * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.4f + i));
                DrawCord(_linkRects[i], _linkImages[i], ChestOf(_host), ChestOf(u), LinkThickness, a);
            }
        }

        private (RectTransform, Image) NewCordImage(string name, RectTransform layer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return (rt, img);
        }

        /// <summary>혼줄 그림(가로 · 왼쪽 = 나온 곳)을 두 점 사이에 늘여 놓는다. 일렁임은 빠져나오기 혼줄과 같은 빠르기.</summary>
        private void DrawCord(RectTransform rt, Image img, Vector2 from, Vector2 to, float thickness, float alpha)
        {
            var frames = FxFrames("soulcord");
            if (rt == null || frames == null || frames.Length < 3) return;
            var d = to - from;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta = new Vector2(d.magnitude, thickness);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            img.sprite = frames[Mathf.FloorToInt(Time.time / SoulCordFrameSeconds) % 3];
            img.color = new Color(1f, 1f, 1f, alpha);
        }

        private void ClearPartyFx()
        {
            for (int i = 0; i < _partyLinks.Count; i++)
            {
                EndLink(_partyLinks[i]);
                if (_partyLinks[i].Target != null) Destroy(_partyLinks[i].Target.gameObject);
            }
            _partyLinks.Clear();
            for (int i = 0; i < _partyReleases.Count; i++)
                if (_partyReleases[i].U != null) Destroy(_partyReleases[i].U.gameObject);
            _partyReleases.Clear();
            for (int i = 0; i < _linkRects.Count; i++)
                if (_linkRects[i] != null) Destroy(_linkRects[i].gameObject);
            _linkRects.Clear();
            _linkImages.Clear();
        }
    }
}
