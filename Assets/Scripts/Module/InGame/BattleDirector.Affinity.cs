using Game.Module.Events;
using UnityEngine;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상성의 세 쪽. **모든 몸과 모든 적이 셋 중 하나다** (가위바위보).
    ///
    ///   힘 → 날 → 술 → 힘   (화살표는 「이긴다」)
    ///   단단한 힘 앞에 총칼은 튕기고 / 총칼은 술사를 베고 / 주술은 갑옷을 뚫는다.
    /// </summary>
    public enum Affinity
    {
        None,
        /// <summary>날 — 총 · 칼 · 창. 노랑.</summary>
        Blade,
        /// <summary>힘 — 단단한 것, 둔기 · 폭발 · 기계. 주황빨강.</summary>
        Force,
        /// <summary>술 — 불 · 얼음 · 주술, 망자와 괴이. 보라.</summary>
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
    /// 날 · 힘 · 술은 호스트가 **지금 들고 있는 무기 그대로**이고, 돌고 도는 이유가 상식이다.
    ///
    /// ⚠ 시험판이다 — 숫자와 표가 코드에 있다. 확정되면 표(`HostTable` 의 쪽 칸, 적 표)로 옮긴다.
    /// </summary>
    public static class AffinityRule
    {
        /// <summary>
        /// 상성 모드. 에디터 메뉴 `Tools/Game/시험판 — 상성 모드` 로 켜고 끈다.
        /// 끄면 예전 규칙 그대로다 — 두 방식을 바꿔 가며 해 볼 수 있어야 한다.
        /// </summary>
        public static bool Enabled
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.AffinityMode", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.AffinityMode", value);
#else
            get => false;
            set { }
#endif
        }

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
            // ── 날: 총 · 칼 · 창을 든 것 ──
            "gangster" or "thug" or "hopper" or "hopper_smg" or "commando_mg" or "ninja"
                or "amazon" or "amazon_elite"
                or "bat" or "python" or "kingpin"
                => Affinity.Blade,
            // ── 힘: 둔기 · 폭발 · 중화기, 그리고 쇠로 된 것 ──
            "baseball" or "guru" or "ninja_chain" or "commando_grenade" or "commando_missile"
                or "commando_laser" or "robot"
                or "scrapgunner" or "actor_enforcer" or "roadwarden" or "coilwalker" or "obj_turret"
                or "robot_snakes" or "crusher" or "guardian"
                => Affinity.Force,
            // ── 술: 불 · 얼음 · 번개 · 독 · 빛 · 어둠, 그리고 망자와 괴이 ──
            "dragoon" or "snowwoman" or "dragon_blue" or "salamander" or "vampire"
                or "white_wizard" or "medium" or "death"
                or "skeleton" or "sludge"
                => Affinity.Magic,
            _ => Affinity.None,
        };

        /// <summary>a 가 b 를 이기는가. 힘 → 날 → 술 → 힘.</summary>
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
        /// 챕터의 적 구성 (날, 힘, 술 — 방 표에 선 잡몹 수). 로비가 「이 챕터엔 어느 쪽이 많은가」를 보여 줄 때 쓴다.
        ///
        /// ⚠ `RoomTable` 실측(2026-09-28)을 옮겨 적은 것이다. 확정되면 방 표에서 직접 센다 —
        ///   손으로 적은 숫자는 방을 고치는 순간 낡는다.
        /// ⚠ 지금 잡몹 7종은 기계가 많아 **「힘」 쪽으로 크게 쏠려 있다.** 새 잡몹을 넣을 때 날 · 술 쪽을 채워야 한다.
        /// </summary>
        public static (int blade, int force, int magic) ChapterMix(int chapter) => chapter switch
        {
            1 => (10, 12, 10),   // 박쥐 / 폐품 사수 / 해골
            2 => (11, 29, 0),    // 박쥐 / 순찰기 · 집행자
            3 => (0, 36, 16),    // 코일 · 집행자 · 십자 포탑 / 해골
            4 => (13, 46, 0),    // 박쥐 / 집행자 · 순찰기 · 코일
            5 => (0, 52, 18),    // 집행자 · 코일 · 십자 포탑 / 해골
            6 => (0, 61, 18),    // 집행자 · 순찰기 · 코일 · 십자 포탑 / 해골
            _ => (0, 0, 0),
        };

        /// <summary>그 챕터에 가장 많은 쪽.</summary>
        public static Affinity MajorOf(int chapter)
        {
            var (blade, force, magic) = ChapterMix(chapter);
            if (blade == 0 && force == 0 && magic == 0) return Affinity.None;
            if (force >= blade && force >= magic) return Affinity.Force;
            return blade >= magic ? Affinity.Blade : Affinity.Magic;
        }

        /// <summary>그 챕터 보스의 쪽.</summary>
        public static Affinity BossKindOf(int chapter) => chapter switch
        {
            1 => KindOf("robot_snakes"),
            2 => KindOf("crusher"),
            3 => KindOf("python"),
            4 => KindOf("sludge"),
            5 => KindOf("guardian"),
            6 => KindOf("kingpin"),
            _ => Affinity.None,
        };

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

        /// <summary>
        /// 가르치는 방 — 1챕터 앞의 세 방은 **한 쪽의 적만** 세우고 열쇠가 되는 몸을 같이 둔다.
        /// 날(권총) 몸으로 들어갔을 때 유리 → 보통 → 불리 순으로 겪게 짰다.
        /// 방 데이터(`RoomTable`)는 건드리지 않는다. 자리는 그대로 쓰고 누가 서는지만 바꾼다.
        /// </summary>
        public static string ActorOverride(string roomId, bool isHostSlot, string actorId)
        {
            if (!Enabled) return actorId;
            return roomId switch
            {
                // 해골(술) — 날이 유리하다. 총칼 든 몸이면 시원하게 잡힌다.
                "ROOM_CH1_001" => isHostSlot ? "amazon" : "skeleton",
                // 박쥐(날) — 날끼리는 보통. 힘(수류탄)이 유리하다.
                "ROOM_CH1_002" => isHostSlot ? "commando_grenade" : "bat",
                // 폐품 사수(힘) — 날은 튕긴다. 술(샐러맨더)로 갈아타야 한다.
                "ROOM_CH1_003" => isHostSlot ? "salamander" : "scrapgunner",
                _ => actorId,
            };
        }
    }

    public sealed partial class BattleDirector
    {
        // ── 그림 ────────────────────────────────────────────────
        //
        // `SpriteAtlas.GetSprite` 는 부를 때마다 새 Sprite 를 만든다 — 한 번만 받아 둔다.

        private readonly Sprite[] _affGems = new Sprite[4];
        private readonly Sprite[] _affTriangles = new Sprite[4];
        private Sprite _affUp, _affDown, _affTake;

        private void CacheAffinitySprites()
        {
            for (int i = 1; i < _affGems.Length; i++)
            {
                _affGems[i] = GetSprite(AffinityRule.GemName((Affinity)i));
                _affTriangles[i] = GetSprite(AffinityRule.TriangleName((Affinity)i));
            }
            _affUp = GetSprite("rps_up");
            _affDown = GetSprite("rps_down");
            _affTake = GetSprite("rps_take");
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
                SpawnFx("weakhit", victim.Position, WeakFxSize);
                GameSound.Cue("hit.weak");
                StrongHitReaction(victim);
                return Mathf.Max(1, Mathf.RoundToInt(damage * AffinityRule.WinDamageMul));
            }
            if (dull) return Mathf.Max(1, Mathf.RoundToInt(damage * AffinityRule.LoseDamageMul));
            return damage;
        }

        private const float WeakFxSize = 150f;

        // ── 타격 반응 (2026-10-01) ──────────────────────────────
        //
        // 「세게 들어간다 / 덜 들어간다」는 숫자가 아니라 **맞은 쪽의 반응**에서 온다.
        //
        //   유리 — 뒤로 밀리고, 하던 공격이 끊기고, 잠깐 굳는다. 화면이 흔들린다.
        //   불리 — 맞아도 꿈쩍 않고 그대로 걸어온다. 불똥만 작게 튀고 「팅」 소리가 난다.
        //
        // ⚠ 치명타와 겹치지 않게 역할을 나눈다: 치명타는 **숫자가 커지고 화면이 멈칫**하고,
        //   상성은 **적이 반응**한다. 그래서 유리 타격에는 화면 멈칫(HitStop)을 넣지 않는다.

        /// <summary>불리한 쪽을 때리는 중인가. 맞기 **전에** 묻는다 — 움찔할지 말지가 여기서 갈린다.</summary>
        private bool IsDullAgainst(Unit victim) => OutcomeAgainst(victim) < 0;

        private const float DullFxScale = 0.5f;
        private const float StrongPushMeters = 0.6f;
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

            var me = Avatar;
            if (me != null)
            {
                var away = victim.Position - me.Position;
                if (away.sqrMagnitude > 0.01f)
                {
                    var push = away.normalized * Meters(StrongPushMeters);
                    victim.Position = ClampedInField(victim, SlideMove(victim, victim.Position, push));
                }
            }
            victim.CancelWindup();
            victim.HoldHit(StrongHoldSeconds);
        }

        /// <summary>유리 숫자 색 — ▲ 와 같은 초록. 치명타(금색)와 갈린다.</summary>
        private static readonly Color WeakDamageColor = new(0.55f, 1f, 0.35f, 1f);

        /// <summary>불리 숫자 — 작고 흐리게. 「덜 들어갔다」가 숫자에서 읽혀야 한다.</summary>
        private static readonly Color DullDamageColor = new(0.62f, 0.66f, 0.72f, 0.9f);

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
        }

        // ── 표시 ────────────────────────────────────────────────

        /// <summary>쪽별 살아 있는 잡몹 수. 매 프레임 다시 센다 — 할당 없이.</summary>
        private readonly int[] _kindCount = new int[4];

        /// <summary>
        /// 적 머리 위 보석과 화살표, 「이 몸을 타라」 표시를 갱신한다.
        ///
        /// 「이 몸을 타라」 = 이 방에 **가장 많은 쪽을 이기는** 몸. 지금 내 몸이 이미 그 쪽을
        /// 이기고 있으면 부르지 않는다 — 갈아탈 이유가 없는데 고리가 뛰면 거짓말이다.
        /// </summary>
        private void RefreshAffinityMarks()
        {
            bool on = AffinityRule.Enabled;

            for (int i = 0; i < _kindCount.Length; i++) _kindCount[i] = 0;
            var major = Affinity.None;
            if (on)
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive || e.IsHostBody) continue;
                    // 보스는 혼자서 방 전체다 — 잡몹 몇 마리에 밀리면 안 된다.
                    _kindCount[(int)AffinityRule.KindOf(e.Key)] += e.IsBoss ? 100 : 1;
                }
                int best = 0;
                for (int i = 1; i < _kindCount.Length; i++)
                    if (_kindCount[i] > best) { best = _kindCount[i]; major = (Affinity)i; }
            }

            var mine = MyKind;
            bool alreadyWinning = on && AffinityRule.Beats(mine, major);

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                if (!on || !e.IsAlive)
                {
                    e.SetKindGem(null); e.SetMatchArrow(null); e.SetTakeMark(null);
                    continue;
                }

                var kind = AffinityRule.KindOf(e.Key);
                e.SetKindGem(_affGems[(int)kind]);

                int outcome = AffinityRule.Outcome(mine, kind);
                e.SetMatchArrow(outcome > 0 ? _affUp : outcome < 0 ? _affDown : null);

                // 「타라」 는 빼앗을 수 있는 몸에만, 그 몸이 이 방에 가장 많은 쪽을 이길 때만.
                bool take = e.IsHostBody && !e.RepossessBanned && !e.IsBoss
                            && !alreadyWinning && AffinityRule.Beats(kind, major);
                e.SetTakeMark(take ? _affTake : null);
            }
        }
    }
}
