using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common.Modes;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 추가 모드(망령의 탑 · 망령의 밤)가 본편 판에 끼어드는 공통 자리.
    /// 모드가 아니면 여기 있는 값은 전부 본편 그대로(배율 1 · 끼어들지 않음)다.
    /// 기획서 AVSR_Content_Tower.md 6-2 · AVSR_Content_NightMode.md 6-2.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string ModeTableAddress = "TableData/ModeTable";

        private ModeTable _modeTable;
        private IModeProgress _modes;

        private static bool IsTowerRun => RunModeContext.IsTower;
        private static bool IsNightRun => RunModeContext.IsNight;
        private static bool IsModeRun => RunModeContext.IsMode;

        /// <summary>모드 판이면 표를 읽는다. 못 읽으면 본편 판으로 돌린다 — 표 없이 모드를 돌리면 빈 방이 된다.</summary>
        private async UniTask LoadModeTableAsync(IResourceManager res)
        {
            if (!IsModeRun) return;
            CoreModule.TryGet(out _modes);
            _modeTable = _modes != null ? _modes.Table : null;
            if (_modeTable == null)
            {
                try { _modeTable = await res.LoadAsync<ModeTable>(ModeTableAddress); }
                catch (Exception e) { Debug.LogError($"[모드] 표 로드 실패 — {e.Message}"); }
            }
            if (_modeTable != null) return;
            Debug.LogError("[모드] 표가 없어 본편 판으로 시작한다");
            RunModeContext.Clear();
        }

        /// <summary>모드 판이 시작하는 방.</summary>
        private string ModeStartRoom()
        {
            if (IsTowerRun) return TowerRoomId();
            if (IsNightRun) return NightArenaRoomId();
            return FirstCanonRoom;
        }

        /// <summary>잡몹 · 적 몸 체력 배율 — 탑은 층 세기, 밤은 지난 시간.</summary>
        private float ModeHpMul => (IsTowerRun && _towerDef != null ? _modeTable.TowerHpMul(_towerFloorNo)
                                  : IsNightRun ? NightTrashHpMul : 1f) * _modeHpExtra;

        /// <summary>한 기만 더 세게 세울 때(탑 정예 층) 잠깐 거는 배율. 세운 뒤 1 로 되돌린다.</summary>
        private float _modeHpExtra = 1f;

        private float ModeAtkMul => IsTowerRun && _towerDef != null ? _modeTable.TowerAtkMul(_towerFloorNo) : 1f;

        private float ModeBossHpMul => IsTowerRun && _towerDef != null ? _modeTable.TowerBossHpMul(_towerFloorNo)
                                     : IsNightRun && _modeTable != null ? _modeTable.BossHpMul : 1f;

        /// <summary>모드 판은 몸으로 나올 호스트가 정본 경로가 아니라 조건으로 정해진다 — 전부 미리 올린다.</summary>
        private void AddModeUnitKeys(List<string> keys)
        {
            if (!IsModeRun || _player == null || !_player.IsReady) return;
            var hosts = _player.AllHosts;
            if (hosts == null) return;
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[i];
                if (h == null || h.IsGhost || PlayerDataService.IsHiddenHost(h.HostKey)) continue;
                if (!keys.Contains(h.SpriteKey)) keys.Add(h.SpriteKey);
            }
            if (_modeTable == null) return;
            AddTrashKeys(keys, _modeTable.TrashOf(Affinity.Blade));
            AddTrashKeys(keys, _modeTable.TrashOf(Affinity.Force));
            AddTrashKeys(keys, _modeTable.TrashOf(Affinity.Magic));
        }

        private void AddTrashKeys(List<string> keys, string[] trash)
        {
            if (trash == null) return;
            for (int i = 0; i < trash.Length; i++)
            {
                var k = TrashKeyAlias(trash[i]);
                var e = k != TrashCrossKey ? TrashEntryOf(k) : null;
                if (e != null && !keys.Contains(e.SpriteKey)) keys.Add(e.SpriteKey);
            }
        }

        /// <summary>호스트 키 목록 → 표 항목. 없는 키는 빠진다.</summary>
        private List<HostEntry> HostEntriesOf(IReadOnlyList<string> keys)
        {
            var list = new List<HostEntry>();
            if (keys == null || _player == null) return list;
            for (int i = 0; i < keys.Count; i++)
            {
                var h = _player.GetHost(keys[i]);
                if (h != null) list.Add(h);
            }
            return list;
        }

        /// <summary>
        /// 몸으로 세울 셋 — 서로 다른 호스트(모자라면 겹침), **가진 몸이 있으면 적어도 하나는 가진 몸**(기획서 3-4).
        /// </summary>
        private List<HostEntry> PickModeBodies(List<HostEntry> pool, int count)
        {
            var picked = new List<HostEntry>();
            if (pool == null || pool.Count == 0 || count <= 0) return picked;
            var bag = new List<HostEntry>(pool);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
            // 가진 몸 하나를 맨 앞으로
            for (int i = 0; i < bag.Count; i++)
            {
                if (!_player.IsHostOwned(bag[i])) continue;
                (bag[0], bag[i]) = (bag[i], bag[0]);
                break;
            }
            for (int i = 0; i < count; i++) picked.Add(bag[i % bag.Count]);
            return picked;
        }

        /// <summary>판 안 시간(초) — 모드 결과 창 · 밤 시계가 같이 쓴다. 카드 창이 떠 있는 동안은 안 흐른다.</summary>
        private float _modeElapsed;
        private int _modePossessCount;
        private int _modeLostCount;

        /// <summary>몸에 들었다 — 탑 「빙의는 한 번」 · 밤 시작 몸 정리.</summary>
        private void NoteModeEnterHost(HostEntry entry)
        {
            if (!IsModeRun) return;
            _modePossessCount++;
            if (IsTowerRun) TowerOnPossessed();
            if (IsNightRun) NightOnPossessed();
        }

        private void NoteModeLostHost()
        {
            if (IsModeRun) _modeLostCount++;
        }

        /// <summary>판 시작 — 표를 읽은 뒤, 캐릭터 그림을 올리기 전에 층 · 요일을 정한다.</summary>
        private void PrepareModeRun()
        {
            _modeElapsed = 0f;
            _modePossessCount = 0;
            _modeLostCount = 0;
            if (IsTowerRun) PrepareTowerFloor();
            if (IsNightRun) PrepareNight();
        }

        private void TickModeClock(float dt)
        {
            if (!IsModeRun) return;
            _modeElapsed += dt;
            if (IsNightRun) TickNight(dt);
        }

        /// <summary>구조 몸 — 모드는 조건에 맞는 호스트(탑 층 조건 · 밤 오늘 목록)에서. 본편이면 null.</summary>
        private HostEntry ModeRescueEntry()
        {
            var pool = IsTowerRun ? _towerPool : IsNightRun ? _nightPool : null;
            return pool != null && pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
        }

        private void NoteModeKill(Unit u)
        {
            if (IsTowerRun) PublishTowerEnemiesLeft();
            else if (IsNightRun) NightOnKill(u);
        }

        /// <summary>일시정지 → 포기. 탑은 입장을 이미 썼으니 할 일이 없다. 밤은 그때까지의 처치를 기록 · 지급한다(실패).</summary>
        private void GiveUpMode()
        {
            if (IsNightRun && _modes != null)
                _modes.RecordNight(_nightDayIndex, _nightKills, _modeElapsed, false, _nightDayKeys, null, null);
        }
    }
}
