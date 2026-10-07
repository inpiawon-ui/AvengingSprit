using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>파티클 종류. 그림 한 장에 한 종류다.</summary>
    public enum ParticleFxKind
    {
        /// <summary>불티 — 때린 자리에서 튄다.</summary>
        Spark,
        /// <summary>연기 — 느리게 부풀며 사라진다.</summary>
        Smoke,
        /// <summary>빛 — 터지는 순간 한 번 부푼다.</summary>
        Glow,
        /// <summary>파편 — 돌며 튄다.</summary>
        Shard,
        /// <summary>잔불 — 작은 불씨가 떠오르다 꺼진다.</summary>
        Ember,
        /// <summary>속도선 — 날아가는 방향으로 길게 늘어난다.</summary>
        Streak,
        /// <summary>충격파 고리 — 한 장이 퍼지며 커진다.</summary>
        Ring,
        /// <summary>반짝이 — 치명타·획득 같은 «좋은 일»에 튄다.</summary>
        Star4,
        /// <summary>유령 빛 알갱이 — 흰 · 하늘색 십자별(승인 시안에서 잘라 낸 그림). 위에서 흘러내린다.</summary>
        GhostMote,
    }

    /// <summary>
    /// 알갱이의 **속성(색)**. 모양은 같고 색만 다른 그림을 고른다.
    ///
    /// ⚠ 런타임 곱셈 색 입히기를 쓰지 않는 이유 —
    ///   칠해진 그림(주황 불티)에 파랑을 곱하면 밝은 데는 회색, 어두운 데는 검정이 되어
    ///   **결이 죽고 다시 «도형»이 된다.** 색조만 돌려 구운 그림을 골라 쓴다.
    /// </summary>
    public enum ParticleElement
    {
        /// <summary>불 — 기본. 주황·노랑.</summary>
        Fire,
        Ice,
        Venom,
        Curse,
        /// <summary>흙먼지 — 채도를 뺀 것. 내려찍기·무너짐에 쓴다.</summary>
        Dust,
    }

    /// <summary>
    /// 파티클 뿌리개 (2026-09-20).
    ///
    /// ── 왜 만들었나 ──────────────────────────────────────────
    /// 이펙트가 전부 `Image` 낱장 애니메이션이라 **알갱이가 흩어지는 연출을 못 했다.**
    /// 불티 · 연기 · 파편처럼 «수십 개가 제각각 날아가는» 것은 낱장 그림으로는
    /// 만들 수 없다(그리는 사람도, 그릴 장 수도 감당이 안 된다).
    ///
    /// 씬 캔버스를 Screen Space - Camera 로 바꾸면서 파티클이 캔버스와 같은 자
    /// (sortingOrder)로 겹침을 다툴 수 있게 됐다 — 자세한 것은
    /// <see cref="InGameMainUI"/> 의 정렬 밴드 설명 참조.
    ///
    /// ── 구조 ────────────────────────────────────────────────
    /// **종류마다 파티클 시스템 하나**를 두고 거기에 알갱이를 밀어 넣는다.
    /// 터질 때마다 `GameObject` 를 만들어 풀에 넣는 방식은 이 경우 손해다 —
    /// 파티클 시스템 하나가 이미 수천 개의 알갱이를 관리하는 풀이기 때문이다.
    ///
    /// ⚠ 이 오브젝트는 **방(`RoomField`) 안**에 붙인다. 그래야 좌표가 방 좌표 그대로이고
    ///   화면이 스크롤·확대될 때 알갱이도 같이 따라간다.
    ///
    /// ⚠ `Time.timeScale` 을 그대로 따른다(`useUnscaledTime = false`). 히트스톱이 걸리면
    ///   알갱이도 같이 멈춰야 «시간이 멎었다»로 읽힌다.
    /// </summary>
    public sealed class ParticleFxPool : MonoBehaviour
    {
        /// <summary>한 종류가 동시에 들고 있을 수 있는 알갱이 수.</summary>
        private const int MaxParticles = 400;

        /// <summary>
        /// 캔버스 배율 보정. 파티클 크기는 월드 단위인데 우리 좌표는 «캔버스 픽셀»이다.
        /// 계층 배율을 그대로 쓰면(<see cref="ParticleSystemScalingMode.Hierarchy"/>)
        /// 캔버스 배율이 곱해져 픽셀 수로 읽히므로 따로 환산하지 않는다.
        /// </summary>
        private const ParticleSystemScalingMode Scaling = ParticleSystemScalingMode.Hierarchy;

        private readonly Dictionary<int, ParticleSystem> _systems = new();
        private readonly Dictionary<int, Material> _materials = new();

        private int _sortingOrder = InGameMainUI.ParticleOrder;

        /// <summary>모양 + 속성 한 쌍을 사전 열쇠 하나로. 종류가 몇 개 안 되어 곱셈이면 충분하다.</summary>
        private static int KeyOf(ParticleFxKind kind, ParticleElement element)
            => (int)kind * 8 + (int)element;

        /// <summary>그림 한 장을 꽂는다. 방이 만들어질 때 한 번. 없으면 기본 재질로 나온다.</summary>
        public void SetMaterial(ParticleFxKind kind, ParticleElement element, Material material)
        {
            if (material == null) return;
            int key = KeyOf(kind, element);
            _materials[key] = material;
            if (_systems.TryGetValue(key, out var ps))
            {
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = material;
                r.enabled = true;   // 재질이 없어 꺼 두었던 것이 늦게 오면 다시 켠다
            }
        }

        public void SetSortingOrder(int order)
        {
            _sortingOrder = order;
            foreach (var ps in _systems.Values)
                ps.GetComponent<ParticleSystemRenderer>().sortingOrder = order;
        }

        /// <summary>방을 나갈 때 — 남은 알갱이를 지운다. 안 지우면 다음 방에 떠 있다.</summary>
        public void ClearAll()
        {
            foreach (var ps in _systems.Values) ps.Clear(true);
        }

        // ── 미리 잡아 둔 연출 ────────────────────────────────────

        /// <summary>때린 자리 — 불티가 사방으로 튄다.</summary>
        public void Hit(Vector2 at, ParticleElement element, float power = 1f)
        {
            Burst(ParticleFxKind.Spark, element, at,
                  count: Mathf.RoundToInt(10 * power), speed: 260f * power,
                  size: 26f, life: 0.32f, spin: 0f);
            // ⚠ 빛 알갱이는 **작아야 한다.** 크게 키우면 더하기 합성이라 바닥이 뿌옇게
            //   들뜬 원판으로 보인다 — 번쩍임이 아니라 «접시가 놓였다»가 된다(2026-09-20).
            Burst(ParticleFxKind.Glow, element, at,
                  count: 1, speed: 0f, size: 46f * power, life: 0.16f, spin: 0f);
        }

        /// <summary>터진 자리 — 연기가 부풀어 오른다.</summary>
        public void Puff(Vector2 at, ParticleElement element, float power = 1f)
            => Burst(ParticleFxKind.Smoke, element, at,
                     count: Mathf.RoundToInt(3 * power), speed: 70f * power,
                     size: 64f * power, life: 0.75f, spin: 25f);

        /// <summary>부서진 자리 — 파편이 돌며 튄다.</summary>
        public void Shards(Vector2 at, ParticleElement element, float power = 1f)
            => Burst(ParticleFxKind.Shard, element, at,
                     count: Mathf.RoundToInt(7 * power), speed: 300f * power,
                     size: 26f, life: 0.5f, spin: 420f);

        /// <summary>
        /// 잡몹·호스트가 쓰러진 자리 — **작고 짧게.**
        ///
        /// 예전에는 `Shards` + `Puff` 를 그대로 불렀다(연기 3덩이 64 px · 0.75초, 파편 7개 · 초속 300).
        /// 구름이 몸집(100 px)만 해서 한 방에 예닐곱이 죽으면 화면이 먼지로 덮였다
        /// (기획 2026-09-28 「먼지 이펙트가 너무 과해」). 죽음은 방마다 여러 번 나는 일이라
        /// 하나하나는 가벼워야 한다 — 크게 터지는 것은 보스 몫이다.
        /// </summary>
        public void Death(Vector2 at, ParticleElement element)
        {
            Burst(ParticleFxKind.Smoke, element, at,
                  count: 2, speed: 35f, size: 34f, life: 0.4f, spin: 25f);
            Burst(ParticleFxKind.Shard, element, at,
                  count: 4, speed: 150f, size: 18f, life: 0.35f, spin: 420f);
        }

        /// <summary>퍼지는 고리 — 한 장이 커지며 사라진다. 폭발의 «밀려 나감»을 맡는다.</summary>
        public void Ring(Vector2 at, ParticleElement element, float size)
            => Burst(ParticleFxKind.Ring, element, at,
                     count: 1, speed: 0f, size: size, life: 0.3f, spin: 0f);

        /// <summary>떠오르는 잔불 — 불타는 자리에 몇 알씩 계속 뿌린다.</summary>
        public void Embers(Vector2 at, ParticleElement element, int count, float spread)
        {
            for (int i = 0; i < count; i++)
                Burst(ParticleFxKind.Ember, element, at + Random.insideUnitCircle * spread,
                      count: 1, speed: 40f, size: 16f, life: 0.8f, spin: 0f);
        }

        /// <summary>
        /// 한 점에서 다른 점으로 **쏜다.** 알갱이가 날아가는 것이 보여야
        /// 「저기로 갔다」가 읽힌다 — 결과만 나타나면 눈이 못 따라간다.
        ///
        /// <paramref name="seconds"/> 안에 닿도록 속도를 맞춘다.
        /// </summary>
        public void Dart(Vector2 from, Vector2 to, ParticleElement element, float seconds, int count = 3)
        {
            var d = to - from;
            float dist = d.magnitude;
            if (dist < 1f || seconds <= 0f) return;
            var dir = d / dist;
            float speed = dist / seconds;

            var ps = SystemOf(ParticleFxKind.Streak, element);
            var p = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false,
                startColor = Color.white,
                startLifetime = seconds,
            };
            for (int i = 0; i < count; i++)
            {
                // 앞뒤로 살짝 흩어 놓아 **한 줄기**가 아니라 «쏟아져 간다»로 보이게
                float lead = i / (float)Mathf.Max(1, count) * 0.22f;
                p.position = from + dir * (dist * lead * 0.25f);
                p.velocity = dir * speed * Random.Range(0.94f, 1.06f);
                p.startSize = 40f * Random.Range(0.8f, 1.15f);
                p.startLifetime = seconds * (1f - lead * 0.3f);
                p.rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                ps.Emit(p, 1);
            }
        }

        /// <summary>반짝이 — 치명타·획득처럼 «좋은 일»에.</summary>
        public void Sparkle(Vector2 at, ParticleElement element, float power = 1f)
            => Burst(ParticleFxKind.Star4, element, at,
                     count: Mathf.RoundToInt(5 * power), speed: 150f * power,
                     size: 26f, life: 0.45f, spin: 0f);

        /// <summary>
        /// 정해진 방향으로 **흘려 보낸다** — 유령 빛의 빛 알갱이처럼 위에서 아래로 떨어지는 것(2026-10-07).
        /// <paramref name="spread"/> 안의 아무 자리에서 태어나 <paramref name="velocity"/> 로 흐르고, 수명 끝에 흐려진다.
        /// </summary>
        public void Drift(ParticleFxKind kind, ParticleElement element, Vector2 at, Vector2 spread,
                          Vector2 velocity, float size, float life, int count = 1)
        {
            if (count <= 0) return;
            var ps = SystemOf(kind, element);
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false, startColor = Color.white };
            for (int i = 0; i < count; i++)
            {
                p.position = at + new Vector2(Random.Range(-spread.x, spread.x), Random.Range(-spread.y, spread.y));
                p.velocity = velocity * Random.Range(0.75f, 1.25f);
                p.startSize = size * Random.Range(0.6f, 1.2f);
                p.startLifetime = life * Random.Range(0.8f, 1.15f);
                p.rotation = 0f;
                p.angularVelocity = 0f;
                ps.Emit(p, 1);
            }
        }

        // ── 알맹이 ──────────────────────────────────────────────

        /// <summary>
        /// 한 자리에서 알갱이를 터뜨린다. 좌표·속도·중력은 **방 좌표(픽셀)** 단위다.
        /// </summary>
        public void Burst(ParticleFxKind kind, ParticleElement element, Vector2 at,
                          int count, float speed, float size, float life, float spin)
        {
            if (count <= 0) return;
            var ps = SystemOf(kind, element);
            var p = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false,
                // ⚠ 그림이 이미 칠해져 있다 — **흰색으로 둔다.** 색을 곱하면 결이 죽는다.
                startColor = Color.white,
                startLifetime = life,
            };
            for (int i = 0; i < count; i++)
            {
                // 사방으로 고르게 — 각도를 잘라 나누고 그 안에서 흔든다.
                // 완전 무작위로 뽑으면 한쪽에 뭉쳐 «터졌다»로 안 읽힌다.
                float a = (i + Random.value) / count * Mathf.PI * 2f;
                float v = speed * Random.Range(0.55f, 1f);
                p.position = at;
                p.velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v;
                p.startSize = size * Random.Range(0.75f, 1.25f);
                p.rotation = spin > 0f ? Random.Range(0f, 360f) : Mathf.Atan2(p.velocity.y, p.velocity.x) * Mathf.Rad2Deg;
                p.angularVelocity = spin > 0f ? Random.Range(-spin, spin) : 0f;
                ps.Emit(p, 1);
            }
        }

        /// <summary>
        /// 종류마다 내려앉는 힘(방 좌표 단위/초²). 양수면 아래로 떨어지고 음수면 떠오른다.
        ///
        /// ⚠ 힘은 파티클 시스템 **전체**에 걸리는 값이라 터질 때마다 바꾸면
        ///   이미 날아가던 알갱이까지 같이 휜다. 그래서 종류를 만들 때 한 번만 건다.
        /// </summary>
        private static float GravityOf(ParticleFxKind kind) => kind switch
        {
            ParticleFxKind.Spark => 320f,
            ParticleFxKind.Shard => 620f,
            ParticleFxKind.Smoke => -40f,    // 연기는 떠오른다
            ParticleFxKind.Ember => -90f,    // 불씨도 떠오른다 — 연기보다 가볍다
            _ => 0f,
        };

        /// <summary>
        /// 종류마다 «살아 있는 동안 크기가 어떻게 변하나».
        ///
        /// 고리는 **커지며** 사라져야 밀려 나가는 것으로 읽히고,
        /// 빛은 **줄며** 사라져야 번쩍임이 된다. 나머지는 그대로다.
        /// </summary>
        private static AnimationCurve SizeCurveOf(ParticleFxKind kind) => kind switch
        {
            ParticleFxKind.Ring => AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f),
            ParticleFxKind.Glow => AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f),
            ParticleFxKind.Smoke => AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.15f),
            _ => null,
        };

        private ParticleSystem SystemOf(ParticleFxKind kind, ParticleElement element)
        {
            int key = KeyOf(kind, element);
            if (_systems.TryGetValue(key, out var found)) return found;

            var go = new GameObject("ParticleFx_" + kind + "_" + element, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // 방 좌표계와 같은 기준점
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var ps = go.AddComponent<ParticleSystem>();
            // ⚠ 컴포넌트를 붙이는 순간 **이미 돌고 있다**(기본값 playOnAwake). 도는 중에는
            //   `duration` 을 못 바꾼다 — 유니티가 경고를 뱉고 값이 안 먹는다.
            //   설정하기 전에 완전히 세운다. 아래에서 다시 `Play()` 한다.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            // ⚠ **계속 돌려 둔다.** 한 번 돌고 멈추는(loop = false) 시스템은 멈춘 뒤에
            //   밀어 넣은 알갱이를 굴려 주지 않는다 — 제자리에 굳어 버린다.
            main.loop = true;
            main.duration = 1f;
            main.maxParticles = MaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = Scaling;
            main.useUnscaledTime = false;      // 히트스톱에 같이 멈춘다
            main.startSpeed = 0f;              // 속도는 발사할 때 하나씩 준다
            main.startSize = 1f;
            main.gravityModifier = 0f;         // 중력도 발사할 때 준다(방 좌표 단위라 직접 건다)

            var emission = ps.emission; emission.enabled = false;   // 우리가 직접 Emit 한다
            var shape = ps.shape; shape.enabled = false;

            // 사라질 때 **알파만** 뺀다. 색은 스킬 색 그대로 둬야 무엇이 터졌는지 읽힌다.
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

            var force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.Local;
            force.y = new ParticleSystem.MinMaxCurve(-GravityOf(kind));

            var curve = SizeCurveOf(kind);
            if (curve != null)
            {
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Local;   // 캔버스 평면에 붙인다
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = _sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // ⚠ 재질을 못 찾으면 유니티 **기본 파티클 재질**로 떨어지는데, 그것이 화면에
            //   **반투명 네모**로 그려진다. 표가 한 칸 비었을 뿐인데 사각형이 뜬다 —
            //   같은 모양의 불(Fire) 것으로 대신하고, 그것마저 없으면 아예 그리지 않는다.
            if (!_materials.TryGetValue(key, out var mat))
                _materials.TryGetValue(KeyOf(kind, ParticleElement.Fire), out mat);
            if (mat != null) renderer.sharedMaterial = mat;
            else
            {
                renderer.enabled = false;
                Debug.LogWarning($"[파티클] 재질 없음 — {kind}/{element}. 그리지 않는다.");
            }

            ps.Play();          // 알갱이를 직접 밀어 넣으려면 시스템이 돌고 있어야 한다
            _systems[key] = ps;
            return ps;
        }

        private static Gradient FadeOutGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }
}
