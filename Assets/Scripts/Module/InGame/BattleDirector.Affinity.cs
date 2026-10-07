using Game.Module.Events;
using UnityEngine;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상성의 세 쪽. **모든 몸과 모든 적이 셋 중 하나다** (가위바위보).
    ///
    ///   파워 → 무기 → 마법 → 파워   (화살표는 「이긴다」)
    ///   단단한 파워 앞에 총칼은 튕기고 / 총칼은 마법사를 베고 / 마법은 갑옷을 뚫는다.
    ///
    /// 이름은 PD 시안(2026-10-06, `_exchange/ref/affinity3/pd_affinity3_mockup.png`)으로 「날 · 힘 · 술」에서 바꿨다.
    /// 규칙(이기는 방향 · ±30%)과 몸의 배정은 그대로다. 코드 이름(Blade · Force · Magic)과 그림 파일 이름도 그대로 둔다.
    /// </summary>
    public enum Affinity
    {
        None,
        /// <summary>무기(옛 「날」) — 총 · 칼 · 창. 금색, 칼과 권총 아이콘.</summary>
        Blade,
        /// <summary>파워(옛 「힘」) — 단단한 것, 둔기 · 폭발 · 기계. 빨강, 주먹 아이콘.</summary>
        Force,
        /// <summary>마법(옛 「술」) — 불 · 얼음 · 주술, 망자와 괴이. 파랑, 별빛 구슬 아이콘.</summary>
        Magic,
    }

    /// <summary>
    /// 상성 시험판의 규칙표 (2026-10-01 시작 · 2026-10-02 가위바위보로 다시 씀).
    ///
    /// **왜 만드나.** 처음 들고 간 몸으로 끝까지 가는 것이 정답이라 빙의가 «몸이 죽었을 때
    /// 쓰는 여분 목숨»으로만 남았다. 적마다 쪽을 두고, 그 쪽을 이기는 몸을 방에 같이 세워
    /// **갈아타는 것이 이득**인 순간을 만든다. 잘 키운 몸은 능력치 차이가 상성보다 커서 그냥 밀고 간다.
    ///
    /// **왜 이 셋인가.** 처음에는 공격 방식 다섯(타격 · 연사 · 관통 · 폭발 · 속성)으로 나눴는데
    /// 「뭐가 뭐에 강한지 느낌이 안 온다」로 반려됐고, 불 · 물 같은 속성은 「호스트에 억지」로 반려됐다.
    /// 파워 · 무기 · 마법은 호스트가 **지금 들고 있는 무기 그대로**이고, 돌고 도는 이유가 상식이다.
    ///
    /// ⚠ 시험판이다 — 숫자와 표가 코드에 있다. 확정되면 표(`HostTable` 의 쪽 칸, 적 표)로 옮긴다.
    /// </summary>
    public static class AffinityRule
    {
        /// <summary>
        /// 상성 모드. 에디터 메뉴 `Tools/Game/시험판 — 상성 모드` 로 켜고 끈다.
        /// 끄면 예전 규칙 그대로다 — 두 방식을 바꿔 가며 해 볼 수 있어야 한다.
        /// </summary>
        /// ⚠ **2026-10-02 정식이 됐다** — 기본이 켜짐이고 빌드에서는 늘 켜져 있다.
        ///   에디터 스위치는 「상성 없이 해 보기」 용으로만 남긴다.
        public static bool Enabled
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.AffinityMode", true);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.AffinityMode", value);
#else
            get => true;
            set { }
