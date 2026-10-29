using System.Collections.Generic;
using Game.Character;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Localize = Game.Module.Common.Localize;

namespace Game.Module.InGame
{
    /// <summary>
    /// **빙의 파티** 규칙 (PD 기획서 v0.1 · 2026-10-29, 브랜치 avsr/possession-party).
    ///
    /// 시험판 HTML(`Projects/AVSR/Docs/AVSR_빙의파티_시험판_2026-10-29.html`)에서 PD 가 해 보고 고친 그대로 옮긴다.
    ///   · 유령 HP 100 = 한 판의 목숨이자 예산. 첫 몸 −20 · 보조 넣기 −10 · 메인 교체 −25(챕터 2회) · 보조 교체 −15(1회) · 메인 사망 −30
    ///   · 메인 = 지금 게임의 몸 그대로(조작 · 액티브 · 필살기 · 패시브 · 체력). 보조 1 · 2 = 체력 없음 · 따라다니며 평타 30% · 6초마다 스킬 자동
    ///   · 적 호스트는 체력 35% 아래면 「빙의 가능」 표시만 붙고 계속 싸우다 죽는다. 죽기 전에 **직접 터치**하면 잠깐 멈추고 고른다
    ///     (메인과 바꾸기 · 보조 1 · 2 와 바꾸기 / 빈 칸에 넣기 · 놔두기). 바뀐 호스트는 사라진다
    ///   · 고를 수 있는 길이 없으면(유령 HP · 교체 횟수) 표시도 선택창도 안 뜬다
    ///   · 메인 사망 −30 → 8초 안에 A 새 빙의(−20) · B 보조 승격(−5) · C 제자리 부활(−40)
    ///
    /// 2단계(태그 시너지 · 레시피 칸 · 카드 둘로 나누기 · 판 밖 성장)는 아직이다.
    /// ⚠ 선택창 그림은 코드로 세운 임시 판이다 — 시안이 나오면 발주해 갈아 끼운다.
    /// </summary>
    public static class PartyRule
    {
        /// <summary>에디터 메뉴 `Tools/Game/시험판 — 빙의 파티` 로 켜고 끈다. 이 브랜치에서는 기본 켜짐.</summary>
        public static bool Enabled
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.PartyMode", true);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.PartyMode", value);
#else
            get => true;
            set { }
#endif
        }

        public const int CostFirst = 20;
        public const int CostSub = 10;
        public const int CostMainSwap = 25;
        public const int CostSubSwap = 15;
        public const int CostDeath = 30;
        public const int CostDeathNew = 20;
        public const int CostDeathPromote = 5;
        public const int CostDeathRevive = 40;
        public const int MainSwapMax = 2;
        public const int SubSwapMax = 1;
        public const int SubSlots = 2;

        /// <summary>적 호스트의 체력이 이 비율 아래면 빙의 가능. 35% 는 너무 빨리 죽어 60% 로 올렸다(PD 2026-10-29).</summary>
        public const float PossessHpRatio = 0.6f;
        /// <summary>보조의 평타 · 스킬 세기(메인 대비).</summary>
        public const float SubPower = 0.3f;
        /// <summary>보조의 공격 속도(메인 대비). 0.5 = 두 번 칠 시간에 한 번.</summary>
        public const float SubRateMul = 0.5f;
        /// <summary>보조의 공격 사거리(메인 대비). 능력치는 다 낮추고 사거리만 넓힌다(PD 2026-10-29).</summary>
        public const float SubRangeMul = 1.3f;
        /// <summary>보조가 메인에게서 벗어나지 않는 반경(m). 보면서 잡는 값(PD 2026-10-29).</summary>
        public const float LeashMeters = 5f;
        /// <summary>보조 스킬 자동 발동 간격(초).</summary>
        public const float SubSkillSeconds = 6f;
        /// <summary>메인 사망 뒤 고르는 시간(초).</summary>
        public const float DeathChoiceSeconds = 8f;
    }

    public sealed partial class BattleDirector
    {
        /// <summary>이 판에 빙의 파티 규칙이 걸리는가 — 탑 · 밤 같은 모드 판은 예전 규칙 그대로.</summary>
        private bool PartyOn => PartyRule.Enabled && !IsModeRun;

        private sealed class PartySub
        {
            public Unit U;
            public float SkillCd;
        }

        private readonly List<PartySub> _subs = new();
        private int _mainSwaps;
        private int _subSwaps;
        /// <summary>메인 사망 뒤 A(새 빙의)를 골랐다 — 다음 빙의에 이 값을 치른다.</summary>
        private int _partyPossessCost;
        private bool _partyStartCharged;
        /// <summary>선택창이 떠 있는 동안 전투를 세운다.</summary>
        private bool _partyHold;
        private float _partyTimer = -1f;
        private GameObject _partyPopup;
        private Unit _partyPick;

        // 보조가 자동으로 써도 되는 스킬 — 시전자 자리에서 적을 치는 것들.
        // 자기 몸을 움직이거나 메인에게 거는 스킬(도약 · 반사 · 방벽 · 결계 · 회피 · 회복)은 보조 전용 「연사」로 대신한다(보완 ④).
        private static readonly HashSet<string> SubSafeSkills = new()
        {
            "gangster", "thug", "dragoon", "salamander", "dragon_blue", "commando_grenade",
            "commando_missile", "commando_laser", "white_wizard", "medium", "robot", "ninja_chain",
        };

        // ─────────────────────────────────────────
        // 호스트 자리 — 랜덤
        // ─────────────────────────────────────────

        /// <summary>빙의할 수 있는 호스트인가 — 유령 · 히든(사신) · 빙의 불가 몸은 뺀다.</summary>
        private static bool PartyPickable(HostEntry h)
            => h != null && !h.IsGhost && !Game.User.PlayerDataService.IsHiddenHost(h.HostKey)
               && h.PossessKind != PossessKind.NotPossessable;

        /// <summary>판 시작에 올릴 그림 — 호스트 자리가 랜덤이라 빙의할 수 있는 호스트를 전부 올린다.</summary>
        private void AddPartyUnitKeys(List<string> keys, IReadOnlyList<HostEntry> hosts)
        {
            if (!PartyOn) return;
            for (int i = 0; i < hosts.Count; i++)
                if (PartyPickable(hosts[i]) && !keys.Contains(hosts[i].SpriteKey)) keys.Add(hosts[i].SpriteKey);
        }

        /// <summary>
        /// 호스트 자리에 세울 몸을 전체에서 랜덤으로 고른다. 파티에 있는 호스트 · 이 방에 이미 선 호스트 · 그림이 안 올라온 몸은 뺀다.
        /// 고를 것이 없으면 정본 호스트 그대로.
        /// </summary>
        private HostEntry PartyRandomHost(HostEntry canon)
        {
            var hosts = _player != null && _player.IsReady ? _player.AllHosts : null;
            if (hosts == null || hosts.Count == 0) return canon;
            SyncPartyKeys();
            _partyPool.Clear();
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[i];
                if (!PartyPickable(h) || Unit.PartyKeys.Contains(h.HostKey) || TrashSprite(h) == null) continue;
                bool here = false;
                for (int k = 0; k < _enemies.Count; k++)
                    if (_enemies[k] != null && _enemies[k].IsHostBody && _enemies[k].Key == h.HostKey) { here = true; break; }
                if (!here) _partyPool.Add(h);
            }
            return _partyPool.Count == 0 ? canon : _partyPool[_rng.Next(_partyPool.Count)];
        }

        private readonly List<HostEntry> _partyPool = new();

        // ─────────────────────────────────────────
        // 판 시작 · 정리
        // ─────────────────────────────────────────

        private void ResetParty()
        {
            for (int i = 0; i < _subs.Count; i++) if (_subs[i].U != null) Destroy(_subs[i].U.gameObject);
            _subs.Clear();
            _mainSwaps = 0;
            _subSwaps = 0;
            _partyPossessCost = 0;
            _partyStartCharged = false;
            ClosePartyPopup();
            Unit.PartyActive = PartyOn;
        }

        /// <summary>로비에서 고른 몸을 입고 시작하는 것도 「첫 빙의」 다.</summary>
        private void ChargePartyStart()
        {
            if (!PartyOn || _partyStartCharged) return;
            _partyStartCharged = true;
            _ghostHp = Mathf.Max(1, _ghostHp - PartyRule.CostFirst);
        }

        private void PartySpend(int cost, Vector2 at)
        {
            _ghostHp = Mathf.Max(0, _ghostHp - cost);
            ShowGhostCost(at, cost);
            PublishHp();
        }

        /// <summary>약해진 호스트를 데려올 길이 하나라도 있는가 — 없으면 표시도 선택창도 안 띄운다.</summary>
        private bool PartyCanChoose()
        {
            if (!PartyOn || _host == null) return false;
            if (_mainSwaps < PartyRule.MainSwapMax && _ghostHp > PartyRule.CostMainSwap) return true;
            if (_subs.Count < PartyRule.SubSlots) return _ghostHp > PartyRule.CostSub;
            return _subSwaps < PartyRule.SubSwapMax && _ghostHp > PartyRule.CostSubSwap;
        }

        // ─────────────────────────────────────────
        // 터치 — 빙의 가능한 적을 직접 누른다(InGameMainUI 패드보다 먼저)
        // ─────────────────────────────────────────

        /// <summary>
        /// 화면을 누른 자리에 빙의할 몸이 있으면 처리하고 true. 없으면 false — 패드가 이어서 받는다.
        /// </summary>
        public bool TryPartyTap(Vector2 screen, Camera cam)
        {
            if (!PartyOn || !_running || _partyHold || _awaitingBuff || IsChanneling) return false;
            bool ghost = _host == null;
            if (!ghost && !PartyCanChoose()) return false;

            Unit best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsPossessable) continue;
                var rt = (RectTransform)e.transform;
                var c = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                var edge = RectTransformUtility.WorldToScreenPoint(cam, rt.position + rt.right * rt.rect.width * rt.lossyScale.x);
                // 판정은 넉넉하게 — 전투 중 정확히 누르기 어렵다(기획서 10-2)
                float reach = Mathf.Max(60f, Vector2.Distance(c, edge) * 1.1f);
                float d = Vector2.Distance(c, screen);
                if (d <= reach && d < bestD) { bestD = d; best = e; }
            }
            if (best == null) return false;

            if (ghost)
            {
                // 유령은 고를 것이 없다 — 누르면 바로 메인으로 들어간다
                _possessTarget = best;
                _repossessLock = 0f;
                TryPossess();
                return true;
            }
            OpenPartyChoice(best);
            return true;
        }

        // ─────────────────────────────────────────
        // 선택창 — 메인과 바꾸기 · 보조 1 · 2 와 바꾸기(빈 칸이면 넣기) · 놔두기
        // ─────────────────────────────────────────

        private void OpenPartyChoice(Unit target)
        {
            _partyPick = target;
            var entry = _player != null ? _player.GetHost(target.Key) : null;
            string name = entry != null ? entry.DisplayName : target.DisplayName;
            int mainLeft = PartyRule.MainSwapMax - _mainSwaps;
            int subLeft = PartyRule.SubSwapMax - _subSwaps;
            var opts = new List<PartyOption>();

            opts.Add(new PartyOption(
                $"메인과 바꾸기 · 유령 HP −{PartyRule.CostMainSwap}  (남은 {mainLeft}회)",
                $"{HostName(_host.Key)} 대신 {name} 이(가) 메인 · {HostName(_host.Key)} 은(는) 사라진다",
                mainLeft > 0 && _ghostHp > PartyRule.CostMainSwap,
                () => PartySwapMain(target)));

            for (int i = 0; i < PartyRule.SubSlots; i++)
            {
                int slot = i;
                if (i < _subs.Count)
                {
                    string old = HostName(_subs[i].U.Key);
                    opts.Add(new PartyOption(
                        $"보조 {i + 1}{(i == 0 ? " 과" : " 와")} 바꾸기 · 유령 HP −{PartyRule.CostSubSwap}  (남은 {subLeft}회)",
                        $"{old} 대신 {name} · 6초마다 스킬 자동 · 평타 30% · {old} 은(는) 사라진다",
                        subLeft > 0 && _ghostHp > PartyRule.CostSubSwap,
                        () => PartySetSub(target, slot, PartyRule.CostSubSwap)));
                }
                else
                {
                    opts.Add(new PartyOption(
                        $"보조 {i + 1} 에 넣기 · 유령 HP −{PartyRule.CostSub}",
                        $"{name} · 6초마다 스킬 자동 · 평타 30% · 체력 없음",
                        _ghostHp > PartyRule.CostSub,
                        () => PartySetSub(target, slot, PartyRule.CostSub)));
                    break;   // 빈 칸은 앞에서부터 하나만 보인다
                }
            }
            opts.Add(new PartyOption("놔두기", "그대로 둔다 — 계속 싸우다 죽는다", true, null));

            ShowPartyPopup($"{name} — 어떻게 할까?", $"남은 유령 HP {_ghostHp}", opts, -1f);
        }

        private string HostName(string key)
        {
            var e = _player != null && key != null ? _player.GetHost(key) : null;
            return e != null ? e.DisplayName : key;
        }

        /// <summary>적 목록에서 몸을 빼낸다 — 빙의와 같은 방식(골드 몫은 덜고 돈은 안 준다).</summary>
        private bool TakeFromEnemies(Unit target)
        {
            if (target == null || !target.IsAlive || !_enemies.Contains(target)) return false;
            if (StageLevelRule) FillStageExp(target);
            _enemies.Remove(target);
            _goldWeightLeft = Mathf.Max(0, _goldWeightLeft - GoldWeightOf(target));
            target.CancelWindup();
            return true;
        }

        private void PartySwapMain(Unit target)
        {
            if (_host == null || target == null || !target.IsAlive) return;
            PartySpend(PartyRule.CostMainSwap, _host.Position);
            _mainSwaps++;
            // 지금 메인은 사라진다 — 쓰러지는 몸이 아니라 놓아준 몸이라 조각 · 사망 처리는 하지 않는다
            var old = _host;
            _ghost.Position = old.Position;
            PlayFx("shield", old.Position, 128f, loop: false);
            Retire(old);
            _host = null;
            Unit.PlayerHasHost = false;
            _possessTarget = target;
            _repossessLock = 0f;
            TryPossess();
        }

        private void PartySetSub(Unit target, int slot, int cost)
        {
            if (_host == null || !TakeFromEnemies(target)) return;
            PartySpend(cost, target.Position);
            if (cost == PartyRule.CostSubSwap) _subSwaps++;

            var entry = _player != null ? _player.GetHost(target.Key) : null;
            var at = target.Position;
            string key = target.Key;
            Destroy(target.gameObject);

            var u = NewUnit($"PartySub_{key}");
            // 메인으로 입었을 때와 같은 식(로비 강화 · 성급 포함)의 30% — 기본값의 30% 면 메인보다 훨씬 약해진다
            int atk = Mathf.Max(1, Mathf.RoundToInt(LeveledAtk(entry) * UpgradeMul(key, HostStat.Atk)
                                                    * MasteryStatMulOf(key) * PartyRule.SubPower));
            u.Setup(UnitSide.Player, key, entry != null ? entry.DisplayName : key, UnitGet(key),
                    9999, atk, HostSpeedOf(entry) * 1.15f, HostRangeOf(entry) * PartyRule.SubRangeMul,
                    HostIntervalOf(entry) / PartyRule.SubRateMul,
                    UnitBox(84f, 80f), isBoss: false, profile: entry);
            u.Position = at;
            u.SetState(EnemyState.Idle);
            ApplyFacingSprites(u, key);
            u.SetHpBarVisible(false);   // 보조는 체력이 없다 — 적이 노리지 않아 맞지도 않는다
            u.MarkPartySub(GetSprite("party_sub_ring"));   // 붙들어 둔 혼 — 비치는 보랏빛 · 숨쉬기 · 발밑 혼령 고리
            PlayFx("shield", at, 128f, loop: false);

            var sub = new PartySub { U = u, SkillCd = Random.Range(1.5f, 3f) };
            if (slot < _subs.Count)
            {
                var old = _subs[slot];
                if (old.U != null) { PlayFx("burst", old.U.Position, 110f, loop: false); Destroy(old.U.gameObject); }
                _subs[slot] = sub;
            }
            else _subs.Add(sub);
            Debug.Log($"[빙의 파티] 보조 {slot + 1} = {key} · 공격 {atk}");
        }

        // ─────────────────────────────────────────
        // 메인 사망 — 8초 안에 A · B · C
        // ─────────────────────────────────────────

        /// <summary>LoseHost 끝에서 부른다. 유령 HP 가 남았을 때만 고른다.</summary>
        private void OpenPartyDeath(string lostKey, Vector2 pos)
        {
            if (_ghostHp <= 0) return;
            var opts = new List<PartyOption>();
            opts.Add(new PartyOption(
                $"A. 근처 호스트에 새로 빙의 · −{PartyRule.CostDeathNew}",
                "유령으로 떠서 다른 호스트를 터치하면 바로 메인 · 보조는 그대로 · 떠 있는 동안 유령 HP 가 샌다",
                _ghostHp > PartyRule.CostDeathNew,
                () => _partyPossessCost = PartyRule.CostDeathNew));
            for (int i = 0; i < _subs.Count; i++)
            {
                int slot = i;
                opts.Add(new PartyOption(
                    $"B. 보조 {i + 1} 「{HostName(_subs[i].U.Key)}」 을(를) 메인으로 · −{PartyRule.CostDeathPromote}",
                    "그 자리에서 바로 메인 · 보조 칸 하나가 빈다",
                    _ghostHp > PartyRule.CostDeathPromote,
                    () => PartyPromote(slot, pos)));
            }
            opts.Add(new PartyOption(
                $"C. 그 자리에서 {HostName(lostKey)} 부활 · −{PartyRule.CostDeathRevive}",
                "보조 그대로 · 체력 가득",
                _ghostHp > PartyRule.CostDeathRevive,
                () => PartyRevive(lostKey, pos)));

            bool any = false;
            for (int i = 0; i < opts.Count; i++) any |= opts[i].Enabled;
            if (!any) return;   // 고를 길이 없다 — 유령으로 남고 다음 틱 규칙대로
            ShowPartyPopup($"메인 「{HostName(lostKey)}」 쓰러짐 — 유령 HP −{PartyRule.CostDeath}",
                           $"남은 유령 HP {_ghostHp} · {PartyRule.DeathChoiceSeconds:0}초 안에 고른다 · 시간이 지나면 게임 오버",
                           opts, PartyRule.DeathChoiceSeconds);
        }

        private void PartyPromote(int slot, Vector2 pos)
        {
            if (slot >= _subs.Count) return;
            var s = _subs[slot];
            _subs.RemoveAt(slot);
            string key = s.U.Key;
            var at = s.U.Position;
            Destroy(s.U.gameObject);
            PartySpend(PartyRule.CostDeathPromote, at);
            var entry = _player != null ? _player.GetHost(key) : null;
            EnterHost(entry, key, key, at, 100);
        }

        private void PartyRevive(string key, Vector2 pos)
        {
            PartySpend(PartyRule.CostDeathRevive, pos);
            var entry = _player != null ? _player.GetHost(key) : null;
            EnterHost(entry, key, key, pos, 100);
        }

        // ─────────────────────────────────────────
        // 매 프레임 — 보조 따라다니기 · 평타 · 스킬
        // ─────────────────────────────────────────

        private void TickParty(float dt)
        {
            SyncPartyKeys();
            if (!PartyOn || _subs.Count == 0) return;
            var me = Avatar;
            if (me == null) return;

            for (int i = 0; i < _subs.Count; i++)
            {
                var s = _subs[i];
                var u = s.U;
                if (u == null) continue;
                u.TickFlash(dt);
                u.TickAnim(dt);

                // 자리: 메인 양옆 조금 아래
                var home = me.Position + new Vector2(i == 0 ? -78f : 78f, -36f);
                // 혼이라 바닥에서 떠 있다 — 자리는 그대로, 그림만 둥실(둘이 엇박으로). 그림자가 실제 자리다
                u.SetSpriteLift(14f + 6f * Mathf.Sin(Time.time * 2.2f + i * 1.7f));
                u.TickPartyRing(Time.time * 3f + i * 1.3f);
                // 방이 바뀌어 멀리 떨어졌으면 곁으로 옮긴다
                if (Vector2.Distance(u.Position, me.Position) > 600f) u.Position = home;

                if (_host == null) { u.SetMoving(false); continue; }   // 메인이 없으면 싸우지 않는다

                // 고르는 적 — 메인 둘레(반경 + 내 사거리) 안에서 나에게 가장 가까운 것. 반경 밖 적은 쫓지 않는다
                float leash = PartyRule.LeashMeters * _pxPerMeter;
                Unit target = null;
                float best = float.MaxValue;
                for (int k = 0; k < _enemies.Count; k++)
                {
                    var e = _enemies[k];
                    if (e == null || !e.IsAlive || e.IsDying) continue;
                    if (Vector2.Distance(e.Position, me.Position) > leash + u.AttackRange) continue;
                    float d = Vector2.Distance(e.Position, u.Position);
                    if (d < best) { best = d; target = e; }
                }

                bool inRange = target != null && best <= u.AttackRange;
                // 사거리 밖이면 적에게 다가간다(근접도 나가서 친다). 적이 없으면 메인 곁으로 돌아온다
                Vector2 goal = target != null ? target.Position : home;
                bool move = target != null ? !inRange : Vector2.Distance(u.Position, home) > 8f;
                if (move)
                {
                    u.SetState(EnemyState.Approach);
                    u.Position = SlideMove(u, u.Position, u.StepToward(goal, dt));
                    u.SetMoving(true);
                }
                else u.SetMoving(false);
                // 메인 반경을 벗어나지 않는다(PD 2026-10-29 「5m 를 벗어나지 말아야」)
                var off = u.Position - me.Position;
                if (off.magnitude > leash) u.Position = me.Position + off.normalized * leash;

                if (target != null)
                {
                    u.SetFacing(target.Position - u.Position);
                    if (inRange && u.TickAttack(dt))
                    {
                        _summonStriking = true;
                        try { PerformAttack(u, target, true); }
                        finally { _summonStriking = false; }
                    }
                }

                s.SkillCd -= dt;
                if (s.SkillCd <= 0f && target != null)
                {
                    s.SkillCd = PartyRule.SubSkillSeconds;
                    CastSubSkill(u, target);
                }
            }
        }

        private void CastSubSkill(Unit u, Unit target)
        {
            ShowSubSkillName(u);
            if (SubSafeSkills.Contains(u.Key))
            {
                CastActiveSkill(u);
                return;
            }
            // 보조 전용 「연사」 — 자기 몸에 거는 스킬 대신 가까운 적에게 세 번 몰아친다
            _summonStriking = true;
            try { for (int i = 0; i < 3; i++) if (target.IsAlive) PerformAttack(u, target, true); }
            finally { _summonStriking = false; }
        }

        private void ShowSubSkillName(Unit u)
        {
            var e = _player != null ? _player.GetHost(u.Key) : null;
            string skill = SubSafeSkills.Contains(u.Key)
                ? (e != null && !string.IsNullOrEmpty(e.ActiveSkillKey) ? Localize.Get($"askill.{e.ActiveSkillKey}.name") : u.Key)
                : "연사";
            var t = RentDamageText();
            if (t != null) t.Show(u.Position + new Vector2(0f, 70f), skill, new Color(0.85f, 0.76f, 1f));
        }

        /// <summary>
        /// 지금 파티(메인 · 보조)에 있는 호스트 — 같은 호스트는 두 번 빙의하지 못한다(PD 2026-10-29 「똑같은 캐릭터 여러 마리는 아니야」).
        /// </summary>
        private void SyncPartyKeys()
        {
            var set = Unit.PartyKeys;
            set.Clear();
            if (!PartyOn) return;
            if (_host != null && _host.Key != null) set.Add(_host.Key);
            for (int i = 0; i < _subs.Count; i++) if (_subs[i].U != null && _subs[i].U.Key != null) set.Add(_subs[i].U.Key);
        }

        /// <summary>깊이 정렬 — 보조도 줄에 세운다.</summary>
        private void AddPartyDepth()
        {
            for (int i = 0; i < _subs.Count; i++)
                if (_subs[i].U != null && _subs[i].U.transform.parent == _unitLayer) AddDepth(_subs[i].U);
        }

        /// <summary>선택창이 떠 있는 동안 — 전투는 서고 사망 선택의 시계만 돈다.</summary>
        private void TickPartyHold()
        {
            if (_partyTimer < 0f) return;
            _partyTimer -= Time.unscaledDeltaTime;
            if (_partyTimerText != null) _partyTimerText.text = $"남은 시간 {Mathf.Max(0f, _partyTimer):0.0}초";
            if (_partyTimer > 0f) return;
            ClosePartyPopup();
            _ghostHp = 0;
            PublishHp();
            Finish(false);
        }

        // ─────────────────────────────────────────
        // 선택창 화면 — 임시(코드로 세움). 시안이 나오면 발주해 갈아 끼운다
        // ─────────────────────────────────────────

        private readonly struct PartyOption
        {
            public readonly string Title;
            public readonly string Detail;
            public readonly bool Enabled;
            public readonly UnityAction Act;
            public PartyOption(string title, string detail, bool enabled, UnityAction act)
            { Title = title; Detail = detail; Enabled = enabled; Act = act; }
        }

        private TextMeshProUGUI _partyTimerText;

        private void ShowPartyPopup(string title, string sub, List<PartyOption> opts, float timer)
        {
            ClosePartyPopup();
            var canvas = _unitLayer != null ? _unitLayer.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            var root = canvas.rootCanvas.transform;

            var go = new GameObject("PartyChoicePopup", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            const float W = 640f;
            float h = 150f + opts.Count * 122f + (timer > 0f ? 40f : 0f);
            var panel = NewRect("Panel", rt, new Vector2(W, h));
            var pImg = panel.gameObject.AddComponent<Image>();
            pImg.color = new Color(0.106f, 0.106f, 0.153f, 0.98f);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.78f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            float y = h * 0.5f - 44f;
            NewText(panel, title, 30, Color.white, new Vector2(0f, y), new Vector2(W - 40f, 44f), true);
            y -= 42f;
            NewText(panel, sub, 20, new Color(0.78f, 0.78f, 0.85f), new Vector2(0f, y), new Vector2(W - 40f, 30f), false);
            y -= 30f;
            _partyTimerText = null;
            if (timer > 0f)
            {
                _partyTimerText = NewText(panel, "", 24, new Color(1f, 0.54f, 0.42f), new Vector2(0f, y - 6f), new Vector2(W - 40f, 34f), true);
                y -= 40f;
            }
            y -= 14f;

            for (int i = 0; i < opts.Count; i++)
            {
                var o = opts[i];
                var b = NewRect($"Option{i + 1}", panel, new Vector2(W - 48f, 110f));
                b.anchoredPosition = new Vector2(0f, y - 55f);
                var img = b.gameObject.AddComponent<Image>();
                bool gold = o.Enabled && o.Act != null && i > 0 && o.Title.StartsWith("보조");
                img.color = !o.Enabled ? new Color(0.08f, 0.08f, 0.08f) : gold ? new Color(0.23f, 0.19f, 0.09f) : new Color(0.145f, 0.145f, 0.2f);
                var btn = b.gameObject.AddComponent<Button>();
                btn.interactable = o.Enabled;
                var act = o.Act;
                btn.onClick.AddListener(() =>
                {
                    ClosePartyPopup();
                    act?.Invoke();
                });
                var tc = !o.Enabled ? new Color(0.35f, 0.35f, 0.35f) : gold ? new Color(1f, 0.84f, 0.42f) : Color.white;
                var t1 = NewText(b, o.Title, 24, tc, new Vector2(0f, 20f), new Vector2(W - 88f, 36f), true);
                t1.alignment = TextAlignmentOptions.MidlineLeft;
                var t2 = NewText(b, o.Detail, 17, new Color(0.67f, 0.67f, 0.73f), new Vector2(0f, -22f), new Vector2(W - 88f, 44f), false);
                t2.alignment = TextAlignmentOptions.TopLeft;
                y -= 122f;
            }

            if (CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady) lang.ApplyFonts(rt);
            _partyPopup = go;
            _partyHold = true;
            _partyTimer = timer;
            if (_partyTimerText != null) _partyTimerText.text = $"남은 시간 {timer:0.0}초";
        }

        private void ClosePartyPopup()
        {
            if (_partyPopup != null) Destroy(_partyPopup);
            _partyPopup = null;
            _partyTimerText = null;
            _partyHold = false;
            _partyTimer = -1f;
            _partyPick = null;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            return rt;
        }

        private static TextMeshProUGUI NewText(Transform parent, string s, float size, Color c, Vector2 pos, Vector2 box, bool bold)
        {
            var rt = NewRect("Text", parent, box);
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = size;
            t.raycastTarget = false;
            return t;
        }
    }
}
