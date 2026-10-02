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

        /// <summary>「이름:프레임@초;」 — 초는 게임 시간이라 방을 깨는 데 걸린 시간을 견줄 수 있다.</summary>
        private void Mark(string what) => Log.Append(what).Append(':').Append(Time.frameCount)
            .Append('@').Append(Time.time.ToString("0.0")).Append(';');

        private bool _loggedClear;

        private void LogScene()
        {
            int room = (int)_fRoomIndex.GetValue(_bd);
            if (room != _loggedRoom)
            {
                _loggedRoom = room;
                _loggedBossDown = false;
                _loggedClear = false;
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
            _mBlockedAt = t.GetMethod("BlockedAt", F);
            _mRange = t.GetMethod("EffectiveRange", F);
            _mCover = t.GetMethod("BlockedByCover", F);
            _mFootHalf = t.GetMethod("FootHalf", BindingFlags.NonPublic | BindingFlags.Static);
            _mFootDrop = t.GetMethod("FootDrop", BindingFlags.NonPublic | BindingFlags.Static);
        }

        // ── 길찾기 ──────────────────────────────────────────────
        //
        // 예전에는 목표로 **곧장** 걷고, 막히면 옆으로 비켜 보는 것이 전부였다(`Unstick`).
        // 해자 · 담 · 기둥 숲처럼 돌아가야 하는 방에서는 틈 앞에서 제자리를 맴돌았다
        // (2026-10-02 자동 검증 — 6챕터 「증류탑」에서 방을 비우고도 문으로 못 갔다).
        // 방을 격자로 나눠 너비 우선으로 길을 찾고, 그 길의 두세 칸 앞을 보고 걷는다.
        // 막힘 판정은 전투와 **같은 자**(`BattleDirector.BlockedAt`)다 — 자가 둘이면 한쪽이 낡는다.

        private const float PathCell = 24f;
        /// <summary>발자국을 이만큼 부풀려 잰다(px) — 모서리를 스치는 길을 고르지 않게.</summary>
        private const float PathPad = 6f;
        private const float PathEvery = 0.25f;

        private MethodInfo _mBlockedAt, _mFootHalf, _mFootDrop, _mRange;

        /// <summary>
        /// 피한 뒤 이만큼은 서 있는다(초). 이 게임은 **멈춰야 쏜다** — 십자 포탑처럼 쉬지 않고 쏘는 적 앞에서
        /// 탄이 올 때마다 피하면 한 발도 못 쏘고 방을 못 끝낸다(2026-10-02 검증에서 세 판이 그렇게 멈췄다).
        /// </summary>
        private const float DodgeRestSeconds = 1.1f;
        private float _dodgeRest;

        private MethodInfo _mCover;
        private readonly object[] _coverArgs = new object[2];

        /// <summary>
        /// 여기서 저 적까지 **내 탄이 닿는가.** 키 큰 것 뒤에 선 적을 향해 제자리에서 쏘기만 하면
        /// 한 발도 안 들어가고 방이 안 끝난다(2026-10-02 검증 — 6챕터 「파이프 골목」에서 난간 뒤 포탑을 못 잡았다).
        /// 전투와 같은 자(`BattleDirector.BlockedByCover`)로 사선을 따라 찍어 본다.
        /// </summary>
        private bool HasLine(Vector2 from, Vector2 to)
        {
            if (_mCover == null) return true;
            var d = to - from;
            int n = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 20f));
            _coverArgs[1] = true;
            for (int i = 1; i < n; i++)
            {
                _coverArgs[0] = from + d * (i / (float)n);
                if ((bool)_mCover.Invoke(_bd, _coverArgs)) return false;
            }
            return true;
        }

        /// <summary>지금 몸이 때릴 수 있는 거리(px). 못 읽으면 넉넉한 기본값.</summary>
        private float ReachOf(Unit me)
        {
            if (_mRange == null) return ChaseRange;
            _footArgs[0] = me;
            float r = (float)_mRange.Invoke(_bd, _footArgs);
            return r > 1f ? Mathf.Min(r, ChaseRange) : ChaseRange;
        }
        private bool[] _pathFree;
        private int[] _pathFrom;
        private readonly System.Collections.Generic.Queue<int> _pathQueue = new();
        private readonly object[] _blockedArgs = new object[3];
        private readonly object[] _footArgs = new object[1];
        private int _pathW, _pathH;
        private float _pathAge;
        private Vector2 _pathGoal, _pathStep;
        private bool _pathHas;

        /// <summary>목표로 가는 방향. 길이 있으면 길을 따라, 못 찾으면 곧장.</summary>
        private Vector2 PathToward(Unit me, Vector2 to)
        {
            var pos = me.Position;
            if ((to - pos).sqrMagnitude < 16f) return Vector2.zero;

            _pathAge -= Time.deltaTime;
            if (!_pathHas || _pathAge <= 0f || (to - _pathGoal).sqrMagnitude > 40f * 40f)
            {
                _pathAge = PathEvery;
                _pathGoal = to;
                _pathHas = FindStep(me, pos, to, out _pathStep);
            }
            if (!_pathHas) return Toward(pos, to);
            var d = _pathStep - pos;
            return d.sqrMagnitude < 9f ? Toward(pos, to) : d.normalized;
        }

        private bool FindStep(Unit me, Vector2 pos, Vector2 to, out Vector2 step)
        {
            step = to;
            var room = (Vector2)_fRoomSize.GetValue(_bd);
            int w = Mathf.Max(1, Mathf.CeilToInt(room.x / PathCell));
            int h = Mathf.Max(1, Mathf.CeilToInt(room.y / PathCell));
            if (_pathFree == null || _pathW != w || _pathH != h)
            {
                _pathW = w; _pathH = h;
                _pathFree = new bool[w * h];
                _pathFrom = new int[w * h];
            }

            _footArgs[0] = me;
            var half = (Vector2)_mFootHalf.Invoke(null, _footArgs) + new Vector2(PathPad, PathPad);
            float drop = (float)_mFootDrop.Invoke(null, _footArgs);
            _blockedArgs[1] = half;
            _blockedArgs[2] = drop;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    _blockedArgs[0] = CellCenter(x, y);
                    _pathFree[y * w + x] = !(bool)_mBlockedAt.Invoke(_bd, _blockedArgs);
                    _pathFrom[y * w + x] = -1;
                }

            int start = CellOf(pos), goal = CellOf(to);
            _pathFree[start] = true;   // 서 있는 자리는 부풀린 자로 재면 막힌 것으로 나올 수 있다
            _pathQueue.Clear();
            _pathQueue.Enqueue(start);
            _pathFrom[start] = start;
            int best = start;
            float bestD = (CellCenter(start % w, start / w) - to).sqrMagnitude;
            while (_pathQueue.Count > 0)
            {
                int c = _pathQueue.Dequeue();
                if (c == goal) { best = c; break; }
                int cx = c % w, cy = c / w;
                float dd = (CellCenter(cx, cy) - to).sqrMagnitude;
                if (dd < bestD) { bestD = dd; best = c; }
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = cy + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int n = ny * w + nx;
                    if (_pathFrom[n] >= 0 || !_pathFree[n]) continue;
                    _pathFrom[n] = c;
                    _pathQueue.Enqueue(n);
                }
            }
            if (best == start) return false;

            // 목표에서 거슬러 올라와 **출발 다음 세 번째 칸**을 본다 — 한 칸 앞만 보면 지그재그로 떤다.
            int a = best, b = best, c3 = best;
            for (int at = best; at != start; at = _pathFrom[at]) { c3 = b; b = a; a = at; }
            step = CellCenter(c3 % w, c3 / w);
            return true;
        }

        private Vector2 CellCenter(int x, int y) => new((x + 0.5f) * PathCell, -(y + 0.5f) * PathCell);

        private int CellOf(Vector2 p)
            => Mathf.Clamp(Mathf.FloorToInt(-p.y / PathCell), 0, _pathH - 1) * _pathW
             + Mathf.Clamp(Mathf.FloorToInt(p.x / PathCell), 0, _pathW - 1);

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

            if (alive == 0 && !_loggedClear) { _loggedClear = true; Mark("clear"); }

            // 상성 시험판 — 유리한 몸이 방에 있으면 그리로 갈아탄다(영상에 그 장면이 나오게).
            if (alive > 0 && SeekAdvantage(me, enemies, dt)) { Unstick(me, dt); return; }

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

        // ── 상성 — 유리한 몸으로 갈아타기 ────────────────────────
        //
        // 방에서 가장 많은 약점을 찌르는 몸이 서 있고 내 몸이 그 계열이 아니면,
        // 잠깐 지켜본 뒤 나와서 그 몸으로 간다. 사람이 「아, 저게 유리하구나」 하고
        // 갈아타는 순서를 그대로 흉내 낸다.

        /// <summary>끄면 갈아타지 않는다 — 「안 갈아탔을 때」와 견주려고 둔다.</summary>
        public bool SeekAdvantageOn = true;

        private readonly int[] _kinds = new int[4];
        private float _seekWait;
        private Unit _seekBody;

        private bool SeekAdvantage(Unit me, IList enemies, float dt)
        {
            if (!SeekAdvantageOn || !AffinityRule.Enabled) return false;

            // 이 방에 가장 많은 쪽 — 그 쪽을 이기는 몸이 「타야 할 몸」이다.
            for (int i = 0; i < _kinds.Length; i++) _kinds[i] = 0;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] is Unit u && u != null && u.IsAlive && !u.IsHostBody)
                    _kinds[(int)AffinityRule.KindOf(u.Key)] += u.IsBoss ? 100 : 1;
            int best = 0; var major = Affinity.None;
            for (int i = 1; i < _kinds.Length; i++) if (_kinds[i] > best) { best = _kinds[i]; major = (Affinity)i; }
            if (major == Affinity.None) { _seekBody = null; return false; }

            var host = _fHost.GetValue(_bd) as Unit;
            if (host != null && AffinityRule.Beats(AffinityRule.KindOf(host.Key), major))
            { _seekBody = null; return false; }

            Unit want = null; float wd = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] is Unit u && u != null && u.IsAlive && u.IsHostBody && !u.RepossessBanned)
                {
                    if (!AffinityRule.Beats(AffinityRule.KindOf(u.Key), major)) continue;
                    float d = (u.Position - me.Position).sqrMagnitude;
                    if (d < wd) { wd = d; want = u; }
                }
            if (want == null) { _seekBody = null; return false; }

            if (want != _seekBody) { _seekBody = want; _seekWait = 2.8f; }   // 안 맞는 몸으로 때리는 모습이 보일 만큼 지켜본다
            if (_seekWait > 0f) { _seekWait -= dt; return false; }

            if (host != null)
            {
                // 몸을 버린다 — 빙의 버튼이 몸이 있을 때는 「나오기」다
                _bd.TryPossess();
                Mark("eject");
                _possessIn = 0.55f;   // 다시 들어갈 수 있게 되는 시간(0.5초)만 기다린다
                return true;
            }

            if (ReferenceEquals(_fPossessTarget.GetValue(_bd), want))
            {
                _bd.MoveInput = Vector2.zero;
                _possessIn -= dt;
                if (_possessIn <= 0f) { _bd.TryPossess(); Mark("possess_adv"); _possessIn = 1.2f; }
            }
            else _bd.MoveInput = PathToward(me, want.Position);
            return true;
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
            if (_dodgeRest > 0f) _dodgeRest -= dt;

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
            if (hit && _dodgeRest <= 0f && Random.value < 0.85f)   // 가끔은 버틴다 — 너무 완벽하면 봇 같다
            {
                _dodgeRest = DodgeRestSeconds;
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
            float reach = ReachOf(me) * 0.85f;
            if (far != null && ((far.Position - pos).sqrMagnitude > reach * reach || !HasLine(pos, far.Position)))
            {
                _dodgeDir = PathToward(me, far.Position);
                if (_dodgeDir.sqrMagnitude < 0.01f) _dodgeDir = (far.Position - pos).normalized;
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
                _bd.MoveInput = PathToward(me, prop.anchoredPosition);
                return;
            }
            if ((bool)_fExitOpen.GetValue(_bd) && _fExits.GetValue(_bd) is IList exits && exits.Count > 0)
            {
                var gate = exits[0];
                var view = gate.GetType().GetField("View").GetValue(gate) as RectTransform;
                if (view != null) { _bd.MoveInput = PathToward(me, view.anchoredPosition); return; }
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
