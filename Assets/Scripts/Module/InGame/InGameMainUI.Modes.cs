using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.Modes;
using Game.Module.Events;
using GameFramework.Core.Module.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 추가 모드(망령의 탑 · 망령의 밤)의 인게임 표시 — 노드는 `ModeInGameBuilder` 가 세운다.
    ///   탑 (T5 · T6) 위 가운데 탑 판(층 · 남은 적) · 층 소개 띠
    ///   밤 (N2 ~ N5) 위 가운데 밤 판(시간 · 처치) · 무리 칩 · 다음 일 · 유리 몸 칩 · 알림 띠
    ///   결과 — `TowerResultPopup`(T8 · T9) · `NightResultPopup`(N7)
    /// 모드 판에는 판 골드 · 챕터 칸이 없다 — 그 자리를 모드 판이 쓴다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const float ModeBannerSeconds = 2.2f;
        private int _modeBannerId;
        private Affinity _modeWaveKind = Affinity.None;

        private void SubscribeModes(IEventBus bus)
        {
            if (!RunModeContext.IsMode) return;
            _tokens.Add(bus.Subscribe<TowerFloorEnteredEvent>(OnTowerFloorEntered));
            _tokens.Add(bus.Subscribe<TowerEnemiesLeftEvent>(OnTowerEnemiesLeft));
            _tokens.Add(bus.Subscribe<TowerPossessLeftEvent>(OnTowerPossessLeft));
            _tokens.Add(bus.Subscribe<TowerFinishedEvent>(OnTowerFinished));
            _tokens.Add(bus.Subscribe<NightStateEvent>(OnNightState));
            _tokens.Add(bus.Subscribe<NightBannerEvent>(OnNightBanner));
            _tokens.Add(bus.Subscribe<NightFinishedEvent>(OnNightFinished));
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(_ => ApplyModeHud()));
        }

        /// <summary>모드 판 — 챕터 칸 · 판 골드를 걷고 모드 판을 세운다(탑 3-8 · 밤 3-5).</summary>
        private void ApplyModeHud()
        {
            if (!RunModeContext.IsMode) return;
            bool tower = RunModeContext.IsTower;
            _ui.SetActive("ChapterGroup", false);
            _ui.SetActive("Hud3StageTrack", false);
            _ui.SetActive("RunResourcePanel", false);
            _ui.SetActive("Hud3GoldChip", false);
            _ui.SetActive("ModeTowerPlate", tower);
            _ui.SetActive("ModeNightPlate", !tower);
            _ui.SetActive("ModeWaveChip", !tower);
            _ui.SetActive("ModeNextText", !tower);
            _ui.SetActive("ModeCounterChip", !tower);
            _ui.SetText("ModeTowerLabel", "TOWER");
            _ui.SetText("ModeNightLabel", "NIGHT");
        }

        // ── 탑 ───────────────────────────────────────────────

        private void OnTowerFloorEntered(TowerFloorEnteredEvent e)
        {
            ApplyModeHud();
            _ui.SetText("ModeTowerFloorText", Localize.Format("ui.tower.floor_f", e.EnteredFloor));
            // 층 소개 띠 — 「13층」 · 「마법의 무리」 · 조건 한 줄
            string rule = e.EnemyAffinityKind != 0
                ? Localize.Format("ui.tower.banner_enemy", AffinityName((Affinity)e.EnemyAffinityKind),
                                  AffinityName((Affinity)e.AdvantageKind))
                : e.AdvantageKind != 0 ? Localize.Format("ui.tower.adv", AffinityName((Affinity)e.AdvantageKind))
                : e.IsLeaveLocked ? Localize.Get("ui.tower.no_leave")
                : e.PossessLimit > 0 ? Localize.Format("ui.tower.possess_left", e.PossessLimit)
                : Localize.Get(e.RuleDescKey);
            ShowModeBanner(Localize.Format("ui.tower.floor", e.EnteredFloor), Localize.Get(e.FloorNameKey), rule);
        }

        private void OnTowerEnemiesLeft(TowerEnemiesLeftEvent e)
            => _ui.SetText("ModeTowerEnemyText", Localize.Format("ui.tower.enemies_left", e.RemainingEnemies));

        private void OnTowerPossessLeft(TowerPossessLeftEvent e)
            => ShowModeBanner(null, Localize.Format("ui.tower.possess_left", e.RemainingPossess), null);

        private void OnTowerFinished(TowerFinishedEvent e)
        {
            if (_finished) return;
            _finished = true;
            HideModeOverlay();
            var popup = GetComponentInChildren<TowerResultPopup>(true);
            if (popup != null) popup.Show(e);
            else Debug.LogError("[탑] 결과 창(TowerResultPopup)이 프리팹에 없다 — 「모드 UI/인게임 세우기」");
        }

        // ── 밤 ───────────────────────────────────────────────

        private void OnNightState(NightStateEvent e)
        {
            int s = Mathf.FloorToInt(e.ElapsedSeconds);
            _ui.SetText("ModeNightTimeText", $"{s / 60}:{s % 60:00}");
            _ui.SetText("ModeNightKillText", Localize.Format("ui.night.kills", e.KillCount));
            var kind = (Affinity)e.WaveAffinityKind;
            _ui.SetText("ModeWaveText", Localize.Format("ui.night.wave", AffinityName(kind)));
            // 속성이 바뀔 때만 그림을 꺼낸다 — 아틀라스 `GetSprite` 는 부를 때마다 새 Sprite 를 만든다
            if (kind != _modeWaveKind)
            {
                _modeWaveKind = kind;
                var gem = _ui.Get<Image>("ModeWaveIcon");
                var sprite = AffinityGem(kind);
                if (gem != null && sprite != null) gem.sprite = sprite;
            }
            int n = Mathf.CeilToInt(e.NextEventSeconds);
            _ui.SetText("ModeNextText", e.NextEventKind == 0 ? string.Empty
                : Localize.Format(e.NextEventKind == 2 ? "ui.night.next_boss" : "ui.night.next_big", $"{n / 60}:{n % 60:00}"));
            _ui.SetActive("ModeCounterChip", e.IsCounterHintShown);
            // 보스 체력 막대가 나오면 무리 칩 · 다음 일을 그 아래로 내린다(막대가 칩을 덮었다)
            var bossBar = _ui.Find("BossGroup");
            ShiftUnderBossBar(bossBar != null && bossBar.gameObject.activeInHierarchy);
            _ui.SetText("ModeCounterText", Localize.Format("ui.night.counter", AffinityName((Affinity)e.CounterAffinityKind)));
        }

        private void OnNightBanner(NightBannerEvent e)
        {
            string main = string.IsNullOrEmpty(e.BannerArg) ? Localize.Get(e.BannerKey) : Localize.Format(e.BannerKey, e.BannerArg);
            string sub = string.IsNullOrEmpty(e.SubKey) ? string.Empty
                       : string.IsNullOrEmpty(e.SubArg) ? Localize.Get(e.SubKey) : Localize.Format(e.SubKey, e.SubArg);
            ShowModeBanner(null, main, sub);
        }

        private void OnNightFinished(NightFinishedEvent e)
        {
            if (_finished) return;
            _finished = true;
            HideModeOverlay();
            var popup = GetComponentInChildren<NightResultPopup>(true);
            if (popup != null) popup.Show(e);
            else Debug.LogError("[밤] 결과 창(NightResultPopup)이 프리팹에 없다 — 「모드 UI/인게임 세우기」");
        }

        private const float BossBarDrop = 48f;
        private bool _modeChipsDropped;

        private void ShiftUnderBossBar(bool drop)
        {
            if (drop == _modeChipsDropped) return;
            _modeChipsDropped = drop;
            float dy = drop ? -BossBarDrop : BossBarDrop;
            foreach (var n in new[] { "ModeWaveChip", "ModeNextText" })
                if (_ui.Find(n) is RectTransform r) r.anchoredPosition += new Vector2(0f, dy);
        }

        /// <summary>판이 끝났다 — 띠 · 칩을 걷는다(결과 창 위로 올라왔다).</summary>
        private void HideModeOverlay()
        {
            _ui.SetActive("ModeBanner", false);
            _ui.SetActive("ModeCounterChip", false);
        }

        // ── 공통 ─────────────────────────────────────────────

        /// <summary>소개 · 알림 띠 — 2.2초 떠 있다 걷힌다. 판을 멈추지 않는다(연출로 게임을 세우지 않는다).</summary>
        private (string Title, string Name, string Sub)? _pendingModeBanner;

        /// <summary>판이 열렸다(덮개가 다 걷힘) — 그 전에 들어온 띠를 띄운다. 덮개 뒤에서 띄우면 아무도 못 본다.</summary>
        private void ShowPendingModeBanner()
        {
            if (_pendingModeBanner is not { } b) return;
            _pendingModeBanner = null;
            ShowModeBanner(b.Title, b.Name, b.Sub);
        }

        private void ShowModeBanner(string title, string name, string sub)
        {
            if (_battle == null || !_battle.IsRunning)
            {
                _pendingModeBanner = (title, name, sub);
                return;
            }
            _ui.SetText("ModeBannerTitle", title ?? string.Empty);
            _ui.SetText("ModeBannerName", name ?? string.Empty);
            _ui.SetText("ModeBannerSub", sub ?? string.Empty);
            _ui.SetActive("ModeBannerTitle", !string.IsNullOrEmpty(title));
            _ui.SetActive("ModeBanner", true);
            HideModeBannerAsync(++_modeBannerId).Forget();   // fire-and-forget: 정해진 시간 뒤 걷는다
        }

        private async UniTaskVoid HideModeBannerAsync(int id)
        {
            var band = _ui.Find("ModeBanner");
            if (band == null) return;
            var group = band.GetComponent<CanvasGroup>();
            if (group == null) group = band.gameObject.AddComponent<CanvasGroup>();
            var token = this.GetCancellationTokenOnDestroy();
            const float fade = 0.25f;
            for (float t = 0f; t < ModeBannerSeconds; t += Time.unscaledDeltaTime)
            {
                if (id != _modeBannerId) return;
                group.alpha = t < fade ? t / fade : t > ModeBannerSeconds - fade ? (ModeBannerSeconds - t) / fade : 1f;
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (id == _modeBannerId) _ui.SetActive("ModeBanner", false);
        }

        private static string AffinityName(Affinity a)
            => Localize.Get(a switch
            {
                Affinity.Blade => "ui.affinity.blade",
                Affinity.Force => "ui.affinity.force",
                Affinity.Magic => "ui.affinity.magic",
                _ => "ui.affinity.none",
            });

        private Sprite AffinityGem(Affinity a)
            => _battle == null ? null : a switch
            {
                Affinity.Blade => _battle.UiSprite("rps_gem_blade"),
                Affinity.Force => _battle.UiSprite("rps_gem_force"),
                Affinity.Magic => _battle.UiSprite("rps_gem_magic"),
                _ => null,
            };
    }
}
