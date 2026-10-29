using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 유령으로 시작하면 셋 중 하나(PD 2026-10-30 「어벤징 원작처럼 유령으로 시작하면 랜덤 3마리 중에서 한 마리를 선택하고
    /// 나머지 두 마리는 사라지면서 시작 · 랜덤이나 호스트 골라서 갈 때는 빼고」).
    /// 첫 방이 열리면 유령 앞에 랜덤 호스트 셋을 세우고 판을 세운다 → 하나를 누르면 그 몸에 빙의 · 나머지 둘은 옅어지며 흩어진다.
    /// 셋은 덤비지 않고 체력이 깎여 있어 바로 빙의할 수 있다. 빙의 비용은 여느 첫 몸과 같다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const int TrioCount = 3;
        private const float TrioHpRatio = 0.4f;

        private bool _trioPending;
        private float _trioWait;
        private readonly List<Unit> _trio = new();

        /// <summary>유령 출전(또는 따라 하기 첫 판) — 첫 방이 열리면 셋을 세운다.</summary>
        private void QueueStartTrio()
        {
            if (!PartyOn) return;
            _trioPending = true;
        }

        /// <summary>따라 하기 틱에서 — 셋을 세우거나, 세워 둔 셋을 고를 때까지 판을 세운다. 세웠으면 true.</summary>
        private bool TickTrio()
        {
            // 첫 판은 그림 설명(빙의 · 두 버튼 · 유령 체력 …)을 먼저 보고 닫은 뒤에 셋을 세운다 — 설명은 방에 들어서고 1.6초 뒤에 뜬다
            //   설명이 끝내 안 뜨면(옛 화면 등) 판이 도는 시간으로 4초 뒤엔 그냥 세운다 — 설명이 떠 있는 동안은 이 틱이 안 돈다
            if (_trioPending) _trioWait += Time.unscaledDeltaTime;
            bool guideFirst = !IsModeRun && !Game.Module.Common.UI.RbGuide.HasSeen("ingame") && _trioWait < 4f;
            // 방이 열리고 잠깐 뒤 — 그림 설명을 여기서 직접 띄운다(설명이 판을 세우고, 닫히면 다음 틱에 셋이 선다)
            if (_trioPending && guideFirst && _trioWait > 0.8f && Ui != null) Ui.ShowFirstRunGuideNow();
            if (_trioPending && !guideFirst && _running && !_awaitingBuff && _ghost != null && _host == null)
            {
                _trioPending = false;
                SpawnStartTrio();
            }
            if (_trio.Count == 0) return false;
            for (int i = _trio.Count - 1; i >= 0; i--)
                if (_trio[i] == null || !_trio[i].IsAlive || !_enemies.Contains(_trio[i])) _trio.RemoveAt(i);
            if (_trio.Count == 0 || _host != null) { EndTrio(); return false; }
            PlaceCoachOnGroup(_trio);
            return true;
        }

        private void SpawnStartTrio()
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null) return;
            // 그림이 이미 올라온 몸을 먼저 — 없으면 아무 몸이나 세우고 그 자리에서 올린다
            var ready = new List<HostEntry>();
            var rest = new List<HostEntry>();
            for (int i = 0; i < hosts.Count; i++)
            {
                if (!PartyPickable(hosts[i])) continue;
                (UnitGet(hosts[i].SpriteKey) != null ? ready : rest).Add(hosts[i]);
            }
            Shuffle(ready);
            Shuffle(rest);
            ready.AddRange(rest);
            int n = Mathf.Min(TrioCount, ready.Count);
            if (n == 0) return;

            var at = _ghost.Position;
            for (int i = 0; i < n; i++)
            {
                var def = ready[i];
                var u = NewUnit($"StartTrio_{def.HostKey}");
                NoteMetHost(def);
                u.Setup(UnitSide.Enemy, def.HostKey, def.DisplayName, UnitGet(def.SpriteKey),
                        EnemyHpOf(def), EnemyAtkOf(def), EnemySpeedOf(def), EnemyRangeOf(def), EnemyIntervalOf(def),
                        UnitBox(84f, 78f), isBoss: false, profile: def);
                // 유령 앞 부채꼴 — 가운데가 조금 더 멀다
                float x = (i - (n - 1) * 0.5f) * 190f;
                u.Position = at + new Vector2(x, i == 1 ? 300f : 250f);
                ClampToField(u);
                u.MarkAsHostBody();
                u.MarkAsNextBody();
                u.PossessPriority = def.PossessPriority;
                u.PossessRange = 0f;
                u.IsAggro = false;   // 먼저 덤비지 않는다
                u.SetHpPercent(Mathf.RoundToInt(TrioHpRatio * 100f));
                u.SetState(EnemyState.Idle);
                ApplyFacingSprites(u, def.SpriteKey);
                _enemies.Add(u);
                if (UnitGet(def.SpriteKey) == null) LoadEventHostAsync(u, def.SpriteKey).Forget();   // fire-and-forget: 그림이 오면 그때 보인다
                _trio.Add(u);
            }
            // 처음 한 번은 손가락까지, 그다음부터는 안내 글만(고를 때까지 판은 늘 선다)
            BuildCoach(null, Tx("ui.tut.coach.pick_trio"));
            if (CoachSeen("main") && _coachHand != null) _coachHand.gameObject.SetActive(false);
            PlaceCoachOnGroup(_trio);
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>셋 중 하나를 눌렀다 — 그 몸에 빙의, 나머지는 옅어지며 흩어진다.</summary>
        private void PickTrio(Unit picked)
        {
            CoachMark("main");
            for (int i = 0; i < _trio.Count; i++)
            {
                var o = _trio[i];
                if (o == null || o == picked) continue;
                if (TakeFromEnemies(o)) BeginRelease(o);
            }
            _trio.Clear();
            EndCoach(false);
            _coachQuietUntil = Time.unscaledTime + 2f;
            _possessTarget = picked;
            _repossessLock = 0f;
            TryPossess();
        }

        private void EndTrio()
        {
            _trio.Clear();
            EndCoach(false);
        }
    }
}
