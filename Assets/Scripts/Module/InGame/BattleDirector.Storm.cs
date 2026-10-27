using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 전체 기믹 — 챕터 표 `hazard` 칸(PD 2026-10-27 난이도 기획안 3절 · 「궁수의 전설은 번개도 떨어지고 다양한데 우리는 너무 쉽다」).
    ///
    ///   lightning_puddle (4챕터 옥상)  바닥에 작은 물웅덩이 3~4개 · 하늘에서 **작은 번개**가 군데군데 떨어진다.
    ///                                  빨간 예고 원(1초) → 낙뢰. 원 안은 나도 적도 맞는다. 웅덩이에 떨어지면 **그 웅덩이만** 감전.
    ///
    /// 박자 · 크기 · 색은 PD 가 고른 시안(`_exchange/in/mock_lightning_puddle_guide_peak.png` · 가이드 `guide_lightning_puddle.md`) —
    /// 1차의 방 절반을 덮는 번개는 「스킬도 아니고 너무 과하다」로 반려됐다. 번개 하나 = 캐릭터 한 명 크기의 원.
    /// 챕터 앞 1/3 방은 없고, 가운데 1/3 은 12초 · 뒤 1/3 은 8초마다. 보스 · 중간보스 방 · 모드 판에는 없다.
    /// 유령(몸 없음)일 때는 떨어지지 않는다 — 다른 장애물과 같은 규칙(「유령은 겨누지 않는다」).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string StormLightningPuddle = "lightning_puddle";
        private const float StormWarnSeconds = 1.0f;
        private const float StormRadiusMeters = 0.75f;      // 예고 원 — 캐릭터 한 명 크기
        private const float StormMidEvery = 12f, StormLateEvery = 8f, StormFirstDelay = 3f;
        private const int StormStrikes = 3;                 // 한 번에 떨어지는 수 — 내 근처 1 · 아무 데나 2
        private const float StormNearJitterMeters = 0.8f;
        private const int StormDamage = 10;                 // 벽 포탑과 같은 자(× 챕터 · 방 성장 × 일반 몬스터 배율)
        private const float StormEnemyHpRatio = 0.3f;
        private const float StormPuddleW = 2.7f, StormPuddleH = 1.6f;   // 웅덩이 크기(m) — 고른 시안을 재서 맞춤(캐릭터 2명쯤)
        private const float StormBoltW = 1.2f, StormBoltH = 2.6f;      // 번개 줄기(m) — 시안: 캐릭터 키 2배쯤 · 하늘 끝까지 긋지 않는다
        private const float StormBoltSeconds = 0.24f, StormBoltHold = 0.1f, StormHitFrame = 0.06f, StormZapSeconds = 0.6f, StormZapFrame = 0.1f;
        private const float StormRippleEvery = 0.9f;

        private sealed class Puddle
        {
            public Vector2 Center, Half;   // 납작한 타원(가로 · 세로 반지름, px)
            public RectTransform Zap;
            public Image ZapImg;
            public float ZapLeft, Ripple;
        }

        private sealed class StormBolt
        {
            public Vector2 At;
            public float Left;
            public bool Fired;
            public RectTransform Beam, Hit;
            public Image BeamImg, HitImg;
            public float After;            // 꽂힌 뒤 흐른 시간
        }

        private readonly List<Puddle> _puddles = new();
        private readonly List<StormBolt> _stormBolts = new();
        private readonly List<GameObject> _stormViews = new();
        private bool _stormOn;
        private float _stormTimer, _stormEvery;

        // 그림 — 아틀라스 `GetSprite` 는 부를 때마다 새로 만든다. 판마다 한 번 받아 둔다
        private Sprite[] _boltFrames, _boltHitFrames, _puddleSprites, _zapFrames;

        private Sprite[] StormFrames(string prefix, int count)
        {
            var list = new List<Sprite>(count);
            for (int i = 0; i < count; i++) { var s = GetSprite($"{prefix}_{i}"); if (s != null) list.Add(s); }
            return list.ToArray();
        }

        private void CacheStormSprites()
        {
            if (_boltFrames != null) return;
            _boltFrames = StormFrames("fx_storm_bolt", 4);
            _boltHitFrames = StormFrames("fx_storm_hit", 6);
            _puddleSprites = StormFrames("obj_storm_puddle", 4);
            _zapFrames = StormFrames("fx_storm_zap", 6);
        }

        /// <summary>방을 세운 뒤 — 그 챕터 기믹이 이 방에서 켜지는지 보고 웅덩이를 깐다.</summary>
        private void SetupStorm(RoomEntry room)
        {
            ClearStorm();
            if (_config == null || IsModeRun || _fieldLayer == null) return;
            if (room != null && (room.IsMidBoss || room.IsBoss)) return;
            if (_roomKind != RoomKind.Normal && _roomKind != RoomKind.Elite) return;
            int ch = _canonRoom != null ? _canonRoom.Chapter : _runChapter;
            if (ChapterRow(ch).Hazard != StormLightningPuddle) return;
            float at = RoomProgress(ch);
            if (at < 1f / 3f) return;

            CacheStormSprites();
            _stormOn = true;
            _stormEvery = at < 2f / 3f ? StormMidEvery : StormLateEvery;
            _stormTimer = StormFirstDelay;
            int want = 3 + (_rng.Next(2));
            for (int tries = 0; tries < 40 && _puddles.Count < want; tries++) TryPlacePuddle();
        }

        private void TryPlacePuddle()
        {
            var half = new Vector2(Meters(StormPuddleW) * 0.5f, Meters(StormPuddleH) * 0.5f);
            float edge = RoomEdgeMeters * _pxPerMeter + half.x;
            float x = Mathf.Lerp(edge, _roomSize.x - edge, (float)_rng.NextDouble());
            float y = -Mathf.Lerp(edge + Meters(2f), _roomSize.y - edge - Meters(2f), (float)_rng.NextDouble());   // 위 · 아래 문 앞은 비운다
            var c = new Vector2(x, y);
            if (BlockedAt(c, half * 0.8f, 0f)) return;
            for (int i = 0; i < _puddles.Count; i++)
                if (Mathf.Abs(_puddles[i].Center.x - c.x) < half.x * 2.4f && Mathf.Abs(_puddles[i].Center.y - c.y) < half.y * 2.6f) return;

            var p = new Puddle { Center = c, Half = half, Ripple = (float)_rng.NextDouble() * StormRippleEvery };
            var sprite = _puddleSprites != null && _puddleSprites.Length > 0 ? _puddleSprites[_rng.Next(_puddleSprites.Length)] : null;
            if (sprite != null)
            {
                var img = LooseImage(_fieldLayer, "StormPuddle", sprite, half * 2f, c);
                img.SetAsFirstSibling();   // 바닥 바로 위 — 예고 원 · 장판 · 몸보다 아래
                _stormViews.Add(img.gameObject);
            }
            if (_zapFrames != null && _zapFrames.Length > 0)
            {
                p.Zap = LooseImage(_fieldLayer, "StormZap", _zapFrames[0], half * 2f, c);
                p.ZapImg = p.Zap.GetComponent<Image>();
                p.ZapImg.material = _propFxMaterial;   // 검은 바탕 그림 — 가산
                p.Zap.gameObject.SetActive(false);
                _stormViews.Add(p.Zap.gameObject);
            }
            _puddles.Add(p);
        }

        private void TickStorm(float dt)
        {
            if (!_stormOn) return;
            TickPuddles(dt);
            TickStormBolts(dt);
            // 방을 깨면 그친다 · 유령은 겨누지 않는다
            if (_exitOpen || _host == null || Avatar == null) return;
            _stormTimer -= dt;
            if (_stormTimer > 0f) return;
            _stormTimer = _stormEvery;
            var me = Avatar;
            for (int i = 0; i < StormStrikes; i++)
            {
                Vector2 at;
                if (i == 0)
                {
                    // 내 근처 — 발밑에서 조금 비켜(서 있기만 하면 맞는 게 아니라 한 걸음이면 피한다)
                    float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                    at = me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(StormNearJitterMeters) * (float)_rng.NextDouble();
                }
                else if (_puddles.Count > 0 && _rng.Next(2) == 0) at = _puddles[_rng.Next(_puddles.Count)].Center;   // 웅덩이를 노린다
                else at = new Vector2((float)_rng.NextDouble() * _roomSize.x, -(float)_rng.NextDouble() * _roomSize.y);
                StartBolt(at);
            }
        }

        private void StartBolt(Vector2 at)
        {
            float r = Meters(StormRadiusMeters);
            at.x = Mathf.Clamp(at.x, r, _roomSize.x - r);
            at.y = Mathf.Clamp(at.y, -_roomSize.y + r, -r);
            if (!IsOnScreenAt(at)) return;   // 화면 밖은 안 떨어진다(다른 예고와 같은 규칙)
            StartWarn(DiscShape(at, r), StormWarnSeconds, 0).NoFlash = true;   // 그림만 — 피해는 꽂힐 때 이쪽이 한 번에(원 · 웅덩이 겹쳐도 한 번)
            _stormBolts.Add(new StormBolt { At = at, Left = StormWarnSeconds });
        }

        private void TickStormBolts(float dt)
        {
            for (int i = _stormBolts.Count - 1; i >= 0; i--)
            {
                var b = _stormBolts[i];
                if (!b.Fired)
                {
                    b.Left -= dt;
                    if (b.Left > 0f) continue;
                    b.Fired = true;
                    FireBolt(b);
                }
                b.After += dt;
                // 줄기 — 짧게 번쩍(0.16초) · 꽂힌 자리 섬광 6장(0.06초씩)
                if (b.BeamImg != null)
                {
                    float k = 1f - Mathf.Clamp01((b.After - StormBoltHold) / (StormBoltSeconds - StormBoltHold));   // 꽉 찬 채 잠깐 머문 뒤 꺼진다
                    b.BeamImg.color = new Color(1f, 0.92f, 0.6f, k);   // 가산이라 심이 하얗게 타 버린다 — 노란 테를 살린다(시안)
                    if (k <= 0f) { Destroy(b.Beam.gameObject); b.Beam = null; b.BeamImg = null; }
                }
                if (b.HitImg != null)
                {
                    int f = Mathf.FloorToInt(b.After / StormHitFrame);
                    if (f >= _boltHitFrames.Length) { Destroy(b.Hit.gameObject); b.Hit = null; b.HitImg = null; }
                    else b.HitImg.sprite = _boltHitFrames[f];
                }
                if (b.Beam == null && b.Hit == null) _stormBolts.RemoveAt(i);
            }
        }

        private void FireBolt(StormBolt b)
        {
            var at = b.At;
            if (_boltFrames != null && _boltFrames.Length > 0 && _shotLayer != null)
            {
                var size = new Vector2(Meters(StormBoltW), Meters(StormBoltH));
                b.Beam = LooseImage(_shotLayer, "StormBolt", _boltFrames[_rng.Next(_boltFrames.Length)], size, at + Vector2.up * size.y * 0.5f);
                b.BeamImg = b.Beam.GetComponent<Image>();
                b.BeamImg.material = _propFxMaterial;
            }
            if (_boltHitFrames != null && _boltHitFrames.Length > 0 && _shotLayer != null)
            {
                float r = Meters(StormRadiusMeters);
                b.Hit = LooseImage(_shotLayer, "StormHit", _boltHitFrames[0], new Vector2(r * 2.6f, r * 2.6f), at);
                b.HitImg = b.Hit.GetComponent<Image>();
                b.HitImg.material = _propFxMaterial;
            }
            _pfx?.Sparkle(at, ParticleElement.Fire, 0.8f);
            Shake(ShakeOnKill * 0.5f);

            // 맞는 자리 = 예고 원 + (웅덩이에 떨어졌으면) 그 웅덩이 전체. 둘 다 걸쳐도 한 번만 맞는다
            int zapped = -1;
            for (int i = 0; i < _puddles.Count; i++)
                if (InPuddle(_puddles[i], at)) { zapped = i; break; }
            if (zapped >= 0)
            {
                var p = _puddles[zapped];
                p.ZapLeft = StormZapSeconds;
                if (p.Zap != null) p.Zap.gameObject.SetActive(true);
                _pfx?.Embers(p.Center, ParticleElement.Fire, 6, p.Half.x);
            }
            float rr = Meters(StormRadiusMeters);
            var me = Avatar;
            if (me != null && _host != null)
            {
                var foot = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
                bool hit = Vector2.Distance(foot, at) <= rr || Vector2.Distance(me.Position, at) <= rr
                        || (zapped >= 0 && InPuddle(_puddles[zapped], foot));
                if (hit) DamagePlayer(StormPlayerDamage);
            }
            // ⚠ 뒤에서부터 돈다 — `KillEnemy` 가 목록에서 뺀다
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                bool hit = Vector2.Distance(e.Position, at) <= rr + e.BodyRadius * 0.5f
                        || (zapped >= 0 && InPuddle(_puddles[zapped], e.Position));
                if (!hit) continue;
                int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * StormEnemyHpRatio));
                e.IsAggro = true;
                ShowDamage(e.Position, dmg, toEnemy: true);
                if (e.TakeDamage(dmg)) KillEnemy(e);
            }
        }

        private int StormPlayerDamage => Mathf.Max(1, Mathf.RoundToInt(StormDamage * EnemyGrowth() * NormalEnemyAtkMul));

        private static bool InPuddle(Puddle p, Vector2 at)
        {
            var d = at - p.Center;
            float x = d.x / Mathf.Max(1f, p.Half.x), y = d.y / Mathf.Max(1f, p.Half.y);
            return x * x + y * y <= 1f;
        }

        private void TickPuddles(float dt)
        {
            for (int i = 0; i < _puddles.Count; i++)
            {
                var p = _puddles[i];
                // 빗방울 파문 — 웅덩이 안 아무 데나 작은 고리(머묾 · 약)
                p.Ripple -= dt;
                if (p.Ripple <= 0f)
                {
                    p.Ripple = StormRippleEvery * (0.6f + (float)_rng.NextDouble() * 0.8f);
                    var at = p.Center + new Vector2(((float)_rng.NextDouble() - 0.5f) * p.Half.x, ((float)_rng.NextDouble() - 0.5f) * p.Half.y);
                    if (IsOnScreenAt(at)) _pfx?.Ring(at, ParticleElement.Dust, Meters(0.35f));
                }
                if (p.ZapLeft <= 0f || p.ZapImg == null) continue;
                p.ZapLeft -= dt;
                int f = Mathf.FloorToInt((StormZapSeconds - p.ZapLeft) / StormZapFrame) % _zapFrames.Length;
                p.ZapImg.sprite = _zapFrames[f];
                // 납품 그림이 흰 줄만이라 옅은 노랑을 곱한다(흰 그림이라 색이 바래지 않는다 — 시안의 노란 · 하얀 감전)
                p.ZapImg.color = new Color(1f, 0.93f, 0.58f, p.ZapLeft < 0.15f ? p.ZapLeft / 0.15f : 1f);
                if (p.ZapLeft <= 0f) p.Zap.gameObject.SetActive(false);
            }
        }

        private void ClearStorm()
        {
            _stormOn = false;
            for (int i = 0; i < _stormViews.Count; i++) if (_stormViews[i] != null) Destroy(_stormViews[i]);
            _stormViews.Clear();
            for (int i = 0; i < _stormBolts.Count; i++)
            {
                if (_stormBolts[i].Beam != null) Destroy(_stormBolts[i].Beam.gameObject);
                if (_stormBolts[i].Hit != null) Destroy(_stormBolts[i].Hit.gameObject);
            }
            _stormBolts.Clear();
            _puddles.Clear();
        }
    }
}