#endif
        }

        /// <summary>
        /// 챕터 표. 로비와 전투가 「이 챕터엔 어느 쪽이 많은가 · 보스는 어느 쪽인가」를 물을 때 본다.
        /// `PlayerDataService` 가 설정을 읽은 뒤 한 번 건다.
        /// </summary>
        private static Game.Character.GameConfig s_config;

        public static void Bind(Game.Character.GameConfig config) => s_config = config;

        /// <summary>유리 · 보통 · 불리의 피해 배율. (제안값 — 유리 130 / 보통 100 / 불리 70)</summary>
        public const float WinDamageMul = 1.3f;
        public const float LoseDamageMul = 0.7f;

        /// <summary>
        /// 스스로 나올 때 내는 유령 에너지(%). 죽어서 나올 때(20%)보다 싸야
        /// «버리고 갈아탄다»가 선택지가 된다.
        /// </summary>
        public const int LeaveCostPercent = 8;

        /// <summary>
        /// 스스로 나온 뒤 다시 들어갈 수 있을 때까지(초). 예전 규칙(1.2초)은 «탈출이 순간이동이
        /// 되지 않게» 였는데, 갈아타는 것이 권장 행동인 지금은 그 시간이 그대로 손해다.
        /// </summary>
        public const float LeaveLockSeconds = 0.5f;

        /// <summary>이 몸(또는 적)이 어느 쪽인가. 표에 없으면 None — 상성을 안 탄다.</summary>
        public static Affinity KindOf(string key) => key switch
        {
            // ── 무기: 총 · 칼 · 창을 든 것 ──
            "gangster" or "thug" or "hopper" or "hopper_smg" or "commando_mg" or "ninja"
                or "amazon" or "amazon_elite"
                or "bat" or "roadwarden" or "scrapgunner" or "mole" or "mantis"
                or "python" or "kingpin"
                => Affinity.Blade,
            // ── 파워: 둔기 · 폭발 · 중화기, 그리고 쇠로 된 것 ──
            "baseball" or "guru" or "ninja_chain" or "commando_grenade" or "commando_missile"
                or "commando_laser" or "robot"
                or "actor_enforcer" or "boar" or "obj_turret" or "turret_cross"
                or "robot_snakes" or "crusher" or "guardian"
                => Affinity.Force,
            // ── 마법: 불 · 얼음 · 번개 · 독 · 빛 · 어둠, 그리고 망자와 괴이 ──
            "dragoon" or "snowwoman" or "dragon_blue" or "salamander" or "vampire"
                or "white_wizard" or "medium" or "death"
                or "skeleton" or "coilwalker" or "sludge"
                => Affinity.Magic,
            _ => Affinity.None,
        };

        /// <summary>a 가 b 를 이기는가. 파워 → 무기 → 마법 → 파워.</summary>
        public static bool Beats(Affinity a, Affinity b)
            => (a == Affinity.Force && b == Affinity.Blade)
            || (a == Affinity.Blade && b == Affinity.Magic)
            || (a == Affinity.Magic && b == Affinity.Force);

        /// <summary>때리는 쪽 기준의 결과. +1 유리 · 0 보통 · −1 불리.</summary>
        public static int Outcome(Affinity attacker, Affinity victim)
        {
            if (attacker == Affinity.None || victim == Affinity.None) return 0;
            if (Beats(attacker, victim)) return 1;
            if (Beats(victim, attacker)) return -1;
            return 0;
        }

        /// <summary>
        /// 챕터의 적 구성 (무기, 파워, 마법 — 방 표에 선 잡몹 수). 로비가 「이 챕터엔 어느 쪽이 많은가」를 보여 줄 때 쓴다.
        ///
        /// ⚠ `RoomTable` 실측(2026-09-28)을 옮겨 적은 것이다. 확정되면 방 표에서 직접 센다 —
        ///   손으로 적은 숫자는 방을 고치는 순간 낡는다.
        /// ⚠ 지금 잡몹 7종은 기계가 많아 **「파워」 쪽으로 크게 쏠려 있다.** 새 잡몹을 넣을 때 무기 · 마법 쪽을 채워야 한다.
        /// </summary>
        public static (int blade, int force, int magic) ChapterMix(int chapter)
        {
            if (s_config == null || !s_config.HasChapterTable) return (0, 0, 0);
            var row = s_config.ChapterOf(chapter);
            return (row.MixBlade, row.MixForce, row.MixMagic);
        }

        /// <summary>그 챕터에 가장 많은 쪽.</summary>
        public static Affinity MajorOf(int chapter)
        {
            var (blade, force, magic) = ChapterMix(chapter);
            if (blade == 0 && force == 0 && magic == 0) return Affinity.None;
            if (force >= blade && force >= magic) return Affinity.Force;
            return blade >= magic ? Affinity.Blade : Affinity.Magic;
        }

        /// <summary>그 챕터 보스의 쪽.</summary>
        public static Affinity BossKindOf(int chapter)
            => s_config != null && s_config.HasChapterTable
                ? KindOf(s_config.ChapterOf(chapter).Boss) : Affinity.None;

        /// <summary>속성 아이콘(주먹 · 칼과 권총 · 별빛 구슬). 파일 이름의 «gem» 은 예전 보석 그림 자리라서다.</summary>
        public static string GemName(Affinity a) => a switch
        {
            Affinity.Blade => "rps_gem_blade",
            Affinity.Force => "rps_gem_force",
            Affinity.Magic => "rps_gem_magic",
            _ => null,
        };

        public static string TriangleName(Affinity a) => a switch
        {
            Affinity.Blade => "rps_tri_blade",
            Affinity.Force => "rps_tri_force",
            Affinity.Magic => "rps_tri_magic",
            _ => null,
        };
    }

    public sealed partial class BattleDirector
    {
        // ── 그림 ────────────────────────────────────────────────
        //
        // `SpriteAtlas.GetSprite` 는 부를 때마다 새 Sprite 를 만든다 — 한 번만 받아 둔다.

        private readonly Sprite[] _affGems = new Sprite[4];
        private readonly Sprite[] _affTriangles = new Sprite[4];
        private Sprite _affUp, _affDown;

        private void CacheAffinitySprites()
        {
            for (int i = 1; i < _affGems.Length; i++)
            {
                _affGems[i] = GetSprite(AffinityRule.GemName((Affinity)i));
                _affTriangles[i] = GetSprite(AffinityRule.TriangleName((Affinity)i));
            }
            _affUp = GetSprite("rps_up");
            _affDown = GetSprite("rps_down");
        }

        /// <summary>그 몸의 보석. HUD 가 지금 몸의 쪽을 그릴 때 쓴다. 모드가 꺼져 있으면 null.</summary>
        public Sprite AffinityIconOf(string hostKey)
            => AffinityRule.Enabled ? _affGems[(int)AffinityRule.KindOf(hostKey)] : null;

        /// <summary>그 몸의 꼭짓점이 빛나는 삼각 상성판. 방에 들어설 때 HUD 가 크게 띄운다.</summary>
        public Sprite AffinityTriangleOf(string hostKey)
            => AffinityRule.Enabled ? _affTriangles[(int)AffinityRule.KindOf(hostKey)] : null;

        // ── 판정 ────────────────────────────────────────────────

        /// <summary>지금 내 몸의 쪽. 유령이면 None.</summary>
        private Affinity MyKind
            => _host != null ? AffinityRule.KindOf(_host.Key) : Affinity.None;

        /// <summary>내 지금 몸으로 이 적을 때릴 때의 결과. +1 유리 · 0 보통 · −1 불리.</summary>
        private int OutcomeAgainst(Unit victim)
        {
            if (!AffinityRule.Enabled || _host == null || victim == null) return 0;
            return AffinityRule.Outcome(MyKind, AffinityRule.KindOf(victim.Key));
        }

        /// <summary>
        /// 상성 보정. 플레이어 공격의 두 길(탄 · 근접/스킬)이 **같은 자**로 잰다.
        /// 터졌으면 이펙트와 소리도 여기서 낸다 — 숫자만 커지면 왜 커졌는지 모른다.
        /// </summary>
        /// <param name="weak">유리 — 이기는 쪽을 때렸다.</param>
        /// <param name="dull">불리 — 지는 쪽을 때렸다.</param>
        private int WithAffinity(Unit victim, int damage, out bool weak, out bool dull)
        {
            int outcome = OutcomeAgainst(victim);
            weak = outcome > 0;
            dull = outcome < 0;
            if (weak)
            {
                // 얼음 조각(fx_weakhit)은 무엇으로 때리든 얼음이라 뺐다(PD) — ▲ 와 같은 주황 불 고리 + 불티
                if (!HxAffinityHit(victim)) WeakBurst(victim);
                GameSound.Cue("hit.weak");
                StrongHitReaction(victim);
                return Mathf.Max(1, Mathf.RoundToInt(damage * AffinityRule.WinDamageMul));
            }
            if (dull) return Mathf.Max(1, Mathf.RoundToInt(damage * AffinityRule.LoseDamageMul));
            return damage;
        }

        // ── 타격 반응 (2026-10-01) ──────────────────────────────
        //
        // 「세게 들어간다 / 덜 들어간다」는 숫자가 아니라 **맞은 쪽의 반응**에서 온다.
        //
        //   유리 — 하던 공격이 끊기고, 제자리에서 잠깐 굳는다(밀지 않는다 — 2026-10-07). 화면이 흔들린다.
        //   불리 — 맞아도 꿈쩍 않고 그대로 걸어온다. 불똥만 작게 튀고 「팅」 소리가 난다.
        //
        // ⚠ 치명타와 겹치지 않게 역할을 나눈다: 치명타는 **숫자가 커지고 화면이 멈칫**하고,
        //   상성은 **적이 반응**한다. 그래서 유리 타격에는 화면 멈칫(HitStop)을 넣지 않는다.

        /// <summary>불리한 쪽을 때리는 중인가. 맞기 **전에** 묻는다 — 움찔할지 말지가 여기서 갈린다.</summary>
        private bool IsDullAgainst(Unit victim) => OutcomeAgainst(victim) < 0;

        private const float StrongHoldSeconds = 0.28f;
        private const float StrongShake = 3f;
        /// <summary>연사 몸이 맞힐 때마다 밀면 적이 영영 못 온다 — 적마다 이 간격에 한 번만.</summary>
        private const float StrongReactCooldown = 0.25f;

        private readonly System.Collections.Generic.Dictionary<Unit, float> _strongReactAt = new();

        private void StrongHitReaction(Unit victim)
        {
            Shake(StrongShake);
            if (victim == null || !victim.IsAlive || victim.IsBoss) return;

            float now = Time.time;
            if (_strongReactAt.TryGetValue(victim, out float last) && now - last < StrongReactCooldown) return;
            _strongReactAt[victim] = now;

            // ⚠ **밀지 않는다** — 맞은 자리에서 굳는다(기획 2026-10-07). 예전에는 0.6 m 뒤로 밀었는데
            //   원거리 몸이 근접 몹을 칠 때 「넉백으로 밀린다」로 읽혔다. 원거리 → 근접 반응(`RangedKnockback`)이
            //   이미 제자리 경직으로 바뀐 것(2026-09-18)과 같은 규칙으로 맞춘다.
            victim.CancelWindup();
            victim.HoldHit(StrongHoldSeconds);
        }

        /// <summary>
        /// 유리 숫자 색 — ▲ 와 같은 주황(기획 2026-10-07 「A 주황」). 빨강은 「위험 · 내가 맞음」, 초록은 회복이라 뺐다.
        /// 치명타(금빛 노랑)와는 색 · 크기로 갈린다.
        /// </summary>
        private static readonly Color WeakDamageColor = new(1f, 0.494f, 0.11f, 1f);

        /// <summary>
        /// 불리 숫자 — 작게, ▼ 와 같은 은빛 강철. 「안 먹힘 · 튕겨냄」이지 위험이 아니다.
        /// 밋밋한 회색은 「꺼진 버튼」 같아 은빛으로 바꿨다(PD 2026-10-07).
        /// </summary>
        private static readonly Color DullDamageColor = new(0.667f, 0.714f, 0.776f, 1f);

        // ── 갈아타기 규칙 ───────────────────────────────────────

        /// <summary>빼앗은 몸의 시작 체력(%). 상성 모드에서는 온전한 몸이다.</summary>
        private int PossessStartHpPercent
            => AffinityRule.Enabled ? 100 : _config.HostStartHpPercent;

        /// <summary>스스로 나올 때 내는 유령 에너지(%).</summary>
        private int LeaveCostPercent
            => AffinityRule.Enabled ? AffinityRule.LeaveCostPercent : _config.GhostLeaveCostPercent;

        /// <summary>
        /// 상성 모드의 «스스로 나오기». 버린 몸은 **그 자리에서 쓰러져 사라진다.**
        ///
        /// 예전 규칙은 버린 몸이 적으로 돌아갔다. 갈아타는 것이 이득이어야 하는 지금은
        /// 그게 이중 벌이 된다 — 에너지도 내고 적도 하나 는다. 몸을 버리는 것 자체가 값이다.
        /// </summary>
        private void LeaveHostCollapse()
        {
            var body = _host;
            if (body == null) return;

            var pos = body.Position;
            var key = body.Key;

            int cost = Mathf.Max(1, GhostHpMax * LeaveCostPercent / 100);
            _ghostHp = Mathf.Max(1, _ghostHp - cost);   // 나오는 것으로 소멸하지는 않는다
            ShowGhostCost(pos, cost);

            Retire(body);
            _host = null;

            _ghost.gameObject.SetActive(true);
            _ghost.transform.localScale = Vector3.one;
            _ghost.SetSpriteOverride(null);
            _ghost.Position = pos;

            _ghostProtect = _config.GhostProtectSeconds;
            _drainCarry = 0f;
            _buffs.SetHost(null);
            SlowNearbyEnemies(pos);

            // 나오자마자 옆 몸으로 들어가면 순간이동이 된다. 짧게 잠근다.
            _repossessLock = AffinityRule.LeaveLockSeconds;
            _repossessLockShown = -1;
            _bus.Publish(new RepossessLockEvent
            {
                Remain = _repossessLock, Total = AffinityRule.LeaveLockSeconds,
            });

            _bus.Publish(new HostLostEvent { LostHostKey = key });
            PublishHp();
            BeginSoulOut(body, pos);
        }

        // ── 표시 ────────────────────────────────────────────────

        /// <summary>
        /// 적 머리 위 보석과 유리 · 불리 화살표를 갱신한다.
        ///
        /// 머리 위 유령 아이콘은 여기서 다루지 않는다 — 「타면 유리하다」 가 아니라
        /// 「탈 수 있다」 로 뜻이 바뀌어 `BattleDirector.SoulFx` 가 맡는다(기획 2026-10-02).
        /// 유리한 몸은 보석과 화살표로 읽는다.
        /// </summary>
        private void RefreshAffinityMarks()
        {
            bool on = AffinityRule.Enabled;

            var mine = MyKind;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                if (!on || !e.IsAlive)
                {
                    e.SetKindGem(null); e.SetMatchArrow(null);
                    continue;
                }

                var kind = AffinityRule.KindOf(e.Key);
                e.SetKindGem(_affGems[(int)kind]);

                int outcome = AffinityRule.Outcome(mine, kind);
                e.SetMatchArrow(outcome > 0 ? _affUp : outcome < 0 ? _affDown : null);
            }
        }
    }
}
