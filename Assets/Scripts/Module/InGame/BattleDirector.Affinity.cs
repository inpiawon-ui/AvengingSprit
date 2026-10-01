using Game.Module.Events;
using UnityEngine;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>상성 계열. 몸은 계열 하나를 갖고, 적은 약점 하나를 갖는다.</summary>
    public enum Affinity
    {
        None,
        /// <summary>타격 — 근접으로 때리는 몸</summary>
        Strike,
        /// <summary>연사 — 총을 빠르게 쏘는 몸</summary>
        Rapid,
        /// <summary>관통 — 뚫고 지나가는 광선을 쏘는 몸</summary>
        Pierce,
        /// <summary>폭발 — 터지는 것을 던지는 몸</summary>
        Blast,
        /// <summary>속성 — 얼리고 태우고 중독시키는 몸</summary>
        Element,
    }

    /// <summary>
    /// 상성 시험판의 규칙표 (2026-10-01).
    ///
    /// **왜 만드나.** 지금은 처음 들고 간 몸으로 끝까지 가는 것이 정답이라
    /// 빙의가 «몸이 죽었을 때 쓰는 여분 목숨»으로만 남는다. 적마다 약점을 두고
    /// 그 약점에 맞는 몸을 방에 같이 세워, **갈아타는 것이 이득**인 순간을 만든다.
    /// 잘 키운 몸은 능력치 차이가 상성 이득보다 커서 그냥 밀고 갈 수 있다.
    ///
    /// ⚠ 시험판이다 — 숫자와 표가 코드에 있다. 방향이 정해지면 표(`HostTable` 의 계열 칸,
    ///   적 표의 약점 칸)로 옮긴다.
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

        /// <summary>
        /// 약점을 맞혔을 때 피해 배율.
        ///
        /// ⚠ 1.5 로 시작했다가 2.0 으로 올렸다(2026-10-01 실측). 1챕터 잡몹은 원래 두세 방에
        ///   죽어서, 1.5 배로는 «한 방 덜 맞고 죽는다» 정도라 갈아타는 2~3초를 못 갚았다
        ///   (갱스터로 끝까지 6.1초 · 아마존으로 갈아타고 8.4초). 갈아탄 보람이 눈에 보여야 한다.
        /// </summary>
        public const float WeakDamageMul = 2.0f;

        /// <summary>
        /// 스스로 나온 뒤 다시 들어갈 수 있을 때까지(초). 예전 규칙(1.2초)은 «탈출이 순간이동이
        /// 되지 않게» 였는데, 갈아타는 것이 권장 행동인 지금은 그 시간이 그대로 손해다.
        /// </summary>
        public const float LeaveLockSeconds = 0.5f;

        /// <summary>
        /// 스스로 나올 때 내는 유령 에너지(%). 죽어서 나올 때(20%)보다 싸야
        /// «버리고 갈아탄다»가 선택지가 된다.
        /// </summary>
        public const int LeaveCostPercent = 8;

        /// <summary>이 몸의 계열. 호스트가 아니면(잡몹·보스) None.</summary>
        public static Affinity FamilyOf(string hostKey) => hostKey switch
        {
            "amazon" or "amazon_elite" or "guru" or "ninja_chain" or "baseball" or "death"
                => Affinity.Strike,
            "gangster" or "thug" or "hopper" or "hopper_smg" or "commando_mg" or "ninja"
                => Affinity.Rapid,
            "commando_laser" or "white_wizard" or "medium" or "robot"
                => Affinity.Pierce,
            "commando_grenade" or "commando_missile" or "dragoon"
                => Affinity.Blast,
            "dragon_blue" or "snowwoman" or "salamander" or "vampire"
                => Affinity.Element,
            _ => Affinity.None,
        };

        /// <summary>
        /// 이 적의 약점. 빼앗을 수 있는 몸(호스트)에는 약점이 없다 — 표에 없으면 None.
        ///
        /// 약점은 그 적의 생김새에서 읽히게 골랐다: 뼈는 부수고(타격), 작고 빠른 것은
        /// 쏟아부어 맞히고(연사), 엄폐 뒤 사수는 터뜨린다(폭발).
        /// </summary>
        public static Affinity WeaknessOf(string enemyKey) => enemyKey switch
        {
            // 잡몹
            "skeleton" => Affinity.Strike,
            "bat" => Affinity.Rapid,
            "scrapgunner" => Affinity.Blast,
            "actor_enforcer" => Affinity.Pierce,
            "roadwarden" => Affinity.Element,
            "coilwalker" => Affinity.Rapid,
            "obj_turret" => Affinity.Blast,
            // 보스 — 직전 방(014)에 유리한 몸이 서 있어야 한다
            "robot_snakes" => Affinity.Strike,
            "crusher" => Affinity.Blast,
            "python" => Affinity.Strike,
            "sludge" => Affinity.Element,
            "guardian" => Affinity.Pierce,
            "kingpin" => Affinity.Rapid,
            _ => Affinity.None,
        };

        /// <summary>계열 아이콘의 스프라이트 이름(아틀라스 `ingamemainui`).</summary>
        public static string IconName(Affinity a) => a switch
        {
            Affinity.Strike => "affinity_strike",
            Affinity.Rapid => "affinity_rapid",
            Affinity.Pierce => "affinity_pierce",
            Affinity.Blast => "affinity_blast",
            Affinity.Element => "affinity_element",
            _ => null,
        };

        /// <summary>
        /// 가르치는 방 — 1챕터 앞의 세 방은 **약점 한 종류만** 세우고 열쇠가 되는 몸을 같이 둔다.
        /// 방 데이터(`RoomTable`)는 건드리지 않는다. 자리는 그대로 쓰고 누가 서는지만 바꾼다 —
        /// 모드를 끄면 원래 방으로 돌아간다.
        /// </summary>
        public static string ActorOverride(string roomId, bool isHostSlot, string actorId)
        {
            if (!Enabled) return actorId;
            return roomId switch
            {
                // 해골만 — 타격에 약하다. 열쇠는 아마존.
                "ROOM_CH1_001" => isHostSlot ? "amazon" : "skeleton",
                // 박쥐만 — 연사에 약하다. 열쇠는 갱스터.
                "ROOM_CH1_002" => isHostSlot ? "gangster" : "bat",
                // 폐품 사수만 — 폭발에 약하다. 열쇠는 코만도(수류탄).
                "ROOM_CH1_003" => isHostSlot ? "commando_grenade" : "scrapgunner",
                _ => actorId,
            };
        }
    }

    public sealed partial class BattleDirector
    {
        // ── 그림 ────────────────────────────────────────────────
        //
        // `SpriteAtlas.GetSprite` 는 부를 때마다 새 Sprite 를 만든다 — 한 번만 받아 둔다.

        private readonly Sprite[] _affIcons = new Sprite[6];
        private Sprite _affAdvMark, _affAdvRing;

        private void CacheAffinitySprites()
        {
            for (int i = 1; i < _affIcons.Length; i++)
                _affIcons[i] = GetSprite(AffinityRule.IconName((Affinity)i));
            _affAdvMark = GetSprite("affinity_adv_mark");
            _affAdvRing = GetSprite("affinity_adv_ring");
        }

        /// <summary>계열 아이콘. HUD 가 지금 몸의 계열을 그릴 때 쓴다. 모드가 꺼져 있으면 null.</summary>
        public Sprite AffinityIconOf(string hostKey)
        {
            if (!AffinityRule.Enabled) return null;
            return _affIcons[(int)AffinityRule.FamilyOf(hostKey)];
        }

        // ── 판정 ────────────────────────────────────────────────

        /// <summary>지금 내 몸의 계열. 유령이면 None.</summary>
        private Affinity MyFamily
            => _host != null ? AffinityRule.FamilyOf(_host.Key) : Affinity.None;

        /// <summary>이 적의 약점. 빼앗을 수 있는 몸과 중간 보스 대장은 약점이 없다.</summary>
        private static Affinity WeaknessOf(Unit u)
        {
            if (u == null || u.IsHostBody) return Affinity.None;
            return AffinityRule.WeaknessOf(u.Key);
        }

        /// <summary>내 지금 몸이 이 적의 약점을 찌르는가.</summary>
        private bool HitsWeakness(Unit victim)
        {
            if (!AffinityRule.Enabled) return false;
            var mine = MyFamily;
            return mine != Affinity.None && WeaknessOf(victim) == mine;
        }

        /// <summary>
        /// 약점 보정. 플레이어 공격의 두 길(탄 · 근접/스킬)이 **같은 자**로 잰다.
        /// 터졌으면 이펙트와 소리도 여기서 낸다 — 숫자만 커지면 왜 커졌는지 모른다.
        /// </summary>
        private int WithAffinity(Unit victim, int damage, out bool weak)
        {
            weak = HitsWeakness(victim);
            if (!weak) return damage;
            SpawnFx("weakhit", victim.Position, WeakFxSize);
            GameSound.Cue("hit.weak");
            return Mathf.Max(1, Mathf.RoundToInt(damage * AffinityRule.WeakDamageMul));
        }

        private const float WeakFxSize = 150f;

        /// <summary>약점 숫자 색. 유리 표시(초록)와 같은 계통 — 초록은 «상성 이득»이다.</summary>
        private static readonly Color WeakDamageColor = new(0.55f, 1f, 0.35f, 1f);

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

        /// <summary>계열별 살아 있는 적 수. 매 프레임 다시 센다 — 할당 없이.</summary>
        private readonly int[] _weakCount = new int[6];

        /// <summary>
        /// 적 머리 위 약점 아이콘과, 유리한 몸의 표시를 갱신한다.
        ///
        /// «유리한 몸» = 이 방에 **가장 많은 약점**을 찌르는 계열의 몸.
        /// 섞인 방에서 모든 몸이 빛나면 아무것도 안 빛나는 것과 같다 — 가장 많은 쪽만 본다.
        /// </summary>
        private void RefreshAffinityMarks()
        {
            bool on = AffinityRule.Enabled;

            for (int i = 0; i < _weakCount.Length; i++) _weakCount[i] = 0;
            int best = 0;
            if (on)
            {
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive) continue;
                    var w = WeaknessOf(e);
                    // 보스는 혼자서 방 전체다 — 잡몹 몇 마리에 밀리면 안 된다.
                    if (w != Affinity.None) _weakCount[(int)w] += e.IsBoss ? 100 : 1;
                }
                for (int i = 1; i < _weakCount.Length; i++)
                    if (_weakCount[i] > best) best = _weakCount[i];
            }

            var mine = MyFamily;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null) continue;
                if (!on || !e.IsAlive) { e.SetWeakIcon(null, false, false); e.SetAdvantage(null, null); continue; }

                var w = WeaknessOf(e);
                e.SetWeakIcon(_affIcons[(int)w], w != Affinity.None && w == mine, w != Affinity.None);

                bool adv = false;
                // ⚠ `IsPossessable` 을 보지 않는다 — 그건 «지금 이 순간» 이라 몸을 입고 있으면 false 다.
                //   유리 표시는 몸을 입고 있을 때 가장 필요하다(나와서 저리로 가라는 뜻이니까).
                if (e.IsHostBody && !e.RepossessBanned && !e.IsBoss && best > 0)
                {
                    var f = AffinityRule.FamilyOf(e.Key);
                    adv = f != Affinity.None && _weakCount[(int)f] == best;
                }
                if (adv) e.SetAdvantage(_affAdvMark, _affAdvRing);
                else e.SetAdvantage(null, null);
            }
        }
    }
}
