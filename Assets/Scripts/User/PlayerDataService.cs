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
            s_testMode = config != null && config.TestMode;
            s_openAllHosts = config != null && config.OpenAllHosts;
            s_powerTest = config != null && config.PowerTest;
        }

        /// <summary>권장 힘 시험(`GameConfig.PowerTest`) — 모든 몸이 고른 챕터의 권장 전투력으로 싸운다. 저장은 그대로다.</summary>
        private static bool s_powerTest;

        // 시험 배율 — 몸 · 챕터마다 한 번 잰다(체력 · 공격에 같은 배율을 곱하면 전투력이 그만큼 바뀐다: √(DPS × 체력))
        private readonly Dictionary<string, float> _powerTestMul = new();
        private int _powerTestChapter = -1;

        private int PowerTestMastery
        {
            get
            {
                var def = _config != null && _config.HasChapterTable ? _config.ChapterOf(SelectedChapter) : default;
                return Mathf.Max(1, def.PowerStar * 2);   // RecommendedPower 와 같은 자(성급 n = 숙련도 2n)
            }
        }

        /// <summary>이 몸을 권장 전투력에 맞추는 체력 · 공격 배율.</summary>
        private float PowerTestMul(string hostKey)
        {
            if (_powerTestChapter != SelectedChapter) { _powerTestMul.Clear(); _powerTestChapter = SelectedChapter; }
            if (_powerTestMul.TryGetValue(hostKey, out float f)) return f;
            var host = GetHost(hostKey);
            int want = RecommendedPower(SelectedChapter);
            var g = HostCombat.Growth.None;
            g.Mastery = PowerTestMastery;
            int now = host != null && _config != null ? HostCombat.Power(_config, host, g) : 0;
            f = want > 0 && now > 0 ? (float)want / now : 1f;
            _powerTestMul[hostKey] = f;
            return f;
        }

        /// <summary>모든 몸을 숙련도 최대로 읽는다(`GameConfig.OpenAllHosts`). 저장은 그대로다.</summary>
        private static bool s_openAllHosts;

        /// <summary>테스트 모드 — 전부 해금 · 무료 입장 · 젬 100만 · 상자 빨리(`GameConfig.TestMode`).</summary>
        private static bool s_testMode;

        public bool TestMode => s_testMode;

        /// <summary>테스트 모드에서 불러올 때 채워 주는 젬.</summary>
        private const int TestGem = 1_000_000;

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

        /// <summary>챕터 수. 챕터 표(`GameConfig.ChapterOf`)의 줄 수다 — 코드에 적지 않는다.</summary>
        public int ChapterCount => _config != null ? _config.ChapterCount : 6;

        public GameConfig.ChapterDef ChapterInfo(int chapter)
            => _config != null ? _config.ChapterOf(chapter) : default;

        public GameConfig Config => _config;

        public int UnlockedChapter => Mathf.Clamp(ClearedChapter + 1, 1, ChapterCount);

        // 0 = 아직 안 골랐다 → 열린 챕터 중 가장 높은 것
        private int _selectedChapter;

        public int SelectedChapter
        {
            get => _selectedChapter <= 0 ? UnlockedChapter : Mathf.Clamp(_selectedChapter, 1, UnlockedChapter);
            set => _selectedChapter = value;
        }

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
        /// 사신은 히든 캐릭터다(PD 2026-10-07) — 방 · 상자 조각 · 상점 · 랜덤 · 긴급 투입 · 육성 목록 어디에도 안 나온다.
        /// 얻는 길을 정하면 그 길에서만 풀어 준다.
        /// 표는 그대로 두고 여기서만 거른다 — 임포터가 표를 다시 써도 빠진 채로 남는다.
        /// </summary>
        public static bool IsHiddenHost(string hostKey) => hostKey == "amazon_elite" || hostKey == "death";

        /// <summary>
        /// 히든 캐릭터 — ? 랜덤 출전에서만 아주 드물게 나와 **그 판만** 쓴다. 조각은 안 준다(PD 2026-10-23).
        /// 아마존 정예는 빼 둔 몸이지 히든이 아니다.
        /// </summary>
        public const string HiddenTrialHostKey = "death";

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
                    if (e != null && IsHostUnlocked(e) && !IsHiddenHost(e.HostKey)) return _data.selectedHostId;
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
            _data.NormalizeStats(StatCount);         // 옛 저장(v3)은 능력치 강화 배열이 비어 있다
            _data.NormalizeSkills(SkillSlotCount);   // 스킬 레벨(2026-10-29)은 옛 저장에 없다 — 0(= Lv1)으로 채운다
            _data.ghostExpMax = ExpToNext(_data.ghostLevel);   // 곡선이 바뀌면 막대 끝도 따라온다
            GrantStarter();
            if (s_testMode && _data.gem < TestGem) _data.gem = TestGem;
            _data.runEntry ??= new RunEntryData();
            _data.modes ??= new ModeProgressData();
            _data.modes.Normalize();
            if (_config != null) _data.staminaMax = _config.StaminaMax;   // 최대는 표가 정한다
            _bus?.Publish(new UserDataReadyEvent { LoadedGhostLevel = _data.ghostLevel });
            PublishCurrency();
        }

        public UniTask SaveAsync()
            => _data == null ? UniTask.CompletedTask : _repo.SaveAsync(_data).AsUniTask();

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
            if (host.IsGhost) return true;
            // **라이브 기준(2026-10-04) — 조각을 모아 해금한다.** 숙련도 ≥ 1 이 곧 「가졌다」다.
            // 예전(2026-09-21)에는 모든 몸이 처음부터 열려 있었다 — 그건 테스트용이었다(기획 2026-10-01 8번).
            // ⚠ 판 안에서 빼앗는 것은 막지 않는다. 안 가진 몸도 빙의는 되고 **스킬만 봉인**된다(`IsSkillSealed`).
            //   이 판정은 로비(시작 몸 고르기)에서만 쓴다.
            return GetMastery(host.HostKey) >= 1;
        }

        /// <summary>
        /// 이 몸의 **스킬이 봉인돼 있는가** (숙련도 0).
        ///
        /// 봉인이면 액티브·패시브가 둘 다 안 나오고 평타만 쓴다.
        /// 몸을 쓰는 것 자체는 막지 않는다 — 파편은 그 몸을 써야 모이므로,
        /// 막으면 열 방법이 사라진다.
        /// </summary>
        // 봉인을 풀었다(PD 2026-10-28 「한 번 빌려 쓰는 거니깐 그냥 스킬을 쓰게」) — 안 가진 몸도 1레벨 스킬을 쓴다.
        // 다시 쓰고 싶으면 돈을 내고 들고 들어가야 하니 그것이 값이다. 필살기는 판당 1회 체험 규칙(`OwnsHostForUlt`)이 따로 지킨다.
        public bool IsSkillSealed(string hostKey) => false;

        /// <summary>
        /// 새 계정에 시작 몸 하나를 준다(숙련도 1). **한 번만** — 저장에 표시가 남는다.
        ///
        /// 유령만으로 시작하면 첫 판에 「몸을 골라 들어간다」를 못 해 보고, 몸의 스킬이 무엇인지도 모른다.
        /// 몸 하나는 쥐고 시작하고, 나머지는 판에서 만나 써 보고 조각을 모아 연다.
        /// </summary>
        private void GrantStarter()
        {
            if (_data == null || _data.starterGranted) return;
            string key = _config != null ? _config.StarterHost : "gangster";
            int at = EnsureHost(key);
            if (at >= 0 && _data.hostMastery[at] < 1) _data.hostMastery[at] = 1;
            _data.starterGranted = true;
            if (string.IsNullOrEmpty(_data.selectedHostId)) _data.selectedHostId = key;
        }

        /// <summary>
        /// ⚠ **더 이상 부르지 않는다**(2026-10-04 라이브 기준) — 모든 몸이 열려 있던 시절의 것이다.
        ///   지금은 시작 몸 하나만 주고(`GrantStarter`) 나머지는 조각으로 연다.
        ///
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
            _data.NormalizeSkills(SkillSlotCount);   // 스킬 레벨 배열도 한 칸(3값) 늘린다 — 뒤에 붙으므로 기존 첨자는 그대로
            return n;
        }

        public int GetMastery(string hostKey)
        {
            int i = IndexOfHost(hostKey);
            int m = i < 0 ? 0 : _data.hostMastery[i];
            if (s_powerTest && !string.IsNullOrEmpty(hostKey) && hostKey != HostEntry.GhostKey) return PowerTestMastery;
            if (s_openAllHosts && !string.IsNullOrEmpty(hostKey) && hostKey != HostEntry.GhostKey) return Mathf.Max(m, MasteryMax);
            // 테스트 모드 — 안 가진 몸도 1 로 읽는다(저장은 그대로라 끄면 원래대로 돌아온다).
            if (s_testMode && m < 1 && !string.IsNullOrEmpty(hostKey) && hostKey != HostEntry.GhostKey) return 1;
            return m;
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
            // 다 키운 몸(숙련도 상한)에게 온 조각은 **정수로 바꾼다** — 버리는 보상이 없게(기획 2026-10-04).
            // 바뀌는 값은 조각 하나에 1. 정수로 조각을 살 때는 등급값(B 2 · A 3 · S 5)을 낸다.
            if (_data.hostMastery[i] >= MasteryMax)
            {
                _data.essence += amount;
                PublishCurrency();
                return;
            }
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

        /// <summary>이 몸의 다음 성급에 드는 골드(등급 배수 포함, 10 단위). 봉인 해제 · 만렙이면 0.</summary>
        public int StarUpGoldCost(string hostKey)
        {
            int lv = GetMastery(hostKey);
            if (_config == null || lv < 0 || lv >= MasteryMax) return 0;
            var host = GetHost(hostKey);
            float mul = host != null ? _config.GradeMultiplier(host.Grade) : 1f;
            return Mathf.RoundToInt(_config.StarUpGoldAt(lv) * mul / 10f) * 10;
        }

        /// <summary>
        /// 조각과 골드를 함께 내고 성급을 한 단계 올린다(기획 2026-10-06).
        /// 둘 다 있는지 **먼저** 본다 — 조각만 빠지고 골드가 모자라 멈추는 반쪽 상태를 만들지 않는다.
        /// </summary>
        public bool TryStarUp(string hostKey)
        {
            int i = IndexOfHost(hostKey);
            if (i < 0 || _data.hostMastery[i] >= MasteryMax) return false;
            int shards = MasteryCost(hostKey), gold = StarUpGoldCost(hostKey);
            if (shards <= 0 || _data.hostShards[i] < shards || _data.gold < gold) return false;
            if (!TrySpendGold(gold)) return false;
            return SpendShards(hostKey, shards);
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
        /// 1챕터 기준 데려가는 값 — S 200 · A 120 · 그 외 60. 챕터 값 배율(`chapters.tsv` 의 priceMul)이 곱해진다.
        ///
        /// ⚠ 2026-10-04 에 S 1000 · A 600 · B 300 고정에서 바꿨다. 그 값은 무료(테스트)로 풀려 있어
        ///   아무도 낸 적이 없었는데, 라이브로 켜 보니 **1챕터를 깨고 받는 돈(478~664)의 절반이 입장료**였다.
        ///   수입의 10% 안팎이 되게 챕터에 묶는다 — 10챕터 B 438.
        /// </summary>
        public static int BaseHostEntryCost(HostGrade grade)
            => grade switch
            {
                HostGrade.S => 200,
                HostGrade.A => 120,
                _           => 60,
            };

        /// <summary>랜덤 선택은 B 값의 80% — 제일 싸다(고를 이유를 값으로 만든다, 기획 2026-09-29).</summary>
        private const float RandomEntryRatio = 0.8f;

        private float EntryMul(int chapter)
        {
            float m = _config != null ? _config.ChapterOf(chapter).PriceMul : 0f;
            return m > 0f ? m : 1f;
        }

        /// <summary>
        /// 그 챕터에 이 몸을 데려가는 **진짜 값**(테스트 모드를 안 본다). 화면에 적는 값이 이쪽이다.
        /// </summary>
        public int EntryPriceOf(HostEntry host, int chapter)
            => host == null || host.IsGhost ? 0
             : Mathf.RoundToInt(BaseHostEntryCost(host.Grade) * EntryMul(chapter));

        /// <summary>그 챕터의 랜덤 선택 값.</summary>
        public int RandomEntryPrice(int chapter)
            => Mathf.RoundToInt(BaseHostEntryCost(HostGrade.B) * RandomEntryRatio * EntryMul(chapter));

        /// <summary>테스트 모드면 몸값이 0 이다(`GameConfig.TestMode`). 라이브는 늘 false.</summary>
        public static bool FreeHostsForTest => s_testMode;

        /// <summary>
        /// 지금 고른 챕터에 이 칸을 데려가며 **실제로 내는 값.** 유령은 공짜, 테스트 모드면 0.
        /// </summary>
        public int EntryCostOf(HostEntry host)
            => FreeHostsForTest ? 0 : EntryPriceOf(host, SelectedChapter);

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

        /// <summary>
        /// 골드를 그냥 낸다. 모자라면 아무 일도 없다.
        ///
        /// 몸값(<see cref="PayHostEntry"/>)은 등급에서 값이 나오는데, 랜덤 선택처럼
        /// **고른 몸과 값이 따로 노는** 지불이 있어 따로 둔다.
        /// ⚠ 테스트용 무료 플래그(<see cref="FreeHostsForTest"/>)는 여기에 안 걸린다 —
        ///   몸값이 아니다.
        /// </summary>
        public bool TrySpendGold(int amount)
        {
            if (_data == null || amount < 0 || _data.gold < amount) return false;
            if (amount == 0) return true;
            _data.gold -= amount;
            PublishCurrency();
            return true;
        }

        // ── 고스트 Lv = 유저 레벨 ─────────────────────────────
        //
        // 챕터를 깨서 받는 경험치로만 오른다(기획 2026-09-21). 예전엔 골드로 샀는데,
        // 골드는 이제 능력치 강화에 쓴다 — 두 곳이 같은 골드를 두고 다투면 레벨 쪽만 산다.

        public int GhostLevelMax => _config != null ? _config.GhostLevelMax : 50;

        private int ExpToNext(int level) => _config != null ? Mathf.Max(1, _config.UserExpToNext(level)) : 100;

        /// <summary>경험치를 더하고 넘친 만큼 레벨을 올린다. 상한이면 막대를 가득 채운 채 멈춘다.</summary>
        private void AddUserExp(int amount)
        {
            if (_data == null || amount <= 0) return;
            _data.ghostExp += amount;
            while (_data.ghostLevel < GhostLevelMax && _data.ghostExp >= ExpToNext(_data.ghostLevel))
            {
                _data.ghostExp -= ExpToNext(_data.ghostLevel);
                _data.ghostLevel++;
            }
            _data.ghostExpMax = ExpToNext(_data.ghostLevel);
            if (_data.ghostLevel >= GhostLevelMax) _data.ghostExp = Mathf.Min(_data.ghostExp, _data.ghostExpMax);
            PublishGhostProgress();
        }

        private void PublishGhostProgress()
            => _bus?.Publish(new GhostProgressChangedEvent
            {
                NewLevel = _data.ghostLevel,
                NewExp = _data.ghostExp,
                NewExpMax = _data.ghostExpMax,
            });

        /// <summary>
        /// 별(성급) — 숙련도 두 단계마다 하나. 숙련도 1·2 → 1성, 3·4 → 2성 … 9·10 → 5성.
        ///
        /// ⚠ 예전에는 `숙련도 ÷ 2` 라 처음 가진 몸(숙련도 1)이 0성이었다. 성급이 강화 상한을
        ///   정하게 되면서(기획 2026-10-02) 0성이면 강화를 하나도 못 한다 — 가진 몸은 1성부터다.
        /// </summary>
        public int StarsOf(string hostKey) => Mathf.Clamp((GetMastery(hostKey) + 1) / 2, 0, 5);

        // ── 전투력 (2026-10-13) ─────────────────────────────
        //
        // 판에서 실제로 쓰는 값(고스트 Lv · 골드 강화 · 숙련도)을 `HostCombat` 한 자로 잰다.
        // 예전 임시값(등급만 — B 8000 · A 11000 · S 14000)은 걷었다. 등급은 힘을 거의 안 정한다.

        /// <summary>이 몸의 지금 전투력. 유령은 몸이 아니라 0 이다.</summary>
        public int PowerOf(string hostKey)
        {
            var host = GetHost(hostKey);
            if (host == null || host.IsGhost || _config == null) return 0;
            var g = new HostCombat.Growth
            {
                Mastery = GetMastery(hostKey),
                HpMul = StatBonusMul(hostKey, HostStat.Hp),
                AtkMul = StatBonusMul(hostKey, HostStat.Atk),
                CritMul = StatBonusMul(hostKey, HostStat.Crit),
                CritFlat = StatBonusFlat(hostKey, HostStat.Crit),
                AtkSpeedMul = StatBonusMul(hostKey, HostStat.AtkSpeed),
                CritDamageFlat = StatBonusFlat(hostKey, HostStat.CritDamage),
                DefenseFlat = StatBonusFlat(hostKey, HostStat.Defense),
            };
            return HostCombat.Power(_config, host, g);
        }

        // 권장 전투력 = 기준 힘 × 그 챕터 배율(`chapters.tsv` power) × 그때의 성급 몫(powerStar).
        //   기준 힘 = 몸을 막 얻은 상태(강화 없음 · 숙련도 0)의 23명 평균.
        //   챕터 배율은 `Tools/balance10.py` 가 「그 챕터를 깨는 판에 들어설 때의 내 힘」으로 낸다 —
        //   적 체력 · 공격력 배율이 거기서 나왔으니 같은 모델로 거꾸로 잰 값이다.
        private int _basePower = -1;

        private int BasePower
        {
            get
            {
                if (_basePower >= 0) return _basePower;
                var list = PlayableHosts;
                if (_config == null || list == null) return 0;
                long sum = 0; int n = 0;
                var none = HostCombat.Growth.None;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null || list[i].IsGhost) continue;
                    sum += HostCombat.Power(_config, list[i], none);
                    n++;
                }
                _basePower = n > 0 ? (int)(sum / n) : 0;
                return _basePower;
            }
        }

        public int RecommendedPower(int chapter)
        {
            if (_config == null || !_config.HasChapterTable) return 0;
            var def = _config.ChapterOf(chapter);
            if (def.PowerMul <= 0f) return 0;
            int mastery = Mathf.Max(1, def.PowerStar * 2);   // 모델의 성급 n = 숙련도 2n(강화 상한 10n 과 같은 자 — balance10.py)
            float v = BasePower * def.PowerMul * (1f + HostCombat.PowerPerMastery * mastery);
            return Mathf.RoundToInt(v / 10f) * 10;   // 10 단위 — 끝자리가 흔들리면 「정확한 값」처럼 읽힌다
        }

        /// <summary>
        /// 이 몸을 **가지고 있는가.** 고르는 목록에 내보낼지를 정한다.
        ///
        /// ⚠ <see cref="IsHostUnlocked"/> 와 다르다. 그쪽은 지금 전부 `true` 다
        ///   (기획 2026-09-21 — 해금 대신 조각으로 별을 올린다). 보유는 **저장에 그 몸이
        ///   있고 봉인이 풀렸는가**로 본다. 유령은 몸이 아니므로 언제나 가지고 있다.
        /// </summary>
        public bool IsHostOwned(HostEntry host)
            => host != null && (host.IsGhost || GetMastery(host.HostKey) >= 1);

        // ── 유령 강화 — 스탯 6줄 (육성 개편 PD 2026-10-29) ─────
        //
        // 유령 레벨 = 강화 상한. 골드로 한 단계씩. **모든 몸**에 붙는다(메인 100% · 보조는 파티 규칙대로).
        // 호스트 탭 능력치 강화는 없앴다 — 몸은 스킬로 키운다(아래 「호스트 스킬 레벨」).
        // 수치는 전부 `GameConfig`(임시값). 여기엔 규칙만 둔다.

        private static readonly int StatCount = Enum.GetValues(typeof(HostStat)).Length;

        public int GhostStatMax => _config != null ? _config.GhostStatMax : 50;
        public int GhostStatCap => _config != null ? _config.GhostStatCap(GhostLevel) : GhostStatMax;

        public int GhostStatLevel(HostStat stat)
        {
            if (_data == null) return 0;
            _data.NormalizeStats(StatCount);
            return _data.ghostStatLevels[(int)stat];
        }

        public int GhostStatCost(HostStat stat)
        {
            int lv = GhostStatLevel(stat);
            // 줄이 없는 능력치(사거리 · 치명타 피해)는 살 수 없다
            return _config == null || lv >= GhostStatCap || _config.GhostStepValue(stat) <= 0f ? 0 : _config.GhostStatCostAt(lv);
        }

        public float GhostStatValue(HostStat stat, int level)
            => _config != null ? _config.GhostStepValue(stat) * Mathf.Max(0, level) : 0f;

        public bool BuyGhostStat(HostStat stat)
        {
            int cost = GhostStatCost(stat);
            if (cost <= 0 || _data.gold < cost) return false;
            _data.gold -= cost;
            _data.ghostStatLevels[(int)stat]++;
            PublishCurrency();
            _bus?.Publish(new HostStatUpgradedEvent { UpgradedHostKey = HostEntry.GhostKey, IsGhostTab = true });
            return true;
        }

        // 사거리는 이제 키우지 않는다(유령 강화 줄 없음) — 옛 저장에 남은 단계가 판에 새지 않게 0
        public float StatLevelEffective(string hostKey, HostStat stat) => 0f;

        public float StatBonusFlat(string hostKey, HostStat stat)
            => s_powerTest || !GameConfig.IsFlatStat(stat) ? 0f   // 권장 힘 시험 — 강화 0
             : GhostStatValue(stat, GhostStatLevel(stat));

        public float StatBonusMul(string hostKey, HostStat stat)
        {
            // 권장 힘 시험 — 강화 대신 체력 · 공격에 같은 배율(그 챕터 권장 전투력이 되게)
            if (s_powerTest)
                return !string.IsNullOrEmpty(hostKey) && (stat == HostStat.Hp || stat == HostStat.Atk) ? PowerTestMul(hostKey) : 1f;
            if (GameConfig.IsFlatStat(stat)) return 1f;   // 더하는 능력치는 StatBonusFlat 이 준다
            return 1f + GhostStatValue(stat, GhostStatLevel(stat)) / 100f;
        }

        // ── 호스트 스킬 레벨 (육성 개편 PD 2026-10-29) ─────────
        //
        // 성급(조각 + 골드)은 상한만 연다 — 액티브 = 성급 단계(최소 1) · 패시브 · 필살기 = 1 + 성급 ÷ 2.
        // 레벨은 골드로 셋을 따로 올린다(「골드를 어디에 쓸까」가 선택이 된다). 성급이 레벨을 자동으로 올리지 않는다.
        // 못 가진 몸(판에서 빙의)은 전부 Lv1.

        private const int SkillSlotCount = 3;

        public int SkillLevelMax(HostSkillSlot slot)
            => _config == null ? (slot == HostSkillSlot.Active ? 10 : 5)
             : slot == HostSkillSlot.Active ? _config.ActiveSkillMax
             : slot == HostSkillSlot.Passive ? _config.PassiveSkillMax
             : _config.UltSkillMax;

        public int SkillLevelCap(string hostKey, HostSkillSlot slot)
        {
            int m = GetMastery(hostKey);
            if (m < 1) return 1;
            int cap = slot == HostSkillSlot.Active ? m : 1 + m / 2;
            return Mathf.Clamp(cap, 1, SkillLevelMax(slot));
        }

        public int SkillLevel(string hostKey, HostSkillSlot slot)
        {
            if (string.IsNullOrEmpty(hostKey) || hostKey == HostEntry.GhostKey) return 1;
            int cap = SkillLevelCap(hostKey, slot);
            // 시험 스위치(전부 열기 · 권장 힘)는 상한까지 올린 것으로 읽는다 — 저장은 그대로
            if (s_powerTest || s_openAllHosts) return cap;
            int i = IndexOfHost(hostKey);
            if (i < 0 || GetMastery(hostKey) < 1) return 1;
            _data.NormalizeSkills(SkillSlotCount);
            return Mathf.Clamp(_data.hostSkillLevels[i * SkillSlotCount + (int)slot], 1, cap);
        }

        public int SkillLevelCost(string hostKey, HostSkillSlot slot)
        {
            if (_config == null || GetMastery(hostKey) < 1) return 0;
            int lv = SkillLevel(hostKey, slot);
            if (lv >= SkillLevelCap(hostKey, slot)) return 0;
            var host = GetHost(hostKey);
            float mul = host != null ? _config.SkillGoldGradeMul(host.Grade) : 1f;
            return Mathf.RoundToInt(_config.SkillGoldStep((int)slot) * (lv + 1) * mul / 10f) * 10;
        }

        public bool BuySkillLevel(string hostKey, HostSkillSlot slot)
        {
            int cost = SkillLevelCost(hostKey, slot);
            if (cost <= 0 || _data.gold < cost) return false;
            int i = EnsureHost(hostKey);
            if (i < 0) return false;
            int lv = SkillLevel(hostKey, slot);
            _data.gold -= cost;
            _data.hostSkillLevels[i * SkillSlotCount + (int)slot] = lv + 1;
            PublishCurrency();
            _bus?.Publish(new HostStatUpgradedEvent { UpgradedHostKey = hostKey, IsGhostTab = false });
            return true;
        }

        // ── 처음 켠 인연 (조각 새 공급, PD 2026-10-29) ───────────

        private static string InyeonPairKey(string mainKey, string partnerKey) => mainKey + "|" + partnerKey;

        public bool IsInyeonClaimed(string mainKey, string partnerKey)
        {
            if (_data == null || _data.inyeonClaimed == null) return false;
            string k = InyeonPairKey(mainKey, partnerKey);
            for (int i = 0; i < _data.inyeonClaimed.Length; i++) if (_data.inyeonClaimed[i] == k) return true;
            return false;
        }

        public bool TryClaimInyeon(string mainKey, string partnerKey)
        {
            if (_data == null || string.IsNullOrEmpty(mainKey) || string.IsNullOrEmpty(partnerKey)) return false;
            if (IsInyeonClaimed(mainKey, partnerKey)) return false;
            _data.inyeonClaimed ??= Array.Empty<string>();
            int n = _data.inyeonClaimed.Length;
            Array.Resize(ref _data.inyeonClaimed, n + 1);
            _data.inyeonClaimed[n] = InyeonPairKey(mainKey, partnerKey);
            return true;
        }

        // ── 유령 성장 경로 ──────────────────────────────────

        public int PathCount => _config != null ? _config.PathCount : 0;
        public int PathLevel(int index) => _config != null ? _config.PathLevel(index) : 0;
        public bool IsPathClaimed(int index) => _data != null && (_data.ghostPathClaimed & (1 << index)) != 0;

        public void GetPathReward(int index, out int gold, out int gem, out int spiritCore)
        {
            gold = _config != null ? _config.PathGold(index) : 0;
            gem = _config != null ? _config.PathGem(index) : 0;
            spiritCore = _config != null ? _config.PathSpiritCore(index) : 0;
        }

        public bool ClaimPath(int index)
        {
            if (_data == null || index < 0 || index >= PathCount || IsPathClaimed(index)) return false;
            if (GhostLevel < PathLevel(index)) return false;
            _data.ghostPathClaimed |= 1 << index;
            AddGrowthCurrency(_config.PathGold(index), _config.PathGem(index), _config.PathSpiritCore(index), 0);
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

        public void SetStamina(int value)
        {
            if (_data == null) return;
            int cap = _config != null ? _config.StaminaCap : 999;
            int v = Mathf.Clamp(value, 0, cap);
            if (v == _data.stamina) return;
            _data.stamina = v;
            PublishCurrency();
        }

        public RunEntryData RunEntry
        {
            get
            {
                if (_data == null) return null;
                _data.runEntry ??= new RunEntryData();
                return _data.runEntry;
            }
        }

        public ModeProgressData Modes
        {
            get
            {
                if (_data == null) return null;
                _data.modes ??= new ModeProgressData();
                return _data.modes;
            }
        }

        public RunStartMode RunStartMode { get; set; }

        public string RunHostKey { get; set; }

        // ── 정수 · 일일 상점 ─────────────────────────────────

        public int Essence => _data?.essence ?? 0;

        public int EssencePriceOf(string hostKey)
        {
            var e = GetHost(hostKey);
            return e == null || e.IsGhost || _config == null ? 0 : _config.EssencePerShard(e.Grade);
        }

        public bool ExchangeEssence(string hostKey, int shards)
        {
            int price = EssencePriceOf(hostKey) * Mathf.Max(0, shards);
            if (_data == null || price <= 0 || _data.essence < price) return false;
            if (GetMastery(hostKey) >= MasteryMax) return false;   // 다 키운 몸의 조각은 다시 정수가 될 뿐이다
            _data.essence -= price;
            int i = EnsureHost(hostKey);
            _data.hostShards[i] += shards;
            PublishCurrency();
            return true;
        }

        public StoreSaveData Store
        {
            get
            {
                if (_data == null) return null;
                _data.store ??= new StoreSaveData();
                return _data.store;
            }
        }

        public int Tickets => _data?.tickets ?? 0;

        public void AddTickets(int amount)
        {
            if (_data == null || amount == 0) return;
            _data.tickets = Mathf.Max(0, _data.tickets + amount);
            PublishCurrency();
        }

        public bool TrySpendTickets(int amount)
        {
            if (_data == null || amount < 0 || _data.tickets < amount) return false;
            _data.tickets -= amount;
            PublishCurrency();
            return true;
        }

        public void AddEssence(int amount)
        {
            if (_data == null || amount == 0) return;
            _data.essence = Mathf.Max(0, _data.essence + amount);
            PublishCurrency();
        }

        public DailyShopData DailyShop
        {
            get
            {
                if (_data == null) return null;
                _data.dailyShop ??= new DailyShopData();
                return _data.dailyShop;
            }
        }

        public bool TrySpendGem(int amount)
        {
            if (_data == null || amount < 0 || _data.gem < amount) return false;
            if (amount == 0) return true;
            _data.gem -= amount;
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

            // ⚠ 방마다 경험치를 주지 않는다. 유저(유령) 경험치는 **챕터를 깼을 때만**
            //   `GrantChapterClearAsync` 가 준다(기획 2026-09-21). `ghostExp` 인자는 쓰지 않는다.
            PublishGhostProgress();

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

        public async UniTask GrantRunGoldAsync(int gold)
        {
            if (_data == null) return;
            // 0 이어도 저장한다 — 판 안에서 모은 조각(`AddShards`)이 같이 남는다
            _data.gold = Mathf.Max(0, _data.gold + Mathf.Max(0, gold));
            PublishCurrency();
            await SaveAsync();
        }

        public async UniTask GrantChapterClearAsync(int chapter, int gold)
        {
            if (_data == null) return;
            // 출전 방식 보너스(AVSR_Content_Store.md B-12) — 깼을 때만. 결과 창이 같은 값을 보이게 계산은 한 곳에서.
            ClearBonusOf(gold, chapter, out int bonusGold, out int bonusShards, out int exp);
            _data.gold = Mathf.Max(0, _data.gold + Mathf.Max(0, gold) + bonusGold);
            PublishCurrency();
            if (bonusShards > 0 && !string.IsNullOrEmpty(RunHostKey))
                AddShards(RunHostKey, bonusShards);   // 랜덤으로 나온 몸 — 안 가진 몸이면 이 조각이 해금 길이 된다
            // 유저(유령) 경험치는 **챕터를 깼을 때만** 준다(기획 2026-09-21) — 죽으면 없다
            if (exp > 0) AddUserExp(exp);

            // 격파 기록은 **올라가기만** 한다 — 1챕터를 다시 깨도 3챕터 기록이 안 내려간다.
            _data.clearedChapter = Mathf.Clamp(Mathf.Max(_data.clearedChapter, chapter), 0, ChapterCount);
            SetProgress(chapter, _data.reachedStage);   // 해금 재평가 + ProgressChangedEvent
            await SaveAsync();
        }

        /// <summary>
        /// 깼을 때 출전 방식이 더 주는 것 — ? 랜덤은 판 골드 +20% · 나온 몸 조각 +3, 유령은 유령 경험치 +50%.
        /// 결과 창이 미리 보여 줄 수 있게 지급과 따로 계산한다.
        /// </summary>
        public void ClearBonusOf(int runGold, int chapter, out int bonusGold, out int bonusShards, out int exp)
        {
            bonusGold = 0; bonusShards = 0;
            exp = _config != null ? _config.ChapterClearExp(chapter) : 0;
            if (_config == null) return;
            if (RunStartMode == RunStartMode.Random)
            {
                bonusGold = Mathf.Max(0, runGold) * _config.RandomLuckGoldPct / 100;
                // 히든은 그 판 체험뿐 — 조각을 주면 얻는 길이 생긴다
                bonusShards = RunHostKey == HiddenTrialHostKey ? 0 : _config.RandomLuckShards;
            }
            else if (RunStartMode == RunStartMode.Ghost)
            {
                exp += exp * _config.GhostStartExpPct / 100;
            }
        }

        private void PublishCurrency()
        {
            if (_data == null) return;
            _bus?.Publish(new CurrencyChangedEvent
            {
                NewStamina = _data.stamina,
                NewGold    = _data.gold,
                NewGem     = _data.gem,
                NewEssence = _data.essence,
            });
        }
    }
}
