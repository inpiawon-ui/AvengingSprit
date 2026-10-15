using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 망령의 탑 결과(화면 설계서 T8 클리어 · T9 실패, 시안 `mock_mode_T8`). 틀은 챕터 결과 창과 같다.
    ///   클리어 — 첫 클리어 보상(이번 주) · 다음 층 · 「다음 층 도전 ▶」
    ///   실패   — 이 층에 맞는 키운 몸 · 키우면 좋은 몸 · 「다시 ▶」
    /// 보상 · 저장은 전투가 이미 했다(클리어 순간). 여기는 보여 주고 다음 판 · 로비로 보낼 뿐이다.
    /// 노드는 `ModeInGameBuilder` 가 세운다.
    /// </summary>
    public sealed class TowerResultPopup : MonoBehaviour
    {
        private UIBinder _ui;
        private SpriteAtlas _faces;
        private bool _leaving;
        private int _goFloor;
        private bool _canGo;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("TowerResultLeaveButton", () => Leave(false));
            _ui.OnClick("TowerResultGoButton", () => Leave(true));
        }

        public void Show(TowerFinishedEvent e)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui — 활성화 시 최상단으로
            ShowAsync(e).Forget();          // fire-and-forget: 얼굴 아틀라스가 오면 초상을 채운다
        }

        private async UniTaskVoid ShowAsync(TowerFinishedEvent e)
        {
            Fill(e);
            Pop();
            if (_faces == null)
            {
                try { _faces = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>("atlas/hostface"); }
                catch (Exception ex) { Debug.LogWarning($"[탑 결과] 얼굴 아틀라스 로드 실패 — {ex.Message}"); }
                if (this != null) Fill(e);
            }
        }

        private void Fill(TowerFinishedEvent e)
        {
            bool clear = e.IsFloorCleared;
            _ui.SetText("TowerResultTitle", clear ? Localize.Format("ui.tower.result.clear", e.PlayedFloor)
                                                  : Localize.Format("ui.tower.result.fail", e.PlayedFloor));
            _ui.SetText("TowerResultSub", Localize.Get(e.PlayedNameKey));

            // 위 칸
            bool reward = clear && (e.RewardGold > 0 || (e.ShardHostKeys != null && e.ShardHostKeys.Length > 0));
            _ui.SetText("TowerResultRow1Label", clear ? Localize.Get("ui.tower.first_clear")
                                                      : Localize.Format("ui.tower.result.hint", e.FailFitTrained, e.FailFitTotal));
            _ui.SetActive("TowerResultCoin", reward);
            _ui.SetActive("TowerResultGoldText", reward);
            _ui.SetText("TowerResultGoldText", $"+{e.RewardGold:N0}");
            // 클리어 = 받은 조각, 실패 = 키우면 좋은 몸(개수 없이)
            string[] keys = clear ? e.ShardHostKeys : e.SuggestHostKeys;
            // 실패면 금화가 없다 — 초상을 금화 자리부터 왼쪽으로 붙인다
            ShiftFaces(clear ? 0f : FailFaceShift);
            for (int i = 0; i < 3; i++)
            {
                bool on = (clear ? reward : true) && keys != null && i < keys.Length;
                _ui.SetActive($"TowerResultFace{i}Frame", on);
                if (!on) continue;
                SetFace($"TowerResultFace{i}", keys[i]);
                _ui.SetText($"TowerResultFace{i}Text", clear && e.ShardCounts != null && i < e.ShardCounts.Length
                                                          ? $"×{e.ShardCounts[i]}" : string.Empty);
            }
            // 클리어인데 받은 것이 없다 = 이번 주에 이미 받은 층(첫 클리어 보상은 주에 한 번)
            _ui.SetText("TowerResultRow1Text", clear && !reward ? Localize.Get("ui.tower.first_clear_done") : string.Empty);

            // 아래 칸
            if (clear)
            {
                _ui.SetText("TowerResultRow2Label", Localize.Get("ui.tower.result.next"));
                _ui.SetText("TowerResultRow2Name", Localize.Format("ui.tower.floor", e.NextFloor) + " · " + Localize.Get(e.NextNameKey));
                _ui.SetText("TowerResultRow2Desc", Localize.Get(e.NextDescKey));
            }
            else
            {
                _ui.SetText("TowerResultRow2Label", Localize.Get("ui.tower.result.next_try"));
                _ui.SetText("TowerResultRow2Name", e.SuggestHostKeys != null && e.SuggestHostKeys.Length > 0
                    ? Localize.Format("ui.tower.result.suggest", HostName(e.SuggestHostKeys[0])) : string.Empty);
                _ui.SetText("TowerResultRow2Desc", Localize.Get("ui.tower.result.fail_desc"));
            }

            // 오늘 입장
            int max = CoreModule.TryGet<IModeProgress>(out var m) && m.Table != null ? m.Table.FreeEntriesPerDay : 3;
            _ui.SetText("TowerResultEntryLabel", Localize.Get("ui.tower.entries_label"));
            _ui.SetText("TowerResultEntryText", Localize.Format("ui.tower.entries", e.EntriesLeft, max));
            ModeResultPips.Set(_ui, "TowerResultPip", e.EntriesLeft);

            // 버튼 — 입장이 없으면 「다음 층 도전」 대신 나가기만
            _canGo = e.EntriesLeft > 0;
            _goFloor = clear ? e.NextFloor : e.PlayedFloor;
            _ui.SetText("TowerResultLeaveText", Localize.Get("ui.tower.leave"));
            // 입장이 없으면 버튼은 남기고 흐리게 막는다 — 숨기면 틀에 그려진 버튼 자리가 빈다
            if (_ui.Get<Button>("TowerResultGoButton") is { } goBtn) goBtn.interactable = _canGo;
            if (_ui.Get<Image>("TowerResultGoButton") is { } goImg) goImg.color = _canGo ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            _ui.SetText("TowerResultGoText", !_canGo ? Localize.Get("ui.tower.no_entries")
                : clear ? Localize.Format("ui.tower.start", e.NextFloor) : Localize.Get("ui.tower.retry"));
        }

        /// <summary>금화 칸(아이콘 + 금액) 폭만큼 — 실패 창에서 초상을 왼쪽으로 붙일 때.</summary>
        private const float FailFaceShift = -132f;
        private readonly float[] _faceHomeX = new float[3];
        private bool _faceHomeSaved;

        private void ShiftFaces(float dx)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!(_ui.Find($"TowerResultFace{i}Frame") is RectTransform r)) continue;
                if (!_faceHomeSaved) _faceHomeX[i] = r.anchoredPosition.x;
                r.anchoredPosition = new Vector2(_faceHomeX[i] + dx, r.anchoredPosition.y);
            }
            _faceHomeSaved = true;
        }

        private void SetFace(string node, string hostKey)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            var s = _faces != null ? _faces.GetSprite($"face_{hostKey}") : null;
            img.sprite = s;
            img.enabled = s != null;
        }

        private static string HostName(string key)
            => CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) && p.GetHost(key) is { } h ? h.DisplayName : key;

        /// <summary>창이 살짝 커졌다 제자리 — 챕터 결과 창과 같은 OutBack.</summary>
        private void Pop()
        {
            var content = _ui.Find("TowerResultContent");
            if (content != null) ModeResultPips.PopAsync(content, this).Forget();   // fire-and-forget: 0.38초 등장
        }

        private void Leave(bool go)
        {
            if (_leaving) return;
            _leaving = true;
            LeaveAsync(go && _canGo).Forget();   // fire-and-forget: 씬 전환
        }

        private async UniTaskVoid LeaveAsync(bool go)
        {
            // 「다음 층 도전」 · 「다시」 — 입장 1번을 쓰고 바로 다시 인게임(탑 3-2 의 6). 못 쓰면 로비
            if (go && CoreModule.TryGet<IModeProgress>(out var modes) && modes.TryUseTowerEntry())
            {
                RunModeContext.SetTowerFloor(_goFloor);
                await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
                {
                    SceneName = SceneNames.InGame,
                    LoadingStyle = LoadingStyle.Overlay,
                });
                return;
            }
            RunModeContext.Clear();
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
