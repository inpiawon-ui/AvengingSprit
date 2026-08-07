using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 로비(HUB). 상단 HUD 를 소유하고, 호스트 선택 패널을 여는 진입점이다.
    ///
    /// 진입 버튼은 3개지만 경로는 2개다 (확정 사항):
    ///   CONTINUE · CHAPTER → 스테이지 진입 모드
    ///   HOST              → 조회·강화 모드
    /// </summary>
    public sealed class LobbyMainUI : MonoBehaviour
    {
        private const float ExpBarWidth = 84f;   // 목업 실측 — GhostExpBarBg 폭

        // TBD-CH — 챕터 미기획. 목업이 노출한 CH3 값만 실제 문구다.
        private static readonly string[] ChapterNames = { "FACTORY", "HARBOR", "TOWER" };
        private static readonly string[] BossNames = { "MAD DOCTOR", "IRON CLAW", "OVERLORD" };

        [SerializeField] private HostSelectPanel _hostSelectPanel;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private readonly List<IDisposable> _tokens = new();

        // 1차 범위 밖 — 버튼은 목업대로 두되 누르면 준비중 안내만 띄운다
        private static readonly (string element, string label)[] NotReady =
        {
            ("MissionTab", "미션"), ("AchievementTab", "업적"), ("RankingTab", "랭킹"),
            ("InventoryTab", "인벤토리"), ("FriendsTab", "친구"),
            ("ShopButton", "상점"), ("BattlePassCard", "배틀패스"),
            ("EventCard", "이벤트"), ("DailyLoginCard", "일일 로그인"),
            ("MailButton", "우편"), ("SettingsButton", "설정"),
            ("ProgressRewardChest", "진행 보상"),
        };

        private void Awake()
        {
            _ui = new UIBinder(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.SetText("VersionText", $"v{Application.version}");

            _ui.OnClick("ContinueButton", () => OpenHostSelect(true));
            _ui.OnClick("ChapterButton",  () => OpenHostSelect(true));
            _ui.OnClick("HostButton",     () => OpenHostSelect(false));

            foreach (var (element, label) in NotReady)
            {
                var captured = label;
                _ui.OnClick(element, () => NotifyNotReady(captured));
            }
        }

        private void OnEnable()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<UserDataReadyEvent>(_ => Refresh()));
            _tokens.Add(bus.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrency()));
            _tokens.Add(bus.Subscribe<GhostProgressChangedEvent>(_ => RefreshGhost()));
            _tokens.Add(bus.Subscribe<ProgressChangedEvent>(_ => RefreshChapter()));
            _tokens.Add(bus.Subscribe<HostSelectRequestedEvent>(OnHostSelectRequested));
            Refresh();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        private void Refresh()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;
            RefreshCurrency();
            RefreshGhost();
            RefreshChapter();
        }

        private void RefreshCurrency()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("StaminaText", $"{_player.Stamina}/{_player.StaminaMax}");
            _ui.SetText("GoldText", _player.Gold.ToString("N0"));
            _ui.SetText("GemText", _player.Gem.ToString("N0"));
        }

        private void RefreshGhost()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("GhostLabelText", "GHOST");
            _ui.SetText("GhostLevelText", $"Lv.{_player.GhostLevel}");
            _ui.SetText("GhostExpText", $"{_player.GhostExp} / {_player.GhostExpMax}");
            float r = _player.GhostExpMax > 0 ? (float)_player.GhostExp / _player.GhostExpMax : 0f;
            _ui.SetFill("GhostExpBarFill", r, ExpBarWidth);
        }

        private void RefreshChapter()
        {
            if (_player == null || !_player.IsReady) return;
            _ui.SetText("ChapterNumberText", $"CHAPTER {_player.CurrentChapter:00}");
            _ui.SetText("ProgressText", $"{_player.ReachedStage} / 30");

            // 챕터 콘텐츠 미기획 — 목업(CH3)의 문구를 임시로 쓴다.
            // ChapterTable 이 생기면 이 배열을 걷어내고 데이터에서 읽는다. (TBD-CH)
            int idx = Mathf.Clamp(_player.CurrentChapter - 1, 0, ChapterNames.Length - 1);
            _ui.SetText("ChapterNameText", ChapterNames[idx]);
            _ui.SetText("BossNameText", BossNames[idx]);
            // 신규 유저에게 '이어서 하기'는 성립하지 않는다 — 상태별 라벨 전환
            bool started = _player.ReachedStage > 1 || _player.ClearedChapter > 0;
            _ui.SetText("ContinueButtonText", started ? "CONTINUE" : "START");
        }

        private void OpenHostSelect(bool isChapterStart)
        {
            CoreModule.Get<IEventBus>()
                .Publish(new HostSelectRequestedEvent { IsChapterStart = isChapterStart });
        }

        private void OnHostSelectRequested(HostSelectRequestedEvent e)
        {
            if (_hostSelectPanel == null)
            {
                Debug.LogError("[LobbyMainUI] HostSelectPanel 이 연결되지 않았습니다.");
                return;
            }
            _hostSelectPanel.Open(e.IsChapterStart);
        }

        private void NotifyNotReady(string label)
        {
            CoreModule.Get<IEventBus>()
                .Publish(new NotImplementedFeatureEvent { FeatureLabel = label });
            Debug.Log($"[Lobby] 준비 중입니다 — {label}");
        }
    }
}
