using System.Collections.Generic;
using Game.Character;
using Game.Module.Common.Modes;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 망령의 탑 — 입장 1번 = 한 층(기획서 AVSR_Content_Tower.md 3-2 · 3-3).
    ///   유령으로 들어선다 → 층 방: 잔몹 N + 조건에 맞는 몸 3 (보스 층은 보스 + 몸 3)
    ///   적이 모두 죽으면 그 자리에서 저장(이번 주 깬 층 · 첫 클리어 보상) → 결과
    ///   유령 에너지 0 · 포기 → 실패(입장은 이미 썼다)
    /// 레벨업 · 카드 · 골드 · 판 안 조각은 없다(3-8) — 「무엇을 키워 왔는가」를 재는 곳이다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private TowerFloor _towerDef;
        private int _towerFloorNo;
        private readonly List<string> _towerFitKeys = new();
        private List<HostEntry> _towerPool = new();
        private bool _towerCleared;
        private int _towerRewardGold;
        private readonly List<string> _towerRewardKeys = new();
        private readonly List<int> _towerRewardCounts = new();

        private bool TowerLeaveLocked => IsTowerRun && _towerDef != null && _towerDef.Rule == TowerRule.NoLeave;

        /// <summary>정예 층의 잔몹 체력 배율(층 세기 위에 곱한다).</summary>
        private const float TowerEliteHpMul = 2f;

        /// <summary>잔몹이 설 가장 위 줄(방 높이 비율) — 위 HUD 밑.</summary>
        private const float TowerTrashTopY = 0.16f;
        private bool TowerOneShot => IsTowerRun && _towerDef != null && _towerDef.Rule == TowerRule.OneShot;

        /// <summary>판 시작 — 층 정의 · 몸 후보를 정한다(방을 세우기 전).</summary>
        private void PrepareTowerFloor()
        {
            _towerFloorNo = Mathf.Max(1, RunModeContext.TowerFloor);
            _towerDef = _modeTable.FloorAt(_towerFloorNo);
            _towerCleared = false;
            _towerRewardGold = 0;
            _towerRewardKeys.Clear();
            _towerRewardCounts.Clear();
            _towerFitKeys.Clear();
            if (_modes != null) _towerFitKeys.AddRange(_modes.TowerFitHosts(_towerDef));
            _towerPool = HostEntriesOf(_towerFitKeys);
            if (_towerDef != null && _towerDef.Rule == TowerRule.SingleHost && _towerPool.Count > 0)
            {
                // 한 호스트의 층 — 가진 몸에서 먼저 하나를 뽑아 셋 다 그 호스트
                var one = PickModeBodies(_towerPool, 1)[0];
                _towerPool = new List<HostEntry> { one };
            }
        }

        /// <summary>층 방 — 보스 층은 그 보스가 서는 정본 보스방, 아니면 표의 방.</summary>
        private string TowerRoomId()
        {
            if (_towerDef == null) return FirstCanonRoom;
            if (_towerDef.IsBoss && _rooms != null)
                for (int i = 0; i < _rooms.Rooms.Count; i++)
                {
                    var r = _rooms.Rooms[i];
                    if (r != null && r.IsBoss && BossSlug(r.BossId) == _towerDef.BossKey) return r.RoomId;
                }
            return string.IsNullOrEmpty(_towerDef.RoomId) ? FirstCanonRoom : _towerDef.RoomId;
        }

        /// <summary>보통 층 — 표의 잔몹 N 을 방 위쪽에, 몸 3 을 아래쪽에 세운다.</summary>
        private void SpawnTowerFloor()
        {
            var def = _towerDef;
            if (def == null) return;
            int n = Mathf.Max(0, def.TrashCount);

            // 「마법의 무리」 같은 층 — 그 속성 잔몹 + 그 속성 호스트(빼앗기 막음)가 3분의 1
            bool enemyAffinity = def.Rule == TowerRule.EnemyAffinity;
            string[] trash = enemyAffinity ? _modeTable.TrashOf(def.Affinity) : def.TrashKeys;
            var blocked = enemyAffinity && _modes != null
                ? HostEntriesOf(_modes.HostsWhere(h => AffinityRule.KindOf(h.HostKey) == def.Affinity))
                : new List<HostEntry>();
            int nBlocked = blocked.Count > 0 ? n / 3 : 0;

            for (int i = 0; i < n; i++)
            {
                var at = SpawnSlot(i, n);
                // 본편 자리표는 방 맨 위(6%)부터라 위 HUD 에 가린다 — 탑 방은 HUD 아래부터 세운다
                at.y = Mathf.Min(at.y, -_roomSize.y * TowerTrashTopY);
                if (i < nBlocked)
                {
                    // 몸 표식(MarkAsHostBody)을 안 붙인다 → 빼앗을 수 없고 「몸」 표식도 안 뜬다
                    var h = blocked[Random.Range(0, blocked.Count)];
                    SpawnRoomUnit(h, false, false, at, $"TowerBlocked_{h.HostKey}_{i}");
                    continue;
                }
                if (trash == null || trash.Length == 0) continue;
                var e = TrashEntryOf(TrashKeyAlias(trash[i % trash.Length]));
                if (e == null) continue;
                bool elite = def.Rule == TowerRule.Elite;
                _modeHpExtra = elite ? TowerEliteHpMul : 1f;
                var u = SpawnRoomUnit(e, false, false, at, $"Trash_{e.HostKey}_{i}");
                _modeHpExtra = 1f;
                if (elite) u.MarkAsElite();   // 정예 표시 — 잡몹 상자 그대로(128 상자에 넣으면 너무 커진다)
            }
            SpawnTowerBodies();
            PublishTowerEnemiesLeft();
        }

        /// <summary>몸 3 — 좌 · 가운데 · 우. 보스 층은 보스 아래 줄에 선다.</summary>
        private void SpawnTowerBodies()
        {
            int count = _modeTable != null ? Mathf.Max(1, _modeTable.BodyCount) : 3;
            var bodies = PickModeBodies(_towerPool, count);
            for (int i = 0; i < bodies.Count; i++)
            {
                float fx = bodies.Count <= 1 ? 0.5f : Mathf.Lerp(0.2f, 0.8f, i / (float)(bodies.Count - 1));
                var at = new Vector2(_roomSize.x * fx, -_roomSize.y * 0.58f);
                SpawnRoomUnit(bodies[i], true, false, at, $"TowerBody_{bodies[i].HostKey}_{i}");
            }
        }

        /// <summary>층에 들어섰다 — 화면 판 · 조건 칩 · 소개 띠.</summary>
        private void PublishTowerFloorEntered()
        {
            var def = _towerDef;
            if (def == null) return;
            Affinity enemy = def.Rule == TowerRule.EnemyAffinity ? def.Affinity : Affinity.None;
            Affinity adv = enemy != Affinity.None ? CounterOf(enemy)
                         : def.Rule is TowerRule.BodyAffinity or TowerRule.Boss ? def.Affinity : Affinity.None;
            _bus.Publish(new TowerFloorEnteredEvent
            {
                EnteredFloor = _towerFloorNo,
                RuleKind = (int)def.Rule,
                FloorNameKey = def.NameKey,
                RuleDescKey = def.DescKey,
                EnemyAffinityKind = (int)enemy,
                AdvantageKind = (int)adv,
                PossessLimit = def.Rule == TowerRule.OneShot ? 1 : 0,
                IsLeaveLocked = def.Rule == TowerRule.NoLeave,
            });
            PublishTowerEnemiesLeft();
        }

        /// <summary>그 속성을 이기는 속성. 파워 → 무기 → 마법 → 파워.</summary>
        private static Affinity CounterOf(Affinity a) => a switch
        {
            Affinity.Blade => Affinity.Force,
            Affinity.Force => Affinity.Magic,
            Affinity.Magic => Affinity.Blade,
            _ => Affinity.None,
        };

        private void PublishTowerEnemiesLeft()
        {
            if (!IsTowerRun) return;
            int alive = 0;
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && _enemies[i].IsAlive) alive++;
            _bus.Publish(new TowerEnemiesLeftEvent { RemainingEnemies = alive });
        }

        /// <summary>「빙의는 한 번」 — 한 번 탄 뒤로는 방의 어느 몸도 빼앗을 수 없다(표식도 꺼진다).</summary>
        private void TowerOnPossessed()
        {
            if (!TowerOneShot) return;
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null) _enemies[i].BanRepossess();
            _bus.Publish(new TowerPossessLeftEvent { RemainingPossess = 0 });
        }

        /// <summary>
        /// 층의 적이 모두 죽었다 — **그 자리에서** 저장 · 첫 클리어 지급(결과 창 전에 앱이 꺼져도 남게).
        /// 문 · 다음 방은 없다. 보스 층이면 막타 슬로우가 풀린 뒤 결과 창.
        /// </summary>
        private void TowerOnRoomCleared()
        {
            if (_towerCleared) return;
            _towerCleared = true;
            _exitOpen = true;   // 다시 클리어 처리하지 않게 — 탑 방에는 문이 없다
            _modes?.RecordTowerClear(_towerFloorNo, RunModeContext.StartWeekKey, _towerFitKeys,
                                     _towerRewardKeys, _towerRewardCounts, out _towerRewardGold);
            FinishAfterBossDownAsync().Forget();   // fire-and-forget: 보스 층은 슬로우 · 폭발 뒤, 보통 층은 바로 결과
        }

        /// <summary>판 끝 — 챕터 지급 없이 탑 결과.</summary>
        private void FinishTower(bool cleared)
        {
            int weekCleared = _modes != null ? _modes.TowerWeekCleared : (cleared ? _towerFloorNo : _towerFloorNo - 1);
            int next = _modes != null ? _modes.TowerNextFloor : _towerFloorNo + (cleared ? 1 : 0);
            var nextDef = _modeTable.FloorAt(next);

            // 실패 — 이 층에 맞는 키운 몸 수 · 키우면 좋은 몸(키운 몸 먼저)
            int trained = 0;
            var suggest = new List<string>();
            for (int i = 0; i < _towerFitKeys.Count; i++)
            {
                bool t = _modes != null && _modes.IsTrained(_towerFitKeys[i]);
                if (t) { trained++; suggest.Insert(0, _towerFitKeys[i]); }
                else suggest.Add(_towerFitKeys[i]);
            }
            if (suggest.Count > 3) suggest.RemoveRange(3, suggest.Count - 3);

            _bus.Publish(new TowerFinishedEvent
            {
                IsFloorCleared = cleared,
                PlayedFloor = _towerFloorNo,
                WeekClearedAfter = weekCleared,
                PlayedNameKey = _towerDef != null ? _towerDef.NameKey : string.Empty,
                NextFloor = next,
                NextNameKey = nextDef != null ? nextDef.NameKey : string.Empty,
                NextDescKey = nextDef != null ? nextDef.DescKey : string.Empty,
                FailFitTrained = trained,
                FailFitTotal = _towerFitKeys.Count,
                SuggestHostKeys = suggest.ToArray(),
                ShardHostKeys = _towerRewardKeys.ToArray(),
                ShardCounts = _towerRewardCounts.ToArray(),
                RewardGold = _towerRewardGold,
                EntriesLeft = _modes != null ? _modes.TowerEntries : 0,
                PossessCount = _modePossessCount,
                LostCount = _modeLostCount,
                ElapsedSeconds = _modeElapsed,
            });
        }
    }
}
