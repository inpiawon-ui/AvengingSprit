using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 호스트 선택 화면. **화면은 1개, 진입 경로는 2개**다 (확정 사항).
    ///   ChapterStart 모드 — 빙의 시작 활성. 스테이지로 진입한다.
    ///   HostBrowse  모드 — 조회·강화. 빙의 시작 대신 로비로 돌아간다.
    ///
    /// 12칸 그리드는 런타임에 생성한다. 호스트별 초상·얼티밋 아이콘 36종이 여기서 쓰인다.
    /// </summary>
    public sealed class HostSelectPanel : MonoBehaviour
    {
        private const string AtlasAddress = "atlas/hostselectpanel";

        /// <summary>
        /// 잠긴 초상을 회색으로 그리는 머티리얼.
        /// `Image.color` 로는 안 된다 — 곱셈이라 어두워질 뿐 채도가 그대로다.
        /// 회색본 PNG 를 굽는 방법도 있지만 아틀라스가 8MB 늘어난다. 셰이더는 공짜다.
        /// </summary>
        private const string GrayMaterialAddress = "material/uigrayscale";
        private const float StatBarWidth = 122f;   // 목업 실측 — StatBarBg 폭
        private const int MaxStat = 100;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private SpriteAtlas _atlas;
        private Material _grayMaterial;

        private readonly List<HostSlotView> _slots = new();
        private readonly List<IDisposable> _tokens = new();
        private string _selectedKey;
        private bool _isChapterStart;
        private bool _built;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("PossessStartButton", OnPossessStart);
            _ui.OnClick("HostUpgradeButton", () =>
                CoreModule.Get<IEventBus>().Publish(
                    new NotImplementedFeatureEvent { FeatureLabel = "호스트 강화" }));

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _tokens.Add(CoreModule.Get<IEventBus>()
                .Subscribe<HostSelectedEvent>(OnHostSelected));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Open(bool isChapterStart)
        {
            _isChapterStart = isChapterStart;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로

            // 선택 호스트를 **동기로 먼저** 확정한다.
            // 그리드 구성은 비동기라, 끝나기 전에 `빙의 시작` 을 누르면
            // _selectedKey 가 비어 아무 일도 일어나지 않는다.
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player != null && _player.IsReady) _selectedKey = _player.SelectedHostId;

            BuildAsync().Forget();          // fire-and-forget: 그리드 구성 완료를 기다릴 필요 없음
        }

        public void Close() => gameObject.SetActive(false);

        private async UniTaskVoid BuildAsync()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;

            if (_atlas == null)
            {
                try { _atlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>(AtlasAddress); }
                catch (Exception e) { Debug.LogError($"[HostSelect] 아틀라스 로드 실패 — {e.Message}"); }
            }

            if (_grayMaterial == null)
            {
                // 못 불러와도 화면은 뜬다 — 잠금 칸이 색을 유지할 뿐이다.
                try { _grayMaterial = await CoreModule.Get<IResourceManager>().LoadAsync<Material>(GrayMaterialAddress); }
                catch (Exception e) { Debug.LogWarning($"[HostSelect] 회색 머티리얼 로드 실패 — {e.Message}"); }
            }

            if (!_built) BuildGrid();
            _built = true;

            _selectedKey = _player.SelectedHostId;
            RefreshSlots();
            RefreshDetail(_selectedKey);
            RefreshFooter();

            _ui.SetText("HeaderTitleText", _isChapterStart ? "HOST 를 선택하세요!" : "HOST 도감");
            _ui.SetText("HeaderDescText", "사망 시 유령으로 돌아가, 다른 HOST에 빙의할 수 있습니다.");
            _ui.SetActive("PossessStartButton", true);
            _ui.SetText("PossessTitleText", _isChapterStart ? "빙의 시작" : "돌아가기");
            _ui.SetText("PossessSubText", _isChapterStart ? "(POSSESS)" : "(BACK)");
            _ui.SetText("StatGroupLabel", "능력치");
            _ui.SetText("HostUpgradeTitleText", "HOST 강화");
            _ui.SetText("HostUpgradeSubText", "능력치 · ULTIMATE · 숙련도");
        }

        /// <summary>설계서의 `HostSlot` 1칸을 호스트 수만큼 복제해 그리드를 만든다.</summary>
        private void BuildGrid()
        {
            var grid = _ui.Find("HostGrid");
            var template = _ui.Find("HostSlot");
            if (grid == null || template == null)
            {
                Debug.LogError("[HostSelect] HostGrid 또는 HostSlot 이 없습니다.");
                return;
            }

            var normal   = GetSprite("hostslotframe");
            var selected = GetSprite("hostslotframe_selected");
            var locked   = GetSprite("hostslotframe_locked");

            // 템플릿은 그리드에서 빼둔다. 자식으로 남기면 GridLayoutGroup 이 한 칸을
            // 더 세어 행이 하나 늘고 마지막 줄이 잘린다.
            template.SetParent(transform, false);
            template.gameObject.SetActive(false);

            var hosts = _player.AllHosts;
            for (int i = 0; i < hosts.Count; i++)
            {
                var go = Instantiate(template.gameObject, grid);
                go.name = $"HostSlot_{hosts[i].HostKey}";
                go.SetActive(true);
                var view = go.GetComponent<HostSlotView>() ?? go.AddComponent<HostSlotView>();
                view.Cache();
                view.SetFrameSprites(normal, selected, locked);
                _slots.Add(view);
            }
        }

        private void RefreshSlots()
        {
            var hosts = _player.AllHosts;
            for (int i = 0; i < _slots.Count && i < hosts.Count; i++)
            {
                var e = hosts[i];
                bool unlocked = _player.IsHostUnlocked(e);
                _slots[i].Bind(e, GetSprite($"hostslotportrait_{e.HostKey}"),
                               unlocked, unlocked ? null : _grayMaterial, OnSlotClicked);
                _slots[i].SetSelected(e.HostKey == _selectedKey);
            }
        }

        private void OnSlotClicked(string hostKey)
        {
            _player.SelectHost(hostKey);   // 이벤트는 서비스가 발행한다
        }

        private void OnHostSelected(HostSelectedEvent e)
        {
            _selectedKey = e.SelectedKey;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(_slots[i].HostKey == _selectedKey);
            RefreshDetail(_selectedKey);
        }

        private void RefreshDetail(string hostKey)
        {
            var e = _player.GetHost(hostKey);
            if (e == null) return;
            bool unlocked = _player.IsHostUnlocked(e);

            _ui.SetText("HostNameEnText", e.NameEn);
            // 어떤 몸을 뺏는지가 곧 빌드다 — 이름 옆에 교전 스타일을 함께 보여준다
            _ui.SetText("HostNameKrText",
                unlocked ? $"{e.NameKr}  ·  {e.Role} ({e.AttackText})" : "???");

            var portrait = _ui.Get<Image>("HostPortraitImage");
            if (portrait != null)
            {
                portrait.sprite = GetSprite($"hostportraitimage_{e.HostKey}");
                portrait.color = Color.white;
                portrait.material = unlocked ? null : _grayMaterial;
            }
            // 상세 미리보기에는 자물쇠를 얹지 않는다 — 그림을 가리고, 잠긴 것은
            // 회색과 `???` 로 이미 충분히 읽힌다. 자물쇠는 목록 칸에만 둔다.
            _ui.SetActive("HostDetailLockIcon", false);

            SetStat("HP",   e.Hp,   unlocked);
            SetStat("ATK",  e.Atk,  unlocked);
            SetStat("SPD",  e.Spd,  unlocked);
            SetStat("DASH", e.Dash, unlocked);

            var ult = _player.GetUltimate(e.UltimateKey);
            _ui.SetText("UltimateLabel", "ULTIMATE");
            _ui.SetText("UltimateNameText", unlocked && ult != null ? ult.NameEn : "???");
            // 잠금 시 얼티밋 설명 자리에 해금 조건을 넣는다 — 목업에 새 요소를 추가하지 않기 위함
            _ui.SetText("UltimateDescText",
                unlocked ? (ult?.Description ?? string.Empty) : e.UnlockText);

            var icon = _ui.Get<Image>("UltimateIcon");
            if (icon != null)
            {
                icon.sprite = GetSprite($"ultimateicon_{e.HostKey}");
                // 아직 아이콘이 없는 호스트가 있다. 스프라이트가 비면 Image 는 흰 사각형을
                // 그리므로 그대로 두면 빈칸이 아니라 **덜 만든 티**가 난다.
                icon.enabled = icon.sprite != null;
                icon.color = unlocked ? Color.white : new Color(0.10f, 0.10f, 0.14f, 1f);
            }

            var possess = _ui.Get<Button>("PossessStartButton");
            if (possess != null) possess.interactable = unlocked;
            var upgrade = _ui.Get<Button>("HostUpgradeButton");
            if (upgrade != null) upgrade.interactable = unlocked;
        }

        private void SetStat(string stat, int value, bool unlocked)
        {
            // 행마다 StatLabelText·StatValueText 이름이 같으므로 반드시 행을 좁혀서 찾는다.
            var row = _ui.Find($"StatRow_{stat}");
            if (row == null) return;
            var label = _ui.Find(row, "StatLabelText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = stat;
            var valueText = _ui.Find(row, "StatValueText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (valueText != null) valueText.text = unlocked ? value.ToString() : "???";
            var fill = _ui.Find(row, "StatBarFill") as RectTransform;
            if (fill != null)
            {
                float r = unlocked ? Mathf.Clamp01((float)value / MaxStat) : 0f;
                fill.sizeDelta = new Vector2(StatBarWidth * r, fill.sizeDelta.y);
            }
        }

        private void RefreshFooter()
        {
            _ui.SetText("TipText",
                "HOST마다 이동속도, 대시(회피) 속도, 공격 방식이 다릅니다.\n다양한 HOST를 경험해 보세요!");
            _ui.SetText("OwnedHostCountText", $"보유 HOST  {_player.OwnedHostCount}/{_player.AllHosts.Count}");
            _ui.SetText("HostListTitleText", "HOST LIST");
        }

        private void OnPossessStart()
        {
            if (!_isChapterStart) { Close(); return; }

            var e = _player.GetHost(_selectedKey);
            if (e == null || !_player.IsHostUnlocked(e)) return;

            CoreModule.Get<IEventBus>()
                .Publish(new PossessStartRequestedEvent { HostKeyToPossess = _selectedKey });
            EnterGameAsync().Forget(); // fire-and-forget: 씬 전환 완료 대기 불필요
        }

        private async UniTaskVoid EnterGameAsync()
        {
            await _player.SaveAsync();   // 선택 호스트를 저장소에 커밋 — 씬을 넘는 단일 출처
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.InGame,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }

        private Sprite GetSprite(string name)
            => _atlas != null ? _atlas.GetSprite(name) : null;
    }
}
