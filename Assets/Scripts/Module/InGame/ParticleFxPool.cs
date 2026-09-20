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

        private readonly Dictionary<ParticleFxKind, ParticleSystem> _systems = new();
        private readonly Dictionary<ParticleFxKind, Material> _materials = new();

        private int _sortingOrder = InGameMainUI.ParticleOrder;

        /// <summary>종류별 그림. 방이 만들어질 때 한 번 꽂아 준다. 없으면 기본 재질로 나온다.</summary>
        public void SetMaterial(ParticleFxKind kind, Material material)
        {
            if (material == null) return;
            _materials[kind] = material;
            if (_systems.TryGetValue(kind, out var ps))
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
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
        public void Hit(Vector2 at, Color color, float power = 1f)
        {
            Burst(ParticleFxKind.Spark, at, color,
                  count: Mathf.RoundToInt(10 * power), speed: 260f * power,
                  size: 10f, life: 0.32f, spin: 0f);
            // ⚠ 빛 알갱이는 **작아야 한다.** 크게 키우면 더하기 합성이라 바닥이 뿌옇게
            //   들뜬 원판으로 보인다 — 번쩍임이 아니라 «접시가 놓였다»가 된다(2026-09-20).
            Burst(ParticleFxKind.Glow, at, color,
                  count: 1, speed: 0f, size: 26f * power, life: 0.14f, spin: 0f);
        }

        /// <summary>터진 자리 — 연기가 부풀어 오른다.</summary>
        public void Puff(Vector2 at, Color color, float power = 1f)
            => Burst(ParticleFxKind.Smoke, at, color,
                     count: Mathf.RoundToInt(5 * power), speed: 90f * power,
                     size: 30f * power, life: 0.7f, spin: 40f);

        /// <summary>부서진 자리 — 파편이 돌며 튄다.</summary>
        public void Shards(Vector2 at, Color color, float power = 1f)
            => Burst(ParticleFxKind.Shard, at, color,
                     count: Mathf.RoundToInt(7 * power), speed: 300f * power,
                     size: 13f, life: 0.5f, spin: 520f);

        // ── 알맹이 ──────────────────────────────────────────────

        /// <summary>
        /// 한 자리에서 알갱이를 터뜨린다. 좌표·속도·중력은 **방 좌표(픽셀)** 단위다.
        /// </summary>
        public void Burst(ParticleFxKind kind, Vector2 at, Color color,
                          int count, float speed, float size, float life, float spin)
        {
            if (count <= 0) return;
            var ps = SystemOf(kind);
            var p = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false,
                startColor = color,
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
            _ => 0f,
        };

        private ParticleSystem SystemOf(ParticleFxKind kind)
        {
            if (_systems.TryGetValue(kind, out var found)) return found;

            var go = new GameObject("ParticleFx_" + kind, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // 방 좌표계와 같은 기준점
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var ps = go.AddComponent<ParticleSystem>();
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

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.Local;   // 캔버스 평면에 붙인다
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = _sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (_materials.TryGetValue(kind, out var mat)) renderer.sharedMaterial = mat;

            ps.Play();          // 알갱이를 직접 밀어 넣으려면 시스템이 돌고 있어야 한다
            _systems[kind] = ps;
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
