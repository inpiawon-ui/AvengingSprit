using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 챕터 선택 (기획 2026-09-18). 로비 PLAY → 여기 → 호스트 선택 → 게임.
    ///
    /// 여섯 칸 중 **열린 챕터만** 누를 수 있다. 1챕터는 처음부터 열려 있고,
    /// 나머지는 앞 챕터를 깨면 열린다(`IPlayerDataService.UnlockedChapter`).
    /// 잠긴 칸은 어둡게 두고 자물쇠를 얹는다.
    ///
    /// ⚠ 지금 판은 **임시**다 — 로비에 있던 그림(판 테두리 · 모드 카드 · 자물쇠)을 모아 세웠다.
    ///   챕터 선택 시안(ui_new_chapter_select_v1)이 통과하면 그 부품으로 갈아 끼운다.
    ///   노드 이름은 그대로 두므로 코드는 안 바뀐다.
    /// </summary>
    public sealed class ChapterSelectPanel : MonoBehaviour
    {
        private const int SlotCount = PlayerDataService.ChapterCount;

        /// <summary>잠긴 칸의 밝기. 목록이 한눈에 「어디까지 왔나」로 읽히게.</summary>
        private const float LockedAlpha = 0.45f;

        private UIBinder _ui;
        private IPlayerDataService _player;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("ChapterSelectCloseButton", Close);
            _ui.OnClick("ChapterSelectDim", Close);
            for (int i = 0; i < SlotCount; i++)
            {
                int chapter = i + 1;   // 클로저가 루프 변수를 잡지 않게 복사한다
                var slot = _ui.Find($"ChapterSlot{chapter}");
                var button = slot != null ? slot.GetComponent<UnityEngine.UI.Button>() : null;
                if (button != null) button.onClick.AddListener(() => Pick(chapter));
            }
            // ⚠ 여기서 `SetActive(false)` 를 하지 마라 — `HostSelectPanel` 과 같은 이유다.
            //   처음 닫아 두는 것은 주인인 `LobbyMainUI` 가 한다.
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            int unlocked = _player != null && _player.IsReady ? _player.UnlockedChapter : 1;
            int cleared = _player != null && _player.IsReady ? _player.ClearedChapter : 0;

            _ui.SetText("ChapterSelectTitleText", Localize.Get("ui.chapter.title"));
            for (int i = 0; i < SlotCount; i++)
            {
                int chapter = i + 1;
                var slot = _ui.Find($"ChapterSlot{chapter}");
                if (slot == null) continue;
                bool open = chapter <= unlocked;

                TextIn(slot, "ChapterSlotNoText", $"CHAPTER {chapter}");
                TextIn(slot, "ChapterSlotNameText", Localize.Get($"stage.{chapter}.1.name"));
                TextIn(slot, "ChapterSlotStateText",
                       !open ? Localize.Get("ui.chapter.locked")
                       : chapter <= cleared ? Localize.Get("ui.chapter.cleared")
                       : Localize.Get("ui.chapter.new"));

                var lockIcon = _ui.Find(slot, "ChapterSlotLockIcon");
                if (lockIcon != null) lockIcon.gameObject.SetActive(!open);

                var group = slot.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = open ? 1f : LockedAlpha;
            }
        }

        private void Pick(int chapter)
        {
            if (_player == null || !_player.IsReady) return;
            if (chapter > _player.UnlockedChapter)
            {
                // 왜 안 눌리는지 알려 준다 — 조용히 무시하면 고장으로 보인다
                SystemPopup.Show(Localize.Format("ui.chapter.locked_hint", chapter - 1), null,
                                 Localize.Get("ui.common.ok"), null);
                return;
            }

            _player.SelectedChapter = chapter;
            GameSound.Cue("ui.play");
            Close();
            CoreModule.Get<IEventBus>().Publish(new HostSelectRequestedEvent { IsChapterStart = true });
        }

        private void TextIn(Transform root, string name, string value)
        {
            var t = _ui.Find(root, name)?.GetComponent<TMPro.TextMeshProUGUI>();
            if (t != null) t.text = value;
        }
    }
}
