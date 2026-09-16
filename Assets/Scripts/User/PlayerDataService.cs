using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Events;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.User
{
    /// <summary>
    /// 유저 데이터의 단일 소유자. 다른 코드는 `UserData` 를 직접 들고 있지 않는다.
    /// 모든 변경이 여기를 지나므로 이벤트 발행과 저장 시점을 한 곳에서 통제할 수 있다.
    /// </summary>
    public sealed class PlayerDataService : IPlayerDataService
    {
        private readonly IUserDataRepository _repo;
        private readonly IEventBus _bus;
        private readonly HostTable _hosts;
        private readonly ActiveSkillTable _activeSkills;
        private readonly PassiveSkillTable _passiveSkills;

        /// <summary>
        /// 성장 수치의 단일 출처. **파편 곡선·등급 배수·드롭·고스트 상한이 전부 여기 있다.**
        /// 없으면(부트 초기 단계) 안전한 기본값으로 떨어진다.
        /// </summary>
        private readonly GameConfig _config;

        private UserData _data;

        public PlayerDataService(IUserDataRepository repo, IEventBus bus,
                                 HostTable hosts, ActiveSkillTable activeSkills,
                                 PassiveSkillTable passiveSkills = null,
                                 GameConfig config = null)
        {
            _repo = repo;
            _bus = bus;
            _hosts = hosts;
            _activeSkills = activeSkills;
            _passiveSkills = passiveSkills;
            _config = config;
        }

        public bool IsReady => _data != null;

        public int Stamina    => _data?.stamina    ?? 0;
        public int StaminaMax => _data?.staminaMax ?? 0;
        public int Gold       => _data?.gold       ?? 0;
        public int Gem        => _data?.gem        ?? 0;
        public int SpiritCore => _data?.spiritCore ?? 0;
        public int HostMemory => _data?.hostMemory ?? 0;

        public int GhostLevel  => _data?.ghostLevel  ?? 1;
        public int GhostExp    => _data?.ghostExp    ?? 0;
        public int GhostExpMax => _data?.ghostExpMax ?? 100;

        public int CurrentChapter => _data?.currentChapter ?? 1;
        public int ReachedStage   => _data?.reachedStage   ?? 1;
        public int ClearedChapter => _data?.clearedChapter ?? 0;

        /// <summary>
        /// 전투가 세우는 몸 전부. ⚠ **숨긴 몸(`IsHiddenHost`)은 뺀다** — 목록에도 적으로도 안 나온다.
        /// </summary>
        public IReadOnlyList<HostEntry> AllHosts
        {
            get
            {
                if (_hosts == null) return System.Array.Empty<HostEntry>();
                if (_visible != null) return _visible;
                var all = _hosts.Entries;
                _visible = new List<HostEntry>(all.Count);
                for (int i = 0; i < all.Count; i++)
                    if (!IsHiddenHost(all[i].HostKey)) _visible.Add(all[i]);
                return _visible;
            }
        }

        private List<HostEntry> _visible;

        /// <summary>
        /// 당분간 게임에서 빼 둔 몸(기획 2026-09-15 — 아마존 정예, 나중에 다시 넣는다).
        /// 표는 그대로 두고 여기서만 거른다 — 임포터가 표를 다시 써도 빠진 채로 남는다.
        /// </summary>
        public static bool IsHiddenHost(string hostKey) => hostKey == "amazon_elite";

        /// <summary>
        /// 호스트 선택 화면에 내보낼 몸. 방패병·센서드론·엘리트처럼 정본이 배우로만 쓰는 행은 뺀다.
        /// 전투는 그 배우들도 세워야 하므로 <see cref="AllHosts"/> 는 전부 그대로 준다.
        /// </summary>
        public IReadOnlyList<HostEntry> PlayableHosts
        {
            get
            {
                if (_hosts == null) return System.Array.Empty<HostEntry>();
                if (_playable != null) return _playable;

                var all = _hosts.Entries;
                _playable = new List<HostEntry>(all.Count + 1);
                // 유령이 **맨 앞**에 선다. 몸 없이 들어가는 것도 하나의 선택이라
                // 고르는 자리가 같아야 한다 — 버튼을 따로 두면 규칙이 둘이 된다.
                _playable.Add(HostEntry.CreateGhost());
                for (int i = 0; i < all.Count; i++)
                    if (!all[i].ActorOnly && !IsHiddenHost(all[i].HostKey)) _playable.Add(all[i]);
                return _playable;
            }
        }

        private List<HostEntry> _playable;

        public string SelectedHostId
        {
            get
            {
                if (_data != null && !string.IsNullOrEmpty(_data.selectedHostId))
                {
                    // 저장된 선택이 아직 잠겨 있으면 시작 보유 호스트로 되돌린다
                    var e = GetHost(_data.selectedHostId);
                    if (e != null && IsHostUnlocked(e)) return _data.selectedHostId;
                }
                // ⚠ 되돌아갈 곳은 **유령**이다. 예전에는 표의 `Owned` 첫 칸으로 갔는데,
                //   `Owned` 인 몸이 하나도 없어지자 그냥 표의 0번(갱스터)이 나왔다 —
                //   처음 시작에 잠긴 몸이 선택된 채로 카드가 열렸다.
                //   유령은 조건 없이 언제나 고를 수 있으므로 여기가 유일한 안전한 바닥이다.
                return HostEntry.GhostKey;
            }
        }

        public async UniTask LoadAsync()
        {
            _data = await _repo.LoadAsync();
            _data.NormalizeChests(ChestSlotCount);   // 옛 저장(v2)은 상자 배열이 비어 있다
            UnsealUnlocked();
            _bus?.Publish(new UserDataReadyEvent { LoadedGhostLevel = _data.ghostLevel });
            PublishCurrency();
        }

        public UniTask SaveAsync()
            => _data == null ? UniTask.CompletedTask : _repo.SaveAsync(_data).AsUniTask();

        /// <summary>
        /// 해금 조건을 무시하고 전부 열어 둔다 — **테스트용**이다.
        /// 21종을 다 만져 봐야 밸런스를 판단할 수 있는데, 정상 진행으로는
        /// 챕터를 깨야 열려서 확인에 며칠이 걸린다.
        ///
        /// ⚠ 출시 전에 반드시 false 로 되돌린다. 켜 두면 해금이라는 성장 축이 통째로 사라진다.
        /// </summary>
        // `const` 로 두면 컴파일러가 아래 분기를 통째로 죽은 코드로 판정해
        // CS0162 경고가 뜬다. 끄고 켜는 시험용 스위치이므로 static readonly 로 둔다.
        public static readonly bool UnlockAllForTest = true;   // ⚠ 임시 (2026-09-09) — 출시 전 false

        /// <summary>
        /// 이 몸을 **쓸 수 있는가.** 진행도로 평가하며 저장하지 않는다.
        ///
        /// ⚠ 숙련도와 **무관하다.** 봉인(숙련도 0)은 **스킬만** 잠근다 —
        ///   그 몸 자체는 로비에서 고르고 전장에서 빼앗아 쓸 수 있다(평타뿐).
        ///   여기에 숙련도를 걸면 봉인된 적만 있는 방에서 손쓸 도리가 없어진다.
        ///   스킬 잠금은 <see cref="IsSkillSealed"/> 가 따로 본다.
        /// </summary>
        public bool IsHostUnlocked(HostEntry host)
        {
            if (host == null) return false;
            if (host.IsGhost) return true;   // 유령은 언제나 고를 수 있다
            if (UnlockAllForTest) return true;
            return host.UnlockType switch
            {
                HostUnlockType.Owned => true,
                HostUnlockType.StageReach =>
                    ClearedChapter >= host.UnlockChapter ||
                    (CurrentChapter == host.UnlockChapter && ReachedStage >= host.UnlockStage) ||
                    CurrentChapter > host.UnlockChapter,
                HostUnlockType.ChapterBossClear => ClearedChapter >= host.UnlockChapter,
                _ => false,
            };
        }

        /// <summary>
        /// 이 몸의 **스킬이 봉인돼 있는가** (숙련도 0).
        ///
        /// 봉인이면 액티브·패시브가 둘 다 안 나오고 평타만 쓴다.
        /// 몸을 쓰는 것 자체는 막지 않는다 — 파편은 그 몸을 써야 모이므로,
        /// 막으면 열 방법이 사라진다.
        /// </summary>
        public bool IsSkillSealed(string hostKey) => GetMastery(hostKey) < 1;

        /// <summary>
        /// **해금된 몸의 봉인을 푼다** (숙련도 0 → 1).
        ///
        /// 해금과 봉인은 원래 다른 축이었다 — 몸을 얻은 뒤 파편으로 스킬을 따로 열게.
        /// 그런데 첫 몸을 손에 쥔 플레이어가 평타만 치게 되어, 액티브 스킬이라는 것이
        /// 있는 줄도 모른 채 몇 판을 보냈다. 그래서 **얻은 몸은 스킬까지 온다** 로 바꿨다.
        /// 파편은 이제 해제가 아니라 **Lv1 → Lv10 강화**에만 쓴다.
        ///
        /// ⚠ 파생값으로 만들지 않고 **저장값을 올린다.** `GetMastery` 가 계산으로
        ///   1 을 돌려주면, 파편을 처음 쓸 때 저장값이 0 → 1 이 되어 화면상 Lv 이
        ///   그대로인 채 파편만 사라진다. 한 단계가 조용히 증발한다.
        ///
        /// 진행도가 오를 때마다 다시 돌므로 새로 열린 몸도 자동으로 따라온다.
        /// </summary>
        private void UnsealUnlocked()
        {
            if (_data == null || _hosts == null) return;
            var all = _hosts.Entries;
            if (all == null) return;

            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || e.IsGhost || !IsHostUnlocked(e)) continue;
                int at = EnsureHost(e.HostKey);
                if (at >= 0 && _data.hostMastery[at] < 1) _data.hostMastery[at] = 1;
            }
        }

        // ── 숙련도 · 파편 ────────────────────────────────────────
        //
        // 두 배열은 `hostKeys` 와 첨자를 공유한다. JsonUtility 가 Dictionary 를
        // 직렬화하지 못해 나란한 배열로 둔다.

        // ⚠ 수치는 **전부 `GameConfig` 에 있다.** 여기에는 규칙만 둔다 —
        //   예전에 코드 상수로 두었던 탓에 숫자 하나 바꾸려면 컴파일을 다시 해야 했다.

        public int MasteryMax => _config != null ? _config.MasteryMax : 10;

        /// <summary>
        /// 이 몸의 **다음 단계**에 드는 파편.
        ///
        /// Lv0 이면 봉인 해제 값이다 — 해제와 레벨업은 같은 동작이라 표도 하나다.
        /// 등급 배수(B/A/S)가 여기서 곱해진다.
        /// </summary>
        public int MasteryCost(string hostKey)
        {
            int lv = GetMastery(hostKey);
            if (_config == null || lv < 0 || lv >= MasteryMax) return 0;
            var host = GetHost(hostKey);
            float mul = host != null ? _config.GradeMultiplier(host.Grade) : 1f;
            return Mathf.RoundToInt(_config.ShardCurveAt(lv) * mul);
        }

        private int IndexOfHost(string hostKey)
        {
            if (_data == null || _data.hostKeys == null || string.IsNullOrEmpty(hostKey)) return -1;
            for (int i = 0; i < _data.hostKeys.Length; i++)
                if (_data.hostKeys[i] == hostKey) return i;
            return -1;
        }

        /// <summary>없으면 자리를 만들어 첨자를 돌려준다. 세 배열이 항상 같은 길이다.</summary>
        private int EnsureHost(string hostKey)
        {
            int i = IndexOfHost(hostKey);
            if (i >= 0 || _data == null || string.IsNullOrEmpty(hostKey)) return i;

            int n = _data.hostKeys.Length;
            Array.Resize(ref _data.hostKeys, n + 1);
            Array.Resize(ref _data.hostMastery, n + 1);
            Array.Resize(ref _data.hostShards, n + 1);
            _data.hostKeys[n] = hostKey;
            _data.hostMastery[n] = 0;
            _data.hostShards[n] = 0;
            return n;
        }

        public int GetMastery(string hostKey)
        {
            int i = IndexOfHost(hostKey);
            return i < 0 ? 0 : _data.hostMastery[i];
        }

        public int GetShards(string hostKey)
        {
            int i = IndexOfHost(hostKey);
            return i < 0 ? 0 : _data.hostShards[i];
        }

        /// <summary>
        /// 그 몸을 잡거나 잃었을 때 나오는 파편 수.
        ///
        /// ⚠ **잃었을 때가 더 많다.** "죽이면만 나온다" 가 되면 파밍이 빙의를 벌줘서
        ///   플레이어가 이 게임의 핵심 재미를 스스로 피한다.
        /// </summary>
        public int ShardDropFor(string hostKey, bool lostWhilePossessing)
        {
            if (_config == null) return lostWhilePossessing ? 3 : 1;
            var d = _config.DropAt(DropTierOf(hostKey));
            return lostWhilePossessing ? d.y : d.x;
        }

        /// <summary>
        /// 드롭 단계.
        ///
        /// **등장 빈도에서 나와야 한다** — 자주 나오는 몸은 파편이 잘 모이고
        /// 드문 몸은 안 모인다. 그런데 방별 몬스터 배치가 아직 안 끝났다.
        /// 배치가 끝나면 **이 한 곳만** 고치면 된다. 부르는 쪽은 안 바뀐다.
        /// </summary>
        private static int DropTierOf(string hostKey) => 0;

        public void AddShards(string hostKey, int amount)
        {
            if (amount <= 0) return;
            int i = EnsureHost(hostKey);
            if (i < 0) return;
            _data.hostShards[i] += amount;
        }

        /// <summary>
        /// 파편을 쓰고 숙련도를 한 단계 올린다.
        ///
        /// **봉인 해제(0 → 1)와 레벨업이 같은 동작이다.** 값만 다르다 —
        /// 두 갈래로 나누면 "해제했는데 Lv 0" 같은 어긋난 상태가 생긴다.
        /// </summary>
        public bool SpendShards(string hostKey, int cost)
        {
            int i = IndexOfHost(hostKey);
            if (i < 0 || cost <= 0) return false;
            if (_data.hostShards[i] < cost) return false;
            if (_data.hostMastery[i] >= MasteryMax) return false;

            _data.hostShards[i] -= cost;
            _data.hostMastery[i]++;
            return true;
        }

        /// <summary>
        /// 이번 판을 **유령으로** 시작하는가.
        ///
        /// 저장하지 않는다 — 판마다 고르는 것이지 계정에 남는 설정이 아니다.
        /// 로비가 켜고, 전투가 읽고, 판이 끝나면 잊는다.
        /// </summary>
        public bool StartAsGhost { get; set; }

        // ── 호스트를 데려오는 값 ────────────────────────────────
        //
        // 로비에서 몸을 골라 들어가면 골드를 낸다. **안 고르면 유령으로 공짜**다.
        //
        // 값이 붙는 이유는 "몸"이 아니라 **선택권**이다 —
        // 유령으로 들어가면 1번 방에 있는 것을 받아야 하지만,
        // 사서 들어가면 **내가 키운 몸**(숙련도 올린 스킬까지)으로 시작한다.
        //
        // ⚠ 숙련도에 비례시키지 않는다. 키운 몸일수록 비싸지면 공들인 쪽이
        //   벌을 받아 진입 장벽만 높아진다. **등급 셋으로만 가른다.**
        /// <summary>
        /// ⚠ **임시로 전부 0 골드다** (2026-09-09). 21종을 다 만져 보려면 값이 걸림돌이라
        ///   `UnlockAllForTest` 와 짝으로 열어 두었다. 출시 전 아래 원래 표로 되돌린다.
        ///
        ///   원래 값 — S 1000 · A 600 · 그 외 300
        /// </summary>
        public static int HostEntryCost(HostGrade grade)
        {
            if (FreeHostsForTest) return 0;
            return grade switch
            {
                HostGrade.S => 1000,
                HostGrade.A => 600,
                _           => 300,
            };
        }

        /// <summary>⚠ 임시 (2026-09-09) — 몸 값을 0 으로. 출시 전 false 로 되돌린다.</summary>
        public static readonly bool FreeHostsForTest = true;

        /// <summary>이 칸을 데려가는 값. **유령은 공짜다** — 몸이 아니다.</summary>
        public static int EntryCostOf(HostEntry host)
            => host == null || host.IsGhost ? 0 : HostEntryCost(host.Grade);

        /// <summary>이 몸을 데려갈 골드가 있는가. 유령은 언제나 true.</summary>
        public bool CanAffordHost(HostEntry host)
            => host != null && _data != null && _data.gold >= EntryCostOf(host);

        /// <summary>
        /// 몸값을 치른다. 판을 시작하는 순간 한 번만 부른다.
        /// 모자라면 아무 일도 없다 — 부르는 쪽이 유령 시작으로 돌린다.
        /// </summary>
        public bool PayHostEntry(HostEntry host)
        {
            if (!CanAffordHost(host)) return false;
            int cost = EntryCostOf(host);
            if (cost <= 0) return true;      // 유령 — 낼 것이 없다
            _data.gold -= cost;
            PublishCurrency();
            return true;
        }

        // ── 고스트 Lv — 골드로 산다 ─────────────────────────────
        //
        // 경험치로 저절로 오르지 않는다. 로비에서 눌러 산다.
        // 골드는 앞으로 아웃게임 상점·인게임 강화 상점에서도 쓰이므로,
        // "레벨을 살까 다른 걸 살까" 가 매번 선택이 된다.

        public int GhostLevelMax => _config != null ? _config.GhostLevelMax : 50;

        /// <summary>
        /// Lv <paramref name="level"/> → 다음 레벨 비용(골드).
        /// 곡선은 `GameConfig` 가 갖는다 — 여기에는 상한 판정만 있다.
        /// </summary>
        public int GhostLevelCost(int level)
            => _config == null || level < 1 || level >= GhostLevelMax
                ? 0 : _config.GhostLevelCost(level);

        /// <summary>지금 골드로 다음 레벨을 살 수 있는가.</summary>
        public bool CanBuyGhostLevel
            => _data != null && GhostLevel < GhostLevelMax
               && _data.gold >= GhostLevelCost(GhostLevel);

        /// <summary>다음 레벨을 산다. 골드가 모자라거나 상한이면 아무 일도 없다.</summary>
        public bool BuyGhostLevel()
        {
            if (!CanBuyGhostLevel) return false;
            _data.gold -= GhostLevelCost(_data.ghostLevel);
            _data.ghostLevel++;
            PublishCurrency();
            _bus?.Publish(new GhostProgressChangedEvent
            {
                NewLevel = _data.ghostLevel,
                NewExp = _data.ghostExp,
                NewExpMax = _data.ghostExpMax,
            });
            return true;
        }

        private HostEntry _ghostEntry;

        /// <summary>
        /// 유령은 `HostTable` 에 없다 — 몸이 아니기 때문이다.
        /// 목록에 세우려고 만든 칸이므로 조회도 여기서 받아 준다.
        /// </summary>
        public HostEntry GetHost(string hostKey)
        {
            if (hostKey == HostEntry.GhostKey)
                return _ghostEntry ??= HostEntry.CreateGhost();
            return _hosts != null ? _hosts.Get(hostKey) : null;
        }

        public ActiveSkillEntry GetActiveSkill(string activeSkillKey)
            => _activeSkills != null ? _activeSkills.Get(activeSkillKey) : null;

        /// <summary>패시브 스킬 조회. 키가 비었거나 표에 없으면 null — 그 몸은 패시브가 없다.</summary>
        public PassiveSkillEntry GetPassiveSkill(string passiveSkillKey)
            => _passiveSkills != null ? _passiveSkills.Get(passiveSkillKey) : null;

        public int OwnedHostCount
        {
            get
            {
                if (_hosts == null) return 0;
                int n = 0;
                var list = PlayableHosts;
                for (int i = 0; i < list.Count; i++)
                    if (!list[i].IsGhost && IsHostUnlocked(list[i])) n++;
                return n;
            }
        }

        public void SelectHost(string hostKey)
        {
            if (_data == null || string.IsNullOrEmpty(hostKey)) return;
            var e = GetHost(hostKey);
            if (e == null) return;

            bool unlocked = IsHostUnlocked(e);
            // 잠금 호스트도 선택은 허용한다(다음 목표 확인). 단 저장은 해금된 것만.
            if (unlocked) _data.selectedHostId = hostKey;

            _bus?.Publish(new HostSelectedEvent { SelectedKey = hostKey, IsUnlockedHost = unlocked });
        }

        public bool TrySpendStamina(int amount)
        {
            if (_data == null || amount <= 0 || _data.stamina < amount) return false;
            _data.stamina -= amount;
            PublishCurrency();
            return true;
        }

        public void AddCurrency(int gold, int gem)
        {
            if (_data == null) return;
            _data.gold = Mathf.Max(0, _data.gold + gold);
            _data.gem  = Mathf.Max(0, _data.gem + gem);
            PublishCurrency();
        }

        public void AddGrowthCurrency(int gold, int gem, int spiritCore, int hostMemory)
        {
            if (_data == null) return;
            _data.gold       = Mathf.Max(0, _data.gold + gold);
            _data.gem        = Mathf.Max(0, _data.gem + gem);
            _data.spiritCore = Mathf.Max(0, _data.spiritCore + spiritCore);
            _data.hostMemory = Mathf.Max(0, _data.hostMemory + hostMemory);
            PublishCurrency();
        }

        // ── 보물상자 칸 ──────────────────────────────────────

        public int ChestSlotCount => 3;

        private bool ChestSlotOk(int slot)
            => _data != null && _data.chestKeys != null
               && slot >= 0 && slot < _data.chestKeys.Length;

        public string GetChestKey(int slot)
            => ChestSlotOk(slot) ? _data.chestKeys[slot] ?? string.Empty : string.Empty;

        public long GetChestUnlockAt(int slot)
            => ChestSlotOk(slot) ? _data.chestUnlockAt[slot] : 0L;

        public int GetChestSeconds(int slot)
            => ChestSlotOk(slot) ? _data.chestSeconds[slot] : 0;

        public void SetChestSlot(int slot, string chestKey, long unlockAt, int seconds)
        {
            if (_data == null) return;
            _data.NormalizeChests(ChestSlotCount);
            if (slot < 0 || slot >= ChestSlotCount) return;
            _data.chestKeys[slot] = chestKey ?? string.Empty;
            _data.chestUnlockAt[slot] = unlockAt;
            _data.chestSeconds[slot] = seconds;
        }

        public void SetProgress(int chapter, int stage)
        {
            if (_data == null) return;
            _data.currentChapter = Mathf.Max(1, chapter);
            _data.reachedStage   = Mathf.Max(1, stage);
            UnsealUnlocked();   // 이번 전진으로 새로 열린 몸이 있으면 그 스킬도 함께 열린다
            _bus?.Publish(new ProgressChangedEvent
            {
                NewChapter        = _data.currentChapter,
                NewStage          = _data.reachedStage,
                NewClearedChapter = _data.clearedChapter,
            });
        }

        public UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared)
            => GrantStageRewardAsync(gold, ghostExp, cleared, 0, 0, 0);

        /// <summary>
        /// 런의 결과를 반영한다. 정본 REWARD_DB 는 방·정예·챕터마다 다른 재화를 준다 —
        /// 골드만 주면 보스를 잡을 이유가 "다음 방으로 간다" 뿐이게 된다.
        /// </summary>
        public async UniTask GrantStageRewardAsync(int gold, int ghostExp, bool cleared,
                                                   int spiritCore, int hostMemory, int gem)
        {
            if (_data == null) return;

            _data.gold = Mathf.Max(0, _data.gold + Mathf.Max(0, gold));
            _data.gem = Mathf.Max(0, _data.gem + Mathf.Max(0, gem));
            // 영구 재화는 실패한 런에서도 남긴다. 정본이 "Run 은 끝나지만 Ghost 의 성장은
            // 계속된다" 를 성장 시스템의 한 줄 요지로 세웠다.
            _data.spiritCore = Mathf.Max(0, _data.spiritCore + Mathf.Max(0, spiritCore));
            _data.hostMemory = Mathf.Max(0, _data.hostMemory + Mathf.Max(0, hostMemory));
            PublishCurrency();

            // ⚠ 경험치로 자동 레벨업하지 않는다. 고스트 Lv 은 **로비에서 골드로 산다**
            //   (`BuyGhostLevel`). 판이 끝났다고 저절로 오르면 "무엇을 살까" 라는
            //   판단이 사라져 골드가 갈 곳을 잃는다.
            //   `ghostExp` 는 화면 진행바 표시용으로만 남는다.
            _bus?.Publish(new GhostProgressChangedEvent
            {
                NewLevel = _data.ghostLevel,
                NewExp = _data.ghostExp,
                NewExpMax = _data.ghostExpMax,
            });

            // 클리어했을 때만 스테이지를 전진시킨다. 실패는 진행도를 건드리지 않는다.
            if (cleared)
            {
                // ⚠ 챕터 격파 기록은 **여기서만** 올라간다.
                //   예전에는 `clearedChapter` 를 아무도 쓰지 않아 값이 0 에 머물렀고,
                //   그래서 보스 격파가 조건인 몸들이 **영원히 안 열렸다.**
                //   표에 `Owned`(무조건 열림) 로 박힌 9종만 처음부터 열려 있던 이유다.
                _data.clearedChapter = Mathf.Max(_data.clearedChapter, _data.currentChapter);
                SetProgress(_data.currentChapter, _data.reachedStage + 1);
            }

            await SaveAsync();
        }

        private void PublishCurrency()
        {
            if (_data == null) return;
            _bus?.Publish(new CurrencyChangedEvent
            {
                NewStamina = _data.stamina,
                NewGold    = _data.gold,
                NewGem     = _data.gem,
            });
        }
    }
}
