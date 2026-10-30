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
        /// <summary>
        /// 따라 하기 첫 판(평생 한 번) — 무리 · 인연을 꼭 겪게 정해 둔 셋(PD 2026-10-30 「인연이랑 시너지 캐릭을 처음에 세팅 · 한 번만」).
        /// 셋 다 코만도 부대(무리 3명 강)이고, 기관총 → 수류탄 → 미사일 → 수류탄 이 인연 짝이라 누구를 메인으로 골라도 남은 둘 중에 짝이 있다.
        /// 고르지 않은 둘은 흩어졌다가 보조 안내 때 하나씩 다시 나온다.
        /// </summary>
        private static readonly string[] TutorialSet = { "commando_mg", "commando_grenade", "commando_missile" };
        private bool _tutorialRun;
        private readonly List<string> _tutorialSubs = new();
        private Unit _tutorialSub;
        private string _tutorialSubKey;
        private float _trioWait;
        private readonly List<Unit> _trio = new();

        /// <summary>유령 출전(또는 따라 하기 첫 판) — 첫 방이 열리면 셋을 세운다.</summary>
        private void QueueStartTrio()
        {
            if (!PartyOn) return;
            _trioPending = true;
            _tutorialRun = !CoachSeen("main");
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
            // 셋이 서기 전에 먼저 다른 적에 빙의했다 — 기다림을 푼다(안 풀면 그 판 보조 안내가 다 막힌다, 안정성 점검 10-30 L1)
            if (_trioPending && _host != null) _trioPending = false;
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
            if (_tutorialRun && TutorialEntries(hosts) is List<HostEntry> set) { SpawnTrioOf(set); return; }
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
            SpawnTrioOf(ready);
        }

        private List<HostEntry> TutorialEntries(IReadOnlyList<HostEntry> hosts)
        {
            var set = new List<HostEntry>();
            foreach (var key in TutorialSet)
                for (int i = 0; i < hosts.Count; i++)
                    if (hosts[i].HostKey == key) { set.Add(hosts[i]); break; }
            if (set.Count < TutorialSet.Length) return null;
            Shuffle(set);
            return set;
        }

        private void SpawnTrioOf(List<HostEntry> ready)
        {
            int n = Mathf.Min(TrioCount, ready.Count);
            if (n == 0) return;

            var at = _ghost.Position;
            for (int i = 0; i < n; i++)
            {
                var def = ready[i];
                var u = SpawnCalmHost(def, at + new Vector2((i - (n - 1) * 0.5f) * 190f, i == 1 ? 300f : 250f), "StartTrio");
                _trio.Add(u);
            }
            // 첫 방에는 고를 셋만 호스트로 둔다 — 빙의하자마자 옆 호스트를 또 보조로 데려가는 건 어색하다(PD 2026-10-30).
            //   방에 원래 선 호스트 몸은 빼고 잡몹은 남긴다. 그래야 따라 하기의 보조(남은 둘 · 인연 짝)도 자리를 뺏기지 않는다
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || _trio.Contains(e) || !e.IsAlive || !e.IsHostBody) continue;
                if (TakeFromEnemies(e)) Destroy(e.gameObject);
            }
            // 방에 원래 선 적과 같은 자리에 서면 겹쳐 보인다(PD 2026-10-30 빌드 캡처) — 둘레 적을 밖으로 민다
            for (int i = 0; i < _trio.Count; i++) ClearAround(_trio[i], TrioClearGap);
            // 처음 한 번은 손가락까지, 그다음부터는 안내 글만(고를 때까지 판은 늘 선다)
            BuildCoach(null, Tx("ui.tut.coach.pick_trio"));
            if (CoachSeen("main") && _coachHand != null) _coachHand.gameObject.SetActive(false);
            PlaceCoachOnGroup(_trio);
        }

        /// <summary>덤비지 않고 바로 빙의할 수 있는 호스트 하나(셋 고르기 · 따라 하기 보조).</summary>
        private const float TrioClearGap = 110f;

        /// <summary>이 몸 둘레 gap 안의 다른 적을 gap 밖으로 민다(셋 고르기 몸끼리는 그대로).</summary>
        private void ClearAround(Unit center, float gap)
        {
            if (center == null) return;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || e == center || !e.IsAlive || _trio.Contains(e)) continue;
                var d = e.Position - center.Position;
                if (d.sqrMagnitude >= gap * gap) continue;
                var dir = d.sqrMagnitude > 1f ? d.normalized : Vector2.up;
                e.Position = ClampedInField(e, center.Position + dir * gap);
            }
        }

        private Unit SpawnCalmHost(HostEntry def, Vector2 pos, string tag, bool calm = true)
        {
            var u = NewUnit($"{tag}_{def.HostKey}");
            NoteMetHost(def);
            u.Setup(UnitSide.Enemy, def.HostKey, def.DisplayName, UnitGet(def.SpriteKey),
                    EnemyHpOf(def), EnemyAtkOf(def), EnemySpeedOf(def), EnemyRangeOf(def), EnemyIntervalOf(def),
                    UnitBox(84f, 78f), isBoss: false, profile: def);
            u.Position = pos;
            ClampToField(u);
            u.MarkAsHostBody();
            u.MarkAsNextBody();
            u.PossessPriority = def.PossessPriority;
            u.PossessRange = 0f;
            if (calm)
            {
                u.IsAggro = false;   // 먼저 덤비지 않는다
                u.SetHpPercent(Mathf.RoundToInt(TrioHpRatio * 100f));
            }
            u.SetState(EnemyState.Idle);
            ApplyFacingSprites(u, def.SpriteKey);
            _enemies.Add(u);
            if (UnitGet(def.SpriteKey) == null) LoadEventHostAsync(u, def.SpriteKey).Forget();   // fire-and-forget: 그림이 오면 그때 보인다
            return u;
        }

        /// <summary>
        /// 따라 하기 첫 판 — 보조 차례에 정해 둔 호스트를 메인 앞에 세운다(무리 · 인연을 꼭 겪게).
        /// 체력이 가득한 채 싸우러 나온다 — 때려서 60% 아래로 떨어져 빙의할 수 있게 된 뒤에만 그 몸을 돌려준다
        /// (PD 2026-10-30 「보조는 공격하다가 HP 가 깎였을 때 뜨는 거 맞지?」). 아직이면 null.
        /// </summary>
        private Unit SpawnTutorialSub()
        {
            // 세워 둔 몸이 아직 있으면 그 몸(화면 밖이라 짚기를 미룬 틱에 또 세우지 않게)
            if (_tutorialSub != null && _tutorialSub.IsAlive && _enemies.Contains(_tutorialSub))
                return _tutorialSub.IsPossessable ? _tutorialSub : null;
            // 때리다 쓰러뜨렸다 — 같은 호스트를 다시 세운다(데려와야 무리 · 인연이 켜진다)
            if (_tutorialSubKey != null && (_tutorialSub == null || !_tutorialSub.IsAlive)) _tutorialSubs.Insert(0, _tutorialSubKey);
            _tutorialSubKey = null;
            if (!_tutorialRun || _tutorialSubs.Count == 0 || _host == null || _player == null) return null;
            var def = _player.GetHost(_tutorialSubs[0]);
            _tutorialSubs.RemoveAt(0);
            _tutorialSubKey = def != null ? def.HostKey : null;
            _tutorialSub = def != null ? SpawnCalmHost(def, _host.Position + new Vector2(0f, 320f), "TutorialSub", false) : null;
            return null;   // 막 나왔다 — 깎인 뒤에 짚는다
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
            _tutorialSubs.Clear();
            for (int i = 0; i < _trio.Count; i++)
            {
                var o = _trio[i];
                if (o == null || o == picked) continue;
                if (_tutorialRun) _tutorialSubs.Add(o.Key);   // 보조 안내 때 다시 나온다
                if (TakeFromEnemies(o)) BeginRelease(o);
            }
            // 고른 메인의 인연 짝이 먼저 나온다 — 보조 자리가 하나만 남아도 인연은 꼭 켜지게(PD 2026-10-30 「인연 캐릭이 안 나온다」)
            if (picked != null && s_inyeon.TryGetValue(picked.Key, out var iy) && iy.Partners != null)
                for (int i = 1; i < _tutorialSubs.Count; i++)
                    if (System.Array.IndexOf(iy.Partners, _tutorialSubs[i]) >= 0)
                    {
                        (_tutorialSubs[0], _tutorialSubs[i]) = (_tutorialSubs[i], _tutorialSubs[0]);
                        break;
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
