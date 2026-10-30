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
        public const int SubSlots = 2;

        /// <summary>적 호스트의 체력이 이 비율 아래면 빙의 가능. 35% 는 너무 빨리 죽어 60% 로 올렸다(PD 2026-10-29).</summary>
        public const float PossessHpRatio = 0.6f;
        /// <summary>보조의 평타 · 스킬 세기(메인 대비).</summary>
        public const float SubPower = 0.3f;
        /// <summary>보조의 공격 속도(메인 대비). 0.5 = 두 번 칠 시간에 한 번.</summary>
        public const float SubRateMul = 0.5f;
        /// <summary>보조의 공격 사거리(메인 대비). 능력치는 다 낮추고 사거리만 넓힌다(PD 2026-10-29).</summary>
        public const float SubRangeMul = 0.65f;   // 1.3 → 0.65(PD 2026-10-29 「보조 사거리가 너무 길다 — 반으로 · 보조 위주로 게임이 굴러가면 안 된다」)
        /// <summary>보조 크기 배율(PD 2026-10-29 「0.8 정도」).</summary>
        public const float SubScale = 0.8f;
        /// <summary>보조가 메인에게서 벗어나지 않는 반경(m). 보면서 잡는 값(PD 2026-10-29).</summary>
        public const float LeashMeters = 2.5f;   // 5 → 2.5(PD 2026-10-29 「나랑 퍼지는 거리를 지금의 반으로」)
        /// <summary>보조끼리 · 보조와 메인 사이 최소 간격(px) — 보조는 84 × 0.8 ≈ 67 px 로 그린다(PD 「보조 1 · 2 가 겹친다」).</summary>
        public const float SubGapEach = 64f;
        public const float SubGapMain = 56f;
        /// <summary>보조 스킬 자동 발동 간격(초).</summary>
        public const float SubSkillSeconds = 6f;
        /// <summary>메인 사망 뒤 고르는 시간(초). 8 → 15(PD 2026-10-30 「선택 시간도 좀 늘려 줘」).</summary>
        public const float DeathChoiceSeconds = 15f;
    }

    public sealed partial class BattleDirector
    {
        /// <summary>이 판에 빙의 파티 규칙이 걸리는가 — 탑 · 밤 같은 모드 판은 예전 규칙 그대로.</summary>
        private bool PartyOn => PartyRule.Enabled && !IsModeRun;

        private sealed class PartySub
        {
            public Unit U;
            public float SkillCd;
            /// <summary>보조 배수를 곱하기 전 공격력(메인으로 입었을 때와 같은 식). 카드 · 시너지가 바뀌면 여기서 다시 곱한다.</summary>
            public int BaseAtk;
        }

        /// <summary>보조 하나를 세운다 — 체력 없음 · 공격 30% · 공격 속도 50% · 사거리 1.3배 · 보랏빛 · 발밑 고리.</summary>
        private PartySub MakeSub(string key, Vector2 at)
        {
            var entry = _player != null ? _player.GetHost(key) : null;
            var u = NewUnit($"PartySub_{key}");
            // 메인으로 입었을 때와 같은 식(로비 강화 · 성급 포함)이 바탕 — 기본값의 30% 면 메인보다 훨씬 약해진다
            int baseAtk = Mathf.Max(1, Mathf.RoundToInt(LeveledAtk(entry) * UpgradeMul(key, HostStat.Atk) * MasteryStatMulOf(key)));
            int atk = Mathf.Max(1, Mathf.RoundToInt(baseAtk * SubPowerOf(key)));
            u.Setup(UnitSide.Player, key, entry != null ? entry.DisplayName : key, UnitGet(key),
                    9999, atk, HostSpeedOf(entry) * 1.15f, HostRangeOf(entry) * PartyRule.SubRangeMul,
                    HostIntervalOf(entry) / PartyRule.SubRateMul,
                    UnitBox(84f, 80f), isBoss: false, profile: entry);
            u.Position = at;
            u.transform.localScale = Vector3.one * PartyRule.SubScale;   // 메인과 한눈에 갈리게 작게(PD 2026-10-29)
            u.SetState(EnemyState.Idle);
            ApplyFacingSprites(u, key);
            u.SetHpBarVisible(false);   // 보조는 체력이 없다 — 적이 노리지 않아 맞지도 않는다
            u.MarkPartySub(GetSprite("party_sub_ring"));   // 붙들어 둔 혼 — 비치는 보랏빛 · 숨쉬기 · 발밑 혼령 고리
            return new PartySub { U = u, SkillCd = Random.Range(1.5f, 3f), BaseAtk = baseAtk };
        }

        private readonly List<PartySub> _subs = new();
        /// <summary>보조가 액티브를 자동으로 쓰는 동안 그 보조(스킬 레벨 · 배율이 보조 호스트를 따르게).</summary>
        private Unit _subCaster;
        /// <summary>메인 사망 뒤 A(새 빙의)를 골랐다 — 다음 빙의에 이 값을 치른다.</summary>
        private int _partyPossessCost;
        private bool _partyStartCharged;
        /// <summary>선택창이 떠 있는 동안 전투를 세운다.</summary>
        private bool _partyHold;

        /// <summary>빙의 파티 창(선택 · 사망 · 설명)이 떠 있나 — 화면이 설명 창을 겹쳐 띄우지 않게 묻는다.</summary>
        public bool PartyPopupOpen => _partyHold;
        private float _partyTimer = -1f;
        private GameObject _partyPopup;
        private Unit _partyPick;

        // 보조가 자동으로 써도 되는 스킬 — 시전자 자리에서 적을 치는 것들.
        // 자기 몸을 움직이거나 메인에게 거는 스킬(도약 · 반사 · 방벽 · 결계 · 회피 · 회복)은 보조 전용 「연사」로 대신한다(보완 ④).
        private static readonly HashSet<string> SubSafeSkills = new()
        {
            "gangster", "thug", "dragoon", "salamander", "dragon_blue", "commando_grenade",
            "commando_missile", "commando_laser", "white_wizard", "medium", "robot", "ninja_chain",
            // 스킬 개편(2026-10-29)으로 적에게 닿는 액티브가 된 것 — 메인 전용 덤(쉴드 등)은 메인일 때만 붙는다
            "hopper", "commando_mg", "amazon_elite", "baseball", "guru", "snowwoman", "vampire",
            // ⚠ ninja(그림자 베기) · hopper_smg(백스텝)는 보조 몸을 옮긴다 — 대열 코드와 깜빡이는지 판에서 본 뒤 넣는다
        };

        // ─────────────────────────────────────────
        // 호스트 자리 — 랜덤
        // ─────────────────────────────────────────

        /// <summary>빙의할 수 있는 호스트인가 — 유령 · 히든(사신) · 빙의 불가 몸은 뺀다.</summary>
        private static bool PartyPickable(HostEntry h)
            => h != null && !h.IsGhost && !Game.User.PlayerDataService.IsHiddenHost(h.HostKey)
               && h.PossessKind != PossessKind.NotPossessable;

        /// <summary>판마다 랜덤 호스트 자리 후보로 미리 올리는 몸 수(인연 짝 · 이 길에 나오는 호스트는 따로 늘 올린다).</summary>
        private const int PartyPreloadCount = 10;

        /// <summary>
        /// 판 시작에 올릴 그림 — 호스트 자리 랜덤 후보.
        ///
        /// ⚠ 예전에는 빙의할 수 있는 호스트를 **전부**(약 40명, 아틀라스 약 44MB) 올렸다. 인게임 크래시(메모리)의
        ///   큰 몫이라(2026-10-30) 지금 메인의 인연 짝 + 판마다 랜덤 <see cref="PartyPreloadCount"/>명만 올린다.
        ///   랜덤 뽑기(<see cref="PartyRandomHost"/>)는 원래 그림이 안 올라온 몸을 빼고 고르므로 그 안에서 뽑힌다.
        /// </summary>
        private void AddPartyUnitKeys(List<string> keys, IReadOnlyList<HostEntry> hosts)
        {
            if (!PartyOn) return;
            // 판 시작 전이라 메인(_host)이 아직 없을 수 있다 — 로비에서 고른 호스트의 짝을 본다
            var start = _host != null ? _host.Key : PickPlayerHost()?.HostKey;
            var inyeon = start != null ? InyeonOf(start) : null;
            _partyPool.Clear();
            for (int i = 0; i < hosts.Count; i++)
            {
                var h = hosts[i];
                if (!PartyPickable(h) || keys.Contains(h.SpriteKey)) continue;
                if (inyeon != null && System.Array.IndexOf(inyeon.Partners, h.HostKey) >= 0) keys.Add(h.SpriteKey);
                else _partyPool.Add(h);
            }
            for (int n = 0; n < PartyPreloadCount && _partyPool.Count > 0; n++)
            {
                int pick = _rng.Next(_partyPool.Count);
                if (!keys.Contains(_partyPool[pick].SpriteKey)) keys.Add(_partyPool[pick].SpriteKey);
                _partyPool.RemoveAt(pick);
            }
            _partyPool.Clear();
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
                if (here) continue;
                _partyPool.Add(h);
                // 지금 메인의 인연 짝은 한 번 더 넣는다 = 약 2배(PD 2026-10-29 「등장 확률을 올려 줘야지」)
                if (IsMainPartner(h.HostKey)) _partyPool.Add(h);
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
            ClearPartyFx();
            _partyPossessCost = 0;
            _partyStartCharged = false;
            System.Array.Clear(_slotLv, 0, _slotLv.Length);
            _subPowCard = 0f;
            _syn = default;
            _synLines.Clear();
            _synCounted = false;   // 다음 프레임에 다시 센다
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
            // 교체 횟수 제한은 없다 — 유령 HP 비용이 곧 제한이다(PD 2026-10-30 「HP 로 제한되니 그런 규칙은 필요 없다」)
            if (_ghostHp > PartyRule.CostMainSwap) return true;
            if (_subs.Count < PartyRule.SubSlots) return _ghostHp > PartyRule.CostSub;
            return _ghostHp > PartyRule.CostSubSwap;
        }

        // ─────────────────────────────────────────
        // 터치 — 빙의 가능한 적을 직접 누른다(InGameMainUI 패드보다 먼저)
        // ─────────────────────────────────────────

        /// <summary>
        /// 화면을 누른 자리에 빙의할 몸이 있으면 처리하고 true. 없으면 false — 패드가 이어서 받는다.
        /// </summary>
        public bool TryPartyTap(Vector2 screen, Camera cam)
        {
            if (!PartyOn || !_running || _partyHold || _awaitingBuff || IsChanneling || PartyLinking || FailSequenceRunning) return false;
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
            if (_trio.Count > 0) { if (best != null && _trio.Contains(best)) PickTrio(best); return true; }   // 유령 출전 셋 고르기
            if (_coachTarget != null) { if (CoachTap(best)) return true; }   // 따라 하기 — 짚은 적만 받는다
            else if (best == null) return false;

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

        // 선택창은 BattleDirector.PartyPopup(시안 F_choice 그대로)

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
            // 지금 메인은 사라진다 — 쓰러지는 몸이 아니라 놓아준 몸이라 조각 · 사망 처리는 하지 않는다
            var old = _host;
            _ghost.Position = old.Position;
            _host = null;
            BeginRelease(old);   // 해골로 쓰러지지 않는다 — 원작 빙의 자세로 옅어지며 흩어짐
            Unit.PlayerHasHost = false;
            _possessTarget = target;
            _repossessLock = 0f;
            TryPossess();
        }

        private void PartySetSub(Unit target, int slot, int cost)
        {
            if (_host == null || !TakeFromEnemies(target)) return;
            if (target == _tutorialSub) _tutorialSubKey = null;   // 따라 하기 보조를 데려갔다 — 다시 세우지 않는다
            PartySpend(cost, target.Position);

            // 보조 교체 — 옛 보조는 놓아주고, 그 자리에서 혼 조각이 새 호스트로 옮겨 간다(BattleDirector.PartyFx)
            Unit oldSub = null;
            if (slot < _subs.Count)
            {
                oldSub = _subs[slot].U;
                _subs.RemoveAt(slot);
                BeginRelease(oldSub);
            }
            BeginSubPossess(target, slot, oldSub);
        }

        // ─────────────────────────────────────────
        // 메인 사망 — 8초 안에 A · B · C
        // ─────────────────────────────────────────

        // 사망창은 BattleDirector.PartyPopup(시안 F_death 그대로)

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
            RecountSynergy();
            RecountInyeon();
            RefreshSubAtk();
            TickPartyFx(dt);
            TickPossessBubble();
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

                // 자리: 메인 왼쪽(보조 1) · 오른쪽(보조 2), 조금 아래. 몸 크기만큼 벌려 그림이 겹치지 않게(PD 2026-10-30)
                float side = i == 0 ? -1f : 1f;
                float gapMain = Mathf.Max(PartyRule.SubGapMain, me.BodyRadius + u.BodyRadius);
                var home = me.Position + new Vector2(side * Mathf.Max(78f, gapMain + 8f), -36f);
                // 혼이라 바닥에서 떠 있다 — 자리는 그대로, 그림만 둥실(둘이 엇박으로). 그림자가 실제 자리다
                u.SetSpriteLift(14f + 6f * Mathf.Sin(Time.time * 2.2f + i * 1.7f));
                u.TickPartyRing(Time.time * 3f + i * 1.3f);
                // 방이 바뀌어 멀리 떨어졌으면 곁으로 옮긴다
                if (Vector2.Distance(u.Position, me.Position) > 600f) u.Position = home;

                if (_host == null) { u.SetMoving(false); continue; }   // 메인이 없으면 싸우지 않는다

                // 고르는 적 — 메인 둘레(반경 + 내 사거리) 안에서 나에게 가장 가까운 것. 반경 밖 적은 쫓지 않는다
                float leash = PartyRule.LeashMeters * _pxPerMeter;
                // 갱스터 인연 「청부 표식」 — 표식 붙은 적을 먼저 노린다(가까운 것처럼 친다)
                bool huntMarks = _inyeonKey == "gangster";
                Unit target = null;
                float best = float.MaxValue;
                for (int k = 0; k < _enemies.Count; k++)
                {
                    var e = _enemies[k];
                    if (e == null || !e.IsAlive || e.IsDying) continue;
                    if (Vector2.Distance(e.Position, me.Position) > leash + u.AttackRange) continue;
                    float d = Vector2.Distance(e.Position, u.Position);
                    if (huntMarks && e.IsMarked) d *= 0.1f;
                    if (d < best) { best = d; target = e; }
                }
                // 이 아래 사거리 판정은 진짜 거리로 한다 — 위 점수(표식 우대)가 사거리를 늘리면 안 된다
                if (target != null) best = Vector2.Distance(target.Position, u.Position);

                bool inRange = target != null && best <= u.AttackRange;
                // 사거리 밖이면 적에게 다가간다(근접도 나가서 친다). 적이 없으면 메인 곁으로 돌아온다.
                // ⚠ 적 한가운데로 가지 않는다 — 보조 둘이 같은 적을 고르면 그 자리에 포개졌다(PD 2026-10-30 「보조끼리 겹친다」).
                //   보조 1 은 적의 왼쪽, 2 는 오른쪽 — 제 사거리 안쪽(원거리는 떨어져서, 근접은 몸이 닿는 자리)에 선다
                Vector2 goal = home;
                if (target != null)
                {
                    float reach = Mathf.Max(u.AttackRange * 0.85f, target.BodyRadius + u.BodyRadius);
                    goal = target.Position + new Vector2(side * Mathf.Min(reach, u.AttackRange), 0f);
                }
                // 반경(2.5 m)은 「그 밖으로 나가 싸우지 않는다」는 뜻이다 — 몸을 반경 끝으로 끌어당기지 않는다(PD 2026-10-30).
                //   가려는 자리만 반경 안으로 잡고, 거기까지 자연스럽게 걸어가 멈춘다
                var toGoal = goal - me.Position;
                if (toGoal.magnitude > leash) goal = me.Position + toGoal.normalized * leash;
                bool move = target != null ? !inRange && Vector2.Distance(u.Position, goal) > 8f
                                           : Vector2.Distance(u.Position, home) > 8f;
                if (move)
                {
                    u.SetState(EnemyState.Approach);
                    // 보조는 혼이라 방 물건을 무시하고 지나간다(PD 2026-10-29) — 걸리면 메인을 못 따라온다
                    // ⚠ StepToward 는 「이번 프레임에 갈 거리」를 돌려준다. 예전에 그 값을 위치로 넣어(u.Position = StepToward)
                    //   보조가 매 프레임 화면 원점으로 튀고, 반경 제한이 메인 반경 끝 한 점으로 끌어와 둘이 포개졌다(2026-10-30 실측)
                    u.MoveToward(goal, dt);
                    u.SetMoving(true);
                }
                else u.SetMoving(false);
                // 겹치지 않게 — 메인 · 다른 보조와 몸 크기만큼 밀어낸다.
                // 간격은 몸 크기로 잰다 — 고정 56 · 64 px 은 그림 폭보다 작아 밀어내도 반쯤 겹쳐 보였다.
                // 메인과는 조금씩(튀지 않게), 보조끼리는 모자란 만큼 한 번에(같은 자리로 모일 때 반만 밀면 반쯤 겹친 채 멈춘다)
                PushApart(u, me.Position, gapMain, side);
                for (int j = 0; j < _subs.Count; j++)
                {
                    var other = _subs[j].U;
                    if (j != i && other != null)
                        PushApart(u, other.Position, Mathf.Max(PartyRule.SubGapEach, u.BodyRadius + other.BodyRadius), side, 1f);
                }

                if (target != null)
                {
                    u.SetFacing(target.Position - u.Position);
                    if (inRange && u.TickAttack(dt, PartySubIntervalMul * MediumIntervalMul(u)))   // 무기 시너지 — 보조는 절반
                    {
                        _summonStriking = true;
                        try { PerformAttack(u, target, true); }
                        finally { _summonStriking = false; }
                    }
                }

                s.SkillCd -= dt;
                if (s.SkillCd <= 0f && target != null)
                {
                    s.SkillCd = SubSkillCdOf(u.Key);
                    CastSubSkill(u, target);
                }
            }
        }

        /// <summary>
        /// u 를 from 에서 gap 만큼 떨어지게 민다 — 기본은 모자란 거리의 60% 씩(다음 프레임에 마저 민다, 튀지 않게).
        /// 딱 같은 자리면 방향이 없으니 보조 1 은 왼쪽 · 2 는 오른쪽으로.
        /// </summary>
        private static void PushApart(Unit u, Vector2 from, float gap, float side, float share = 0.6f)
        {
            var d = u.Position - from;
            float m = d.magnitude;
            if (m >= gap) return;
            var dir = m < 0.5f ? new Vector2(side, 0f) : d / m;
            u.Position += dir * ((gap - m) * share);
        }

        private void CastSubSkill(Unit u, Unit target)
        {
            ShowSubSkillName(u);
            if (SubSafeSkills.Contains(u.Key))
            {
                // 보조는 자기 호스트의 액티브 레벨로 쓴다(메인 레벨을 빌리지 않는다 — 육성 개편 2026-10-29)
                _subCaster = u;
                try { CastActiveSkill(u); }
                finally { _subCaster = null; }
                return;
            }
            // 보조 전용 「연사」 — 자기 몸에 거는 스킬 대신 가까운 적에게 세 번 몰아친다
            _summonStriking = true;
            try { for (int i = 0; i < 3; i++) if (target.IsAlive) PerformAttack(u, target, true); }
            finally { _summonStriking = false; }
        }

        private void ShowSubSkillName(Unit u)
        {
            // 글자 대신 스킬 아이콘(PD 2026-10-30 「미니 골렘 · 빛의 인력 글자 대신 스킬 아이콘을 깔끔하게」) —
            // 적 스킬과 같은 표시: 몸이 짧게 번쩍이고 머리 위에 그 스킬 아이콘. 아이콘이 없는 보조 전용 연사만 글자로 둔다
            if (SubSafeSkills.Contains(u.Key) && CachedSkillIcon(u.Key) is Sprite icon)
            {
                u.ShowSkillCast(icon);
                return;
            }
            var e = _player != null ? _player.GetHost(u.Key) : null;
            string skill = SubSafeSkills.Contains(u.Key)
                ? (e != null && !string.IsNullOrEmpty(e.ActiveSkillKey) ? Localize.Get($"askill.{e.ActiveSkillKey}.name") : u.Key)
                : Tx("ui.party.sub_strike");
            var t = RentDamageText();
            if (t != null) t.ShowMinor(u.Position + new Vector2(0f, 70f), skill, new Color(0.85f, 0.76f, 1f));   // 데미지 숫자를 덮지 않게 작게
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
            if (_partyTimerText != null) _partyTimerText.text = Tf("ui.party.p2.seconds", Mathf.Max(0f, _partyTimer));
            if (_partyTimerFill != null) _partyTimerFill.fillAmount = Mathf.Clamp01(_partyTimer / Mathf.Max(0.01f, _partyTimerTotal));
            if (_partyTimer > 0f) return;
            // 시간이 다 돼도 판을 끝내지 않는다(PD 2026-10-30) — 보조가 있으면 보조 1 을 메인으로(고를 때와 같은 값),
            // 없거나 유령 체력이 모자라면 유령으로 계속 간다(다음 빙의 · 유령 체력 규칙대로)
            var auto = _deathAutoAct;
            _deathAutoAct = null;
            ClosePartyPopup();
            auto?.Invoke();
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
            /// <summary>추천 줄(금빛 · 왕관) — 보조에 넣기 · 바꾸기. 예전엔 제목이 「보조」로 시작하는지로 갈랐다(번역하면 깨진다).</summary>
            public readonly bool Best;
            public PartyOption(string title, string detail, bool enabled, UnityAction act, bool best = false)
            { Title = title; Detail = detail; Enabled = enabled; Act = act; Best = best; }
        }

        // 파티 화면 글자는 전부 언어팩 `ui.party.*`(5언어) — 숫자 · 이름이 끼면 Format
        private static string Tx(string key) => Localize.Get(key);
        private static string Tf(string key, params object[] args) => Localize.Format(key, args);

        private TextMeshProUGUI _partyTimerText;
        private Image _partyTimerFill;
        private float _partyTimerTotal = 1f;

        /// <summary>부품 그림을 9분할로 깐다. 그림이 없으면(아틀라스가 아직 안 올라옴) 예전 단색으로 둔다.</summary>
        private void Sliced(Image img, string sprite, Color fallback)
        {
            var sp = GetSprite(sprite);
            img.sprite = sp;
            img.type = sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = sp != null ? Color.white : fallback;
        }

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
            // 창 판 · 줄 · 시계 테는 발주 부품(UI-PARTY-09~11, 시안 mock_partyui_F_choice · F_death) — 가운데에 띄운다(PD 「빙의 선택창은 가운데」)
            var panel = NewRect("Panel", rt, new Vector2(W, h));
            Sliced(panel.gameObject.AddComponent<Image>(), "party_popup_panel", new Color(0.106f, 0.106f, 0.153f, 0.98f));

            float y = h * 0.5f - 44f;
            // 시계가 있으면 제목은 시계 테(오른쪽 위 64)를 비켜 왼쪽으로 · 부제는 두 줄까지(2026-10-29 판 검수 — 제목이 창 밖으로 나갔다)
            float titleW = timer > 0f ? W - 150f : W - 48f;
            float titleX = timer > 0f ? -40f : 0f;
            NewText(panel, title, 26, Color.white, new Vector2(titleX, y), new Vector2(titleW, 44f), true);
            y -= 44f;
            NewText(panel, sub, 17, new Color(0.78f, 0.78f, 0.85f), new Vector2(0f, y - 4f), new Vector2(W - 48f, 40f), false);
            y -= 40f;
            _partyTimerText = null;
            if (timer > 0f)
            {
                _partyTimerText = NewText(panel, "", 24, new Color(1f, 0.54f, 0.42f), new Vector2(0f, y - 6f), new Vector2(W - 40f, 34f), true);
                // 시계 테 + 줄어드는 채움(코드가 돌린다)
                var ring = NewRect("TimerRing", panel, new Vector2(64f, 64f));
                ring.anchoredPosition = new Vector2(W * 0.5f - 52f, h * 0.5f - 52f);
                var ringImg = ring.gameObject.AddComponent<Image>();
                ringImg.sprite = GetSprite("party_timer_ring");
                ringImg.raycastTarget = false;
                var fill = NewRect("TimerFill", ring, new Vector2(50f, 50f));
                _partyTimerFill = fill.gameObject.AddComponent<Image>();
                _partyTimerFill.sprite = GetSprite("party_timer_ring");
                _partyTimerFill.type = Image.Type.Filled;
                _partyTimerFill.fillMethod = Image.FillMethod.Radial360;
                _partyTimerFill.fillOrigin = (int)Image.Origin360.Top;
                _partyTimerFill.color = new Color(1f, 0.54f, 0.42f, 0.85f);
                _partyTimerFill.raycastTarget = false;
                _partyTimerTotal = timer;
                y -= 40f;
            }
            y -= 14f;

            for (int i = 0; i < opts.Count; i++)
            {
                var o = opts[i];
                var b = NewRect($"Option{i + 1}", panel, new Vector2(W - 48f, 110f));
                b.anchoredPosition = new Vector2(0f, y - 55f);
                var img = b.gameObject.AddComponent<Image>();
                bool gold = o.Enabled && o.Act != null && i > 0 && o.Best;
                Sliced(img, !o.Enabled ? "party_row_off" : gold ? "party_row_best" : "party_row",
                       !o.Enabled ? new Color(0.08f, 0.08f, 0.08f) : gold ? new Color(0.23f, 0.19f, 0.09f) : new Color(0.145f, 0.145f, 0.2f));
                var btn = b.gameObject.AddComponent<Button>();
                btn.interactable = o.Enabled;
                var act = o.Act;
                btn.onClick.AddListener(() =>
                {
                    ClosePartyPopup();
                    act?.Invoke();
                });
                var tc = !o.Enabled ? new Color(0.35f, 0.35f, 0.35f) : gold ? new Color(1f, 0.84f, 0.42f) : Color.white;
                // 추천 줄은 왼쪽에 왕관 꼬리표, 못 고름 줄은 오른쪽에 자물쇠가 그려져 있다 — 그쪽 40 을 비운다
                float inset = gold ? 40f : !o.Enabled ? -40f : 0f;
                float rowW = W - 88f - Mathf.Abs(inset);
                var t1 = NewText(b, o.Title, 22, tc, new Vector2(inset * 0.5f, 20f), new Vector2(rowW, 36f), true);
                t1.alignment = TextAlignmentOptions.MidlineLeft;
                var t2 = NewText(b, o.Detail, 16, new Color(0.67f, 0.67f, 0.73f), new Vector2(inset * 0.5f, -22f), new Vector2(rowW, 44f), false);
                t2.alignment = TextAlignmentOptions.TopLeft;
                y -= 122f;
            }

            if (CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady) lang.ApplyFonts(rt);
            _partyPopup = go;
            _partyHold = true;
            _partyTimer = timer;
            if (_partyTimerText != null) _partyTimerText.text = Tf("ui.party.timer", timer);
        }

        private void ClosePartyPopup()
        {
            if (_partyPopup != null) Destroy(_partyPopup);
            _partyPopup = null;
            _partyTimerText = null;
            _partyTimerFill = null;
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
            t.fontSizeMin = 10f;
            t.fontSizeMax = size;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
