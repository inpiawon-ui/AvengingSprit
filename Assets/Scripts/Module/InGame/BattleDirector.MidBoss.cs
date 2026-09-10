using System.Collections.Generic;
using Game.Character;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 중간 보스 — 방 005 의 **호스트 대장**.
    ///
    /// ── 왜 이 방이 있나 ──────────────────────────────────────────
    /// 원작 보스 여섯은 전부 빙의 불가다. 그래서 보스전에 들어서는 순간
    /// 이 게임의 코어 루프(몸을 빼앗아 싸운다)가 통째로 끊긴다.
    /// **이 방이 그 반대다** — 대장은 못 뺏고 부하 셋은 뺏을 수 있으니,
    /// 부하를 빼앗아 대장을 치는 전투가 된다. 빙의가 보스전에서 처음 성립하는 자리다.
    ///
    /// ── 그림이 한 장도 안 든다 ──────────────────────────────────
    /// 대장은 새 패턴도 새 그림도 갖지 않는다. **이미 있는 호스트 23명 중 하나를
    /// 키워서 세운다** — 크기 1.8배 · HP 3배 · 공격력 1.4배.
    /// 자기 액티브 스킬을 그대로 쓰므로 행동도 새로 짜지 않는다.
    ///
    /// ── 취약 창 ─────────────────────────────────────────────────
    /// 부하 셋을 전부 잡으면 대장이 3초 경직한다. 이것이 유일한 창이라
    /// "부하부터 정리한다" 가 정답이 된다 — 그런데 그 부하가 곧 내 몸이므로
    /// **마지막 한 기를 언제 죽일지**가 이 방의 선택이 된다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float MidBossScale = 1.8f;
        /// <summary>
        /// 대장 체력 배율. 2026-09-10 에 3 → 1.5 로 내렸다.
        ///
        /// ⚠ 이 값은 `EnemyHpOf` **위에** 곱해진다. 전날 잡몹 체력을 전체 2배로
        ///   올리면서(`GameConfig.EnemyHpMul`) 대장도 같이 두 배가 됐다 —
        ///   3배 × 2배 = 6배가 되어 「중간 보스 피가 너무 많다」가 됐다.
        ///   여기서 절반으로 내려 예전 체감(3배)으로 돌린다.
        /// </summary>
        private const float MidBossHpMul = 1.5f;
        private const float MidBossAtkMul = 1.4f;

        /// <summary>부하를 다 잡았을 때 대장이 굳는 시간. 정본 3초.</summary>
        private const float MidBossStunSeconds = 3f;

        private Unit _midBoss;
        private float _midBossStun;
        private bool _midBossStunUsed;

        /// <summary>지금 방에 대장이 서 있는가.</summary>
        private bool HasMidBoss => _midBoss != null && _midBoss.IsAlive;

        /// <summary>대장이 굳어 있는가. 이 동안에는 움직이지도 때리지도 않는다.</summary>
        private bool IsMidBossStunned => _midBossStun > 0f;

        private void ClearMidBoss()
        {
            _midBoss = null;
            _midBossStun = 0f;
            _midBossStunUsed = false;
        }

        /// <summary>
        /// 대장을 세운다. 부하를 **다 세운 뒤에** 부른다 — 부하 수를 알아야
        /// 취약 창이 언제 열리는지 셀 수 있다.
        /// </summary>
        private void SpawnMidBoss(RoomEntry room, IReadOnlyList<HostEntry> hosts)
        {
            ClearMidBoss();
            if (room == null || !room.IsMidBoss || hosts == null) return;

            // 대장의 몸은 호스트 표에서 찾는다. 없는 키면 이 방은 그냥 일반 방이 된다 —
            // 데이터가 어긋났다고 방을 못 지나가게 만들지는 않는다.
            HostEntry leader = null;
            for (int i = 0; i < hosts.Count; i++)
                if (hosts[i] != null && hosts[i].HostKey == room.MidBossKey) { leader = hosts[i]; break; }
            if (leader == null)
            {
                Debug.LogWarning($"[중간보스] 호스트 표에 없는 키: {room.MidBossKey} — 대장 없이 진행한다");
                return;
            }

            var u = NewUnit($"MidBoss_{leader.HostKey}");
            u.Setup(UnitSide.Enemy, leader.HostKey, leader.NameKr, TrashSprite(leader),
                    Mathf.RoundToInt(EnemyHpOf(leader) * MidBossHpMul),
                    Mathf.RoundToInt(EnemyAtkOf(leader) * MidBossAtkMul),
                    EnemySpeedOf(leader),
                    EnemyRangeOf(leader),
                    EnemyIntervalOf(leader),
                    // 잡몹 상자(84×78)를 1.8배로 키운다. 캔버스가 아니라 **상자**를 키워야
                    // 그림이 같은 비율로 커진다 — 여백까지 함께 늘어난다.
                    UnitBox(84f * MidBossScale, 78f * MidBossScale),
                    isBoss: false, profile: leader);

            // 방 위쪽 가운데. 부하 셋이 그 앞에 서므로 대장이 뒤에 있어야 구도가 읽힌다.
            u.Position = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.22f);
            ClearOfCover(u);

            // ⚠ **못 뺏는다.** `MarkAsHostBody` 를 부르지 않는 것이 곧 그 규칙이다.
            //   여기서 뺏을 수 있게 하면 이 방의 전투가 통째로 사라진다.
            u.MarkAsElite();          // 덩치 값이 아니라 "이건 관문이다" 라는 표시다
            u.PossessPriority = 0;
            u.PossessRange = 0f;
            u.SetState(EnemyState.Idle);
            u.ResetPattern();
            ApplyFacingSprites(u, leader.SpriteKey);
            _enemies.Add(u);
            _midBoss = u;

            // 대장도 체력 막대를 쓴다. 최종 보스와 같은 자리에 뜨는 것이 맞다 —
            // 플레이어에게는 "이 방의 관문" 이라는 점이 같다.
            _bus.Publish(new BossHpChangedEvent { BossHp = u.Hp, BossHpMax = u.HpMax, Phase = 1 });
            Debug.Log($"[중간보스] {leader.NameKr} HP {u.Hp} 공격 {u.Atk} — 부하를 다 잡으면 {MidBossStunSeconds}초 경직");
        }

        /// <summary>
        /// 부하가 다 죽었는지 보고 취약 창을 연다.
        ///
        /// ⚠ **한 번만 연다.** 부하가 다 죽은 상태가 계속 유지되므로,
        ///   매 프레임 다시 걸면 대장이 영영 굳어 있게 된다.
        /// </summary>
        private void TickMidBoss(float dt)
        {
            if (_midBossStun > 0f) _midBossStun -= dt;
            if (!HasMidBoss || _midBossStunUsed) return;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || e == _midBoss) continue;
                if (e.IsAlive && !e.IsDying) return;   // 아직 부하가 남았다
            }

            _midBossStunUsed = true;
            _midBossStun = MidBossStunSeconds;
            _midBoss.SetMoving(false);
            _midBoss.SetState(EnemyState.Idle);
            _midBoss.PlayHit();   // 굳는 순간이 보여야 "지금이다" 가 읽힌다
            Debug.Log($"[중간보스] 부하 전멸 — {MidBossStunSeconds}초 경직");
        }
    }
}
