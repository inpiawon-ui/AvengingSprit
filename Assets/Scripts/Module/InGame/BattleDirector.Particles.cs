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

        /// <summary>종류 ↔ 주소. `CreateParticleFx` 에디터 툴이 같은 이름으로 만든다.</summary>
        private static readonly (ParticleFxKind Kind, string Address)[] ParticleArt =
        {
            (ParticleFxKind.Spark,  "ParticleFx/spark"),
            (ParticleFxKind.Smoke,  "ParticleFx/smoke"),
            (ParticleFxKind.Glow,   "ParticleFx/glow"),
            (ParticleFxKind.Shard,  "ParticleFx/shard"),
            (ParticleFxKind.Ember,  "ParticleFx/ember"),
            (ParticleFxKind.Streak, "ParticleFx/streak"),
            (ParticleFxKind.Ring,   "ParticleFx/ring"),
            (ParticleFxKind.Star4,  "ParticleFx/star4"),
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
                case "burst":          Boom(at, FireColor, power); break;
                case "grenade_burst":  Boom(at, FireColor, power * 1.3f); break;
                case "slam":
                    _pfx.Shards(at, DustColor, power);
                    _pfx.Puff(at, DustColor, power);
                    _pfx.Ring(at, DustColor, size * 1.1f);
                    break;
                case "shatter":
                    _pfx.Shards(at, IceColor, power);
                    _pfx.Sparkle(at, IceColor, power * 0.6f);
                    break;
                case "freeze":
                case "iceblock":       _pfx.Shards(at, IceColor, power * 0.7f); break;
                case "burn":           _pfx.Embers(at, FireColor, 3, size * 0.25f); break;
                case "venom":          _pfx.Embers(at, VenomColor, 3, size * 0.25f); break;
                case "goo_burst":
                    _pfx.Hit(at, VenomColor, power);
                    _pfx.Puff(at, VenomColor, power);
                    break;
                case "sludge_drop":    _pfx.Puff(at, VenomColor, power * 0.8f); break;
                case "dash":           _pfx.Puff(at, DustColor, power * 0.6f); break;
                case "crit":           _pfx.Sparkle(at, GoldColor, power); break;
                case "reflect":        _pfx.Sparkle(at, IceColor, power * 0.8f); break;
                case "bolt":           _pfx.Hit(at, PfxBoltColor, power * 0.8f); break;
                case "scythe":         _pfx.Hit(at, CurseColor, power); break;
                case "leech":          _pfx.Embers(at, BloodColor, 3, size * 0.2f); break;
                case "heal_plus":      _pfx.Sparkle(at, PfxHealColor, power * 0.8f); break;
                case "burrow":
                case "bulge":
                    _pfx.Shards(at, DustColor, power * 0.8f);
                    _pfx.Puff(at, DustColor, power);
                    break;
                case "turret":         _pfx.Puff(at, DustColor, power * 0.8f); break;
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
                    case FieldEffect.Damage: _pfx.Embers(f.Center, FireColor, count, f.Radius * 0.8f); break;
                    case FieldEffect.Freeze: _pfx.Sparkle(f.Center + Random.insideUnitCircle * f.Radius * 0.8f, IceColor, 0.35f); break;
                    case FieldEffect.Curse:  _pfx.Embers(f.Center, CurseColor, count, f.Radius * 0.8f); break;
                }
            }
        }

        /// <summary>폭발 한 벌 — 불티·연기·밀려 나가는 고리·파편을 한 프레임에 겹친다.</summary>
        private void Boom(Vector2 at, Color color, float power)
        {
            _pfx.Hit(at, color, power);
            _pfx.Puff(at, DustColor, power);
            _pfx.Ring(at, color, 120f * power);
            _pfx.Shards(at, color, power * 0.7f);
        }

        private async UniTask LoadParticleArtAsync(IResourceManager res)
        {
            if (_pfx == null) return;
            foreach (var (kind, address) in ParticleArt)
            {
                try { _pfx.SetMaterial(kind, await res.LoadAsync<Material>(address)); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Battle] 파티클 재질 없음({address}) — 기본 재질로 나온다. {e.Message}");
                }
            }
        }
    }
}
