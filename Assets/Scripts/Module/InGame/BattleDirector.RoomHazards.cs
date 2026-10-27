using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 전체 기믹 둘째 묶음 — 챕터 표 `hazard` 칸(PD 2026-10-27 「추천대로 가자」 · 4챕터 번개는 `BattleDirector.Storm`).
    ///
    ///   bomb_target  (2챕터 미사일 저장기지)  빨간 조준 표적 3곳(내 근처 1 · 무작위 2) → 1.2초 뒤 작은 미사일이 꽂혀 도트 폭발.
    ///   neon_tracker (3챕터 밤의 도시 거리)   내 발밑을 **따라오는** 원 1 + 제자리 원 2 · 원 위에 늘어진 네온 전깃줄 → 1.0초 뒤 줄 끝이 때려 분홍 방전.
    ///   gas_band     (5챕터 유령 연구소)      방의 가로 한 줄에 빗금 예고 띠(1.2초) → 연두 독가스가 그 줄에 2초 머문다(0.5초마다 독).
    ///
    /// 크기 · 색 · 박자는 PD 가 고른 시안 — `_exchange/guide_{bomb_target|neon_tracker|gas_band}.md` 의 「고른 시안」 줄.
    /// 4챕터 번개와 같은 규칙: 위험 하나 = 캐릭터 한두 명 크기, 챕터 앞 1/3 없음 · 가운데 12초 · 뒤 8초,
    /// 보스 · 중간보스 · 모드 판 · 유령(몸 없음)일 때 없음, **적도 맞는다.**
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string HazardBomb = "bomb_target", HazardNeon = "neon_tracker", HazardGas = "gas_band";

        private const float BombWarnSeconds = 1.2f, BombStrikeRadiusMeters = 0.8f, BombShellSeconds = 0.4f, BombBlastFrame = 0.06f;
        private const float NeonWarnSeconds = 1.0f, NeonRadiusMeters = 0.7f, NeonFollowSpeed = 2.4f, NeonArcFrame = 0.06f;
        private const float NeonCableW = 1.3f, NeonCableH = 4f,   // 시안: 화면 위에서 길게 늘어진 줄
                             NeonCableFrame = 0.12f, NeonTrailEvery = 0.12f;
        private const float GasWarnSeconds = 1.2f, GasStaySeconds = 2f, GasTickSeconds = 0.5f, GasFrame = 0.1f, GasVentFrame = 0.08f;
        private const float GasBandMeters = 1.6f;            // 띠 높이 — 캐릭터 1.5명(방 높이의 15% 안)
        private const float HazardEnemyHpRatio = 0.3f;        // 한 번 맞을 때 적이 잃는 몫(번개와 같다)
        private const float GasEnemyHpRatio = 0.08f;          // 가스는 0.5초마다 — 2초 다 맞으면 약 0.3

        private sealed class RoomStrike
        {
            public string Kind;
            public Vector2 At;
            public float Left, Total, After;
            public bool Fired, Follow;
            public Warn Warn;
            public RectTransform Reticle, Shell, Cable, Burst;
            public Image ReticleImg, ShellImg, CableImg, BurstImg;
            public float TrailLeft;
        }

        private sealed class GasRow
        {
            public float Y;                      // 띠 가운데(px, 음수 = 아래)
            public float Left, StayLeft, TickLeft, Age;
            public bool Fired;
            public Warn Warn;
            public readonly List<Image> Clouds = new();
            public readonly List<Image> Vents = new();
            public readonly List<Image> Hatch = new();   // 예고 빗금 띠 토막 — 왼쪽부터 차오른다(시안 guide_idle)
        }

        private readonly List<RoomStrike> _strikes = new();
        private readonly List<GasRow> _gasRows = new();
        private string _roomHazard;
        private float _roomHazardTimer, _roomHazardEvery;
        private int _lastGasRow = -1;

        private Sprite _bombReticle, _gasWarn;
        private Sprite[] _bombBlast, _bombShell, _neonCable, _neonArc, _gasCloud, _gasVent;

        private void CacheHazardSprites()
        {
            if (_bombBlast != null) return;
            _bombReticle = GetSprite("fx_bomb_reticle");
            _bombBlast = StormFrames("fx_bomb_blast", 8);
            _bombShell = StormFrames("fx_bomb_shell", 2);
            _neonCable = StormFrames("obj_neon_cable", 4);
            _neonArc = StormFrames("fx_neon_arc", 6);
            _gasCloud = StormFrames("fx_gas_cloud", 8);
            _gasVent = StormFrames("fx_gas_vent", 6);
            _gasWarn = GetSprite("fx_gas_warn");
        }

        /// <summary>방을 세운 뒤 — 이 챕터 기믹이 폭격 · 네온 · 가스 중 하나이고 이 방에서 켜지는지 본다.</summary>
        private void SetupRoomHazards(RoomEntry room)
        {
            ClearRoomHazards();
            if (_config == null || IsModeRun || _fieldLayer == null) return;
            if (room != null && (room.IsMidBoss || room.IsBoss)) return;
            if (_roomKind != RoomKind.Normal && _roomKind != RoomKind.Elite) return;
            int ch = _canonRoom != null ? _canonRoom.Chapter : _runChapter;
            string key = ChapterRow(ch).Hazard;
            if (key != HazardBomb && key != HazardNeon && key != HazardGas) return;
            float at = RoomProgress(ch);
            if (at < 1f / 3f) return;
            StartRoomHazard(key, at < 2f / 3f ? StormMidEvery : StormLateEvery, StormFirstDelay);
        }

        /// <summary>기믹을 켠다. 녹화 도구가 1번 방에서 바로 켤 때도 이 길로 온다.</summary>
        private void StartRoomHazard(string key, float every, float firstDelay)
        {
            CacheHazardSprites();
            _roomHazard = key;
            _roomHazardEvery = every;
            _roomHazardTimer = firstDelay;
        }

        private void TickRoomHazards(float dt)
        {
            if (_roomHazard == null) return;
            TickStrikes(dt);
            TickTrailRings(dt);
            TickGasRows(dt);
            if (_exitOpen || _host == null || Avatar == null) return;
            _roomHazardTimer -= dt;
            if (_roomHazardTimer > 0f) return;
            _roomHazardTimer = _roomHazardEvery;
            if (_roomHazard == HazardGas) { StartGasRow(); return; }
            var me = Avatar;
            for (int i = 0; i < StormStrikes; i++)
            {
                Vector2 at;
                if (i == 0)
                {
                    float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                    at = me.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Meters(StormNearJitterMeters) * (float)_rng.NextDouble();
                }
                else at = new Vector2((float)_rng.NextDouble() * _roomSize.x, -(float)_rng.NextDouble() * _roomSize.y);
                // 네온은 첫 원이 내 발밑을 따라온다(멈춰 쏘면 금방 따라잡힌다 — 쏘는 틈을 고르게)
                StartStrike(_roomHazard, at, follow: _roomHazard == HazardNeon && i == 0);
            }
        }

        // ─────────────────────────────────────────
        // 폭격 · 네온 — 원 하나짜리

        private void StartStrike(string kind, Vector2 at, bool follow)
        {
            bool bomb = kind == HazardBomb;
            float r = Meters(bomb ? BombStrikeRadiusMeters : NeonRadiusMeters);
            at.x = Mathf.Clamp(at.x, r, _roomSize.x - r);
            at.y = Mathf.Clamp(at.y, -_roomSize.y + r, -r);
            if (!IsOnScreenAt(at)) return;
            float secs = bomb ? BombWarnSeconds : NeonWarnSeconds;
            var s = new RoomStrike { Kind = kind, At = at, Left = secs, Total = secs, Follow = follow };
            s.Warn = StartWarn(DiscShape(at, r), secs, 0);   // 그림만 — 피해는 터질 때 이쪽이 잰다
            s.Warn.NoFlash = true;                             // 터지는 자리는 폭발 · 방전 그림이 맡는다(시안)
            if (bomb && _bombReticle != null)
            {
                s.Reticle = LooseImage(_fieldLayer, "BombReticle", _bombReticle, new Vector2(r * 2.3f, r * 2.3f * 0.75f), at);
                s.ReticleImg = s.Reticle.GetComponent<Image>();
                s.ReticleImg.material = _propFxMaterial;
            }
            if (!bomb && _neonCable != null && _neonCable.Length > 0 && _shotLayer != null)
            {
                var size = new Vector2(Meters(NeonCableW), Meters(NeonCableH));
                s.Cable = LooseImage(_shotLayer, "NeonCable", _neonCable[0], size, at + Vector2.up * size.y * 0.5f);
                s.CableImg = s.Cable.GetComponent<Image>();
            }
            _strikes.Add(s);
        }

        private void TickStrikes(float dt)
        {
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                var s = _strikes[i];
                if (!s.Fired)
                {
                    s.Left -= dt;
                    if (s.Follow) FollowStrike(s, dt);
                    TickStrikeWarnArt(s);
                    if (s.Left > 0f) continue;
                    s.Fired = true;
                    FireStrike(s);
                }
                s.After += dt;
                if (s.BurstImg != null)
                {
                    var frames = s.Kind == HazardBomb ? _bombBlast : _neonArc;
                    int f = Mathf.FloorToInt(s.After / (s.Kind == HazardBomb ? BombBlastFrame : NeonArcFrame));
                    if (f >= frames.Length) { Destroy(s.Burst.gameObject); s.Burst = null; s.BurstImg = null; }
                    else s.BurstImg.sprite = frames[f];
                }
                // 조준 고리는 폭발 밑에 잠깐 남아 옅어진다(시안 — 터진 자리에 빨간 고리)
                if (s.Reticle != null)
                {
                    float k = 1f - Mathf.Clamp01(s.After / 0.35f);
                    s.ReticleImg.color = new Color(1f, 1f, 1f, k);
                    if (k <= 0f) { Destroy(s.Reticle.gameObject); s.Reticle = null; }
                }
                // 줄은 때린 뒤 잠깐 남았다가 빠르게 옅어진다
                if (s.CableImg != null)
                {
                    float k = 1f - Mathf.Clamp01((s.After - 0.15f) / 0.2f);
                    s.CableImg.color = new Color(1f, 1f, 1f, k);
                    if (k <= 0f) { Destroy(s.Cable.gameObject); s.Cable = null; s.CableImg = null; }
                }
                if (s.Burst == null && s.Cable == null && s.Reticle == null) _strikes.RemoveAt(i);
            }
        }

        /// <summary>따라오는 원 — 내 발밑 쪽으로 일정한 속도로 미끄러진다. 지난 자리에 옅은 고리(끌린 자국, 시안).</summary>
        private void FollowStrike(RoomStrike s, float dt)
        {
            var me = Avatar;
            if (me == null) return;
            var foot = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
            s.At = Vector2.MoveTowards(s.At, foot, Meters(NeonFollowSpeed) * dt);
            float r = Meters(NeonRadiusMeters);
            s.Warn.Shape = DiscShape(s.At, r);
            if (s.Warn.View != null) s.Warn.View.Reaim(s.Warn.Shape);
            s.TrailLeft -= dt;
            if (s.TrailLeft <= 0f)
            {
                s.TrailLeft = NeonTrailEvery;
                SpawnTrailRing(s.At, r);
            }
        }

        private readonly List<Image> _trailRings = new();

        /// <summary>끌린 자국 — 지난 자리에 옅은 빨간 고리(시안 crop_follow). 조준 고리 그림을 옅게 다시 쓴다.</summary>
        private void SpawnTrailRing(Vector2 at, float r)
        {
            if (_bombReticle == null || _fieldLayer == null) return;
            var rt = LooseImage(_fieldLayer, "NeonTrail", _bombReticle, new Vector2(r * 2.2f, r * 2.2f * 0.75f), at);
            var img = rt.GetComponent<Image>();
            img.material = _propFxMaterial;
            img.color = new Color(1f, 1f, 1f, 0.55f);
            _trailRings.Add(img);
        }

        private void TickTrailRings(float dt)
        {
            for (int i = _trailRings.Count - 1; i >= 0; i--)
            {
                var img = _trailRings[i];
                if (img == null) { _trailRings.RemoveAt(i); continue; }
                var c = img.color;
                c.a -= dt * 1.4f;
                if (c.a <= 0f) { Destroy(img.gameObject); _trailRings.RemoveAt(i); continue; }
                img.color = c;
            }
        }

        private void TickStrikeWarnArt(RoomStrike s)
        {
            float p = 1f - s.Left / s.Total;
            if (s.Reticle != null) s.Reticle.anchoredPosition = s.At;
            if (s.Cable != null)
            {
                s.Cable.anchoredPosition = s.At + Vector2.up * s.Cable.sizeDelta.y * 0.5f;
                int f = Mathf.FloorToInt((s.Total - s.Left) / NeonCableFrame) % _neonCable.Length;
                s.CableImg.sprite = _neonCable[f];
                // 끝에서 분홍 불티가 조금씩 떨어진다 — 차오를수록 잦게
                if (_rng.NextDouble() < 0.05 + p * 0.15) _pfx?.Embers(s.At + Vector2.up * Meters(0.3f), ParticleElement.Dust, 1, Meters(0.15f));
            }
            // 폭격 — 끝 0.4초에 작은 미사일이 위에서 비스듬히 내려온다
            if (s.Kind == HazardBomb && s.Left <= BombShellSeconds && _bombShell != null && _bombShell.Length > 0 && _shotLayer != null)
            {
                float t = 1f - s.Left / BombShellSeconds;
                var from = s.At + new Vector2(Meters(1.2f), Meters(4f));
                if (s.Shell == null)
                {
                    s.Shell = LooseImage(_shotLayer, "BombShell", _bombShell[0], new Vector2(Meters(0.35f), Meters(0.7f)), from);
                    s.ShellImg = s.Shell.GetComponent<Image>();
                    var d = s.At - from;
                    s.Shell.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f);
                }
                s.Shell.anchoredPosition = Vector2.Lerp(from, s.At, t * t);
                s.ShellImg.sprite = _bombShell[Mathf.FloorToInt(t * 8f) % _bombShell.Length];
            }
        }

        private void FireStrike(RoomStrike s)
        {
            bool bomb = s.Kind == HazardBomb;
            var at = s.At;
            if (s.Shell != null) { Destroy(s.Shell.gameObject); s.Shell = null; s.ShellImg = null; }
            var frames = bomb ? _bombBlast : _neonArc;
            float r = Meters(bomb ? BombStrikeRadiusMeters : NeonRadiusMeters);
            if (frames != null && frames.Length > 0 && _shotLayer != null)
            {
                // 폭발은 칸 아래 가운데가 바닥(납품 그림 규격) — 위로 반 칸 올린다. 방전은 바닥에 납작하게 가운데
                var size = bomb ? new Vector2(r * 2.4f, r * 2.4f) : new Vector2(r * 3.4f, r * 3.4f * 160f / 192f);   // 시안 방전은 원보다 넓게 퍼진다
                var pos = bomb ? at + Vector2.up * size.y * 0.3f : at;
                s.Burst = LooseImage(_shotLayer, bomb ? "BombBlast" : "NeonArc", frames[0], size, pos);
                s.BurstImg = s.Burst.GetComponent<Image>();
                s.BurstImg.material = _propFxMaterial;
            }
            if (bomb) { _pfx?.Sparkle(at, ParticleElement.Fire, 0.9f); _pfx?.Puff(at, ParticleElement.Dust, 0.6f); }
            else _pfx?.Shards(at, ParticleElement.Dust, 0.6f);
            Shake(ShakeOnKill * (bomb ? 0.6f : 0.45f));
            HitCircle(at, r);
        }

        /// <summary>원 안의 나와 적을 친다 — 번개와 같은 자(발밑 · 몸 가운데 둘 중 하나라도 안이면 맞음).</summary>
        private void HitCircle(Vector2 at, float r)
        {
            var me = Avatar;
            if (me != null && _host != null)
            {
                var foot = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
                if (Vector2.Distance(foot, at) <= r || Vector2.Distance(me.Position, at) <= r) DamagePlayer(StormPlayerDamage);
            }
            // ⚠ 뒤에서부터 돈다 — `KillEnemy` 가 목록에서 뺀다
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (Vector2.Distance(e.Position, at) > r + e.BodyRadius * 0.5f) continue;
                HurtEnemyByHazard(e, HazardEnemyHpRatio);
            }
        }

        private void HurtEnemyByHazard(Unit e, float ratio)
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(e.HpMax * ratio));
            e.IsAggro = true;
            ShowDamage(e.Position, dmg, toEnemy: true);
            if (e.TakeDamage(dmg)) KillEnemy(e);
        }

        // ─────────────────────────────────────────
        // 가스 — 가로 한 줄

        private void StartGasRow()
        {
            float h = Meters(GasBandMeters);
            float edge = RoomEdgeMeters * _pxPerMeter;
            // 위 · 아래 문 앞은 비우고, 방을 줄로 나눠 지난번과 다른 줄을 고른다(빈 줄로 옮겨 다니게)
            int rows = Mathf.Max(2, Mathf.FloorToInt((_roomSize.y - edge * 2f - Meters(4f)) / h));
            // 방이 화면보다 길다 — 띠 전체가 화면 안에 보이고 위 HUD(Lv 줄) 밑이 아닌 줄만 고른다(게임 정보를 가리지 않게)
            int pick = -1;
            float y = 0f;
            for (int tries = 0; tries < 12; tries++)
            {
                int cand = _rng.Next(rows);
                if (cand == _lastGasRow && rows > 1) continue;
                float cy = -(edge + Meters(2f) + h * (cand + 0.5f));
                var v = RoomToView(new Vector2(_roomSize.x * 0.5f, cy));
                if (v.y + h * 0.5f > -Meters(1.2f) || v.y - h * 0.5f < -_field.rect.height + Meters(0.5f)) continue;
                pick = cand;
                y = cy;
                break;
            }
            if (pick < 0) return;   // 이번엔 쉰다 — 다음 박자에 다시 고른다
            _lastGasRow = pick;
            var g = new GasRow { Y = y, Left = GasWarnSeconds };
            // 시안 예고는 빗금 띠만 있다 — 빗금 그림이 없을 때만 공용 빨간 판으로 대신한다
            if (_gasWarn == null)
            {
                g.Warn = StartWarn(BandShape(new Vector2(0f, y), Vector2.right, h, _roomSize.x), GasWarnSeconds, 0);
                g.Warn.NoFlash = true;
            }
            else if (_fieldLayer != null)
            {
                var size = new Vector2(h * 2f, h);
                int n = Mathf.CeilToInt(_roomSize.x / size.x);
                for (int c = 0; c < n; c++)
                {
                    var rt = LooseImage(_fieldLayer, "GasWarn", _gasWarn, size, new Vector2(size.x * (c + 0.5f), y));
                    var img = rt.GetComponent<Image>();
                    img.material = _propFxMaterial;
                    img.color = new Color(1f, 1f, 1f, 0f);
                    g.Hatch.Add(img);
                }
            }
            _gasRows.Add(g);
        }

        private void TickGasRows(float dt)
        {
            for (int i = _gasRows.Count - 1; i >= 0; i--)
            {
                var g = _gasRows[i];
                g.Age += dt;
                if (!g.Fired)
                {
                    g.Left -= dt;
                    // 예고 — 그 줄 배관 틈에서 작은 김(시안 guide_idle)
                    if (_rng.NextDouble() < 0.12)
                        _pfx?.Puff(new Vector2((float)_rng.NextDouble() * _roomSize.x, g.Y), ParticleElement.Dust, 0.25f);
                    // 빗금이 왼쪽부터 토막씩 켜진다 — 다 차면 샌다
                    float fill = (1f - g.Left / GasWarnSeconds) * g.Hatch.Count;
                    for (int c = 0; c < g.Hatch.Count; c++)
                        g.Hatch[c].color = new Color(1f, 1f, 1f, Mathf.Clamp01(fill - c) * 0.7f);   // 시안 빗금은 바닥이 비칠 만큼 옅다
                    if (g.Left > 0f) continue;
                    for (int c = 0; c < g.Hatch.Count; c++) if (g.Hatch[c] != null) Destroy(g.Hatch[c].gameObject);
                    g.Hatch.Clear();
                    g.Fired = true;
                    g.StayLeft = GasStaySeconds;
                    g.TickLeft = 0f;
                    g.Age = 0f;
                    FireGasRow(g);
                }
                g.StayLeft -= dt;
                float fade = g.StayLeft < 0.4f ? Mathf.Clamp01(g.StayLeft / 0.4f) : Mathf.Clamp01(g.Age / 0.15f);
                int cf = _gasCloud.Length > 0 ? Mathf.FloorToInt(g.Age / GasFrame) % _gasCloud.Length : 0;
                for (int c = 0; c < g.Clouds.Count; c++)
                {
                    // 토막마다 박자를 어긋내 같은 구름이 줄줄이 반복돼 보이지 않게
                    g.Clouds[c].sprite = _gasCloud[(cf + c * 3) % _gasCloud.Length];
                    g.Clouds[c].color = new Color(1f, 1f, 1f, fade);
                }
                // 분출 기둥은 머무는 동안 계속 솟는다 — 기둥마다 박자를 어긋낸다(시안 free_peak)
                for (int v = 0; v < g.Vents.Count; v++)
                {
                    int vf = Mathf.FloorToInt(g.Age / GasVentFrame + v * 2) % _gasVent.Length;
                    g.Vents[v].sprite = _gasVent[vf];
                    g.Vents[v].color = new Color(1f, 1f, 1f, fade);
                }
                g.TickLeft -= dt;
                if (g.TickLeft <= 0f && g.StayLeft > 0.2f)
                {
                    g.TickLeft = GasTickSeconds;
                    HitGasRow(g);
                }
                if (g.StayLeft > 0f) continue;
                for (int c = 0; c < g.Clouds.Count; c++) if (g.Clouds[c] != null) Destroy(g.Clouds[c].gameObject);
                for (int v = 0; v < g.Vents.Count; v++) if (g.Vents[v] != null) Destroy(g.Vents[v].gameObject);
                _gasRows.RemoveAt(i);
            }
        }

        private void FireGasRow(GasRow g)
        {
            float h = Meters(GasBandMeters);
            if (_gasCloud != null && _gasCloud.Length > 0 && _shotLayer != null)
            {
                // 토막(256 × 128)을 방 폭만큼 이어 깐다 — 구름은 토막 아래 2/3 에 있어 띠 바닥에 맞춘다
                var size = new Vector2(h * 2.4f, h * 1.2f);   // 시안 구름은 띠보다 높이 솟는다
                int n = Mathf.CeilToInt(_roomSize.x / size.x);
                float start = (_roomSize.x - n * size.x) * 0.5f + size.x * 0.5f;
                for (int c = 0; c < n; c++)
                {
                    var rt = LooseImage(_shotLayer, "GasCloud", _gasCloud[0], size, new Vector2(start + c * size.x, g.Y + h * 0.1f));
                    var img = rt.GetComponent<Image>();
                    img.material = _propFxMaterial;
                    img.color = new Color(1f, 1f, 1f, 0f);
                    g.Clouds.Add(img);
                }
            }
            if (_gasVent != null && _gasVent.Length > 0 && _shotLayer != null)
            {
                for (int v = 0; v < 4; v++)
                {
                    float x = _roomSize.x * (0.14f + 0.24f * v) + ((float)_rng.NextDouble() - 0.5f) * Meters(0.6f);
                    var rt = LooseImage(_shotLayer, "GasVent", _gasVent[0], new Vector2(h * 0.5f, h), new Vector2(x, g.Y + h * 0.3f));
                    var img = rt.GetComponent<Image>();
                    img.material = _propFxMaterial;
                    g.Vents.Add(img);
                }
            }
        }

        private bool InGasRow(GasRow g, Vector2 p) => Mathf.Abs(p.y - g.Y) <= Meters(GasBandMeters) * 0.5f;

        private void HitGasRow(GasRow g)
        {
            var me = Avatar;
            if (me != null && _host != null)
            {
                var foot = new Vector2(me.Position.x, me.Position.y - FootDrop(me));
                if (InGasRow(g, foot) || InGasRow(g, me.Position)) DamagePlayer(GasPlayerDamage);
            }
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || e.IsDying || e.IsBoss) continue;
                if (!InGasRow(g, e.Position)) continue;
                HurtEnemyByHazard(e, GasEnemyHpRatio);
            }
        }

        // 가스는 0.5초마다 — 2초를 다 맞으면 번개 한 대쯤(가운데서 빠져나오면 한두 번)
        private int GasPlayerDamage => Mathf.Max(1, Mathf.RoundToInt(StormPlayerDamage * 0.3f));

        private void ClearRoomHazards()
        {
            _roomHazard = null;
            for (int i = 0; i < _strikes.Count; i++)
            {
                var s = _strikes[i];
                if (s.Reticle != null) Destroy(s.Reticle.gameObject);
                if (s.Shell != null) Destroy(s.Shell.gameObject);
                if (s.Cable != null) Destroy(s.Cable.gameObject);
                if (s.Burst != null) Destroy(s.Burst.gameObject);
            }
            _strikes.Clear();
            for (int i = 0; i < _trailRings.Count; i++) if (_trailRings[i] != null) Destroy(_trailRings[i].gameObject);
            _trailRings.Clear();
            for (int i = 0; i < _gasRows.Count; i++)
            {
                var g = _gasRows[i];
                for (int c = 0; c < g.Clouds.Count; c++) if (g.Clouds[c] != null) Destroy(g.Clouds[c].gameObject);
                for (int v = 0; v < g.Vents.Count; v++) if (g.Vents[v] != null) Destroy(g.Vents[v].gameObject);
                for (int c = 0; c < g.Hatch.Count; c++) if (g.Hatch[c] != null) Destroy(g.Hatch[c].gameObject);
            }
            _gasRows.Clear();
            _lastGasRow = -1;
        }
    }
}
