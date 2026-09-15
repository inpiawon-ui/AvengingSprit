using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Module.Title
{
    /// <summary>
    /// 타이틀 화면. 기능은 전면 탭 1종뿐이다.
    /// 원작 아케이드의 "화면 하나 · 프롬프트 하나" 를 따른다 — 버튼 위젯을 두지 않는다.
    /// </summary>
    public sealed class TitleMainUI : MonoBehaviour, IBackTarget
    {
        private const float BlinkPeriod = 1.1f;

        /// <summary>로고 색이 한 단계 넘어가는 간격. 원작 기판은 약 8프레임(60Hz)마다 바꾼다.</summary>
        private const float LogoCycleSeconds = 8f / 60f;
        private const int LogoFrames = 6;
        private const string AtlasAddress = "atlas/titlemainui";

        private UIBinder _ui;
        private IDisposable _startToken;
        private Transform _tapText;
        private UnityEngine.UI.Image _logo;
        private readonly Sprite[] _logoSprites = new Sprite[LogoFrames];
        private float _logoTimer;
        private int _logoIndex;
        private bool _transitioning;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            // 본문 폰트를 지금 언어 것으로 — 일본어를 한글 폰트로 그리면 한자가 한국식으로 나온다
            Localize.ApplyFonts(transform);
            _tapText = _ui.Find("TapToStartText");

            _ui.SetText("TapToStartText", "TAP TO START");
            _ui.SetText("SubtitleText", "RE:BORN");
            _ui.SetText("CopyrightText", "©1991 JALECO / CITY CONNECTION");
            _ui.SetText("VersionText", $"v{Application.version}");

            var logoTransform = _ui.Find("TitleLogo");
            if (logoTransform != null) _logo = logoTransform.GetComponent<UnityEngine.UI.Image>();
            LoadLogoFramesAsync().Forget(); // fire-and-forget: 못 불러와도 타이틀은 뜬다

            _ui.OnClick("TouchArea", OnTapped);
            gameObject.AddComponent<BackButtonRouter>();
        }

        /// <summary>
        /// 원작 타이틀 로고는 색이 순환한다. 시트에 들어 있는 6장은 서로 다른 그림이 아니라
        /// **같은 로고의 색만 바뀐 것**이라 순서대로 갈아 끼우면 그대로 재현된다.
        ///
        /// 6장을 프리팹 인스펙터에 물려 두지 않고 아틀라스에서 이름으로 꺼낸다 —
        /// 배열을 노출하면 리소스를 다시 뽑을 때마다 손으로 다시 물려야 한다.
        /// </summary>
        private async UniTaskVoid LoadLogoFramesAsync()
        {
            if (_logo == null) return;

            SpriteAtlas atlas;
            try { atlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Title] 아틀라스 로드 실패 — 로고 색 순환 없이 진행한다: {e.Message}");
                return;
            }

            for (int i = 0; i < LogoFrames; i++)
                _logoSprites[i] = atlas.GetSprite($"titlelogo_{i}");

            // 0번을 못 찾으면 프리팹이 들고 있던 것을 그대로 쓴다.
            if (_logoSprites[0] == null) _logoSprites[0] = _logo.sprite;
        }

        /// <summary>타이틀에는 되돌아갈 화면이 없다 — 라우터가 종료 확인을 띄운다.</summary>
        public bool OnBackPressed() => false;

        private void OnEnable()
        {
            _startToken = CoreModule.Get<IEventBus>()
                .Subscribe<TitleStartRequestedEvent>(OnStartRequested);
        }

        private void OnDisable()
        {
            _startToken?.Dispose();
            _startToken = null;
        }

        private void Update()
        {
            CycleLogo();

            if (_tapText == null) return;
            // 아케이드식 점멸 프롬프트. 알파만 흔들어 도트를 건드리지 않는다.
            float a = Mathf.PingPong(Time.unscaledTime / BlinkPeriod, 1f);
            var tmp = _tapText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                var c = tmp.color;
                c.a = Mathf.Lerp(0.25f, 1f, a);
                tmp.color = c;
            }
        }

        private void CycleLogo()
        {
            if (_logo == null || _logoSprites[LogoFrames - 1] == null) return;

            _logoTimer += Time.unscaledDeltaTime;
            if (_logoTimer < LogoCycleSeconds) return;

            // 프레임이 밀렸을 때 한 칸씩만 넘기면 색 순환이 느려진다. 밀린 만큼 건너뛴다.
            int steps = (int)(_logoTimer / LogoCycleSeconds);
            _logoTimer -= steps * LogoCycleSeconds;
            _logoIndex = (_logoIndex + steps) % LogoFrames;
            _logo.sprite = _logoSprites[_logoIndex];
        }

        private void OnTapped()
        {
            if (_transitioning) return;
            CoreModule.Get<IEventBus>().Publish(new TitleStartRequestedEvent());
        }

        private void OnStartRequested(TitleStartRequestedEvent _)
        {
            if (_transitioning) return;
            _transitioning = true;
            GoLobbyAsync().Forget(); // fire-and-forget: 씬 전환 완료를 기다릴 필요가 없다
        }

        /// <summary>
        /// 시작을 눌렀다. **첫 실행이면 오프닝을 먼저 보여 준다.**
        ///
        /// 오프닝은 「왜 유령인가 · 왜 싸우는가 · 에너지가 한정돼 있다」 세 가지를
        /// 말하는 화면이라, 그 셋을 모르는 채로 로비에 들어가면 호스트를 고를 이유가 없다.
        /// 한 번 본 사람은 바로 로비로 간다 — 두 번째부터는 아는 이야기다.
        /// </summary>
        private async UniTaskVoid GoLobbyAsync()
        {
            // ⚠ `AlwaysShow` 가 켜져 있는 동안은 본 기록을 무시하고 매번 띄운다.
            //   만드는 중이라 확인할 때마다 봐야 한다. 끄는 방법은 그 상수 주석에 있다.
            bool seen = !Opening.OpeningMainUI.AlwaysShow
                     && PlayerPrefs.GetInt(Opening.OpeningMainUI.SeenKey, 0) != 0;
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = seen ? SceneNames.Lobby : SceneNames.Opening,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
