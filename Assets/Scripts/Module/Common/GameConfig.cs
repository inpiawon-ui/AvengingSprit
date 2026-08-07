using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 인게임 공통 밸런싱 수치의 단일 출처(SO). 코드 하드코딩 대신 이 에셋에서 관리한다.
    /// Addressable 주소: TableData/GameConfig (04_scenes 규약)
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("펫 케어 — 자연 변화 (초당)")]
        [SerializeField] private float _fullnessDecayPerSec = 0.6f;
        [SerializeField] private float _energyDecayPerSec = 0.5f;
        [SerializeField] private float _cleanlinessDecayPerSec = 0.4f;
        [SerializeField] private float _sleepDecayFactor = 0.3f;
        [SerializeField] private float _energyRecoverPerSec = 3.0f;
        [SerializeField] private float _moodConvergePerSec = 0.2f;

        [Header("펫 케어 — 액션 회복량")]
        [SerializeField] private float _feedAmount = 30f;
        [SerializeField] private float _cleanAmount = 40f;
        [SerializeField] private float _petMoodAmount = 8f;
        [SerializeField] private float _petCooldownSec = 3f;

        [Header("펫 케어 — 친밀도 공급")]
        [SerializeField] private float _feedIntimacy = 2f;
        [SerializeField] private float _cleanIntimacy = 2f;
        [SerializeField] private float _petIntimacyGain = 3f;

        [Header("펫 케어 — 기타")]
        [SerializeField] private float _rejectThreshold = 95f;
        [SerializeField] private float _publishIntervalSec = 0.5f;

        public float FullnessDecayPerSec => _fullnessDecayPerSec;
        public float EnergyDecayPerSec => _energyDecayPerSec;
        public float CleanlinessDecayPerSec => _cleanlinessDecayPerSec;
        public float SleepDecayFactor => _sleepDecayFactor;
        public float EnergyRecoverPerSec => _energyRecoverPerSec;
        public float MoodConvergePerSec => _moodConvergePerSec;
        public float FeedAmount => _feedAmount;
        public float CleanAmount => _cleanAmount;
        public float PetMoodAmount => _petMoodAmount;
        public float PetCooldownSec => _petCooldownSec;
        public float FeedIntimacy => _feedIntimacy;
        public float CleanIntimacy => _cleanIntimacy;
        public float PetIntimacyGain => _petIntimacyGain;
        public float RejectThreshold => _rejectThreshold;
        public float PublishIntervalSec => _publishIntervalSec;
    }
}
