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
        /// <summary>상시 실 전용 그림(A안 곧은 토막 `fx_partythread_1`, 512 × 64 칸에 심 2~3 px)을 그리는 높이.</summary>
        private const float ThreadThickness = 26f;

        // ── 상시 실 = 마디 줄 (PD 2026-10-29 「계속 움직이니 자연스럽게 — 위치도 길이도 계속 바뀐다」) ──
        // 그림 한 장을 두 점 사이에 늘이면 길이가 바뀔 때 물결 간격이 늘었다 줄고, 실이 막대기처럼 통째로 돈다.
        // 마디 줄로 계산해 끝은 가슴에 붙이고 가운데는 늦게 따라오게 하면 끌리며 휘었다가 천천히 제자리를 찾는다.
        private const int RopeNodes = 20;
        /// <summary>마디 속도가 남는 비율 — 작으면 뻣뻣하고, 크면 오래 출렁인다.</summary>
        private const float RopeDamping = 0.86f;
        /// <summary>마디 사이 길이 = 곧은 길이 × 이 값. 1 보다 조금 커야 살짝 늘어진다.</summary>
        private const float RopeSlack = 1.05f;
        /// <summary>곧은 자리로 돌아가려는 힘 — 없으면 마디가 한쪽에 뭉친다.</summary>
        private const float RopeStraighten = 0.05f;
        /// <summary>줄 위를 흐르는 작은 일렁임. 길이와 상관없이 굽이 크기가 같다(굽이 수만 늘어난다).</summary>
        private const float RopeWaveAmp = 4f;
        private const float RopeWaveSpeed = 3.2f;
        private const float RopeWaveLength = 140f;
        /// <summary>한 틱에 이보다 멀리 끝이 튀면(순간 이동 · 방 바뀜) 줄을 곧게 새로 편다.</summary>
        private const float RopeSnapJump = 160f;

        private sealed class PartyRope
        {
            public readonly Vector2[] P = new Vector2[RopeNodes];
            public readonly Vector2[] Prev = new Vector2[RopeNodes];
            public readonly RectTransform[] Seg = new RectTransform[RopeNodes - 1];
            public readonly Image[] Img = new Image[RopeNodes - 1];
            public Unit Sub;
            public bool Ready;
            public bool Visible = true;
        }

        private readonly List<PartyRope> _ropes = new();
        // 그리기용 재사용 버퍼 — 매 프레임 배열을 새로 만들지 않는다
        private static readonly Vector2[] s_ropeRaw = new Vector2[RopeNodes];
        private static readonly Vector2[] s_ropeDraw = new Vector2[RopeNodes];
        /// <summary>PD 2026-10-29 「알파 좀 빼고」 — 0.42 에서 내렸다.</summary>
        private const float LinkAlpha = 0.3f;
        /// <summary>실을 따라 떠오르는 빛 점 — 실마다 이 간격으로 하나(PD 「파티클 같은 걸 약간만」).</summary>
        private const float LinkMoteGap = 0.35f;
        private static readonly Color LinkMoteColor = new(0.73f, 0.55f, 1f, 0.8f);
        private float _linkMoteTimer;
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

        /// <summary>메인 ↔ 보조 상시 혼줄(마디 줄). 메인이 없으면(유령 · 빙의 중) 감춘다.</summary>
        private void DrawPartyLinks()
        {
            int need = _host != null && PartyOn ? _subs.Count : 0;
            while (_ropes.Count < need) _ropes.Add(NewRope());

            var thread = FxFrames("partythread");
            var fallback = FxFrames("soulcord");
            var segSprite = thread != null ? thread[0] : fallback != null ? fallback[0] : null;
            float thick = thread != null ? ThreadThickness : LinkThickness;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            _linkMoteTimer -= dt;
            bool mote = _linkMoteTimer <= 0f;
            if (mote) _linkMoteTimer = LinkMoteGap;

            for (int i = 0; i < _ropes.Count; i++)
            {
                var r = _ropes[i];
                var u = i < need ? _subs[i].U : null;
                bool show = u != null && segSprite != null;
                SetRopeVisible(r, show);
                if (!show) { r.Ready = false; continue; }

                var from = ChestOf(_host);
                var to = ChestOf(u);
                if (!r.Ready || r.Sub != u || (r.P[0] - from).sqrMagnitude > RopeSnapJump * RopeSnapJump
                    || (r.P[RopeNodes - 1] - to).sqrMagnitude > RopeSnapJump * RopeSnapJump)
                    LayRope(r, u, from, to);
                StepRope(r, from, to);

                // 혼줄이 숨쉬듯 아주 조금 짙어졌다 옅어진다 — 멈춘 선이면 바닥 무늬로 읽힌다
                float a = LinkAlpha * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.4f + i));
                DrawRope(r, segSprite, thick, a);

                // 실 위 아무 마디에서 빛 점 하나가 천천히 떠올라 사라진다 — 양 끝(몸 가까이)은 피한다
                if (mote && _pfx != null)
                    _pfx.Spray(ParticleFxKind.HxDot, RopePoint(r, Random.Range(4, RopeNodes - 4)), Vector2.up, 35f, 1,
                               10f, 22f, new Vector2(12f, 12f), 0.9f, LinkMoteColor);
            }
        }

        private PartyRope NewRope()
        {
            var r = new PartyRope();
            for (int k = 0; k < r.Seg.Length; k++) (r.Seg[k], r.Img[k]) = NewCordImage("PartyRopeSeg", _fieldLayer);
            return r;
        }

        private static void SetRopeVisible(PartyRope r, bool on)
        {
            if (r.Visible == on) return;
            r.Visible = on;
            for (int k = 0; k < r.Seg.Length; k++) r.Seg[k].gameObject.SetActive(on);
        }

        /// <summary>곧게 새로 편다 — 처음 · 보조가 바뀜 · 끝이 멀리 튐.</summary>
        private static void LayRope(PartyRope r, Unit sub, Vector2 from, Vector2 to)
        {
            for (int k = 0; k < RopeNodes; k++)
                r.P[k] = r.Prev[k] = Vector2.Lerp(from, to, k / (RopeNodes - 1f));
            r.Sub = sub;
            r.Ready = true;
        }

        private static void StepRope(PartyRope r, Vector2 from, Vector2 to)
        {
            int last = RopeNodes - 1;
            // 가운데 마디는 제 관성으로 움직인다 — 끝이 움직이면 늦게 따라오며 휜다
            for (int k = 1; k < last; k++)
            {
                var v = (r.P[k] - r.Prev[k]) * RopeDamping;
                r.Prev[k] = r.P[k];
                r.P[k] += v;
                r.P[k] = Vector2.Lerp(r.P[k], Vector2.Lerp(from, to, k / (float)last), RopeStraighten);
            }
            r.P[0] = r.Prev[0] = from;
            r.P[last] = r.Prev[last] = to;

            float rest = Vector2.Distance(from, to) / last * RopeSlack;
            for (int it = 0; it < 4; it++)
                for (int k = 0; k < last; k++)
                {
                    var d = r.P[k + 1] - r.P[k];
                    float len = d.magnitude;
                    if (len < 0.001f) continue;
                    var fix = d * ((len - rest) / len * 0.5f);
                    if (k > 0) r.P[k] += fix;
                    if (k + 1 < last) r.P[k + 1] -= fix;
                }
        }

        /// <summary>마디 자리 + 줄을 따라 흐르는 작은 일렁임(양 끝은 0 — 몸에 붙은 곳은 흔들리지 않는다).</summary>
        private static Vector2 RopePoint(PartyRope r, int k)
        {
            int last = RopeNodes - 1;
            var along = r.P[last] - r.P[0];
            float total = along.magnitude;
            if (total < 1f) return r.P[k];
            var n = new Vector2(-along.y, along.x) / total;
            float dist = total * k / last;
            float w = Mathf.Sin(dist / RopeWaveLength * Mathf.PI * 2f - Time.time * RopeWaveSpeed)
                      * RopeWaveAmp * Mathf.Sin(Mathf.PI * k / last);
            return r.P[k] + n * w;
        }

        private static void DrawRope(PartyRope r, Sprite seg, float thickness, float alpha)
        {
            int segs = RopeNodes - 1;
            // 그리는 점은 이웃과 평균 낸다 — 마디가 꺾인 자리가 각지게 보였다(2026-10-29 확대 검수)
            for (int k = 0; k < RopeNodes; k++) s_ropeRaw[k] = RopePoint(r, k);
            s_ropeDraw[0] = s_ropeRaw[0];
            s_ropeDraw[segs] = s_ropeRaw[segs];
            for (int k = 1; k < segs; k++)
                s_ropeDraw[k] = (s_ropeRaw[k - 1] + s_ropeRaw[k] * 2f + s_ropeRaw[k + 1]) * 0.25f;
            for (int k = 0; k < segs; k++)
            {
                var a = s_ropeDraw[k];
                var b = s_ropeDraw[k + 1];
                var d = b - a;
                var rt = r.Seg[k];
                rt.anchoredPosition = (a + b) * 0.5f;
                // 마디끼리 1.5 px 겹친다 — 꺾인 곳에 틈이 보이지 않게
                rt.sizeDelta = new Vector2(d.magnitude + 1.5f, thickness);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                // 양 끝 두 마디는 옅게 — 몸에 붙는 쪽이 딱 끊기지 않게
                int edge = Mathf.Min(k, segs - 1 - k);
                float fade = edge == 0 ? 0.3f : edge == 1 ? 0.7f : 1f;
                var img = r.Img[k];
                img.sprite = seg;
                img.color = new Color(1f, 1f, 1f, alpha * fade);
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
        private void DrawCord(RectTransform rt, Image img, Vector2 from, Vector2 to, float thickness, float alpha,
                              Sprite[] frames = null, int loop = 3)
        {
            // 빠져나오기 혼줄은 4 장 중 앞 3 장이 일렁임(4 번째는 끊김) — 상시 실은 4 장 모두 반복
            frames ??= FxFrames("soulcord");
            if (rt == null || frames == null || frames.Length < loop) return;
            var d = to - from;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta = new Vector2(d.magnitude, thickness);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            img.sprite = frames[Mathf.FloorToInt(Time.time / SoulCordFrameSeconds) % loop];
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
            for (int i = 0; i < _ropes.Count; i++)
                for (int k = 0; k < _ropes[i].Seg.Length; k++)
                    if (_ropes[i].Seg[k] != null) Destroy(_ropes[i].Seg[k].gameObject);
            _ropes.Clear();
        }
    }
}
