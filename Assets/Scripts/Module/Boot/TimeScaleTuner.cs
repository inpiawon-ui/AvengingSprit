using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Module.Boot
{
    /// <summary>
    /// 개발용 시간 배속 조절기. `GameLauncher` 에 붙여 두면 씬을 넘어가도 살아남는다.
    ///
    /// 인스펙터 슬라이더를 끌면 그 즉시 반영된다 — 플레이 중에 손맛·연출 길이를
    /// 확인할 때 재실행 없이 바로 본다. 키보드로도 조절한다(에디터·개발 빌드 한정).
    ///
    /// `Time.timeScale` 만 건드리지 않는다. `fixedDeltaTime` 을 같이 옮기지 않으면
    /// 배속을 올렸을 때 물리가 성기게 돌아 판정이 달라진다.
    /// </summary>
    public sealed class TimeScaleTuner : MonoBehaviour
    {
        private const float MinScale = 0f;     // 0 은 일시정지 — 연출 한 장씩 볼 때 쓴다
        private const float MaxScale = 8f;

        [Tooltip("1 = 정상 속도. 플레이 중에 끌면 즉시 반영된다.")]
        [Range(MinScale, MaxScale)]
        [SerializeField] private float _timeScale = 1f;

        [Tooltip("끄면 이 컴포넌트는 아무것도 하지 않는다. 납품 빌드에서는 끈다.")]
        [SerializeField] private bool _enableHotkeys = true;

        [Tooltip("단축키가 순서대로 도는 배속 표. 0 은 일시정지다.")]
        [SerializeField] private float[] _presets = { 0f, 0.25f, 0.5f, 1f, 2f, 4f };

        /// <summary>배속을 안 건드렸을 때의 물리 간격. 배속을 되돌릴 기준점이다.</summary>
        private float _baseFixedDelta;
        private float _applied = -1f;
        private int _presetIndex = -1;

        private void Awake()
        {
            _baseFixedDelta = Time.fixedDeltaTime;
            Apply(_timeScale);
        }

        private void Update()
        {
            if (_enableHotkeys) ReadHotkeys();
            if (!Mathf.Approximately(_timeScale, _applied)) Apply(_timeScale);
        }

        /// <summary>씬을 옮겨도 배속이 남으면 다음 실행이 이상해진다. 나갈 때 되돌린다.</summary>
        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (_baseFixedDelta > 0f) Time.fixedDeltaTime = _baseFixedDelta;
        }

        /// <summary>인스펙터에서 슬라이더를 끌 때 — 플레이 중이면 바로 반영한다.</summary>
        private void OnValidate()
        {
            _timeScale = Mathf.Clamp(_timeScale, MinScale, MaxScale);
            if (Application.isPlaying && _baseFixedDelta > 0f) Apply(_timeScale);
        }

        /// <summary>코드에서도 바꿀 수 있게 열어 둔다 (예: 치트 패널).</summary>
        public void SetScale(float scale)
        {
            _timeScale = Mathf.Clamp(scale, MinScale, MaxScale);
            Apply(_timeScale);
        }

        private void Apply(float scale)
        {
            _applied = scale;
            Time.timeScale = scale;
            // 0 배속에서 나누면 무한대가 된다 — 물리는 손대지 않고 그냥 멈춘다.
            if (_baseFixedDelta > 0f && scale > 0f)
                Time.fixedDeltaTime = _baseFixedDelta * scale;
        }

        private void ReadHotkeys()
        {
            // 프레임워크 InputManager 를 쓰지 않는다 — 이건 개발용 곁가지라
            // 게임 입력 흐름에 끼어들면 안 된다.
            // 이 프로젝트는 Input System 패키지를 쓴다. 구형 `Input` 은 예외를 던진다.
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.leftBracketKey.wasPressedThisFrame) Step(-1);
            else if (kb.rightBracketKey.wasPressedThisFrame) Step(1);
            else if (kb.backslashKey.wasPressedThisFrame) SetScale(1f);
        }

        /// <summary>표에서 한 칸 옮긴다. 처음 눌렀을 때는 지금 값과 가장 가까운 칸에서 출발한다.</summary>
        private void Step(int delta)
        {
            if (_presets == null || _presets.Length == 0) return;
            if (_presetIndex < 0) _presetIndex = NearestPreset(_timeScale);
            _presetIndex = Mathf.Clamp(_presetIndex + delta, 0, _presets.Length - 1);
            SetScale(_presets[_presetIndex]);
            Debug.Log($"[TimeScale] {Time.timeScale:0.##}배");
        }

        private int NearestPreset(float scale)
        {
            int best = 0;
            float gap = float.MaxValue;
            for (int i = 0; i < _presets.Length; i++)
            {
                float d = Mathf.Abs(_presets[i] - scale);
                if (d >= gap) continue;
                gap = d;
                best = i;
            }
            return best;
        }
    }
}
