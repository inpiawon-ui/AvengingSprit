using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Module.InGame
{
    /// <summary>
    /// 파티클 배선 (2026-09-20).
    ///
    /// 뿌리개(<see cref="ParticleFxPool"/>)를 **방 안에** 달고, 재질 넉 장을 주소로 받아 꽂는다.
    /// 방 안에 달아야 좌표가 방 좌표 그대로이고 화면이 흔들릴 때 알갱이도 같이 흔들린다.
    ///
    /// ⚠ 재질이 없으면(주소 로드 실패) 뿌리개는 **기본 재질**로 그냥 돈다 —
    ///   흰 네모가 튀지만 판이 멈추지는 않는다. 그림 하나 때문에 전투를 못 하면 안 된다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private ParticleFxPool _pfx;

        /// <summary>
        /// 알갱이 층. **다른 방 레이어와 똑같이 <see cref="Place"/> 로 움직인다** —
        /// 스크롤·줌·흔들림을 한 자로 재지 않으면 알갱이만 제자리에 남는다.
        /// (처음에 앵커 값을 한 번 베껴 놓았더니, 방이 스크롤하는 순간 유령이 서 있는 자리와
        ///  알갱이가 터지는 자리가 화면 한 칸만큼 어긋났다 — 2026-09-20)
        /// </summary>
        private RectTransform _pfxLayer;

        /// <summary>
        /// 모양 · 속성 ↔ 주소. `CreateParticleFx` 에디터 툴이 같은 이름으로 재질을 만든다.
        ///
        /// ⚠ 속성 변형은 **색조만 돌려 구운 그림**이다(`Tools/particle_hue.py`).
        ///   런타임 곱셈 색 입히기는 칠해진 그림의 결을 죽인다 —
        ///   주황 불티에 파랑을 곱하면 밝은 데는 회색, 어두운 데는 검정이 되어 «도형»으로 돌아간다.
        ///
        /// 전용 그림이 없는 칸은 가장 가까운 것을 돌려 쓴다(얼음 연기 → 흙먼지 연기).
        /// </summary>
        private static readonly (ParticleFxKind Kind, ParticleElement Element, string Address)[] ParticleArt =
        {
            (ParticleFxKind.Spark,  ParticleElement.Fire,  "ParticleFx/spark"),
            (ParticleFxKind.Spark,  ParticleElement.Ice,   "ParticleFx/spark_ice"),
            (ParticleFxKind.Spark,  ParticleElement.Venom, "ParticleFx/spark_venom"),
            (ParticleFxKind.Spark,  ParticleElement.Curse, "ParticleFx/spark_curse"),
            (ParticleFxKind.Spark,  ParticleElement.Dust,  "ParticleFx/spark"),
            (ParticleFxKind.Glow,   ParticleElement.Fire,  "ParticleFx/glow"),
            (ParticleFxKind.Glow,   ParticleElement.Ice,   "ParticleFx/glow_ice"),
            (ParticleFxKind.Glow,   ParticleElement.Venom, "ParticleFx/glow_venom"),
            (ParticleFxKind.Glow,   ParticleElement.Curse, "ParticleFx/glow_curse"),
            (ParticleFxKind.Glow,   ParticleElement.Dust,  "ParticleFx/glow"),
            (ParticleFxKind.Smoke,  ParticleElement.Fire,  "ParticleFx/smoke"),
            (ParticleFxKind.Smoke,  ParticleElement.Dust,  "ParticleFx/smoke_dust"),
            (ParticleFxKind.Smoke,  ParticleElement.Ice,   "ParticleFx/smoke_dust"),
            (ParticleFxKind.Smoke,  ParticleElement.Venom, "ParticleFx/smoke_dust"),
            (ParticleFxKind.Smoke,  ParticleElement.Curse, "ParticleFx/smoke_dust"),
            (ParticleFxKind.Shard,  ParticleElement.Ice,   "ParticleFx/shard"),
            (ParticleFxKind.Shard,  ParticleElement.Dust,  "ParticleFx/shard_dust"),
            (ParticleFxKind.Shard,  ParticleElement.Fire,  "ParticleFx/shard_dust"),
            (ParticleFxKind.Shard,  ParticleElement.Venom, "ParticleFx/shard_dust"),
            (ParticleFxKind.Shard,  ParticleElement.Curse, "ParticleFx/shard_dust"),
            (ParticleFxKind.Ember,  ParticleElement.Fire,  "ParticleFx/ember"),
            (ParticleFxKind.Ring,   ParticleElement.Fire,  "ParticleFx/ring"),
            (ParticleFxKind.Ring,   ParticleElement.Ice,   "ParticleFx/ring_ice"),
            (ParticleFxKind.Ring,   ParticleElement.Venom, "ParticleFx/ring_venom"),
            (ParticleFxKind.Ring,   ParticleElement.Curse, "ParticleFx/ring_curse"),
            (ParticleFxKind.Ring,   ParticleElement.Dust,  "ParticleFx/ring"),
            (ParticleFxKind.Star4,  ParticleElement.Fire,  "ParticleFx/star4"),
            (ParticleFxKind.GhostMote, ParticleElement.Fire, "ParticleFx/ghostmote"),
            (ParticleFxKind.Streak, ParticleElement.Fire,  "ParticleFx/streak"),
        };

        private void MakeParticleLayer()
        {
            if (_pfx != null || _unitLayer == null) return;

            var go = new GameObject("ParticleLayer", typeof(RectTransform));
            go.transform.SetParent(_unitLayer.parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = _unitLayer.anchorMin;
            rt.anchorMax = _unitLayer.anchorMax;
            rt.pivot = _unitLayer.pivot;
            rt.sizeDelta = _unitLayer.sizeDelta;
            _pfxLayer = rt;                    // 자리는 ApplyScroll 이 매 프레임 잡는다

            _pfx = go.AddComponent<ParticleFxPool>();
            _pfx.SetSortingOrder(InGameMainUI.ParticleOrder);
        }

        // ── 낱장 그림 ↔ 알갱이 ──────────────────────────────────
        //
        // 낱장 그림(`fx_*`)은 **모양**을 말한다 — 불덩이가 부풀고, 얼음이 솟는다.
        // 알갱이는 **흩어짐**을 말한다 — 부서진 것이 사방으로 날아가고 연기가 밀려 나간다.
        // 둘은 다른 일이라 하나로 대신할 수 없다. 그래서 갈아 끼우지 않고 **겹친다.**
        //
        // ⚠ 자주 나는 것(`muzzle`·`hit`)은 넣지 않는다. 총을 쏠 때마다 알갱이가 튀면
        //   화면이 쉴 새 없이 번쩍여 **정작 큰 일이 안 보인다.**

        private static readonly Color FireColor  = new(1f, 0.62f, 0.25f);
        private static readonly Color IceColor   = new(0.62f, 0.9f, 1f);
        private static readonly Color DustColor  = new(0.84f, 0.8f, 0.72f);
        private static readonly Color VenomColor = new(0.6f, 1f, 0.45f);
        // ⚠ 이름이 겹치면 안 된다 — `BoltColor`(카드)·`HealColor`(회복 숫자)가 이미 있다.
        //   알갱이 색은 `Pfx` 를 붙여 구분한다.
        private static readonly Color PfxBoltColor  = new(0.55f, 0.85f, 1f);
        private static readonly Color GoldColor     = new(1f, 0.85f, 0.35f);
        private static readonly Color BloodColor    = new(1f, 0.35f, 0.35f);
        private static readonly Color PfxHealColor  = new(0.5f, 1f, 0.6f);
        private static readonly Color CurseColor = new(0.75f, 0.5f, 1f);

        /// <summary>
        /// 한 번 보여 주고 끝나는 표시에 알갱이를 얹는다. <see cref="PlayFx"/> 한 곳에서만 부른다 —
        /// 부르는 자리가 수십 군데라 일일이 붙이면 반드시 빠뜨린다.
        /// </summary>
        private void EmitFxParticles(string name, Vector2 at, float size)
        {
            if (_pfx == null) return;
            float power = Mathf.Clamp(size / 96f, 0.6f, 3f);
            switch (name)
            {
                case "burst":          Boom(at, ParticleElement.Fire, power); break;
                case "grenade_burst":  Boom(at, ParticleElement.Fire, power * 1.3f); break;
                case "slam":
                    _pfx.Shards(at, ParticleElement.Dust, power);
                    _pfx.Puff(at, ParticleElement.Dust, power);
                    _pfx.Ring(at, ParticleElement.Dust, size * 1.1f);
                    break;
                case "shatter":
                    _pfx.Shards(at, ParticleElement.Ice, power);
                    _pfx.Sparkle(at, ParticleElement.Fire, power * 0.6f);
                    break;
                case "freeze":
                case "iceblock":       _pfx.Shards(at, ParticleElement.Ice, power * 0.7f); break;
                case "burn":           _pfx.Embers(at, ParticleElement.Fire, 3, size * 0.25f); break;
                case "venom":          _pfx.Hit(at, ParticleElement.Venom, power * 0.7f); break;
                case "goo_burst":
                    _pfx.Hit(at, ParticleElement.Venom, power);
                    _pfx.Puff(at, ParticleElement.Venom, power);
                    break;
                case "sludge_drop":    _pfx.Puff(at, ParticleElement.Venom, power * 0.8f); break;
                case "dash":           _pfx.Puff(at, ParticleElement.Dust, power * 0.6f); break;
                case "crit":           _pfx.Sparkle(at, ParticleElement.Fire, power); break;
                case "reflect":        _pfx.Sparkle(at, ParticleElement.Fire, power * 0.8f); break;
                case "bolt":           _pfx.Hit(at, ParticleElement.Ice, power * 0.8f); break;
                case "scythe":         _pfx.Hit(at, ParticleElement.Curse, power); break;
                case "leech":          _pfx.Embers(at, ParticleElement.Fire, 3, size * 0.2f); break;
                case "heal_plus":      _pfx.Sparkle(at, ParticleElement.Fire, power * 0.8f); break;
                case "burrow":
                case "bulge":
                    _pfx.Shards(at, ParticleElement.Dust, power * 0.8f);
                    _pfx.Puff(at, ParticleElement.Dust, power);
                    break;
                case "turret":         _pfx.Puff(at, ParticleElement.Dust, power * 0.8f); break;
            }
        }

        // ── 장판 알갱이 ─────────────────────────────────────────
        //
        // 불 장판은 **계속 타고 있어야** 한다. 낱장 그림은 같은 네 장을 되풀이할 뿐이라
        // 가만히 보면 멈춘 무늬로 읽힌다 — 불씨가 하나씩 떠올라야 «타는 중»이 된다.

        /// <summary>장판에서 불씨를 뿌리는 간격(초). 너무 잦으면 바닥이 하얗게 뜬다.</summary>
        private const float FieldEmberInterval = 0.12f;

        private float _fieldEmberTimer;

        private void TickFieldParticles(float dt)
        {
            if (_pfx == null || _fields.Count == 0) return;
            _fieldEmberTimer -= dt;
            if (_fieldEmberTimer > 0f) return;
            _fieldEmberTimer = FieldEmberInterval;

            for (int i = 0; i < _fields.Count; i++)
            {
                var f = _fields[i];
                if (!f.IsActive) continue;
                // 반지름이 클수록 알갱이도 많아야 «면»으로 읽힌다. 상한을 둬 작은 장판 수십 개가
                // 겹쳐도 화면이 터지지 않게 한다.
                int count = Mathf.Clamp(Mathf.RoundToInt(f.Radius / 70f), 1, 3);
                switch (f.Effect)
                {
                    case FieldEffect.Burn:
                    case FieldEffect.Damage: _pfx.Embers(f.Center, ParticleElement.Fire, count, f.Radius * 0.8f); break;
                    case FieldEffect.Freeze: _pfx.Sparkle(f.Center + Random.insideUnitCircle * f.Radius * 0.8f, ParticleElement.Ice, 0.35f); break;
                    case FieldEffect.Curse:  _pfx.Embers(f.Center, ParticleElement.Curse, count, f.Radius * 0.8f); break;
                }
            }
        }

        /// <summary>폭발 한 벌 — 섬광·불티·연기·밀려 나가는 고리를 한 프레임에 겹친다.</summary>
        private void Boom(Vector2 at, ParticleElement element, float power)
        {
            _pfx.Hit(at, element, power);
            _pfx.Puff(at, element, power);
            _pfx.Ring(at, element, 120f * power);
        }

        private async UniTask LoadParticleArtAsync(IResourceManager res)
        {
            if (_pfx == null) return;
            foreach (var (kind, element, address) in ParticleArt)
            {
                try { _pfx.SetMaterial(kind, element, await res.LoadAsync<Material>(address)); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Battle] 파티클 재질 없음({address}) — 기본 재질로 나온다. {e.Message}");
                }
            }
            // 유령 빛(알파 빛 셰이더) — 없으면 유령 빛이 예전 원뿔 한 장으로 돌아간다(`GlReady`)
            try { _glMaterial = await res.LoadAsync<Material>("ParticleFx/glight"); }
            catch (Exception e) { Debug.LogWarning($"[Battle] 유령 빛 재질 없음 — {e.Message}"); }
        }
    }
}
