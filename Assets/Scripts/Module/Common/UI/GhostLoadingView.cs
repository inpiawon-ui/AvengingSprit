using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Loading;
using GameFramework.Core.Module.Resource;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 씬 전환 가림막.
    ///
    /// 지금까지 프레임워크 기본 뷰(`FullLoadingView.CreateDefault`)를 그대로 썼는데
    /// 그 뷰는 **빈 GameObject** 다 — 그릴 것이 하나도 없어서 전환 내내 검은 화면이었다.
    /// 캐릭터 아틀라스를 스무 장 가까이 올리는 인게임 진입에서 특히 길게 보였다.
    ///
    /// `SystemPopup` 과 같은 이유로 **코드로 만든다**(06_ui.md 규약의 의도된 예외):
    /// 씬·패널이 하나도 안 올라온 순간에도 떠 있어야 하므로 프리팹·아틀라스에
    /// 기대면 안 된다. 유일한 외부 의존인 고스트 그림은 없으면 없는 대로 그린다.
    /// </summary>
    public sealed class GhostLoadingView : MonoBehaviour, ILoadingView
    {
        // SystemPopup(999) 바로 아래. 로딩 중에도 시스템 알림은 위에 떠야 한다.
        private const int SortingOrder = 900;
        private const float RefWidth = 720f;
        private const float RefHeight = 1280f;

        private const string GhostAtlasAddress = "atlas/unit_ghost";

        /// <summary>로딩 전용 그림 아틀라스. 없으면 인게임 고스트로 버틴다.</summary>
        private const string LoadingAtlasAddress = "atlas/loading";

        /// <summary>배경·프레임을 몇 번까지 찾아볼 것인가. 장 수는 그림이 정한다.</summary>
        private const int MaxArtFrames = 12;

        /// <summary>
        /// 고스트를 띄우는 크기. 원본이 96px 이므로 **정확히 2배**다.
        /// 1.5배(144)로 띄웠더니 대각선으로 흐르는 꼬리에서 픽셀이 불균등하게 늘어나
        /// 꼬리만 뭉개져 보였다 — 도트는 정수배가 아니면 반드시 티가 난다.
        /// </summary>
        private const float GhostSize = 192f;

        /// <summary>
        /// 고스트 루프 한 장의 시간. 4프레임이면 한 바퀴 0.64초 —
        /// 제자리에서 숨 쉬듯 뜨는 속도다. 더 느리면 멈춘 그림처럼 보인다.
        /// </summary>
        private const float FrameSeconds = 0.16f;

        /// <summary>위아래로 떠다니는 폭(px)과 주기(초).</summary>
        private const float BobAmplitude = 14f;
        private const float BobSeconds = 2.2f;

        /// <summary>막대가 목표치까지 따라붙는 속도. 즉시 채우면 툭툭 끊겨 보인다.</summary>
        private const float BarFollow = 6f;

        /// <summary>
        /// 가림막이 최소한 떠 있는 시간.
        ///
        /// 로드가 이보다 빨리 끝나면 막대가 62%쯤에서 화면이 넘어가 버린다 —
        /// 그러면 진행률이 거짓말이 되고, 로딩이 "끝났다"가 아니라 "잘렸다"로 보인다.
        /// 실제 로드가 더 길면 그쪽에 맞춘다. 짧을 때만 이 시간까지 기다린다.
        /// </summary>
        private const float MinShowSeconds = 2f;

        /// <summary>100%를 채운 뒤 눈에 남기는 시간. 차자마자 사라지면 채운 것을 못 본다.</summary>
        private const float FullHoldSeconds = 0.2f;

        /// <summary>
        /// 걷히는 데 걸리는 시간. 뚝 끄면 검은 틈이 한 박자 보인다 —
        /// 서서히 투명해지는 동안 뒤 화면이 비쳐 나오면 그 틈이 사라진다.
        /// </summary>
        private const float FadeOutSeconds = 0.35f;

        private static readonly Color Backdrop = new(0.027f, 0.035f, 0.063f, 1f);
        private static readonly Color Band = new(0.055f, 0.082f, 0.145f, 1f);
        private static readonly Color BarBack = new(0.086f, 0.106f, 0.161f, 1f);
        private static readonly Color BarEdge = new(0.208f, 0.259f, 0.361f, 1f);
        private static readonly Color GhostBlue = new(0.369f, 0.784f, 1f, 1f);
        private static readonly Color Dim = new(0.596f, 0.647f, 0.741f, 1f);

        /// <summary>
        /// 팁 한 줄이 머무는 시간. 한 문장을 읽고 한 박자 쉴 만큼은 돼야 한다 —
        /// 너무 빨리 넘기면 읽다 만 문장만 스쳐 간다.
        /// </summary>
        private const float TipSeconds = 4f;

        /// <summary>
        /// 기다리는 동안 읽을 것. 규칙을 설명하는 자리이기도 하다 —
        /// 이 게임의 규칙은 대부분 화면 어디에도 안 적혀 있다.
        /// </summary>
        private static readonly string[] Tips =
        {
            "고스트는 떠 있는 동안 체력이 깎인다. 몸은 빨리 고를수록 좋다.",
            "멈춰야 쏜다. 움직이는 동안에는 자동 공격이 멈춘다.",
            "몸을 스스로 놓아주면 그 몸은 이 방에서 다시 탈 수 없다.",
            "보스는 빼앗을 수 없다. 보스방은 타고 들어간 몸으로 끝내야 한다.",
            "조준 링이 회색 자물쇠면 아직 조건이 안 찼다. 더 두들기면 열린다.",
            "몸을 잃으면 Ghost HP 20%가 날아간다. 놓아주는 편이 싸다.",
            "회복 방에서는 Ghost HP 가 돌아온다. 다음 방을 보고 들어가라.",
            "몸이 바뀌어도 버프는 남는다. 스탯이 아니라 나에게 붙기 때문이다.",
            "머리 위 금색 조준 링이 지금 빙의 버튼이 노리는 몸이다.",
            "빙의 표식은 가까운 순으로 다섯까지만 뜬다. 나머지는 사거리 밖이다.",
            "붉은 X 가 붙은 몸은 내가 버린 몸이다. 이 방에서는 다시 못 탄다.",
            "액티브 스킬은 몸마다 다르다. 어떤 몸을 탔는지가 곧 어떤 액티브 스킬를 쓰는지다.",
            "주위에 뺏을 몸이 하나도 없으면 잠시 뒤 몸 하나가 던져진다. 대신 값이 비싸다.",
            "화면 가장자리 화살표는 창 밖에 뺏을 몸이 있다는 뜻이다.",
        };

        /// <summary>한국어 원문은 위 배열이 쥐고, 번역은 문자열 표 `tip.loading.{번호}` 에 있다.</summary>
        private static string TipOf(int index) => Localize.FromTable($"tip.loading.{index}", Tips[index]);

        private CanvasGroup _group;
        private Image _backdrop;
        private Image _band;
        private Image _ghost;
        private Image _barFill;
        private RectTransform _barFillRect;
        private RectTransform _ghostRect;
        private TextMeshProUGUI _percent;
        private TextMeshProUGUI _message;
        private TextMeshProUGUI _tip;

        private Sprite[] _frames;
        private Sprite[] _backdrops;
        private bool _artRequested;
        private float _frameTimer;
        private int _frameIndex;
        private float _bobTime;
        private float _ghostHome;

        private float _tipTimer;
        private int _tipIndex;

        /// <summary>가림막이 뜬 시각(무보정 시간). 최소 노출 시간을 여기서 잰다.</summary>
        private float _shownAt;

        /// <summary>
        /// 걷는 중인가. 이때는 바깥에서 오는 진행률을 **받지 않는다.**
        ///
        /// 인게임 진입에서 순서가 엇갈린다 — 씬의 `BattleDirector` 가 캐릭터를 다 올리고
        /// 100%를 찍은 뒤에야 `OnSceneLoaded` 가 도착해 진행률을 60%로 되돌리는 경우가 있다.
        /// 그러면 "100%가 될 때까지 기다린다" 는 조건이 영영 안 차서 가림막이 안 걷히고,
        /// 그 아래에서 전투가 그대로 진행된다(레벨업 창이 가림막 뒤에서 열린다).
        /// </summary>
        private bool _hiding;

        private float _progress;
        private float _shown;
        private float _barWidth;

        public static GhostLoadingView CreateDefault()
        {
            var root = new GameObject("[LoadingView]", typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var view = root.AddComponent<GhostLoadingView>();
            view.Build();
            root.SetActive(false);
            return view;
        }

        // ─────────────────────────────────────────────────────────
        public UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_group != null) _group.alpha = 1f;   // 지난번 페이드아웃 값이 남아 있을 수 있다

            _shownAt = Time.unscaledTime;
            _hiding = false;
            _progress = 0f;
            _shown = 0f;
            _bobTime = 0f;
            ApplyBar(0f);
            if (_message != null) _message.text = string.Empty;

            // 전환마다 다른 배경. 같은 그림만 나오면 몇 번만 봐도 로딩이 길게 느껴진다.
            PickBackdrop();

            // 팁은 시작 지점만 무작위다. 이후로는 순서대로 돌아 한 바퀴 안에 겹치지 않는다.
            _tipIndex = Random.Range(0, Tips.Length);
            _tipTimer = TipSeconds;
            if (_tip != null) _tip.text = TipOf(_tipIndex);

            // 그림은 한 번만 받아 둔다. 아직 안 왔으면 이번 전환에는 없는 대로 뜬다 —
            // 가림막이 즉시 뜨는 것 자체가 목적이라 그림을 기다리지 않는다.
            if (!_artRequested)
            {
                _artRequested = true;
                LoadArtAsync().Forget();   // fire-and-forget: 그림은 늦게 와도 된다
            }
            return UniTask.CompletedTask;
        }

        private void PickBackdrop()
        {
            if (_backdrops == null || _backdrops.Length == 0 || _backdrop == null) return;
            _backdrop.sprite = _backdrops[Random.Range(0, _backdrops.Length)];
            _backdrop.color = Color.white;
            // 그림에는 자기 그라데이션이 있다. 코드로 깐 띠는 그 위에서 겉돈다.
            if (_band != null) _band.gameObject.SetActive(false);
        }

        /// <summary>
        /// 걷는다. 다만 **막대를 100%까지 채우고 나서** 걷는다.
        ///
        /// 로드가 짧으면 최소 노출 시간(2초)까지 기다리고, 길면 로드가 끝난 그 순간부터
        /// 막대가 차오르는 시간만 더 쓴다. 어느 쪽이든 100%를 지나서 걷는다.
        /// </summary>
        public async UniTask HideAsync()
        {
            if (!gameObject.activeSelf || _hiding) return;

            _hiding = true;
            _progress = 1f;
            var token = this.GetCancellationTokenOnDestroy();

            float wait = MinShowSeconds - (Time.unscaledTime - _shownAt);
            if (wait > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(wait),
                                    DelayType.UnscaledDeltaTime, cancellationToken: token);

            // 막대는 Update 가 채운다. 실제로 다 찼는지 눈으로 확인하고 넘어간다.
            await UniTask.WaitUntil(() => _shown >= 1f, cancellationToken: token);
            await UniTask.Delay(System.TimeSpan.FromSeconds(FullHoldSeconds),
                                DelayType.UnscaledDeltaTime, cancellationToken: token);

            // ⚠ 그냥 끄면 **검은 화면이 한 박자 보인다.**
            //   가림막이 사라진 프레임과 뒤 화면이 처음 그려지는 프레임이 어긋나기 때문이다.
            //   뒤 화면이 이미 그려진 것을 확인한 뒤(한 프레임 넘기고) 서서히 걷는다 —
            //   페이드 중에는 뒤가 비쳐 보이므로 검은 틈이 생길 자리가 없다.
            await UniTask.NextFrame(cancellationToken: token);

            if (_group != null)
            {
                float t = 0f;
                while (t < FadeOutSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    _group.alpha = Mathf.Clamp01(1f - t / FadeOutSeconds);
                    await UniTask.NextFrame(cancellationToken: token);
                }
                _group.alpha = 0f;
            }

            gameObject.SetActive(false);
            if (_group != null) _group.alpha = 1f;   // 다음 전환을 위해 되돌려 둔다
        }

        public void UpdateProgress(float value)
        {
            if (_hiding) return;   // 걷는 중에는 뒤늦게 온 진행률로 되돌리지 않는다
            _progress = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 가운데 한 줄. 지금은 아무도 쓰지 않지만(진행은 막대가, 읽을거리는 팁이 맡는다)
        /// 프레임워크 계약이라 살려 둔다 — 로드 단계를 알려야 할 일이 생기면 여기다.
        /// </summary>
        public void UpdateMessage(string message)
        {
            if (_message != null) _message.text = message ?? string.Empty;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;   // 로딩 중에는 timeScale 을 믿을 수 없다

            _shown = Mathf.Lerp(_shown, _progress, 1f - Mathf.Exp(-BarFollow * dt));
            // 지수 보간은 목표에 영영 닿지 않는다. 눈에 구분 안 되는 거리에서 붙여 준다 —
            // 안 붙이면 100% 대기가 끝나지 않는다.
            if (Mathf.Abs(_shown - _progress) < 0.002f) _shown = _progress;
            ApplyBar(_shown);

            if (_ghostRect != null)
            {
                _bobTime += dt;
                float y = _ghostHome + Mathf.Sin(_bobTime / BobSeconds * 2f * Mathf.PI) * BobAmplitude;
                _ghostRect.anchoredPosition = new Vector2(0f, y);
            }

            if (_tip != null)
            {
                _tipTimer -= dt;
                if (_tipTimer <= 0f)
                {
                    _tipTimer += TipSeconds;
                    // 한 바퀴 돌 때까지 같은 팁이 다시 나오지 않는다
                    _tipIndex = (_tipIndex + 1) % Tips.Length;
                    _tip.text = TipOf(_tipIndex);
                }
            }

            if (_frames == null || _frames.Length < 2 || _ghost == null) return;
            _frameTimer -= dt;
            if (_frameTimer > 0f) return;
            _frameTimer += FrameSeconds;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            _ghost.sprite = _frames[_frameIndex];
        }

        private void ApplyBar(float t)
        {
            if (_barFillRect == null) return;
            _barFillRect.sizeDelta = new Vector2(_barWidth * Mathf.Clamp01(t), 0f);
            if (_percent != null) _percent.text = $"{Mathf.RoundToInt(t * 100f)}%";
        }

        /// <summary>
        /// 로딩 전용 그림을 올린다.
        ///
        /// 전용 아틀라스(`atlas/loading`)에서 **번호가 붙은 만큼** 찾는다 —
        /// 배경 `loading_bg_1..`, 고스트 `loading_ghost_1..`. 장 수를 코드에 못 박지
        /// 않으므로 그림을 더 그려 넣기만 하면 그대로 늘어난다.
        ///
        /// 아직 전용 그림이 없으면 인게임 고스트(정지 한 장)로 버틴다. 걷기 프레임은
        /// 꼬리를 옆으로 뻗은 **나아가는** 자세라 제자리에 떠 있는 화면에는 안 맞는다.
        /// </summary>
        private async UniTaskVoid LoadArtAsync()
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;

            var loading = await TryLoadAtlasAsync(res, LoadingAtlasAddress, warn: false);
            if (loading != null)
            {
                _backdrops = Collect(loading, "loading_bg_");
                var frames = Collect(loading, "loading_ghost_");
                if (frames != null)
                {
                    _frames = frames;
                    ApplyGhostFrame();
                }
                PickBackdrop();      // 이번 전환에도 바로 반영한다
            }
            if (_frames != null) return;

            var ghost = await TryLoadAtlasAsync(res, GhostAtlasAddress, warn: true);
            if (ghost == null) return;
            var idle = ghost.GetSprite("unit_ghost_s") ?? ghost.GetSprite("unit_ghost_s_walk1");
            if (idle == null) return;
            _frames = new[] { idle };
            ApplyGhostFrame();
        }

        private void ApplyGhostFrame()
        {
            if (_ghost == null || _frames == null || _frames.Length == 0) return;
            _frameIndex = 0;
            _frameTimer = FrameSeconds;
            _ghost.sprite = _frames[0];
            _ghost.color = Color.white;
        }

        private static async UniTask<SpriteAtlas> TryLoadAtlasAsync(IResourceManager res,
                                                                    string address, bool warn)
        {
            try { return await res.LoadAsync<SpriteAtlas>(address); }
            catch (System.Exception e)
            {
                // 그림이 없어도 가림막은 제 몫을 한다 — 막대와 문구는 그대로 뜬다.
                if (warn) Debug.LogWarning($"[Loading] 그림 로드 실패 {address} — {e.Message}");
                return null;
            }
        }

        /// <summary>`{prefix}1` 부터 끊길 때까지 모은다. 하나도 없으면 null.</summary>
        private static Sprite[] Collect(SpriteAtlas atlas, string prefix)
        {
            var list = new List<Sprite>(MaxArtFrames);
            for (int i = 1; i <= MaxArtFrames; i++)
            {
                var s = atlas.GetSprite(prefix + i);
                if (s == null) break;
                list.Add(s);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// 한글 본문용 고딕. 픽셀 폰트(`OriginalPixel SDF`)에는 한글이 없어 대체 폰트로
        /// 떨어지는데, 그러면 자간이 픽셀 폰트 기준으로 벌어져 낱말이 흩어져 보인다.
        /// FontPolicy 대로 **한글은 고딕을 직접** 쓴다. 대체 목록의 첫 폰트가 그 고딕이다.
        /// </summary>
        private static TMP_FontAsset KoreanFont(TMP_FontAsset pixel)
        {
            var fallbacks = pixel != null ? pixel.fallbackFontAssetTable : null;
            if (fallbacks != null)
                for (int i = 0; i < fallbacks.Count; i++)
                    if (fallbacks[i] != null && fallbacks[i].HasCharacter('가')) return fallbacks[i];
            return pixel;
        }

        private void Build()
        {
            var font = TMP_Settings.defaultFontAsset;   // 픽셀 — 영문·숫자 (FontPolicy)
            var kr = KoreanFont(font);                  // 고딕 — 한글 본문
            var root = (RectTransform)transform;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;

            _backdrop = NewImage("Backdrop", root, Backdrop);
            Stretch(_backdrop.rectTransform);
            _backdrop.raycastTarget = true;   // 로딩 중 조작을 막는다
            _backdrop.preserveAspect = false; // 배경은 화면을 꽉 채운다

            // 가운데 띠 — 통짜 검정이면 화면이 꺼진 것으로 보인다.
            // 전용 배경 그림이 들어오면 이 띠는 꺼진다(그림이 자기 그라데이션을 갖는다).
            _band = NewImage("Band", root, Band);
            Center(_band.rectTransform, RefWidth, 420f, 60f);

            _ghost = NewImage("Ghost", root, new Color(1f, 1f, 1f, 0f));  // 그림 오기 전에는 안 보인다
            _ghostRect = _ghost.rectTransform;
            Center(_ghostRect, GhostSize, GhostSize, 150f);
            _ghost.preserveAspect = true;
            _ghost.raycastTarget = false;
            _ghostHome = _ghostRect.anchoredPosition.y;

            var title = NewText("LoadingLabel", root, font, 34f, GhostBlue);
            Center(title.rectTransform, 400f, 46f, -10f);
            title.text = "LOADING";
            title.characterSpacing = 14f;

            _message = NewText("MessageText", root, kr, 22f, Dim);
            Center(_message.rectTransform, 620f, 32f, -56f);

            // 막대 — 테두리 오브젝트 안에 배경, 그 안에 채움. SystemPopup 과 같은 구조다.
            _barWidth = 460f;
            var edge = NewImage("BarEdge", root, BarEdge);
            Center(edge.rectTransform, _barWidth + 6f, 22f, -110f);
            var bg = NewImage("BarBg", edge.transform, BarBack);
            Stretch(bg.rectTransform, 3f);

            _barFill = NewImage("BarFill", bg.transform, GhostBlue);
            _barFillRect = _barFill.rectTransform;
            _barFillRect.anchorMin = new Vector2(0f, 0f);
            _barFillRect.anchorMax = new Vector2(0f, 1f);
            _barFillRect.pivot = new Vector2(0f, 0.5f);
            _barFillRect.anchoredPosition = Vector2.zero;
            _barFillRect.sizeDelta = new Vector2(0f, 0f);

            _percent = NewText("PercentText", root, font, 20f, Dim);
            Center(_percent.rectTransform, 200f, 28f, -140f);

            _tip = NewText("TipText", root, kr, 21f, Dim);
            var trt = _tip.rectTransform;
            trt.anchorMin = new Vector2(0.5f, 0f);
            trt.anchorMax = new Vector2(0.5f, 0f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, 120f);
            trt.sizeDelta = new Vector2(640f, 90f);
            _tip.textWrappingMode = TextWrappingModes.Normal;
            // 한글은 낱말 중간에서도 끊긴다. 낱말을 살리려면 어절 단위로 넘겨야 한다.
            _tip.wordWrappingRatios = 0.2f;
            _tip.lineSpacing = 12f;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent,
                                               TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Center(RectTransform rt, float w, float h, float y = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, y);
        }
    }
}
