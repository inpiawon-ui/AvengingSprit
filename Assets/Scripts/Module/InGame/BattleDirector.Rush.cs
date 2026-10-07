using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 돌진 예고 위의 **흐르는 꺾쇠 · 발밑 흙먼지** (2026-10-07 PD 「PPT 도형 같다 — 무난한 이미지로」 → 시안 A + B 흙먼지 통과).
    /// 시안 `_exchange/ref/rush_fx/mock_rush_ABC.png` · 부품 `rush_parts_raw.png`.
    ///
    /// 띠 몸통 · 테두리는 바닥 예고(`DangerView`)가 그린다. 여기서는 그 위에
    ///   · 꺾쇠(›) 여러 개가 시작점에서 끝으로 **계속 흘러간다** — 어느 쪽으로 달려오는지
    ///   · 예고가 찰수록 더 빨리 · 더 밝게 흐른다 — 곧 온다
    ///   · 발밑에서 흙먼지가 피어오른다(4컷을 돌린다) — 땅을 긁으며 겨누는 중
    /// 그림은 한 장씩만 받고 움직임은 여기서 준다.
    ///
    /// **공용이다**(PD 「노티 같은 건 다른 곳에서도 함께 — 리소스를 너무 다양하게 하지 말 것」).
    ///   · 꺾쇠 : 일직선 띠 예고 전부 — 박쥐 급강하 · 돌 고릴라 돌진 · 멧돼지 돌진 · 화염 분사구 · 저격 조준선
    ///   · 흙먼지 : 몸이 땅을 박차는 돌진에만(`dust: true`)
    /// 예고와 같이 켜지고 같이 꺼진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private sealed class RushFx
        {
            public Unit Owner;
            public RectTransform Root;
            public Image[] Chevrons;
            public Image Dust;
            public Sprite[] DustFrames;   // 시작할 때 한 번 찾는다 — 매 프레임 이름을 조립하지 않게
            public float Length, Total, Left, Scroll, Spacing;
            public bool Active;
            public bool HasOwner;   // 몸이 건 예고 — 그 몸이 죽으면 같이 거둔다(분사구는 몸이 없다)
        }

        private readonly List<RushFx> _rushFx = new();

        private const int RushChevronMax = 14;
        private const float RushChevronGapMeters = 0.95f;   // 꺾쇠 사이 — 시안 A 의 간격
        private const float RushChevronScale = 0.85f;       // 꺾쇠 크기 / 띠 폭
        private const float RushChevronMinMeters = 0.55f;   // 가는 띠(저격 조준선)에서도 보이는 최소 크기
        private const float RushFlowMinMps = 2.2f;          // 처음 흐르는 빠르기(m/초)
        private const float RushFlowMaxMps = 9.0f;          // 막 달려 나가기 직전
        private const float RushFadeMeters = 0.6f;          // 띠 양 끝에서 나타나고 사라지는 거리
        private const float RushDustFps = 10f;
        private const float RushDustMeters = 1.3f;
        private static readonly string[] RushDustNames = { "fx_rush_dust_1", "fx_rush_dust_2", "fx_rush_dust_3", "fx_rush_dust_4" };

        /// <summary>일직선 띠 예고를 걸 때 같이 부른다. 띠와 같은 자리 · 같은 길이 · 같은 시간.</summary>
        private void StartRushFx(Unit owner, Vector2 from, Vector2 to, float width, float seconds, bool dust)
        {
            if (_fieldLayer == null) return;
            var chev = GetSprite("fx_rush_chevron");
            if (chev == null) return;   // 그림이 아직 없으면 띠만 보인다(예전 모양)

            RushFx fx = null;
            for (int i = 0; i < _rushFx.Count; i++)
                if (!_rushFx[i].Active) { fx = _rushFx[i]; break; }
            if (fx == null)
            {
                fx = new RushFx();
                var go = new GameObject("RushFx", typeof(RectTransform));
                fx.Root = (RectTransform)go.transform;
                fx.Root.SetParent(_fieldLayer, false);
                fx.Root.anchorMin = fx.Root.anchorMax = new Vector2(0f, 1f);   // 방 좌표 그대로(DangerView 와 같다)
                fx.Root.pivot = new Vector2(0f, 0.5f);
                fx.Chevrons = new Image[RushChevronMax];
                for (int k = 0; k < RushChevronMax; k++) fx.Chevrons[k] = NewRushImage("Chevron", fx.Root);
                fx.Dust = NewRushImage("Dust", _fieldLayer);
                _rushFx.Add(fx);
            }

            var dir = to - from;
            // 방 밖으로는 그리지 않는다 — 조준선은 방 대각선만큼 뻗는다(방 좌표: x 0 → 폭, y 0 → −높이)
            float len = dir.magnitude, room = len;
            if (len > 0.001f)
            {
                var n = dir / len;
                if (n.x > 0.0001f) room = Mathf.Min(room, (_roomSize.x - from.x) / n.x);
                if (n.x < -0.0001f) room = Mathf.Min(room, -from.x / n.x);
                if (n.y > 0.0001f) room = Mathf.Min(room, -from.y / n.y);
                if (n.y < -0.0001f) room = Mathf.Min(room, (-_roomSize.y - from.y) / n.y);
                dir = n * Mathf.Max(0f, room);
            }
            fx.Length = dir.magnitude;
            if (fx.Length < 1f) return;
            fx.Owner = owner;
            fx.HasOwner = owner != null;
            fx.Total = fx.Left = Mathf.Max(0.05f, seconds);
            fx.Scroll = 0f;
            // 긴 띠(방을 가로지르는 조준선)에서도 꺾쇠가 끝까지 닿게 — 모자라면 간격을 벌린다
            fx.Spacing = Mathf.Max(Meters(RushChevronGapMeters), fx.Length / RushChevronMax);
            fx.Active = true;

            fx.Root.gameObject.SetActive(true);
            fx.Root.SetAsLastSibling();   // 바닥 예고 위에
            fx.Root.anchoredPosition = from;
            fx.Root.sizeDelta = new Vector2(fx.Length, width);
            fx.Root.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            float s = Mathf.Max(width * RushChevronScale, Meters(RushChevronMinMeters));
            for (int k = 0; k < fx.Chevrons.Length; k++)
            {
                var im = fx.Chevrons[k];
                im.sprite = chev;
                ((RectTransform)im.transform).sizeDelta = new Vector2(s, s);
            }

            // 흙먼지 — 발밑에서, 달려 나갈 쪽 반대로 조금 물린 자리
            fx.DustFrames ??= new Sprite[4];
            for (int k = 0; k < 4; k++) fx.DustFrames[k] = GetSprite(RushDustNames[k]);
            var dustArt = dust && owner != null ? fx.DustFrames[0] : null;
            fx.Dust.gameObject.SetActive(dustArt != null);
            if (dustArt != null)
            {
                float d = Meters(RushDustMeters);
                var rt = (RectTransform)fx.Dust.transform;
                rt.sizeDelta = new Vector2(d, d);
                var foot = owner != null ? new Vector2(owner.Position.x, owner.Position.y - FootDrop(owner)) : from;
                rt.anchoredPosition = foot - dir.normalized * (d * 0.25f) + new Vector2(0f, d * 0.3f);
                // 그림은 오른쪽으로 달리는 몸 뒤 먼지다(왼쪽으로 쏠림) — 왼쪽으로 달리면 뒤집는다
                rt.localScale = new Vector3(dir.x < 0f ? -1f : 1f, 1f, 1f);
                fx.Dust.sprite = dustArt;
                fx.Dust.transform.SetAsLastSibling();
            }
            TickOneRush(fx, 0f);
        }

        private static Image NewRushImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, parent is RectTransform p && p.pivot.y == 0.5f ? 0.5f : 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var im = go.GetComponent<Image>();
            im.raycastTarget = false;
            go.SetActive(false);
            return im;
        }

        /// <summary>매 프레임 — `TickWarns` 가 부른다.</summary>
        private void TickRushFx(float dt)
        {
            for (int i = 0; i < _rushFx.Count; i++)
            {
                var fx = _rushFx[i];
                if (!fx.Active) continue;
                fx.Left -= dt;
                if (fx.Left <= 0f || (fx.HasOwner && (fx.Owner == null || !fx.Owner.IsAlive))) { HideRush(fx); continue; }
                TickOneRush(fx, dt);
            }
        }

        private void TickOneRush(RushFx fx, float dt)
        {
            float p = 1f - Mathf.Clamp01(fx.Left / fx.Total);   // 0 → 1 예고가 찬 만큼
            fx.Scroll += Meters(Mathf.Lerp(RushFlowMinMps, RushFlowMaxMps, p * p)) * dt;
            float cycle = fx.Spacing * RushChevronMax;
            float fade = Meters(RushFadeMeters);
            float glow = Mathf.Lerp(0.55f, 1f, p);
            for (int k = 0; k < fx.Chevrons.Length; k++)
            {
                var im = fx.Chevrons[k];
                float x = Mathf.Repeat(k * fx.Spacing + fx.Scroll, cycle);
                bool on = x <= fx.Length;
                if (im.gameObject.activeSelf != on) im.gameObject.SetActive(on);
                if (!on) continue;
                ((RectTransform)im.transform).anchoredPosition = new Vector2(x, 0f);
                // 양 끝에서 스며 나오고 스며 든다 — 띠 밖으로 튀어나가 보이지 않게
                float a = Mathf.Clamp01(x / fade) * Mathf.Clamp01((fx.Length - x) / fade);
                im.color = new Color(glow, glow, glow, a);
            }

            if (fx.Dust.gameObject.activeSelf)
            {
                var s = fx.DustFrames[Mathf.FloorToInt((fx.Total - fx.Left) * RushDustFps) % 4];
                if (s != null) fx.Dust.sprite = s;
            }
        }

        private static void HideRush(RushFx fx)
        {
            fx.Active = false;
            fx.Owner = null;
            if (fx.Root != null) fx.Root.gameObject.SetActive(false);
            if (fx.Dust != null) fx.Dust.gameObject.SetActive(false);
        }

        /// <summary>그 몸이 건 돌진 예고 장식을 거둔다(`CancelWarnsOf` 와 같이).</summary>
        private void StopRushFxOf(Unit owner)
        {
            for (int i = 0; i < _rushFx.Count; i++)
                if (_rushFx[i].Active && _rushFx[i].Owner == owner) HideRush(_rushFx[i]);
        }

        /// <summary>방을 나갈 때(`ClearWarns` 와 같이).</summary>
        private void ClearRushFx()
        {
            for (int i = 0; i < _rushFx.Count; i++) HideRush(_rushFx[i]);
        }
    }
}
