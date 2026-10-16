using System.Collections.Generic;
using Game.Character;
using Game.Module.Common.Modes;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 망령의 밤 — 한 방(아레나)에서 5분 버티고 보스를 잡는다(기획서 AVSR_Content_NightMode.md 3-1).
    ///   0:00 유령 · 시작 몸 3(하나를 타면 둘은 사라진다) · 무리는 2초 뒤부터 계속
    ///   0:40~ 45초마다 대형 호스트(깎아야 빼앗는다) · 1분마다 무리 속성 전환 + 회복 15%
    ///   2:00 · 3:30 대장 · 5:00 보스(무리 멈춤) — 보스 처치 = 클리어 · 유령 에너지 0 = 실패
    /// 보상은 처치 수(4-4). 골드 · 판 안 조각은 없다. 경험치 · 카드는 본편 그대로.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>밤 보스 후보 — 아레나 한 방에서 싸울 수 있는 보스만(파이썬은 무대 벽이 따로 있어 뺐다).</summary>
        private static readonly string[] NightBossKeys = { "crusher", "kingpin" };

        private const float NightBodyRowY = 0.62f;
        private const float NightTrashTopY = 0.14f;   // 위 HUD 밑으로 숨지 않게
        private const float NightTrashKeepAwayMeters = 2f;
        private const float NightBannerSeconds = 2.2f;

        private NightDay _nightDay;
        private int _nightDayIndex;
        private readonly List<string> _nightDayKeys = new();
        private List<HostEntry> _nightPool = new();
        private readonly List<Unit> _nightStartBodies = new();
        private readonly HashSet<Unit> _nightBigHosts = new();
        private readonly HashSet<Unit> _nightChiefs = new();
        private bool _nightChosen;
        private int _nightKills;
        private float _nightSpawnTimer;
        private Affinity _nightWaveKind;
        private int _nightWaveIndex;
        private float _nightNextBigHost;
        private int _nightChiefIndex;
        private bool _nightBossSpawned;
        private bool _nightCleared;
        private string _nightBossKey;
        private int _nightLastSecond = -1;
        private float _nightHpOverride;

        /// <summary>잔몹 체력 배율 `1 + t/300`. 대형 · 대장은 세울 때 따로 정한다(<see cref="_nightHpOverride"/>).</summary>
        private float NightTrashHpMul
            => _nightHpOverride > 0f ? _nightHpOverride
             : _modeTable != null ? 1f + _modeElapsed / Mathf.Max(1f, _modeTable.TrashHpSlope) : 1f;

        private string NightArenaRoomId()
            => _modeTable != null && !string.IsNullOrEmpty(_modeTable.ArenaRoomId) ? _modeTable.ArenaRoomId : FirstCanonRoom;

        /// <summary>판 시작 — 요일 · 오늘의 호스트 · 첫 무리 속성 · 보스를 정한다.</summary>
        private void PrepareNight()
        {
            var days = _modeTable.Days;
            _nightDayIndex = Mathf.Clamp(RunModeContext.NightDay, 0, Mathf.Max(0, (days?.Length ?? 1) - 1));
            _nightDay = days != null && days.Length > 0 ? days[_nightDayIndex] : null;
            _nightDayKeys.Clear();
            if (_modes != null) _nightDayKeys.AddRange(_modes.NightDayHosts(_nightDay));
            _nightPool = HostEntriesOf(_nightDayKeys);
            if (_nightPool.Count < 3) Debug.LogWarning($"[밤] 오늘 호스트가 {_nightPool.Count}명뿐이다 — 있는 만큼만 세운다");

            _nightStartBodies.Clear();
            _nightBigHosts.Clear();
            _nightChiefs.Clear();
            _nightChosen = false;
            _nightKills = 0;
            _nightSpawnTimer = _modeTable.TrashFirstDelay;
            _nightWaveKind = (Affinity)Random.Range(1, 4);
            _nightWaveIndex = 0;
            _nightNextBigHost = _modeTable.BigHostFirst;
            _nightChiefIndex = 0;
            _nightBossSpawned = false;
            _nightCleared = false;
            _nightLastSecond = -1;
            _nightHpOverride = 0f;
            _nightBossKey = _nightDay != null && !string.IsNullOrEmpty(_nightDay.BossKey)
                ? _nightDay.BossKey : NightBossKeys[Random.Range(0, NightBossKeys.Length)];
        }

        /// <summary>아레나에 들어섰다 — 시작 몸 셋을 아래쪽 좌 · 가운데 · 우에.</summary>
        private void SpawnNightStart()
        {
            var bodies = PickModeBodies(_nightPool, Mathf.Min(3, _nightPool.Count));
            // 서로 다른 셋이어야 고르는 의미가 있다 — 모자라면 있는 만큼만
            var seen = new HashSet<string>();
            int col = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                if (!seen.Add(bodies[i].HostKey)) continue;
                float fx = Mathf.Lerp(0.2f, 0.8f, col++ / 2f);
                var at = new Vector2(_roomSize.x * fx, -_roomSize.y * NightBodyRowY);
                var u = SpawnRoomUnit(bodies[i], true, false, at, $"NightStart_{bodies[i].HostKey}");
                _nightStartBodies.Add(u);
            }
            NightBanner("ui.night.start_hint", null, "ui.night.start_hint_sub", null);
        }

        /// <summary>시작 몸 하나를 탔다 → 나머지 둘은 연기와 함께 사라진다(처치로 안 센다).</summary>
        private void NightOnPossessed()
        {
            if (_nightChosen) return;
            _nightChosen = true;
            for (int i = 0; i < _nightStartBodies.Count; i++)
            {
                var u = _nightStartBodies[i];
                if (u == null || !_enemies.Contains(u)) continue;
                PlayFx("souldeath", u.Position, SoulDeathFxSize, loop: false);
                _enemies.Remove(u);
                if (u == _possessTarget) _possessTarget = null;
                Destroy(u.gameObject);
            }
            _nightStartBodies.Clear();
        }

        // ── 시계 ─────────────────────────────────────────────

        private void TickNight(float dt)
        {
            float t = _modeElapsed;
            var table = _modeTable;

            // 무리 — 보스가 나오면 멈춘다
            if (!_nightBossSpawned && t >= table.TrashFirstDelay)
            {
                _nightSpawnTimer -= dt;
                if (_nightSpawnTimer <= 0f)
                {
                    _nightSpawnTimer = Mathf.Max(table.SpawnIntervalMin,
                                                 table.SpawnIntervalStart - t / Mathf.Max(1f, table.SpawnIntervalSlope));
                    int n = 1 + Mathf.FloorToInt(t / Mathf.Max(1f, table.SpawnCountStep));
                    for (int i = 0; i < n && NightTrashAlive() < table.EnemyCap; i++) SpawnNightTrash();
                }
            }

            // 1분마다 무리 속성 전환 + 회복
            if (t >= (_nightWaveIndex + 1) * table.WaveInterval && t < table.BossTime)
            {
                _nightWaveIndex++;
                var prev = _nightWaveKind;
                do _nightWaveKind = (Affinity)Random.Range(1, 4); while (_nightWaveKind == prev);
                HealOnClear(table.WaveHealPercent);
                NightBanner("ui.night.banner.wave", NightAffinityName(_nightWaveKind),
                            "ui.night.banner.heal", table.WaveHealPercent.ToString());
            }

            // 대형 호스트 — 보스가 나와도 계속
            if (t >= _nightNextBigHost)
            {
                _nightNextBigHost += table.BigHostInterval;
                SpawnBigHost();
            }

            // 대장
            var chiefs = table.ChiefTimes;
            if (chiefs != null && _nightChiefIndex < chiefs.Length && t >= chiefs[_nightChiefIndex])
            {
                _nightChiefIndex++;
                SpawnChief();
            }

            // 보스
            if (!_nightBossSpawned && t >= table.BossTime)
            {
                _nightBossSpawned = true;
                SpawnNightBoss(_nightBossKey);
            }

            int sec = Mathf.FloorToInt(t);
            if (sec != _nightLastSecond) { _nightLastSecond = sec; PublishNightState(); }
        }

        private int NightTrashAlive()
        {
            int n = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var u = _enemies[i];
                if (u != null && u.IsAlive && !u.IsHostBody && !u.IsBoss && !u.IsElite) n++;
            }
            return n;
        }

        /// <summary>무리 한 기 — 60% 위 가장자리 · 20% 왼쪽 · 20% 오른쪽(위쪽 절반). 내 몸 2 m 안에는 안 세운다.</summary>
        private void SpawnNightTrash()
        {
            var list = _modeTable.TrashOf(_nightWaveKind);
            if (list == null || list.Length == 0) return;
            var e = TrashEntryOf(TrashKeyAlias(list[Random.Range(0, list.Length)]));
            if (e == null) return;
            var at = NightEdgeSpot();
            if (at == null) return;
            var u = SpawnRoomUnit(e, false, false, at.Value, $"NightTrash_{e.HostKey}");
            u.IsAggro = true;   // 무리는 기다리지 않는다 — 나오자마자 쫓아온다
        }

        private Vector2? NightEdgeSpot()
        {
            var me = Avatar;
            float keep = Meters(NightTrashKeepAwayMeters);
            for (int tries = 0; tries < 6; tries++)
            {
                float r = Random.value;
                Vector2 at = r < 0.6f
                    ? new Vector2(_roomSize.x * Random.Range(0.08f, 0.92f), -_roomSize.y * NightTrashTopY)
                    : new Vector2(_roomSize.x * (r < 0.8f ? 0.05f : 0.95f), -_roomSize.y * Random.Range(0.05f, 0.5f));
                if (me != null && (me.Position - at).magnitude < keep) continue;
                return at;
            }
            return null;
        }

        /// <summary>대형 호스트 — 오늘 목록에서 하나. ×1.35 · 체력 ×(5 + t/90) · 공격 ×1.3. 유령이면 바로 빼앗는다.</summary>
        private void SpawnBigHost()
        {
            if (_nightPool.Count == 0) return;
            var e = _nightPool[Random.Range(0, _nightPool.Count)];
            var at = new Vector2(_roomSize.x * Random.Range(0.25f, 0.75f), -_roomSize.y * NightTrashTopY);
            float hpMul = _modeTable.BigHostHpBase + _modeElapsed / Mathf.Max(1f, _modeTable.BigHostHpSlope);
            _nightHpOverride = hpMul;
            var u = SpawnRoomUnit(e, true, false, at, $"NightBig_{e.HostKey}");
            _nightHpOverride = 0f;
            u.IsAggro = true;
            u.MarkAsElite();
            // 유령이면 다른 몸처럼 바로 탄다(PD 2026-10-16 — 탈출이 없어져 「몸에 든 채로 깎아 빼앗기」가 불가능해졌다)
            u.transform.localScale = Vector3.one * _modeTable.BigHostScale;
            u.ScaleAttack(_modeTable.BigHostAtkMul);
            _nightBigHosts.Add(u);
            NightBanner("ui.night.banner.big", e.DisplayName, "ui.night.banner.big_sub", null);
        }

        /// <summary>대장 — 지금 무리 속성의 호스트. 빼앗을 수 없다(몸 표식 없음).</summary>
        private void SpawnChief()
        {
            List<string> keys = _modes != null
                ? _modes.HostsWhere(h => AffinityRule.KindOf(h.HostKey) == _nightWaveKind) : null;
            var pool = HostEntriesOf(keys);
            if (pool.Count == 0) pool = _nightPool;
            if (pool.Count == 0) return;
            var e = pool[Random.Range(0, pool.Count)];
            var at = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * NightTrashTopY);
            _nightHpOverride = (1f + _modeElapsed / Mathf.Max(1f, _modeTable.TrashHpSlope)) * _modeTable.ChiefHpMul;
            var u = SpawnRoomUnit(e, false, true, at, $"NightChief_{e.HostKey}");
            _nightHpOverride = 0f;
            u.IsAggro = true;
            _nightChiefs.Add(u);
            NightBanner("ui.night.banner.chief", e.DisplayName, null, null);
        }

        /// <summary>
        /// 5:00 보스 — 그 보스가 서는 정본 보스방의 값(체력 · 공격 · 속도 · 단계 · 패턴)을 그대로 쓴다.
        /// 본편 보스방 세우기(`EnterRoom`)와 같은 순서다. 체력은 ×0.9(표).
        /// </summary>
        private void SpawnNightBoss(string bossKey)
        {
            RoomEntry room = null;
            if (_rooms != null)
                for (int i = 0; i < _rooms.Rooms.Count && room == null; i++)
                {
                    var r = _rooms.Rooms[i];
                    if (r != null && r.IsBoss && BossSlug(r.BossId) == bossKey) room = r;
                }
            var def = _bossTable != null ? _bossTable.ByKey(bossKey) : null;
            if (room == null && def == null)
            {
                Debug.LogError($"[밤] 보스 {bossKey} 를 못 찾았다 — 보스 없이 버틴다");
                return;
            }
            string stand = UnitGet(bossKey) != null ? bossKey : BossStand(bossKey);
            var art = UnitGet(stand) ?? UnitGet("boss");
            int hp = room != null ? room.BossHp
                   : def.HasCanonStats ? def.CanonHp : Mathf.RoundToInt(_config.BossHp(1) * def.HpMul);
            int atk = room != null ? room.BossAtk
                    : def.HasCanonStats ? def.CanonAtk : Mathf.RoundToInt(_config.BossAtk * def.AtkMul);

            var boss = NewUnit("Boss");
            boss.Setup(UnitSide.Enemy, bossKey, def != null ? def.DisplayName : bossKey, art,
                       Mathf.RoundToInt(BossHpMul * ModeBossHpMul * hp),
                       Mathf.RoundToInt(BossAtkMulOf(bossKey) * atk),
                       room != null ? room.BossMoveSpeed * _pxPerMeter : _config.BossMoveSpeed * def.MoveSpeedMul,
                       _config.BossAttackRange, _config.BossAttackInterval,
                       UnitBox(256f * BossScale, 256f * BossScale), isBoss: true);
            ApplyFacingSprites(boss, stand);
            var tells = new Sprite[Unit.FacingSuffix.Length];
            for (int i = 0; i < tells.Length; i++) tells[i] = UnitGet(stand, $"{Unit.FacingSuffix[i]}_tell");
            boss.SetTellSprites(tells);
            boss.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * BossStandY);
            _enemies.Add(boss);
            _boss = boss;
            _roomBoss = def;
            _brain.Setup(def);
            if (room != null)
            {
                var tel = new List<float>();
                for (int i = 0; i < room.BossPhases.Count; i++) tel.Add(room.BossPhases[i].TelegraphSeconds);
                _brain.SetCanonPhases(room.BossPhaseGates, tel);
                _brain.SetCanonMoves(room.BossMoves);
            }
            var named = _bossTable != null ? _bossTable.ByKey(bossKey) : null;
            _bus.Publish(new BossHpChangedEvent
            {
                BossHp = boss.Hp, BossHpMax = boss.HpMax,
                BossName = named != null ? named.DisplayName : bossKey, Phase = 1,
            });
            NightBanner("ui.night.banner.boss", named != null ? named.DisplayName : bossKey, "ui.night.banner.boss_sub", null);
        }

        /// <summary>잡았다 — 처치 수 · 대형 · 대장 덤 · 보스면 클리어.</summary>
        private void NightOnKill(Unit u)
        {
            if (_nightStartBodies.Contains(u)) return;   // 시작 몸은 처치로 안 센다
            _nightKills++;
            if (_nightBigHosts.Remove(u))
                GainExp(_config.ExpPerEnemy * Mathf.Max(0, _modeTable.ExpMulBigHost - 1));
            if (_nightChiefs.Remove(u))
            {
                GainExp(_config.ExpPerEnemy * Mathf.Max(0, _modeTable.ExpMulChief - 1));
                HealOnClear(_modeTable.ChiefHealPercent);
            }
            if (u.IsBoss && !_nightCleared)
            {
                _nightCleared = true;
                FinishAfterBossDownAsync().Forget();   // fire-and-forget: 막타 슬로우 · 폭발 뒤 결과
            }
            PublishNightState();
        }

        private void PublishNightState()
        {
            float t = _modeElapsed;
            int nextKind = 0;
            float nextAt = float.MaxValue;
            if (_nightNextBigHost < nextAt) { nextAt = _nightNextBigHost; nextKind = 1; }
            if (!_nightBossSpawned && _modeTable.BossTime <= nextAt) { nextAt = _modeTable.BossTime; nextKind = 2; }
            _bus.Publish(new NightStateEvent
            {
                ElapsedSeconds = t,
                KillCount = _nightKills,
                WaveAffinityKind = (int)_nightWaveKind,
                NextEventKind = nextKind,
                NextEventSeconds = Mathf.Max(0f, nextAt - t),
                IsCounterHintShown = true,
                CounterAffinityKind = (int)CounterOf(_nightWaveKind),
            });
        }

        private void NightBanner(string key, string arg, string subKey, string subArg)
            => _bus.Publish(new NightBannerEvent
            {
                BannerKey = key, BannerArg = arg, SubKey = subKey, SubArg = subArg, Seconds = NightBannerSeconds,
            });

        private static string NightAffinityName(Affinity a)
            => Game.Module.Common.Localize.Get(a switch
            {
                Affinity.Blade => "ui.affinity.blade",
                Affinity.Force => "ui.affinity.force",
                Affinity.Magic => "ui.affinity.magic",
                _ => "ui.affinity.none",
            });

        /// <summary>판 끝 — 기록 · 처치 보상(저장) → 밤 결과.</summary>
        private void FinishNight(bool cleared)
        {
            var keys = new List<string>();
            var counts = new List<int>();
            int prev = _modes != null
                ? _modes.RecordNight(_nightDayIndex, _nightKills, _modeElapsed, cleared, _nightDayKeys, keys, counts) : 0;
            _bus.Publish(new NightFinishedEvent
            {
                IsCleared = cleared,
                NightDayIndex = _nightDayIndex,
                BossKey = _nightBossKey,
                FinalKills = _nightKills,
                PreviousBest = prev,
                IsNewBest = _nightKills > prev,
                SurvivedSeconds = _modeElapsed,
                ReachedLevel = _level,
                ShardHostKeys = keys.ToArray(),
                ShardCounts = counts.ToArray(),
                EntriesLeft = _modes != null ? _modes.NightEntries : 0,
                PossessCount = _modePossessCount,
                LostCount = _modeLostCount,
            });
        }
    }
}
