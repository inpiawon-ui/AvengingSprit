#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 홍보 영상 녹화용 자동 조종 (에디터 전용, 2026-09-18). 사람이 하는 것처럼 싸운다.
    ///
    /// - 싸울 때는 멈춰서 쏜다(이동 중엔 안 쏘는 규칙). 보스 예고 지대 · 장판 · 날아오는 탄 ·
    ///   가까운 적의 공격 준비가 보이면 옆으로 피하고, 가끔 자리를 옮긴다.
    /// - 유령이면 빙의 대상에게 빙의한다.
    /// - 방을 깨면 천사 · 악마의 제단 · 상점에 먼저 들렀다가 문으로 나간다.
    /// - 레벨업 · 천사 · 악마 · 상점 창은 1.5초쯤 읽는 척하고 고른다.
    /// - 스킬은 적이 많을 때 · 보스전에서 가끔 쓴다.
    ///
    /// 게임 코드는 건드리지 않는다 — 조이스틱이 넣는 `MoveInput` 과 화면 버튼만 쓴다.
    /// ⚠ 빌드에는 안 들어간다(UNITY_EDITOR). 녹화가 끝나면 컴포넌트를 떼면 된다.
    /// </summary>
    public sealed class PromoPilot : MonoBehaviour
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>이보다 멀면 다가간다(픽셀). 몸마다 사거리가 달라 넉넉히 잡는다.</summary>
        private const float ChaseRange = 330f;

        /// <summary>스킬 간격(초). 스킬 연출이 끝까지 보이도록 너무 잦지 않게.</summary>
        public static float SkillEveryMin = 7f, SkillEveryMax = 10f;

        private FieldInfo _fSkillGauge;

        private BattleDirector _bd;
        private FieldInfo _fEnemies, _fShots, _fFields, _fHost, _fGhost, _fRoomSize, _fExits, _fExitOpen,
                          _fDanger, _fRoomProp, _fRoomPropUsed, _fPossessTarget, _fInvuln, _fGhostHp,
                          _fSandboxTag, _fBoss, _fRunning;
        private PropertyInfo _pAvatar;

        private Vector2 _dodgeDir;
        private float _dodgeLeft;
        private float _repositionIn = 2.5f;
        private float _popupWait;
        private float _skillIn = 3f;
        private float _possessIn = 1.2f;

        /// <summary>죽지 않게 — 녹화가 도중에 끊기지 않게.</summary>
        public bool KeepAlive = true;

        /// <summary>
        /// 장면 기록 「이름:프레임;」 — 편집할 때 방 · 창 · 보스 격파 자리를 찾는다.
        /// 녹화기가 프레임을 30fps 로 고정하므로 (프레임 − 녹화 시작 프레임) / 30 이 영상 속 초다.
        /// </summary>
        public static readonly System.Text.StringBuilder Log = new();

        private int _loggedRoom = -1;
        private string _loggedPopup;
        private bool _loggedBossDown;
        private FieldInfo _fRoomIndex, _fRoomKind;

        private void Mark(string what) => Log.Append(what).Append(':').Append(Time.frameCount).Append(';');

        private void LogScene()
        {
            int room = (int)_fRoomIndex.GetValue(_bd);
            if (room != _loggedRoom)
            {
                _loggedRoom = room;
                _loggedBossDown = false;
                Mark($"room{room + 1}_{_fRoomKind.GetValue(_bd)}");
            }
            string popup = Active("BuffChoicePanel") ? "levelup" : Active("ShrinePanel") ? "angel"
                         : Active("EventPanel") ? "devil" : Active("ShopPanel") ? "shop" : null;
            if (popup != _loggedPopup) { if (popup != null) Mark($"{popup}@{room + 1}"); _loggedPopup = popup; }
            if (!_loggedBossDown && _fBoss.GetValue(_bd) == null && room == 14
                && _fEnemies.GetValue(_bd) is IList en && en.Count == 0)
            { _loggedBossDown = true; Mark("boss_down"); }
        }

        private void Awake()
        {
            var t = typeof(BattleDirector);
            _fEnemies = t.GetField("_enemies", F);
            _fShots = t.GetField("_shots", F);
            _fFields = t.GetField("_fields", F);
            _fHost = t.GetField("_host", F);
            _fGhost = t.GetField("_ghost", F);
            _fRoomSize = t.GetField("_roomSize", F);
            _fExits = t.GetField("_exits", F);
            _fExitOpen = t.GetField("_exitOpen", F);
            _fDanger = t.GetField("_danger", F);
            _fRoomProp = t.GetField("_roomProp", F);
            _fRoomPropUsed = t.GetField("_roomPropUsed", F);
            _fPossessTarget = t.GetField("_possessTarget", F);
            _fInvuln = t.GetField("_invuln", F);
            _fGhostHp = t.GetField("_ghostHp", F);
            _fSandboxTag = t.GetField("_sandboxTag", F);
            _fBoss = t.GetField("_boss", F);
            _fRunning = t.GetField("_running", F);
            _pAvatar = t.GetProperty("Avatar", F);
            _fRoomIndex = t.GetField("_roomIndex", F);
            _fRoomKind = t.GetField("_roomKind", F);
            _fSkillGauge = t.GetField("_skillCooldown", F);
        }

        private void Update()
        {
            if (_bd == null) _bd = FindAnyObjectByType<BattleDirector>();
            if (_bd == null || !(bool)_fRunning.GetValue(_bd)) return;
            float dt = Time.deltaTime;

            if (KeepAlive)
            {
                _fInvuln.SetValue(_bd, 9999f);
                _fGhostHp.SetValue(_bd, 100);
            }
            if (_fSandboxTag.GetValue(_bd) is RectTransform tag) tag.localScale = Vector3.zero;
            LogScene();

            if (HandlePopups(dt)) { _bd.MoveInput = Vector2.zero; return; }

            var me = _pAvatar.GetValue(_bd) as Unit;
            if (me == null) return;
            var enemies = _fEnemies.GetValue(_bd) as IList;
            int alive = 0;
            if (enemies != null) for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] is Unit u && u != null && u.IsAlive) alive++;

            // 유령이면 몸부터 — 빙의가 이 게임의 핵심이다
            if (_fHost.GetValue(_bd) == null && _fPossessTarget.GetValue(_bd) != null)
            {
                _possessIn -= dt;
                if (_possessIn <= 0f) { _bd.TryPossess(); _possessIn = 1.2f; }
            }

            if (alive > 0) Fight(me, enemies, alive, dt);
            else Explore(me);

            Unstick(me, dt);
        }

        // ── 막힘 풀기 ──────────────────────────────────────────
        //
        // 목표로 곧장 걸으면 기둥 · 상자에 막혀 제자리걸음을 한다(2026-09-18 녹화에서 실제로 그랬다).
        // 밀고 있는데 0.3초 동안 거의 안 움직였으면 옆으로 비켜 돌아간다 — 번갈아 왼쪽 · 오른쪽.

        private Vector2 _lastPos;
        private float _stuckFor;
        private float _detourLeft;
        private Vector2 _detourDir;
        private int _detourSide = 1;

        private void Unstick(Unit me, float dt)
        {
            var pos = me.Position;
            var want = _bd.MoveInput;

            if (_detourLeft > 0f)
            {
                _detourLeft -= dt;
                _bd.MoveInput = _detourDir;
                _lastPos = pos;
                return;
            }
            if (want.sqrMagnitude < 0.01f) { _stuckFor = 0f; _lastPos = pos; return; }

            float moved = (pos - _lastPos).magnitude;
            _lastPos = pos;
            if (moved < 40f * dt) _stuckFor += dt; else _stuckFor = 0f;
            if (_stuckFor < 0.3f) return;

            // 가려던 쪽에 수직으로, 조금 뒤로 물러서며 비켜 간다
            _stuckFor = 0f;
            _detourSide = -_detourSide;
            var side = new Vector2(-want.y, want.x) * _detourSide;
            _detourDir = (side - want * 0.3f).normalized;
            _detourLeft = Random.Range(0.45f, 0.7f);
            _bd.MoveInput = _detourDir;
        }

        // ── 싸움 ────────────────────────────────────────────────

        private void Fight(Unit me, IList enemies, int alive, float dt)
        {
            var room = (Vector2)_fRoomSize.GetValue(_bd);
            var pos = me.Position;

            // 1) 보스 예고 지대 안이면 가장 가까운 안전한 곳으로
            var danger = (DangerShape)_fDanger.GetValue(_bd);
            if (!danger.IsNone && danger.Contains(pos, room))
            {
                var safe = NearestSafe(pos, room, danger);
                _bd.MoveInput = (safe - pos).normalized;
                _dodgeLeft = 0f;
                return;
            }

            // 2) 피하는 중이면 계속
            if (_dodgeLeft > 0f)
            {
                _dodgeLeft -= dt;
                _bd.MoveInput = KeepInside(pos, _dodgeDir, room);
                return;
            }

            // 3) 위협 — 장판 · 가까운 적탄 · 공격 준비 중인 가까운 적
            Vector2 threat = Vector2.zero; bool hit = false;
            if (_fFields.GetValue(_bd) is IList fields)
                for (int i = 0; i < fields.Count; i++)
                    if (fields[i] is Field f && f != null && f.IsActive && !f.FromPlayer && f.Contains(pos))
                    { threat = f.Center; hit = true; break; }
            if (!hit && _fShots.GetValue(_bd) is IList shots)
                for (int i = 0; i < shots.Count; i++)
                    if (shots[i] is Projectile s && s != null && s.IsActive && !s.FromPlayer
                        && (s.Position - pos).sqrMagnitude < 150f * 150f)
                    { threat = s.Position; hit = true; break; }
            if (!hit)
                for (int i = 0; i < enemies.Count; i++)
                    if (enemies[i] is Unit u && u != null && u.IsAlive && u.IsWindingUp
                        && (u.Position - pos).sqrMagnitude < (u.IsBoss ? 420f * 420f : 200f * 200f))
                    { threat = u.Position; hit = true; break; }
            if (hit && Random.value < 0.85f)   // 가끔은 버틴다 — 너무 완벽하면 봇 같다
            {
                var away = pos - threat;
                if (away.sqrMagnitude < 1f) away = Random.insideUnitCircle;
                var side = new Vector2(-away.y, away.x) * (Random.value < 0.5f ? 1f : -1f);
                _dodgeDir = (away.normalized * 0.4f + side.normalized).normalized;
                _dodgeLeft = Random.Range(0.35f, 0.55f);
                _bd.MoveInput = KeepInside(pos, _dodgeDir, room);
                return;
            }

            // 4) 적이 멀리 서 있으면 다가간다 — 안 오는 적(원거리 · 제자리형) 앞에서 멈춰 버리지 않게
            var far = Nearest(enemies, pos);
            if (far != null && (far.Position - pos).sqrMagnitude > ChaseRange * ChaseRange)
            {
                _dodgeDir = (far.Position - pos).normalized;
                _dodgeLeft = Random.Range(0.4f, 0.7f);
                _bd.MoveInput = KeepInside(pos, _dodgeDir, room);
                return;
            }

            // 5) 가끔 자리 옮기기 — 너무 붙은 적에게서는 물러선다
            _repositionIn -= dt;
            if (_repositionIn <= 0f)
            {
                _repositionIn = Random.Range(2.2f, 3.6f);
                var near = Nearest(enemies, pos);
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (near != null && (near.Position - pos).sqrMagnitude < 170f * 170f)
                    dir = (pos - near.Position).normalized;
                _dodgeDir = dir;
                _dodgeLeft = Random.Range(0.25f, 0.45f);
                _bd.MoveInput = KeepInside(pos, _dodgeDir, room);
                return;
            }

            // 6) 스킬 — 몸이 있을 때, 적이 많거나 보스전일 때
            _skillIn -= dt;
            if (_skillIn <= 0f && _fHost.GetValue(_bd) != null
                && (alive >= 3 || _fBoss.GetValue(_bd) != null))
            {
                // 녹화용 — 게이지를 채워 두고 쓴다(게이지는 0 에서 차오른다). 영상에 스킬이 꼭 나오게.
                _fSkillGauge.SetValue(_bd, 999f);
                _bd.TryActiveSkill();
                Mark("skill");
                _skillIn = Random.Range(SkillEveryMin, SkillEveryMax);
            }

            _bd.MoveInput = Vector2.zero;   // 멈춰서 쏜다
        }

        // ── 방을 깬 뒤 ──────────────────────────────────────────

        private void Explore(Unit me)
        {
            var pos = me.Position;
            // 천사 · 악마의 제단 · 상점에 먼저 들른다
            if (_fRoomProp.GetValue(_bd) is RectTransform prop && prop != null
                && !(bool)_fRoomPropUsed.GetValue(_bd))
            {
                _bd.MoveInput = Toward(pos, prop.anchoredPosition);
                return;
            }
            if ((bool)_fExitOpen.GetValue(_bd) && _fExits.GetValue(_bd) is IList exits && exits.Count > 0)
            {
                var gate = exits[0];
                var view = gate.GetType().GetField("View").GetValue(gate) as RectTransform;
                if (view != null) { _bd.MoveInput = Toward(pos, view.anchoredPosition); return; }
            }
            _bd.MoveInput = Vector2.zero;
        }

        // ── 창 ──────────────────────────────────────────────────

        private bool HandlePopups(float dt)
        {
            string open = Active("BuffChoicePanel") ? "buff"
                        : Active("ShrinePanel") ? "shrine"
                        : Active("EventPanel") ? "event"
                        : Active("ShopPanel") ? "shop" : null;
            if (open == null) { _popupWait = 0f; return false; }

            _popupWait += dt;
            if (_popupWait < 1.6f) return true;   // 읽는 척
            _popupWait = 0f;

            switch (open)
            {
                case "buff": Click($"BuffCard{Random.Range(0, 3)}"); break;
                case "shrine": Click("ShrineChoice0"); break;
                case "event": if (!Click("EventAcceptButton")) Click("EventDeclineButton"); break;
                case "shop":
                    bool bought = false;
                    for (int i = 0; i < 4 && !bought; i++) bought = Click($"ShopItem{i}");
                    if (!bought) Click("ShopLeaveButton");
                    break;
            }
            return true;
        }

        private static bool Active(string name)
        {
            var go = GameObject.Find(name);
            return go != null && go.activeInHierarchy;
        }

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            var b = go != null ? go.GetComponent<Button>() : null;
            if (b == null || !b.interactable || !b.gameObject.activeInHierarchy) return false;
            b.onClick.Invoke();
            return true;
        }

        // ── 도우미 ──────────────────────────────────────────────

        private static Unit Nearest(IList enemies, Vector2 pos)
        {
            Unit best = null; float bd = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] is Unit u && u != null && u.IsAlive)
                {
                    float d = (u.Position - pos).sqrMagnitude;
                    if (d < bd) { bd = d; best = u; }
                }
            return best;
        }

        private static Vector2 Toward(Vector2 from, Vector2 to)
        {
            var d = to - from;
            return d.sqrMagnitude < 16f ? Vector2.zero : d.normalized;
        }

        /// <summary>방 가장자리로 밀려가지 않게 — 벽 쪽이면 방 안쪽으로 꺾는다.</summary>
        private static Vector2 KeepInside(Vector2 pos, Vector2 dir, Vector2 room)
        {
            const float M = 90f;
            if (pos.x < M && dir.x < 0f) dir.x = -dir.x;
            if (pos.x > room.x - M && dir.x > 0f) dir.x = -dir.x;
            if (pos.y > -M && dir.y > 0f) dir.y = -dir.y;               // 방 위쪽(y 0)
            if (pos.y < -room.y + M && dir.y < 0f) dir.y = -dir.y;      // 방 아래쪽
            return dir.normalized;
        }

        private static Vector2 NearestSafe(Vector2 pos, Vector2 room, DangerShape danger)
        {
            Vector2 best = pos; float bestD = float.MaxValue;
            for (int r = 1; r <= 4; r++)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI * 2f / 16f;
                    var p = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (70f * r);
                    if (p.x < 60f || p.x > room.x - 60f || p.y > -60f || p.y < -room.y + 60f) continue;
                    if (danger.Contains(p, room)) continue;
                    float d = (p - pos).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = p; }
                }
            return best;
        }
    }
}
#endif
