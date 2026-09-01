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
        /// ⚠ **정상 동작(첫 실행에만)으로 돌리려면 이 한 줄을 `false` 로 바꾼다.**
        ///   사용자가 "이제 꺼도 된다" 고 할 때까지 `true` 로 둔다 (2026-09-01 지시).
        /// </summary>
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

            var cutT = _ui.Find("CutImage");
            if (cutT != null)
            {
                _cut = cutT.GetComponent<Image>();
                _cutGroup = cutT.GetComponent<CanvasGroup>() ?? cutT.gameObject.AddComponent<CanvasGroup>();
            }
            var backT = _ui.Find("CutImageBack");
            if (backT != null) _cutBack = backT.GetComponent<Image>();

            _box = _ui.Find("TextBox");
            if (_box != null)
                _boxGroup = _box.GetComponent<CanvasGroup>() ?? _box.gameObject.AddComponent<CanvasGroup>();

            _ui.SetText("SkipText", "건너뛰기");
            _ui.OnClick("SkipButton", Skip);
            _ui.OnClick("TouchArea", OnTapped);
            gameObject.AddComponent<BackButtonRouter>();

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

            if (_autoLeft <= 0f) return;
            _autoLeft -= Time.deltaTime;
            if (_autoLeft <= 0f) Next();
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
            if (cut.HasLine) _ui.SetText("LineText", cut.Line);

            ShowArtAsync(cut).Forget();   // fire-and-forget: 그림이 한 프레임 늦어도 된다
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
            if (string.IsNullOrEmpty(_heldAddress)) return;
            if (CoreModule.TryGet<IResourceManager>(out var res)) res.Release(_heldAddress);
            _heldAddress = null;
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

        private void OnDestroy() => Release();
    }
}
