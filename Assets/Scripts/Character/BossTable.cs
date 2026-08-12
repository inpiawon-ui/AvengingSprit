using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 보스 행동 1종. 잡몹의 `AttackKind` 와 별개다 — 잡몹은 한 가지만 반복하지만
    /// 보스는 여러 행동을 쿨다운으로 돌려 쓰고, 체력이 깎이면 쓸 수 있는 행동이 늘어난다.
    /// </summary>
    public enum BossPattern
    {
        /// <summary>플레이어 방향으로 부채꼴 탄막</summary>
        Volley,
        /// <summary>사방 360° 탄막. 붙어 있으면 피하기 어렵다</summary>
        Ring,
        /// <summary>플레이어를 향해 돌진. 접촉 피해</summary>
        Charge,
        /// <summary>잡몹 소환. 보스만 노리다 둘러싸이게 만든다</summary>
        Summon,
        /// <summary>조준 연사. 짧은 간격으로 직선탄</summary>
        AimedBurst,

        // ── 정본이 이름 붙인 패턴 ──────────────────────────────────
        // 여기까지는 "탄을 몇 발 어느 각도로" 였다. 아래 셋은 보스마다 다른
        // **자리 싸움**을 만든다 — 무엇을 피하느냐가 보스마다 달라진다.

        /// <summary>
        /// 번갈아 솟는 레이저 (정본 B01 Alternating Pop-up Laser).
        /// 바닥에 금이 갔다가 그 줄에서 세로 광선이 솟는다. 쓸 때마다 줄이 바뀐다.
        /// </summary>
        PopupLaser,

        /// <summary>
        /// 실드 순환 + 내려찍기 (정본 B02 Crusher + Shield Cycle).
        /// 일정 시간 받는 피해가 줄고, 그동안 플레이어 자리에 그림자를 깔았다가 찍는다.
        /// </summary>
        ShieldCycle,

        /// <summary>
        /// 독구름 (정본 B03 Venom Clouds). 플레이어 자리 주변에 저주 장판을 남긴다.
        /// </summary>
        VenomCloud,
    }

    [Serializable]
    public sealed class BossMove
    {
        [SerializeField] private BossPattern _pattern;
        [Tooltip("이 페이즈부터 사용한다 (1=시작, 2=체력 60% 이하, 3=30% 이하)")]
        [SerializeField] private int _fromPhase = 1;
        [SerializeField] private float _cooldown = 4f;
        [SerializeField] private int _shotCount = 5;
        [SerializeField] private float _spreadDegrees = 60f;
        [SerializeField] private float _damageMul = 1f;

        public BossPattern Pattern => _pattern;
        public int FromPhase => Mathf.Max(1, _fromPhase);
        public float Cooldown => Mathf.Max(0.4f, _cooldown);
        public int ShotCount => Mathf.Max(1, _shotCount);
        public float SpreadDegrees => _spreadDegrees;
        public float DamageMul => _damageMul <= 0f ? 1f : _damageMul;
    }

    [Serializable]
    public sealed class BossEntry
    {
        [SerializeField] private string _bossKey;
        [SerializeField] private int _chapter = 1;
        [SerializeField] private string _nameEn;
        [SerializeField] private string _nameKr;
        [SerializeField] private string _spriteName = "unit_boss";

        [Header("전투")]
        [SerializeField] private float _hpMul = 1f;
        [SerializeField] private float _atkMul = 1f;
        [SerializeField] private float _moveSpeedMul = 1f;
        [Tooltip("페이즈가 오를 때마다 쿨다운에 곱하는 값. 1 미만이면 점점 빨라진다.")]
        [SerializeField] private float _phaseCooldownMul = 0.78f;

        [SerializeField] private BossMove[] _moves = Array.Empty<BossMove>();

        public string BossKey => _bossKey;
        public int Chapter => _chapter;
        public string NameEn => _nameEn;
        public string NameKr => _nameKr;
        public string SpriteName => string.IsNullOrEmpty(_spriteName) ? "unit_boss" : _spriteName;
        public float HpMul => _hpMul <= 0f ? 1f : _hpMul;
        public float AtkMul => _atkMul <= 0f ? 1f : _atkMul;
        public float MoveSpeedMul => _moveSpeedMul <= 0f ? 1f : _moveSpeedMul;
        public float PhaseCooldownMul => Mathf.Clamp(_phaseCooldownMul, 0.3f, 1f);
        public IReadOnlyList<BossMove> Moves => _moves;
    }

    /// <summary>
    /// 챕터 보스 정의. HostTable 과 같은 규약 — 배열 하나, 개별 asset 분리 금지.
    /// 에셋 `Assets/BundleResource/TableData/BossTable.asset` · 주소 `TableData/BossTable`
    /// </summary>
    [CreateAssetMenu(fileName = "BossTable", menuName = "AVSR/Boss Table")]
    public sealed class BossTable : ScriptableObject
    {
        [SerializeField] private BossEntry[] _entries = Array.Empty<BossEntry>();

        public IReadOnlyList<BossEntry> Entries => _entries;

        /// <summary>해당 챕터의 보스. 없으면 첫 번째로 대체한다.</summary>
        public BossEntry ForChapter(int chapter)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].Chapter == chapter) return _entries[i];
            return _entries.Length > 0 ? _entries[0] : null;
        }
    }
}
