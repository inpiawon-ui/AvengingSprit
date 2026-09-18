using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 챕터 클리어 결과 (기획 2026-09-18 · 시안 ui_new_chapter_result_v1).
    ///
    /// 클리어했을 때만 뜬다 — 죽으면 보상이 없어 알림창 하나로 끝난다.
    /// 골드와 상자는 **전투가 이미 넣었다.** 여기는 보여 주고 OK 로 로비에 보낼 뿐이다.
    /// 상자 칸이 가득 차 상자를 못 받았으면 빨간 띠로 알린다(골드는 그래도 받았다).
    ///
    /// 노드는 `ChapterScreensBuilder` 가 세운다. 그림은 발주 부품이 오면 같은 이름으로 갈아 끼운다.
    /// </summary>
    public sealed class ChapterResultPopup : MonoBehaviour
    {
        [Serializable]
        private struct ChestArt
        {
            public string Key;
            public Sprite Sprite;
        }

        /// <summary>제목 그림 「CHAPTER n CLEAR」 — 챕터 1 ~ 6. 없으면 글자로 대신 적는다.</summary>
        [SerializeField] private Sprite[] _titles = new Sprite[6];
        [SerializeField] private ChestArt[] _chestArts = Array.Empty<ChestArt>();

        /// <summary>못 받은 상자는 흐리게 — 「이걸 받을 뻔했다」가 보이되 받은 것처럼 보이면 안 된다.</summary>
        private static readonly Color LostChest = new(1f, 1f, 1f, 0.4f);

        private UIBinder _ui;
        private bool _leaving;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("ResultOkButton", OnOk);
        }

        public void Show(StageFinishedEvent e)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로

            int ch = Mathf.Clamp(e.FinishedChapter, 1, 6);
            var titleSprite = _titles != null && ch - 1 < _titles.Length ? _titles[ch - 1] : null;
            var title = _ui.Get<Image>("ResultTitleImage");
            if (title != null)
            {
                title.sprite = titleSprite;
                title.enabled = titleSprite != null;
            }
            _ui.SetActive("ResultTitleText", titleSprite == null);
            _ui.SetText("ResultTitleText", $"CHAPTER {ch} CLEAR");

            _ui.SetText("ResultSubText", Localize.Get($"stage.{ch}.1.name"));
            _ui.SetText("ResultGoldLabelText", Localize.Get("ui.result.gold_label"));
            _ui.SetText("ResultGoldValueText", $"+{e.RewardGold:N0}");

            var chest = _ui.Get<Image>("ResultChestArt");
            if (chest != null)
            {
                var art = ChestArtOf(e.RewardChestKey);
                if (art != null) chest.sprite = art;
                chest.color = e.ChestAccepted ? Color.white : LostChest;
            }
            _ui.SetText("ResultChestNameText", Localize.Get($"chest.{e.RewardChestKey}.name"));

            _ui.SetActive("ResultWarnBar", !e.ChestAccepted);
            _ui.SetText("ResultWarnText", Localize.Get("ui.lobby.chest.full"));
            _ui.SetText("ResultOkText", "OK");
        }

        private Sprite ChestArtOf(string key)
        {
            for (int i = 0; i < _chestArts.Length; i++)
                if (_chestArts[i].Key == key) return _chestArts[i].Sprite;
            return null;
        }

        private void OnOk()
        {
            if (_leaving) return;   // 두 번 눌러 씬을 두 번 부르지 않게
            _leaving = true;
            GoLobbyAsync().Forget();   // fire-and-forget: 씬 전환 대기 불필요
        }

        private static async UniTaskVoid GoLobbyAsync()
        {
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
