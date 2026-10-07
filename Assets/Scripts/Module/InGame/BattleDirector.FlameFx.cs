using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 화염 분사구 불길 — **뿜어져 나가는 불**로 (2026-10-07 PD 「5-13 · 14 불꽃이 쌩뚱맞다 — 화염식으로 이펙트를 뿌려야지,
    /// 불 이미지를 대충 올려 놓은 느낌」).
    ///
    /// 예전 불길은 불 그림 석 장을 같은 크기로 늘어놓고 번갈아 바꾸기만 했다. 이제
    ///   · 켜지는 순간 분사구에서 불이 확 터진다(섬광 + 연기)
    ///   · 켜져 있는 동안 불꽃 줄기가 분사구에서 끝까지 **쏟아져 나간다**(공용 파티클 `Dart` · `Embers`)
    ///   · 불 그림 석 장은 분사구에서 멀수록 크게 · 제각각 일렁인다
    ///   · 꺼지는 순간 끝자리에 연기가 남는다
    /// 새 그림은 없다 — 파티클은 개발 세션이 만든 공용 것을 쓴다(PD 「노티 · 연출은 공통으로」).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float FlameEmitSeconds = 0.045f;   // 불꽃 줄기를 이만큼마다 한 번 쏜다
        private const float FlameDartSeconds = 0.26f;    // 줄기가 끝까지 가는 시간
        private const float FlameEmberChance = 0.35f;

        private readonly HashSet<Obstacle> _flameOn = new();
        private readonly Dictionary<Obstacle, float> _flameEmit = new();

        /// <summary>한 프레임 — `TickFlame` 이 불 그림을 넘긴 뒤 부른다.</summary>
        private void TickFlameFx(Obstacle o, bool on, float dt)
        {
            var dir = FlameDir(o);
            var nozzle = o.ShotBounds.center + dir * (o.ShotBounds.width * 0.5f);
            float len = Meters(FlameLengthMeters), wid = Meters(FlameWidthMeters);

            // 불 그림 석 장 — **뿜는 쪽으로 눕힌다**(그림은 위로 타오르는 불이라, 옆으로 뿜는 분사구에서 모닥불 셋이
            // 줄지어 선 것처럼 보였다 — PD 「쌩뚱맞다」의 정체). 분사구에서 멀수록 크게(0.65 → 1.25), 제각각 일렁인다
            if (on && o.Puffs != null)
            {
                int n = o.Puffs.Length;
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                for (int k = 0; k < n; k++)
                {
                    var img = o.Puffs[k];
                    if (img == null) continue;
                    float grow = n > 1 ? Mathf.Lerp(0.65f, 1.25f, k / (float)(n - 1)) : 1f;
                    float flicker = 1f + 0.09f * Mathf.Sin(Time.time * 19f + k * 2.1f);
                    var rt = img.rectTransform;
                    rt.localEulerAngles = new Vector3(0f, 0f, ang);
                    rt.localScale = new Vector3(grow * flicker, grow * (2f - flicker), 1f);
                }
            }

            if (_pfx == null || !IsOnScreenAt(o.ShotBounds.center)) { if (!on) _flameOn.Remove(o); return; }

            bool was = _flameOn.Contains(o);
            if (on && !was)
            {
                _flameOn.Add(o);
                _pfx.Hit(nozzle, ParticleElement.Fire, 0.9f);                     // 확 붙는다
                _pfx.Puff(nozzle + dir * (wid * 0.6f), ParticleElement.Fire, 0.7f);
            }
            else if (!on && was)
            {
                _flameOn.Remove(o);
                _pfx.Puff(nozzle + dir * (len * 0.7f), ParticleElement.Dust, 0.6f);   // 꺼진 자리에 연기
            }
            if (!on) return;

            _flameEmit.TryGetValue(o, out float t);
            t -= dt;
            while (t <= 0f)
            {
                t += FlameEmitSeconds;
                var perp = new Vector2(-dir.y, dir.x);
                var to = nozzle + dir * (len * Random.Range(0.85f, 1.05f)) + perp * (wid * Random.Range(-0.35f, 0.35f));
                _pfx.Dart(nozzle, to, ParticleElement.Fire, FlameDartSeconds, 2);
                if (Random.value < FlameEmberChance)
                    _pfx.Embers(nozzle + dir * (len * Random.Range(0.3f, 0.95f)), ParticleElement.Fire, 1, wid * 0.3f);
            }
            _flameEmit[o] = t;
        }

        /// <summary>방을 나갈 때 — `ClearWarns` 가 부른다.</summary>
        private void ClearFlameFx()
        {
            _flameOn.Clear();
            _flameEmit.Clear();
        }
    }
}
