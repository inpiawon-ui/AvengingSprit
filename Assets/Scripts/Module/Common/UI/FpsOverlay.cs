using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 화면 맨 아래 가운데에 FPS · 최저 FPS · 메모리를 늘 띄운다 — 폰에서 빌드마다 최적화 효과를 숫자로 비교하려는 시험용 표시(PD 2026-10-30).
    ///
    /// 0.5초마다 한 번만 글자를 바꾼다(매 프레임 바꾸면 그것 자체가 부하다). `SetText` 의 숫자 자리 채우기는 문자열을 새로 만들지 않는다.
    /// 메모리는 「Total Used Memory」(유니티가 이 앱에 잡아 둔 양) — 개발 빌드가 아니어도 값이 나온다.
    /// 예전 「System Used Memory」 는 기기에서 7246MB 처럼 나와 기기 전체를 센 값으로 보였다(PD 2026-10-30 캡처).
    /// 프리팹이 아니라 코드로 만든다: 씬 · 로딩과 상관없이 처음부터 끝까지 떠 있어야 해서 SystemPopup 과 같은 예외로 둔다.
    /// 출시 전에 <see cref="s_enabled"/> 를 끈다.
    /// </summary>
    public sealed class FpsOverlay : MonoBehaviour
    {
        /// <summary>에디터 · 개발 빌드에서만 켠다 — 납품(출시) 빌드에는 안 뜬다(안정성 점검 10-30).</summary>
        private static readonly bool s_enabled = Debug.isDebugBuild;

        private const float Window = 0.5f;
        private const int SortingOrder = 1000;   // SystemPopup(999) 보다 위 — 팝업이 떠도 숫자는 보인다

        private TextMeshProUGUI _text;
        private ProfilerRecorder _memory;
        private float _elapsed;
        private int _frames;
        private float _worst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!s_enabled) return;
            var go = new GameObject("FpsOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsOverlay>();
        }

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0f;
            // 터치를 막지 않는다 — GraphicRaycaster 를 붙이지 않고 글자도 raycastTarget 을 끈다

            // 화면 맨 아래 가운데 — 왼쪽 위에 두었더니 판 레벨 칸 · 메인 얼굴을 덮었다(PD 2026-10-30 캡처)
            var bg = new GameObject("FpsBack", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, 2f);
            bgRect.sizeDelta = new Vector2(230f, 22f);
            var img = bg.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = false;

            var label = new GameObject("FpsText", typeof(RectTransform));
            label.transform.SetParent(bg.transform, false);
            var r = (RectTransform)label.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(6f, 0f);
            r.offsetMax = new Vector2(-4f, 0f);
            _text = label.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 15f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.raycastTarget = false;
            _text.color = Color.white;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.text = "FPS --";
        }

        private void OnEnable() => _memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");

        private void OnDisable() => _memory.Dispose();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            _frames++;
            if (dt > _worst) _worst = dt;
            if (_elapsed < Window) return;

            float fps = _frames / _elapsed;
            float low = _worst > 0f ? 1f / _worst : fps;
            long mb = _memory.Valid ? _memory.LastValue / (1024 * 1024) : 0;
            // 시험용 표시라 언어팩을 거치지 않는다(한글은 픽셀 폰트의 대체 폰트가 그린다)
            _text.SetText("FPS {0:0}  최저 {1:0}  메모리 {2:0}MB", fps, low, mb);
            _text.color = fps >= 50f ? Color.white : fps >= 30f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.4f);

            _elapsed = 0f;
            _frames = 0;
            _worst = 0f;
        }
    }
}
