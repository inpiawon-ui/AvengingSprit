using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Opening
{
    /// <summary>
    /// 오프닝 화면. 컷을 한 장씩 넘기고, 끝나면 게임으로 넘어간다.
    ///
    /// ── 조작 ─────────────────────────────────────────────────────
    ///   화면 탭   다음 컷 (자동으로 넘어가는 컷은 무시)
    ///   건너뛰기  전체 스킵
    ///
    /// ── 한 번만 본다 ─────────────────────────────────────────────
    /// 첫 실행에만 뜬다. 한 번 보고 나면 `PlayerPrefs` 에 표시가 남아 다음부터 건너뛴다.
    /// 스킵해도 본 것으로 친다 — 스킵은 "이미 안다" 는 뜻이다.
    ///
    /// ⚠ 그림은 **한 장씩** 불러오고 넘어갈 때 놓아 준다. 스무 장을 한꺼번에 물면
    ///   오프닝에서만 쓰는 640×640 무압축 스무 장이 판 내내 메모리에 남는다.
    /// </summary>
    public sealed class OpeningMainUI : MonoBehaviour, IBackTarget
    {
        /// <summary>본 적 있는가. 한 번 보면 다음부터 안 뜬다.</summary>
        public const string SeenKey = "AVSR.OpeningSeen";

        /// <summary>
        /// **지금은 매번 뜬다.** 본 기록을 무시한다.
        ///
        /// 오프닝을 만드는 동안은 확인할 때마다 다시 봐야 하는데, 한 번 건너뛰면
        /// 기록이 남아 그다음부터 안 떠서 "오프닝이 안 나온다" 로 보인다 —
        /// 실제로 그렇게 한 번 헤맸다.
        ///
        /// ⚠ 2026-09-02 에 껐다 — "이제 한 번만 보면 안 나오게" 지시.
        ///   확인하다 다시 보려면 메뉴 `Tools/Game/테스트 — 오프닝 다시 보기` 로
        ///   본 기록을 지운다. 이 상수를 다시 켤 일은 없다.
        /// </summary>
        /// ⚠ 2026-09-15 에 다시 켰다 — 시연용이라 **켤 때마다** 오프닝이 나와야 한다. 건너뛰기 버튼은 그대로다.
        public const bool AlwaysShow = true;

        private const string AddressPrefix = "cutscene/";

        /// <summary>
        /// 컷이 바뀔 때 새 그림이 스며드는 시간.
        ///
        /// ⚠ **연속 프레임(발포·유령)에는 안 쓴다.** 0.13초짜리 프레임에 0.18초 페이드를
        ///   걸면 그림이 뜨기도 전에 다음 장으로 넘어가 화면이 깜박인다.
        /// </summary>
        private const float FadeSeconds = 0.18f;

        private UIBinder _ui;
        private Image _cut;          // 앞 겹 — 지금 컷
        private Image _cutBack;      // 뒷 겹 — 지나가는 컷을 받쳐 준다
        private Image _ghost;        // 관 안 유령 — 컷 위에 얹는다
        private Sprite[] _ghostFrames;
        private float _ghostTimer;
        private int _ghostIndex;
        private CanvasGroup _cutGroup;
        private Transform _box;
        private CanvasGroup _boxGroup;

        private OpeningCut[] _cuts;
        private int _index = -1;
        private float _autoLeft;
        private float _fadeLeft;
        private bool _leaving;

        /// <summary>지금 화면에 물고 있는 그림. 다음 컷으로 넘어갈 때 놓아 준다.</summary>
        private string _heldAddress;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            // 본문 폰트를 지금 언어 것으로 — 일본어를 한글 폰트로 그리면 한자가 한국식으로 나온다
            Localize.ApplyFonts(transform);

            var cutT = _ui.Find("CutImage");
            if (cutT != null)
            {
                _cut = cutT.GetComponent<Image>();
                _cutGroup = cutT.GetComponent<CanvasGroup>() ?? cutT.gameObject.AddComponent<CanvasGroup>();
            }
            var backT = _ui.Find("CutImageBack");
            if (backT != null) _cutBack = backT.GetComponent<Image>();
            var ghostT = _ui.Find("GhostImage");
            if (ghostT != null) _ghost = ghostT.GetComponent<Image>();

            _box = _ui.Find("TextBox");
            if (_box != null)
                _boxGroup = _box.GetComponent<CanvasGroup>() ?? _box.gameObject.AddComponent<CanvasGroup>();

            _ui.SetText("SkipText", Localize.Get("ui.opening.skip"));
            _ui.OnClick("SkipButton", Skip);
            _ui.OnClick("TouchArea", OnTapped);
            gameObject.AddComponent<BackButtonRouter>();
            GameSound.Music("screen.opening");

            _cuts = OpeningCuts.All();
            Next();
        }

        private void Update()
        {
            if (_fadeLeft > 0f)
            {
                _fadeLeft -= Time.deltaTime;
                float t = Mathf.Clamp01(1f - _fadeLeft / FadeSeconds);
                if (_cutGroup != null) _cutGroup.alpha = t;
                if (_boxGroup != null) _boxGroup.alpha = t;
                // 새 그림이 다 스며들었으면 받쳐 주던 겹을 내린다.
                if (_fadeLeft <= 0f && _cutBack != null) _cutBack.enabled = false;
            }

            TickGhost(Time.deltaTime);

            if (_autoLeft <= 0f) return;
            _autoLeft -= Time.deltaTime;
            if (_autoLeft <= 0f) Next();
        }

        /// <summary>
        /// 관 안 유령을 돌린다. 네 장이 같은 자리·같은 진하기라 자세만 바뀐다 —
        /// **옅어지는 것은 알파가 만든다.**
        /// </summary>
        private void TickGhost(float dt)
        {
            if (_ghost == null || !_ghost.enabled || _ghostFrames == null) return;
            _ghostTimer -= dt;
            if (_ghostTimer > 0f) return;
            _ghostTimer = OpeningCuts.GhostFrameSeconds;
            _ghostIndex = (_ghostIndex + 1) % _ghostFrames.Length;
            var s = _ghostFrames[_ghostIndex];
            if (s != null) _ghost.sprite = s;
        }

        /// <summary>
        /// 관 안 유령을 켜거나 끈다.
        ///
        /// 네 장은 **투명 배경 PNG** 라 그림 위에 그대로 얹힌다 — 경계가 이미 관 안쪽이다.
        /// 진하기는 <see cref="OpeningCuts.GhostAlpha"/> 하나로 정한다.
        /// 본편 HUD 게이지도 같은 함수를 쓴다 — 여기서 본 것을 거기서 바로 읽게 하려면
        /// 두 곳이 같은 값이어야 한다.
        /// </summary>
        private async UniTaskVoid ApplyGhostAsync(OpeningCut cut)
        {
            if (_ghost == null) return;

            if (!cut.Ghost)
            {
                _ghost.enabled = false;
                return;
            }

            if (_ghostFrames == null)
            {
                if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
                var frames = new Sprite[OpeningCuts.GhostFrames.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    try
                    {
                        frames[i] = await res.LoadAsync<Sprite>(
                            AddressPrefix + OpeningCuts.GhostFrames[i], gameObject.scene.name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[오프닝] 유령 프레임 못 불러옴: {OpeningCuts.GhostFrames[i]} — {e.Message}");
                    }
                }
                if (this == null) return;
                _ghostFrames = frames;
            }

            // 불러오는 사이에 넘어갔을 수 있다.
            if (_index < 0 || _index >= _cuts.Length || !_cuts[_index].Ghost) return;

            _ghostIndex = 0;
            _ghostTimer = OpeningCuts.GhostFrameSeconds;
            if (_ghostFrames[0] != null) _ghost.sprite = _ghostFrames[0];
            var c = Color.white;
            c.a = OpeningCuts.GhostAlpha(cut.Energy);
            _ghost.color = c;
            _ghost.enabled = true;
        }

        // ── 넘기기 ───────────────────────────────────────────────

        /// <summary>탭. 자동으로 넘어가는 컷은 탭을 먹지 않는다 — 애니메이션이 끊긴다.</summary>
        private void OnTapped()
        {
            if (_leaving) return;
            if (_index >= 0 && _index < _cuts.Length && _cuts[_index].IsAuto) return;
            Next();
        }

        private void Next()
        {
            if (_leaving) return;
            _index++;
            if (_index >= _cuts.Length) { Finish(); return; }

            var cut = _cuts[_index];
            _autoLeft = cut.AutoSeconds;

            // ⚠ **여기서 페이드를 시작하지 않는다.** 그림은 비동기로 온다 —
            //   도착 전에 알파를 0 으로 떨어뜨리면 옛 그림이 흐려졌다가 새 그림이
            //   튀어나온다. 페이드는 그림이 실제로 바뀌는 순간(`ShowArtAsync`)에 건다.

            // 글상자 — 대사가 없는 컷은 아예 숨긴다. 빈 상자가 떠 있으면 화면을 먹는다.
            if (_box != null) _box.gameObject.SetActive(cut.HasLine);
            // 대사 원문은 컷 표가 쥔다. 키는 전체 순번이다 — 같은 그림을 두 번 쓰는 컷이 있어 그림 키로는 못 가른다
            if (cut.HasLine) _ui.SetText("LineText", Localize.FromTable($"opening.line.{_index}", cut.Line));

            ShowArtAsync(cut).Forget();     // fire-and-forget: 그림이 한 프레임 늦어도 된다
            ApplyGhostAsync(cut).Forget();  // fire-and-forget: 유령도 마찬가지다
        }

        /// <summary>
        /// 이 컷이 **연속 프레임**인가. 발포 5장·유령 4장처럼 앞뒤가 같은 장면이면
        /// 겹쳐 넘기지 않고 그냥 갈아 끼운다 — 0.13초짜리에 0.18초 페이드를 걸면
        /// 그림이 뜨기도 전에 다음 장이라 화면이 깜박인다.
        ///
        /// 판단 기준은 **앞 컷도 자동이었는가** 다. 연속 프레임의 첫 장은
        /// 새 장면이므로 겹쳐 들어오고, 둘째 장부터 갈아 끼운다.
        /// </summary>
        private bool IsSameShot(int index)
            => index > 0 && _cuts[index].IsAuto && _cuts[index - 1].IsAuto;

        /// <summary>
        /// 이 컷의 그림을 띄운다.
        ///
        /// ⚠ **먼저 놓고 나서 부르지 않는다.** 놓아 버리면 새 그림이 오는 동안 화면이
        ///   한 번 비어 깜빡인다. 새것을 받은 다음에 지난 것을 놓는다.
        ///
        /// ⚠ 그림이 **실제로 바뀌는 순간**에만 페이드를 건다. 넘기자마자 걸면
        ///   아직 옛 그림인 채로 흐려졌다가 새 그림이 튀어나온다.
        /// </summary>
        private async UniTaskVoid ShowArtAsync(OpeningCut cut)
        {
            if (_cut == null) return;

            if (!cut.HasArt)
            {
                // 그림 없는 컷(암전). 겹쳐 넘길 것이 없으니 그냥 끈다.
                if (_cutBack != null) _cutBack.enabled = false;
                _cut.enabled = false;
                if (_cutGroup != null) _cutGroup.alpha = 1f;
                _fadeLeft = 0f;
                if (_boxGroup != null) _boxGroup.alpha = 1f;
                Release();
                return;
            }

            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;

            string address = AddressPrefix + cut.Key;
            Sprite sprite = null;
            try { sprite = await res.LoadAsync<Sprite>(address, gameObject.scene.name); }
            catch (Exception e) { Debug.LogWarning($"[오프닝] 컷 못 불러옴: {address} — {e.Message}"); }

            // 불러오는 사이에 사용자가 넘겼을 수 있다. 늦게 온 그림을 덮어씌우면 안 된다.
            if (this == null || _index < 0 || _index >= _cuts.Length
                || _cuts[_index].Key != cut.Key) { res.Release(address); return; }

            string previous = _heldAddress;
            _heldAddress = address;

            if (sprite == null) { _cut.enabled = false; return; }

            // 회상 컷은 색을 뺀다. 그림은 프롤로그 것 그대로다 — 원작도 그렇게 다시 쓴다.
            if (cut.Sepia) sprite = SepiaOf(sprite, cut.Key);

            if (IsSameShot(_index))
            {
                // 같은 장면의 다음 프레임 — 갈아 끼우기만 한다.
                if (_cutBack != null) _cutBack.enabled = false;
                _fadeLeft = 0f;
                if (_cutGroup != null) _cutGroup.alpha = 1f;
                if (_boxGroup != null) _boxGroup.alpha = 1f;
                _cut.sprite = sprite;
                _cut.enabled = true;
            }
            else
            {
                // 새 장면 — 지나가는 그림을 뒤에 받쳐 두고 그 위로 스며들게 한다.
                // 검은 화면을 거치지 않으므로 끊겨 보이지 않는다.
                if (_cutBack != null && _cut.enabled && _cut.sprite != null)
                {
                    _cutBack.sprite = _cut.sprite;
                    _cutBack.enabled = true;
                }
                _cut.sprite = sprite;
                _cut.enabled = true;
                _fadeLeft = FadeSeconds;
                if (_cutGroup != null) _cutGroup.alpha = 0f;
                if (_boxGroup != null) _boxGroup.alpha = 0f;
            }

            if (!string.IsNullOrEmpty(previous) && previous != address) res.Release(previous);
        }

        private void Release()
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) { _heldAddress = null; return; }
            if (!string.IsNullOrEmpty(_heldAddress)) res.Release(_heldAddress);
            _heldAddress = null;
        }

        /// <summary>유령 네 장을 놓아 준다. 오프닝이 끝날 때만 부른다.</summary>
        private void ReleaseGhost()
        {
            if (_ghostFrames == null) return;
            _ghostFrames = null;
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
            foreach (var k in OpeningCuts.GhostFrames) res.Release(AddressPrefix + k);
        }

        // ── 끝 ───────────────────────────────────────────────────

        private void Skip()
        {
            if (_leaving) return;
            // 스킵은 "이미 안다" 는 뜻이다. 본 것으로 친다.
            Finish();
        }

        /// <summary>
        /// 오프닝이 끝났다. **로비로 간다.**
        ///
        /// ⚠ 한때 전투로 바로 보냈다. 컷신이 「딸을 구해 주게」로 끝나니 그대로
        ///   싸우러 가는 것이 자연스럽다고 봤는데, 그러면 **어떤 몸으로 들어갈지
        ///   고르는 자리가 통째로 없어진다.** 로비가 호스트를 고르는 화면이다.
        ///   오프닝은 이야기의 문이지 전투의 문이 아니다.
        /// </summary>
        private void Finish()
        {
            if (_leaving) return;
            _leaving = true;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Release();
            ReleaseGhost();
            GoLobbyAsync().Forget();   // fire-and-forget: 씬 전환을 기다릴 일이 없다
        }

        private async UniTaskVoid GoLobbyAsync()
        {
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }

        /// <summary>뒤로가기는 스킵과 같다. 오프닝에서 돌아갈 화면이 없다.</summary>
        public bool OnBackPressed()
        {
            Skip();
            return true;
        }

        private void OnDestroy() { Release(); ReleaseGhost(); DropSepia(); }

        // ── 회상 컷 — 색을 뺀다 ──────────────────────────────────────
        //
        // 원작 시작 컷신에서 납치 장면만 단색이다. 지금 벌어지는 일이 아니라
        // 노인이 들려주는 지난 일이라서다 — **색을 빼는 것이 곧 시제 표시**다.
        //
        // 그림을 따로 받지 않는 이유는 `OpeningCuts.Sepia` 주석에 적어 두었다.
        // 여기서는 **어떤 색으로 빼느냐**만 정한다.
        //
        // ── 색은 상상하지 않고 원작에서 뽑았다 ──────────────────────
        // `Reference/Original/Miscellaneous - Start Cutscene.png` 의 3번 컷
        // 안쪽 100×100 을 세어 보면 색이 20개뿐이고, 자홍 테두리를 빼면
        // 9단계가 하나의 사다리 위에 놓인다. 밝기(L)와 각 채널의 차가 **일정하다**:
        //
        //   L  75.9 → (90, 74, 49)     L +14.1 · −1.9 · −26.9
        //   L 108.9 → (123,107, 82)    L +14.1 · −1.9 · −26.9
        //   L 141.9 → (156,140,115)    L +14.1 · −1.9 · −26.9
        //   L 191.3 → (206,189,165)    L +14.7 · −2.3 · −26.3
        //
        // 곱이 아니라 **더하기**다. 그래서 한 식으로 끝난다.
        private const float SepiaR = 14.5f, SepiaG = -2f, SepiaB = -26.5f;

        /// <summary>
        /// 검정이 붉게 뜨는 것을 막는 문턱. 오프셋을 이 밝기까지 서서히 넣는다.
        ///
        /// ⚠ 그냥 더하면 배경 (0,0,0) 이 (15,0,0) 이 되어 **화면 전체가 검붉어진다.**
        ///   원작의 가장 어두운 칸도 (8,8,0) 이라 거의 검정이다. 어두운 쪽은 빼 준다.
        /// </summary>
        private const float SepiaFloor = 40f;

        private Texture2D _sepiaTex;
        private Sprite _sepiaSprite;
        private string _sepiaFor;

        /// <summary>
        /// 색을 뺀 한 장을 만들어 돌려준다. 같은 컷이면 만들어 둔 것을 그대로 준다.
        ///
        /// ⚠ 원본 텍스처는 건드리지 않는다 — 프롤로그에서 **컬러 그대로** 또 나온다.
        /// </summary>
        private Sprite SepiaOf(Sprite src, string key)
        {
            if (src == null) return null;
            if (_sepiaFor == key && _sepiaSprite != null) return _sepiaSprite;

            var tex = src.texture;
            Color32[] px;
            // 읽기 설정(`isReadable`)이 꺼져 있으면 예외가 난다.
            // 그때는 **컬러로라도 보여 준다** — 빈 화면보다 낫다.
            try { px = tex.GetPixels32(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[오프닝] 세피아 못 만듦({key}) — 컬러로 간다: {e.Message}");
                return src;
            }

            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float l = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                float f = Mathf.Min(1f, l * (1f / SepiaFloor));
                px[i] = new Color32(
                    (byte)Mathf.Clamp(l + SepiaR * f, 0f, 255f),
                    (byte)Mathf.Clamp(l + SepiaG * f, 0f, 255f),
                    (byte)Mathf.Clamp(l + SepiaB * f, 0f, 255f),
                    c.a);
            }

            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,      // 픽셀아트 — 원본과 같은 설정
                wrapMode = TextureWrapMode.Clamp,
            };
            copy.SetPixels32(px);
            copy.Apply(false, false);

            DropSepia();
            _sepiaTex = copy;
            _sepiaSprite = Sprite.Create(copy, new Rect(0f, 0f, copy.width, copy.height),
                                         new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            _sepiaFor = key;
            return _sepiaSprite;
        }

        /// <summary>만들어 둔 세피아를 버린다. 이건 Addressable 이 아니라 우리가 만든 것이라 직접 지운다.</summary>
        private void DropSepia()
        {
            if (_sepiaSprite != null) UnityEngine.Object.Destroy(_sepiaSprite);
            if (_sepiaTex != null) UnityEngine.Object.Destroy(_sepiaTex);
            _sepiaSprite = null;
            _sepiaTex = null;
            _sepiaFor = null;
        }
    }
}
