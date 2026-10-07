using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 장애물 **움직임 패턴** — 회전 톱니 (2026-10-07 PD 「톱니가 중점을 기준으로 선을 그어서 한 바퀴 도는 패턴 …
    /// 너무 획일화되어 찍어내는 데에만 집중되어 있다」. 기획 `AVSR_Content_EnemyGimmick.md` 5-3).
    ///
    /// 예전 톱니는 모두 반경 2.2 m · 2.6초 한 바퀴 · 같은 방향이었다. 이제 톱니마다 움직임을 하나 고른다.
    ///
    ///   팔(Arm)      축에서 사슬 팔이 그어지고 끝의 톱니가 돈다. **팔도 아프다** — 팔이 지나간 뒤를 따라 돈다
    ///   다중 팔       같은 축에 팔 2 · 3개. 팔 사이 칸으로 들어가 같이 돈다
    ///   가감속        반 바퀴는 느리게, 반 바퀴는 빠르게. 느린 구간을 노려 들어간다
    ///   팔 길이 바뀜  돌면서 팔이 늘었다 줄었다 — 안쪽 · 바깥 안전 칸이 바뀐다
    ///   (반대 방향)   같은 방의 톱니는 번갈아 반대로 돈다 — 둘 사이가 열렸다 닫혔다 한다
    ///
    /// 고르는 법 : 방 ID 와 톱니 자리로 정한다(같은 방은 늘 같은 움직임). 챕터가 깊을수록 고를 수 있는 것이 는다.
    /// ⚠ 배치표에 움직임 칸이 아직 없다 — 생기면 여기서 고르지 않고 표 값을 쓴다(TBD: rooms90 OBJ 칸 추가).
    ///
    /// 그림은 있는 것을 쓴다 — 톱니(`obj_blade`) · 축 · 사슬(`obj_hammer_chain`).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private enum BladeMotion { Arm, MultiArm, Ease, Pulse }

        private sealed class BladeRig
        {
            public BladeMotion Motion;
            public int Arms = 1;
            public float RadiusMeters;
            public float TurnSeconds;
            public float Dir = 1f;           // +1 반시계 · −1 시계
            public RectTransform[] Chains;   // 팔마다 사슬
            public RectTransform[] Extras;   // 둘째 · 셋째 톱니(첫째는 장애물 본체)
            public Image[] ExtraImgs;
            public Vector2[] Blades;         // 이번 프레임 톱니 자리(방 좌표) — 판정이 쓴다
            public float ArmLength;          // 이번 프레임 팔 길이(px)
            public float Angle;              // 이번 프레임 첫 팔 각도(라디안)
        }

        private readonly Dictionary<Obstacle, BladeRig> _bladeRigs = new();

        /// <summary>팔의 굵기(판정 반폭, m). 그림 사슬보다 조금 얇게 — 스친 것 같은데 맞으면 억울하다.</summary>
        private const float BladeArmHalfMeters = 0.18f;

        /// <summary>
        /// 톱니 하나의 움직임을 정한다. `SetupMoving` 의 ROTATING_BLADE 가 부른다(축 그림을 만든 뒤).
        /// </summary>
        private void SetupBladeRig(Obstacle ob)
        {
            if (ob.View2 == null) return;
            int ch = _canonRoom != null ? _canonRoom.Chapter : _runChapter;
            int h = MotionHash((_canonRoom != null ? _canonRoom.RoomId : "") + ob.Home.center);
            var rig = new BladeRig
            {
                RadiusMeters = new[] { 1.8f, 2.2f, 2.6f }[h % 3],
                TurnSeconds = new[] { 2.4f, 3.0f, 3.6f }[(h / 3) % 3],
                // 같은 방 안에서 번갈아 반대로 — 짝수 번째 톱니는 시계 방향
                Dir = (CountBladeRigsInRoom() % 2 == 0) ? 1f : -1f,
            };

            // 챕터가 깊을수록 고를 수 있는 움직임이 는다. 2 챕터는 팔 하나로 「팔도 아프다」를 먼저 배운다
            var pool = ch <= 2 ? new[] { BladeMotion.Arm }
                     : ch <= 5 ? new[] { BladeMotion.Arm, BladeMotion.Ease, BladeMotion.Pulse }
                     : new[] { BladeMotion.MultiArm, BladeMotion.Ease, BladeMotion.Pulse, BladeMotion.MultiArm };
            rig.Motion = pool[(h / 9) % pool.Length];
            if (rig.Motion == BladeMotion.MultiArm) rig.Arms = ch >= 8 && (h & 1) == 1 ? 3 : 2;

            var chainArt = GetSprite("obj_hammer_chain") ?? GetSprite("obj_crusher_chain");
            rig.Chains = new RectTransform[rig.Arms];
            rig.Extras = new RectTransform[rig.Arms - 1];
            rig.ExtraImgs = new Image[rig.Arms - 1];
            rig.Blades = new Vector2[rig.Arms];
            float chainWidth = Meters(0.35f);
            for (int i = 0; i < rig.Arms; i++)
            {
                // 사슬 — 축에 한쪽 끝이 박혀 있다(피벗 위 가운데). 아래로 늘어진 것을 톱니 쪽으로 돌린다
                var c = new GameObject("Arm", typeof(RectTransform), typeof(Image));
                var crt = (RectTransform)c.transform;
                crt.SetParent(ob.View2, false);
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(chainWidth, Meters(rig.RadiusMeters));
                var ci = c.GetComponent<Image>();
                ci.sprite = chainArt;
                ci.type = Image.Type.Tiled;     // 늘어나도 고리가 늘어지지 않게 반복한다
                ci.enabled = chainArt != null;
                ci.raycastTarget = false;
                crt.SetAsFirstSibling();
                rig.Chains[i] = crt;
            }
            for (int i = 0; i < rig.Arms - 1; i++)
            {
                var b = new GameObject("Blade", typeof(RectTransform), typeof(Image));
                var brt = (RectTransform)b.transform;
                brt.SetParent(ob.View2, false);
                brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = ((RectTransform)ob.View.transform).sizeDelta;
                var bi = b.GetComponent<Image>();
                bi.sprite = ob.Img != null ? ob.Img.sprite : null;
                bi.raycastTarget = false;
                rig.Extras[i] = brt;
                rig.ExtraImgs[i] = bi;
            }
            _bladeRigs[ob] = rig;
        }

        private int CountBladeRigsInRoom()
        {
            int n = 0;
            foreach (var pair in _bladeRigs)
                if (pair.Key.View2 != null) n++;
            return n;
        }

        /// <summary>방이 바뀌면 지난 방 톱니를 잊는다. 그림은 축(`View2`)의 자식이라 축과 같이 지워진다.</summary>
        private void ClearBladeRigs() => _bladeRigs.Clear();

        /// <summary>한 프레임 — 톱니 자리 · 사슬 · 그림. 처리했으면 true.</summary>
        private bool TickBladeRig(Obstacle o, RectTransform rt)
        {
            if (!_bladeRigs.TryGetValue(o, out var rig)) return false;

            float t = Mathf.Repeat(Time.time / rig.TurnSeconds + o.Phase, 1f);
            if (rig.Motion == BladeMotion.Ease)
                t = t + 0.12f * Mathf.Sin(t * Mathf.PI * 2f);   // 반 바퀴는 느리고 반 바퀴는 빠르다
            float ang = rig.Dir * t * Mathf.PI * 2f;
            float rad = Meters(rig.RadiusMeters);
            if (rig.Motion == BladeMotion.Pulse)
                rad *= 0.62f + 0.38f * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / (rig.TurnSeconds * 0.8f) + o.Phase * 6f));
            rig.Angle = ang;
            rig.ArmLength = rad;

            var axis = o.Home.center;
            for (int i = 0; i < rig.Arms; i++)
            {
                float a = ang + Mathf.PI * 2f * i / rig.Arms;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                rig.Blades[i] = axis + dir * rad;

                var chain = rig.Chains[i];
                chain.sizeDelta = new Vector2(chain.sizeDelta.x, rad);
                // 기본이 아래(−y)로 늘어진다 — 아래 방향을 톱니 쪽으로 돌린다
                chain.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);

                if (i == 0) MoveObstacle(o, rt, rig.Blades[0]);
                else rig.Extras[i - 1].anchoredPosition = rig.Blades[i] - axis;
            }

            // 톱니 그림 — 넉 장을 돌리고, 한 장뿐인 무대 톱날은 그림을 돌린다(예전과 같다)
            int frame = Mathf.FloorToInt(Mathf.Repeat(t * 16f, 4f));
            SetFrame(o, frame);
            bool single = o.Frames != null && o.Frames.Length > 1 && o.Frames[0] == o.Frames[1];
            float spin = -rig.Dir * t * 360f * 4f;
            if (single) rt.localRotation = Quaternion.Euler(0f, 0f, spin);
            for (int i = 0; i < rig.ExtraImgs.Length; i++)
            {
                if (o.Frames != null && o.Frames.Length > 0) rig.ExtraImgs[i].sprite = o.Frames[frame % o.Frames.Length];
                if (single) rig.Extras[i].localRotation = Quaternion.Euler(0f, 0f, spin);
            }
            return true;
        }

        /// <summary>
        /// 톱니 판정 — 톱니마다 원 · 팔마다 선. `TickHazards` 가 이 톱니는 여기로 보낸다.
        /// 맞는 박자(`Tick`)는 예전 판정과 같은 타이머를 쓴다 — 톱니에서 팔로 옮겨 가도 두 번 맞지 않는다.
        /// </summary>
        private bool BurnBladeRig(Obstacle o, Unit u, float dt)
        {
            if (!_bladeRigs.TryGetValue(o, out var rig)) return false;
            if (u == null || !u.IsAlive || u.IsDying) return true;
            var foot = new Vector2(u.Position.x, u.Position.y - FootDrop(u));

            float bladeR = Mathf.Min(o.ShotBounds.width, o.ShotBounds.height) * 0.45f;
            float armHalf = Meters(BladeArmHalfMeters);
            bool hit = false;
            for (int i = 0; i < rig.Arms && !hit; i++)
            {
                if ((foot - rig.Blades[i]).sqrMagnitude <= bladeR * bladeR) { hit = true; break; }
                // 팔 — 축에서 톱니 안쪽 끝까지(톱니 몸은 위 원이 맡는다)
                var a = o.Home.center;
                var b = rig.Blades[i];
                var ab = b - a;
                float len = ab.magnitude;
                if (len < 1f) continue;
                float along = Vector2.Dot(foot - a, ab) / len;
                if (along < 0f || along > len - bladeR) continue;
                float side = Mathf.Abs(Vector2.Dot(foot - a, new Vector2(-ab.y, ab.x) / len));
                if (side <= armHalf) hit = true;
            }

            if (!hit) { _hazardTimer.Remove(u); return true; }
            _hazardTimer.TryGetValue(u, out float tm);
            tm -= dt;
            if (tm > 0f) { _hazardTimer[u] = tm; return true; }
            _hazardTimer[u] = o.Tick;
            if (u == _host || u == _ghost) DamagePlayer(o.Damage);
            else { u.TakeDamage(o.Damage); ShowDamage(u.Position, o.Damage, true); }
            return true;
        }

        private static int MotionHash(string s)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h & 0x7FFFFFFF;
            }
        }
    }
}
