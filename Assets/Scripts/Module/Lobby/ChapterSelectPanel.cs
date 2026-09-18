using System;
using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 챕터 선택 (기획 2026-09-18 · 시안 ui_new_chapter_select_v1). 로비 PLAY → 여기 → 호스트 선택 → 게임.
    ///
    /// 카드 여섯 장 중 **열린 챕터만** 고를 수 있다. 1챕터는 처음부터 열려 있고,
    /// 나머지는 앞 챕터를 깨면 열린다(`IPlayerDataService.UnlockedChapter`).
    /// 카드를 누르면 금테로 **고르고**, START 로 들어간다. 잠긴 카드는 어둡게 두고 자물쇠를 얹는다.
    ///
    /// 노드는 `ChapterScreensBuilder` 가 세운다. 그림은 발주 부품이 오면 같은 이름으로 갈아 끼운다.
    /// </summary>
    public sealed class ChapterSelectPanel : MonoBehaviour
    {
        [SerializeField] private Sprite _frameNormal;
        [SerializeField] private Sprite _frameSelected;

        /// <summary>카드 오른쪽 아래 상자 — 그 챕터를 깨면 받는 등급.</summary>
        [SerializeField] private Sprite[] _chapterChest = new Sprite[SlotCount];

        private const int SlotCount = PlayerDataService.ChapterCount;

        /// <summary>잠긴 카드 그림의 밝기. 목록이 한눈에 「어디까지 왔나」로 읽히게.</summary>
        private static readonly Color LockedArt = new(0.45f, 0.45f, 0.5f, 1f);

        private static readonly Color NoSelected = new(1f, 0.86f, 0.3f);
        private static readonly Color NoNormal = new(0.78f, 0.82f, 0.88f);

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();
        private int _picked = 1;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("ChapterSelectBackButton", Close);
            _ui.OnClick("ChapterStartButton", OnStart);
            for (int i = 0; i < SlotCount; i++)
            {
                int chapter = i + 1;   // 클로저가 루프 변수를 잡지 않게 복사한다
                var card = _ui.Find($"ChapterCard{chapter}");
                var button = card != null ? card.GetComponent<Button>() : null;
                if (button != null) button.onClick.AddListener(() => Pick(chapter));
            }
            // ⚠ 여기서 `SetActive(false)` 를 하지 마라 — `HostSelectPanel` 과 같은 이유다.
            //   처음 닫아 두는 것은 주인인 `LobbyMainUI` 가 한다.
        }

        private void OnEnable()
        {
            if (!CoreModule.TryGet<IEventBus>(out var bus)) return;
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            _picked = _player != null && _player.IsReady ? _player.SelectedChapter : 1;
            Refresh();
            RefreshCurrency();
        }

        public void Close() => gameObject.SetActive(false);

        private int Unlocked => _player != null && _player.IsReady ? _player.UnlockedChapter : 1;

        private void Refresh()
        {
            // 제목 판 그림이 오기 전에는 글자로 대신 적는다 — 판이 오면 글자는 끈다
            var title = _ui.Get<Image>("ChapterSelectTitle");
            _ui.SetActive("ChapterSelectTitleText", title == null || title.sprite == null);
            _ui.SetText("ChapterSelectTitleText", "CHAPTER SELECT");
            _ui.SetText("ChapterStartText", "START");

            for (int i = 0; i < SlotCount; i++)
            {
                int chapter = i + 1;
                var card = _ui.Find($"ChapterCard{chapter}");
                if (card == null) continue;
                bool open = chapter <= Unlocked;
                bool picked = chapter == _picked;

                var no = _ui.Find(card, "ChapterCardNoText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (no != null)
                {
                    no.text = $"CHAPTER {chapter}";
                    no.color = picked ? NoSelected : NoNormal;
                }
                var name = _ui.Find(card, "ChapterCardNameText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (name != null) name.text = Localize.Get($"stage.{chapter}.1.name");

                var frame = _ui.Find(card, "ChapterCardFrame")?.GetComponent<Image>();
                if (frame != null)
                {
                    var want = picked && _frameSelected != null ? _frameSelected : _frameNormal;
                    if (want != null) frame.sprite = want;
                }
                var art = _ui.Find(card, "ChapterCardArt")?.GetComponent<Image>();
                if (art != null) art.color = open ? Color.white : LockedArt;

                var lockIcon = _ui.Find(card, "ChapterCardLockIcon");
                if (lockIcon != null) lockIcon.gameObject.SetActive(!open);

                var chest = _ui.Find(card, "ChapterCardChestIcon")?.GetComponent<Image>();
                if (chest != null && _chapterChest != null && i < _chapterChest.Length && _chapterChest[i] != null)
                    chest.sprite = _chapterChest[i];
            }
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("ChapterGoldText", _player.Gold.ToString("N0"));
            _ui.SetText("ChapterGemText", _player.Gem.ToString("N0"));
        }

        private void Pick(int chapter)
        {
            if (chapter > Unlocked)
            {
                // 왜 안 눌리는지 알려 준다 — 조용히 무시하면 고장으로 보인다
                SystemPopup.Show(Localize.Format("ui.chapter.locked_hint", chapter - 1), null,
                                 Localize.Get("ui.common.ok"), null);
                return;
            }
            _picked = chapter;
            Refresh();
        }

        private void OnStart()
        {
            if (_player == null || !_player.IsReady) return;
            _player.SelectedChapter = _picked;
            GameSound.Cue("ui.play");
            Close();
            CoreModule.Get<IEventBus>().Publish(new HostSelectRequestedEvent { IsChapterStart = true });
        }
    }
}
