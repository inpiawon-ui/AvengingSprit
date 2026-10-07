using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 피스톤 압착기 — 7챕터 새 기믹(기획 5-2 「박자 맞춰 지나가기」, 2026-10-07).
    ///
    ///   PISTON_E · PISTON_W   바닥에 박힌 몸통(1칸)에서 머리가 오른쪽 · 왼쪽으로 2 m 튀어나왔다 들어간다.
    ///
    /// 한 바퀴 2.72초: 들어감 1.2 → 떨림 예고 0.4(상태등이 호박색 · 몸통이 떤다) → 쾅 0.12 → 뻗은 채 0.6 → 들어감 0.4.
    /// 뻗은 동안은 **몸도 탄도 못 지나간다** — 머리 · 축이 곧 벽이다(그림 = 판정, 다른 물건처럼 0.7배로 줄이지 않는다).
    /// 쾅 할 때 그 자리에 있던 것은 나든 적이든 맞고 밀려난다 — 적을 끌어들여 찍는 「이용」 물건이기도 하다.
    ///
    /// 그림(코덱스 시안 `mock_piston_01.png` 을 쪼갠 넉 장): `obj_piston_body_off` · `obj_piston_body_warn` ·
    /// `obj_piston_shaft` · `obj_piston_head`. 오른쪽으로 뻗는 것만 그렸다 — 왼쪽은 좌우를 뒤집는다.
    /// 쾅의 불티 · 파편은 공용 파티클이다(연출 리소스는 공통으로).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float PistonRestSeconds = 1.2f;
        private const float PistonWarnSeconds = 0.4f;
        private const float PistonSlamSeconds = 0.12f;
        private const float PistonHoldSeconds = 0.6f;
        private const float PistonBackSeconds = 0.4f;
        private const float PistonCycle = PistonRestSeconds + PistonWarnSeconds + PistonSlamSeconds
                                        + PistonHoldSeconds + PistonBackSeconds;
        private const float PistonReachMeters = 2.0f;     // rooms90_build.py PISTON_REACH 와 같다
        private const float PistonRamMeters = 0.8f;       // 머리 · 축이 막는 두께
        private const int PistonDamage = 14;
        private const float PistonEnemyHpRatio = 0.35f;   // 적은 체력 비율로 — 끌어들여 찍으면 잡몹 셋에 하나는 쓰러진다
        private const float PistonShakePx = 2f;

        // 그림 원본 크기(방 좌표 1칸 = 72px 기준)
        private const float PistonShaftW = 144f, PistonShaftH = 28f;
        private const float PistonHeadW = 40f, PistonHeadH = 72f;

        private sealed class PistonRig
        {
            public RectTransform Body, Rig, Shaft, Head;
            public Image ShaftImg, HeadImg;
            public Sprite Off, Warn;
            public Vector2 BodyPos;
            public Rect BodyRect;
            public float Dir;          // 1 = 오른쪽, -1 = 왼쪽
            public int HitCycle = -1;  // 이번 바퀴에 이미 찍었는가
            public bool Slammed;
        }

        private readonly Dictionary<Obstacle, PistonRig> _pistons = new();

        private static bool IsPiston(Obstacle o) => o.Kind == "PISTON_E" || o.Kind == "PISTON_W";

        /// <summary>`SetupHazard` 가 부른다. 몸통을 칸 크기 그대로로 되돌리고 축 · 머리를 단다.</summary>
        private void SetupPiston(Obstacle ob)
        {
            var body = ob.View != null ? (RectTransform)ob.View.transform : null;
            if (body == null) return;

            // 그림 = 판정 — 0.7배로 줄었던 칸을 1칸 그대로 되돌린다
            float m = Meters(1f);
            var c = ob.Home.center;
            var rect = new Rect(c.x - m * 0.5f, c.y - m * 0.5f, m, m);
            ob.Home = ob.Bounds = ob.ShotBounds = rect;
            body.sizeDelta = new Vector2(Mathf.Round(m), Mathf.Round(m));
            body.anchoredPosition = c;

            var rig = new PistonRig
            {
                Body = body, BodyPos = c, BodyRect = rect,
                Dir = ob.Kind == "PISTON_W" ? -1f : 1f,
                Off = GetSprite("obj_piston_body_off"),
                Warn = GetSprite("obj_piston_body_warn"),
            };
            if (rig.Off != null && ob.Img != null) { ob.Img.sprite = rig.Off; ob.Img.color = Color.white; ob.BaseColor = Color.white; }

            // 축 · 머리는 몸통의 자식 — 몸통과 같이 앞뒤 줄을 선다. 좌우는 뿌리 하나를 뒤집어 맞춘다
            float k = m / 72f;
            var rootGo = new GameObject("PistonRig", typeof(RectTransform));
            rig.Rig = (RectTransform)rootGo.transform;
            rig.Rig.SetParent(body, false);
            rig.Rig.anchorMin = rig.Rig.anchorMax = rig.Rig.pivot = new Vector2(0.5f, 0.5f);
            rig.Rig.sizeDelta = Vector2.zero;
            rig.Rig.localScale = new Vector3(rig.Dir, 1f, 1f);

            rig.ShaftImg = PistonPart(rig.Rig, "Shaft", "obj_piston_shaft",
                                      new Vector2(PistonShaftW, PistonShaftH) * k, new Vector2(1f, 0.5f));
            rig.ShaftImg.type = Image.Type.Filled;
            rig.ShaftImg.fillMethod = Image.FillMethod.Horizontal;
            rig.ShaftImg.fillOrigin = (int)Image.OriginHorizontal.Right;   // 머리 쪽(오른쪽)부터 보인다
            rig.Shaft = rig.ShaftImg.rectTransform;
            rig.HeadImg = PistonPart(rig.Rig, "Head", "obj_piston_head",
                                     new Vector2(PistonHeadW, PistonHeadH) * k, new Vector2(1f, 0.5f));
            rig.Head = rig.HeadImg.rectTransform;

            ob.Timer = 0f;
            _pistons[ob] = rig;
            ApplyPistonReach(ob, rig, 0f);
        }

        private Image PistonPart(RectTransform parent, string name, string sprite, Vector2 size, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            var img = go.GetComponent<Image>();
            img.sprite = GetSprite(sprite);
            img.color = img.sprite != null ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);   // 그림 오기 전 자리표시
            img.raycastTarget = false;
            return img;
        }

        /// <summary>한 프레임 — `TickHazards2` 가 부른다.</summary>
        private void TickPiston(Obstacle o, Unit me, Vector2 foot, float dt)
        {
            if (!_pistons.TryGetValue(o, out var rig) || rig.Body == null) return;

            float clock = Time.time + o.Phase * PistonCycle;
            int cycle = Mathf.FloorToInt(clock / PistonCycle);
            float t = clock - cycle * PistonCycle;

            float warnAt = PistonRestSeconds;
            float slamAt = warnAt + PistonWarnSeconds;
            float holdAt = slamAt + PistonSlamSeconds;
            float backAt = holdAt + PistonHoldSeconds;

            bool warn = t >= warnAt && t < slamAt;
            float reach = t < slamAt ? 0f
                        : t < holdAt ? (t - slamAt) / PistonSlamSeconds
                        : t < backAt ? 1f
                        : 1f - Mathf.SmoothStep(0f, 1f, (t - backAt) / PistonBackSeconds);

            // 예고 — 상태등이 켜지고 몸통이 정수 픽셀로 떤다(흐려지지 않게)
            var want = warn || (t >= slamAt && t < backAt) ? rig.Warn : rig.Off;
            if (want != null && o.Img != null && o.Img.sprite != want) o.Img.sprite = want;
            float shake = warn ? (Mathf.Repeat(Time.time, 0.1f) < 0.05f ? PistonShakePx : -PistonShakePx) : 0f;
            rig.Body.anchoredPosition = rig.BodyPos + new Vector2(shake, 0f);

            ApplyPistonReach(o, rig, reach);

            // 쾅 — 다 뻗는 순간 한 번. 불티 · 파편은 공용 파티클
            bool extended = t >= holdAt && t < backAt;
            if (extended && !rig.Slammed)
            {
                rig.Slammed = true;
                var tip = PistonTip(rig, 1f);
                if (_pfx != null && IsOnScreenAt(tip)) _pfx.Hit(tip, ParticleElement.Fire, 0.6f);
            }
            if (!extended) rig.Slammed = false;

            // 뻗어 나가는 동안 · 뻗은 채 — 그 자리에 있는 것을 찍고 밀어낸다(바퀴마다 한 번)
            if (reach <= 0.05f || t < slamAt || t >= backAt) return;
            var ram = PistonRam(rig, reach);
            // 뻗은 머리 안에 갇히지 않게 얕은 쪽으로 밀어낸다
            if (rig.HitCycle == cycle) { if (me != null) ResolveObstacles(me); return; }

            bool hit = false;
            // ⚠ 몸 폭까지 넣어 잰다 — 중심점으로 재면 머리가 몸 가장자리에 닿는 순간 먼저 밀어내 버려서
            //   한 번도 안 맞고 앞으로 밀려만 갔다(실측 2026-10-07).
            if (me != null && _host != null && Inflate(ram, FootHalf(me)).Contains(foot))
            {
                DamagePlayer(PistonDamage);
                hit = true;
            }
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (!Inflate(ram, new Vector2(e.BodyRadius * 0.5f, e.BodyRadius * 0.5f)).Contains(e.Position)) continue;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * PistonEnemyHpRatio));
                e.IsAggro = true;
                ShowDamage(e.Position, dmg, toEnemy: true);
                hit = true;
                if (e.TakeDamage(dmg)) KillEnemy(e);
                else ResolveObstacles(e);
            }
            if (hit) rig.HitCycle = cycle;
            if (me != null) ResolveObstacles(me);
        }

        private static Rect Inflate(Rect r, Vector2 half)
            => new Rect(r.xMin - half.x, r.yMin - half.y, r.width + half.x * 2f, r.height + half.y * 2f);

        /// <summary>머리 끝(방 좌표).</summary>
        private Vector2 PistonTip(PistonRig rig, float reach)
            => rig.BodyPos + new Vector2(rig.Dir * (rig.BodyRect.width * 0.5f + Meters(PistonReachMeters) * reach), 0f);

        /// <summary>뻗은 만큼의 막는 칸(몸통 밖). 몸통 가장자리에서 머리 끝까지.</summary>
        private Rect PistonRam(PistonRig rig, float reach)
        {
            float edge = rig.BodyPos.x + rig.Dir * rig.BodyRect.width * 0.5f;
            float tip = edge + rig.Dir * Meters(PistonReachMeters) * reach;
            float h = Meters(PistonRamMeters);
            return new Rect(Mathf.Min(edge, tip), rig.BodyPos.y - h * 0.5f, Mathf.Abs(tip - edge), h);
        }

        /// <summary>뻗은 정도를 그림과 판정에 같이 넣는다 — 보이는 만큼 막는다.</summary>
        private void ApplyPistonReach(Obstacle o, PistonRig rig, float reach)
        {
            float k = Meters(1f) / 72f;
            float half = rig.BodyRect.width * 0.5f;
            float travel = Meters(PistonReachMeters) * reach;
            // 뿌리 좌표(뒤집힌 쪽도 같은 식) — 머리의 오른쪽 끝 = 몸통 가장자리 + 뻗은 만큼
            float headRight = half + travel;
            rig.Head.anchoredPosition = new Vector2(headRight, 0f);
            float headLeft = headRight - PistonHeadW * k;
            float shaftLen = Mathf.Max(0f, headLeft - half);   // 몸통 밖으로 보이는 축만
            rig.Shaft.anchoredPosition = new Vector2(headLeft, 0f);
            rig.ShaftImg.fillAmount = Mathf.Clamp01(shaftLen / (PistonShaftW * k));
            rig.ShaftImg.enabled = shaftLen > 0.5f;

            if (reach <= 0.01f)
            {
                o.Bounds = o.ShotBounds = rig.BodyRect;
                return;
            }
            var ram = PistonRam(rig, reach);
            var all = Rect.MinMaxRect(Mathf.Min(rig.BodyRect.xMin, ram.xMin), rig.BodyRect.yMin,
                                      Mathf.Max(rig.BodyRect.xMax, ram.xMax), rig.BodyRect.yMax);
            o.Bounds = o.ShotBounds = all;
        }
    }
}
