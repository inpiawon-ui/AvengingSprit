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
    /// 12칸 그리드는 런타임에 생성한다. 호스트별 초상·액티브 스킬 아이콘 36종이 여기서 쓰인다.
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
        private const float StatBarWidth = 113f;   // 2열 격자의 StatBarBg 폭
        private const int MaxStat = 100;

        /// <summary>
        /// 사거리·공격속도 바의 기준 최대값.
        ///
        /// HP·ATK·이속·치명타는 0~100 이라 그대로 그리면 되는데, 이 둘은 단위가
        /// 다르다(m · 초당 횟수). 바가 얼마나 찼는지를 말하려면 기준이 필요하다.
        /// </summary>
        /// <summary>등급은 일곱 칸 모두 1~10 이다. 바는 이 값으로 가득 찬다.</summary>
        private const float GradeMax = 10f;

        /// <summary>
        /// 그리드가 보이는 높이. 목업의 `624` 는 12칸(3×4) 기준이었다.
        /// 그리드 아래(패널 y 819~1090)는 비어 있고 버튼은 전부 오른쪽에 있으므로,
        /// TipBar(y 1098) 바로 위까지 내려 한 화면에 다섯 줄 반을 보인다.
        /// </summary>
        private const float GridViewHeight = 890f;

        private const float ScrollbarWidth = 5f;   // 그리드 오른쪽 여백 6.2px 안에 들어가야 한다

        private UIBinder _ui;
        private IPlayerDataService _player;
        private SpriteAtlas _atlas;
        private Material _grayMaterial;
        private ScrollRect _scroll;

        private readonly List<HostSlotView> _slots = new();
        private readonly List<IDisposable> _tokens = new();
        private string _selectedKey;
        private bool _isChapterStart;
        private bool _built;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            // 본문 폰트를 지금 언어 것으로 — 일본어를 한글 폰트로 그리면 한자가 한국식으로 나온다
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);

            _ui.OnClick("PossessStartButton", OnPossessStart);
            _ui.OnClick("HostUpgradeButton", OnUpgrade);
            _ui.OnClick("HostSelectBackButton", Back);
            // 창 뒤 어둡게 막을 누르면 아무 일도 없다 — 로비가 눌리지 않게 막기만 한다
            // ⚠ 여기서 `SetActive(false)` 를 하지 마라. 켜지는 도중의 끄기는 그 프레임에
            //   안 먹어 판 조각이 로비 아래로 삐져나오고, 부모가 먼저 꺼 두면 이 `Awake`
            //   자체가 `Open()` 때 돌면서 다시 꺼 버려 판이 영영 안 열린다(2026-09-16).
            //   처음 닫아 두는 것은 주인인 `LobbyMainUI` 가 한다.
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

        /// <summary>
        /// 뒤로 — 창을 닫고, 판을 시작하러 들어온 길이면 **챕터 선택으로 돌아간다**.
        /// 뒤로가기 버튼과 기기 뒤로 키가 같은 길을 쓴다.
        /// </summary>
        public void Back()
        {
            bool fromChapter = _isChapterStart;
            Close();
            if (fromChapter) CoreModule.Get<IEventBus>().Publish(new ChapterSelectRequestedEvent());
        }

        private async UniTaskVoid BuildAsync()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);
            if (_player == null || !_player.IsReady) return;

            // ⚠ 셋을 **한꺼번에** 띄운다. 순서대로 기다리면 각 로드 시간이 그대로 더해져
            //   패널이 열리는 데 몇 초가 걸린다 — 서로 기다릴 이유가 없는 것들이다.
            {
                var res = CoreModule.Get<IResourceManager>();
                await UniTask.WhenAll(
                    LoadOrWarnAsync<SpriteAtlas>(res, AtlasAddress, _atlas,
                        v => _atlas = v, "아틀라스"),
                    LoadOrWarnAsync<Game.Character.GameConfig>(res, GameConfigAddress, _config,
                        v => _config = v, "GameConfig"),
                    LoadOrWarnAsync<Material>(res, GrayMaterialAddress, _grayMaterial,
                        v => _grayMaterial = v, "회색 머티리얼"));
            }

            // ⚠ 세우기와 검사 사이에 await 가 있으면 안 된다.
            //    위의 아틀라스·머티리얼 로딩이 도는 동안 패널을 한 번 더 열면
            //    두 번째 호출이 가드를 그냥 통과해 `BuildGrid` 가 두 번 돈다.
            //    두 번째에는 템플릿(`HostSlot`)이 이미 복제되며 이름이 바뀐 뒤라
            //    찾지 못하고 오류를 남긴다.
            if (!_built)
            {
                _built = true;      // 같은 프레임 안에서 세운다 — 중간에 넘어갈 틈이 없다
                BuildGrid();
            }

            _selectedKey = _player.SelectedHostId;
            // ⚠ 고른 몸이 없으면 카드가 통째로 빈 채 열린다 —
            //   `RefreshDetail("")` 이 호스트를 못 찾고 그냥 빠져나가기 때문이다.
            //   처음 열었거나 저장된 선택이 사라졌을 때가 그렇다. **첫 몸으로 채운다.**
            if (string.IsNullOrEmpty(_selectedKey) || _player.GetHost(_selectedKey) == null)
            {
                var list = _player.PlayableHosts;
                if (list != null && list.Count > 0) _selectedKey = list[0].HostKey;
            }
            RefreshSlots();
            ScrollToSelected();
            RefreshDetail(_selectedKey);
            RefreshFooter();

            _ui.SetText("HeaderTitleText", _isChapterStart ? Localize.Get("ui.hostselect.title.pick") : Localize.Get("ui.hostselect.title.codex"));
            _ui.SetText("HeaderDescText", Localize.Get("ui.hostselect.header_desc"));
            _ui.SetActive("PossessStartButton", true);
            _ui.SetText("PossessTitleText", _isChapterStart ? Localize.Get("ui.hostselect.possess_start") : Localize.Get("ui.hostselect.back"));
            _ui.SetText("PossessSubText", _isChapterStart ? "(POSSESS)" : "(BACK)");
            _ui.SetText("StatGroupLabel", Localize.Get("ui.hostselect.stats"));
            _ui.SetText("HostUpgradeTitleText", Localize.Get("ui.hostselect.upgrade.title"));
            _ui.SetText("HostUpgradeSubText", Localize.Get("ui.hostselect.upgrade.sub"));
        }

        /// <summary>설계서의 `HostSlot` 1칸을 호스트 수만큼 복제해 그리드를 만든다.</summary>
        private void BuildGrid()
        {
            var grid = _ui.Find("HostGrid") as RectTransform;
            var template = _ui.Find("HostSlot");
            if (grid == null || template == null)
            {
                Debug.LogError("[HostSelect] HostGrid 또는 HostSlot 이 없습니다.");
                return;
            }

            _scroll = WrapGridInScroll(grid);

            // 잠금 칸도 같은 테두리를 쓴다 — `hostslotframe_locked` 는 더 이상 안 읽는다.
            var normal   = GetSprite("hostslotframe");
            var selected = GetSprite("hostslotframe_selected");

            // 템플릿은 그리드에서 빼둔다. 자식으로 남기면 GridLayoutGroup 이 한 칸을
            // 더 세어 행이 하나 늘고 마지막 줄이 잘린다.
            template.SetParent(transform, false);
            template.gameObject.SetActive(false);

            var hosts = _player.PlayableHosts;
            for (int i = 0; i < hosts.Count; i++)
            {
                var go = Instantiate(template.gameObject, grid);
                go.name = $"HostSlot_{hosts[i].HostKey}";
                go.SetActive(true);
                var view = go.GetComponent<HostSlotView>() ?? go.AddComponent<HostSlotView>();
                view.Cache();
                view.SetFrameSprites(normal, selected);
                _slots.Add(view);
            }
        }

        /// <summary>
        /// `HostGrid` 를 세로 스크롤 안으로 집어넣는다.
        ///
        /// 목업은 12칸으로 그려졌는데 정본 호스트가 21종이다. 그대로 두면 5행부터
        /// 패널 밖으로 밀려 **마지막 줄(흡혈귀)은 화면에 아예 없다.**
        /// 칸을 줄여 욱여넣는 대신 스크롤을 붙인다 — 호스트가 더 늘어도 레이아웃을
        /// 다시 건드릴 일이 없다.
        ///
        /// 프리팹을 고치지 않고 런타임에 감싸는 이유: `HostGrid` 라는 이름이 곧
        /// 바인딩 키다. 프리팹에서 계층을 갈아엎으면 이름 규약이 흔들린다.
        /// </summary>
        private static ScrollRect WrapGridInScroll(RectTransform grid)
        {
            var existing = grid.GetComponentInParent<ScrollRect>();
            if (existing != null) return existing;   // 이미 감쌌다

            var scrollGo = new GameObject("HostGridScroll", typeof(RectTransform), typeof(ScrollRect));
            var root = (RectTransform)scrollGo.transform;
            root.SetParent(grid.parent, false);
            root.SetSiblingIndex(grid.GetSiblingIndex());
            root.anchorMin = grid.anchorMin;
            root.anchorMax = grid.anchorMax;
            root.pivot = grid.pivot;
            root.anchoredPosition = grid.anchoredPosition;
            root.sizeDelta = new Vector2(grid.sizeDelta.x, GridViewHeight);

            // Viewport 에는 RectMask2D 만 둔다 (05_prefabs 규약 — Image 금지).
            var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewport = (RectTransform)viewGo.transform;
            viewport.SetParent(root, false);
            Stretch(viewport);

            grid.SetParent(viewport, false);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.anchoredPosition = Vector2.zero;
            grid.sizeDelta = new Vector2(0f, grid.sizeDelta.y);

            // 행이 늘면 내용 높이도 따라 늘어야 스크롤 범위가 맞는다
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit
                = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = grid;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = BuildScrollbar(root);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scroll;
        }

        /// <summary>
        /// 고른 몸이 스크롤 밖에 있으면 아무것도 안 고른 것처럼 보인다. 보이는 자리로 끌어온다.
        /// </summary>
        private void ScrollToSelected()
        {
            if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return;

            int index = -1;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].HostKey == _selectedKey) { index = i; break; }
            if (index < 0) return;

            Canvas.ForceUpdateCanvases();   // ContentSizeFitter 가 높이를 잡은 뒤라야 잴 수 있다
            float span = _scroll.content.rect.height - _scroll.viewport.rect.height;
            if (span <= 0f) return;         // 다 보인다 — 움직일 이유가 없다

            var slot = (RectTransform)_slots[index].transform;
            float top = -slot.anchoredPosition.y;   // 내용 위쪽에서 잰 거리
            float offset = Mathf.Clamp(
                top - (_scroll.viewport.rect.height - slot.rect.height) * 0.5f, 0f, span);
            _scroll.verticalNormalizedPosition = 1f - offset / span;
        }

        /// <summary>
        /// 그리드 오른쪽 여백에 세우는 가는 막대.
        /// 손을 대지 않아도 보여야 하므로 자동 숨김을 쓰지 않는다.
        /// </summary>
        private static Scrollbar BuildScrollbar(RectTransform root)
        {
            var barGo = new GameObject("HostGridScrollbar",
                typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var bar = (RectTransform)barGo.transform;
            bar.SetParent(root, false);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.anchoredPosition = new Vector2(1f, 0f);
            bar.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            barGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            var areaGo = new GameObject("SlidingArea", typeof(RectTransform));
            var area = (RectTransform)areaGo.transform;
            area.SetParent(bar, false);
            Stretch(area);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handle = (RectTransform)handleGo.transform;
            handle.SetParent(area, false);
            handle.sizeDelta = Vector2.zero;
            var handleImage = handleGo.GetComponent<Image>();
            handleImage.color = new Color(1f, 0.84f, 0.35f, 0.85f);

            var scrollbar = barGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private void RefreshSlots()
        {
            var hosts = _player.PlayableHosts;
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

            // 표의 영문명은 "GANGSTER — GUN" 처럼 무기까지 붙어 있다. 이름줄에는
            // **몸 이름만** 남긴다 — 무기 구분은 바로 아래 한 줄 설명이 맡는다.
            _ui.SetText("HostNameEnText", ShortNameEn(e.NameEn));
            _ui.SetText("GradeBadgeText", string.Empty);   // 그림으로 대신한다
            // 이름줄 오른쪽 끝은 **이 몸에 쌓인 파편**이다. 목록을 훑으며
            // "누가 곧 열리나" 를 보는 값이라 카드를 열자마자 눈에 들어와야 한다.
            _ui.SetText("HostShardCountText", e.IsGhost ? string.Empty
                : _player.GetShards(e.HostKey).ToString("00"));
            _ui.SetActive("HostShardIcon", !e.IsGhost);
            var grade = _ui.Get<Image>("GradeBadge");
            if (grade != null)
            {
                grade.sprite = e.IsGhost ? null
                    : GetSprite($"icon_grade_{e.Grade.ToString().ToLowerInvariant()}");
                grade.enabled = grade.sprite != null;
            }
            _ui.SetText("HostNameKrText", e.DisplayName);

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

            // 일곱 칸. **전부 "길수록 강함"** 으로 방향을 맞춘다 —
            // 공격 간격(초)을 그대로 쓰면 낮을수록 좋은 값이라 바가 거꾸로 읽힌다.
            // 일곱 칸 **전부 등급(1~10) 으로 바를 긋고, 글자는 그 등급의 실제 수치**를 적는다
            // (기획 2026-09-15). 예전에는 바를 0~100 눈금으로 긋고 글자는 정본 절대값을
            // 적었다 — 게임이 읽는 값과 화면이 적는 값이 서로 다른 자라서,
            // 아마존이 화면에는 초당 1.8 회로 떠 있는데 판에서는 4.2 회를 때렸다.
            // 이제 양쪽 다 `GameConfig` 의 등급 곡선 하나만 읽는다.
            SetStat("HP",   e.HpGrade,      $"{_config.HpOfGrade(e.HpGrade)}",                GradeMax, e.HpGrade);
            SetStat("ATK",  e.AtkGrade,     $"{AtkOf(e)}",                                    GradeMax, e.AtkGrade);
            SetStat("DEF",  e.DefenseGrade, $"{e.DefensePercent:0}%",                         GradeMax, e.DefenseGrade);
            SetStat("SPD",  e.SpdGrade,     $"{_config.MoveOfGrade(e.SpdGrade):0.0}",         GradeMax, e.SpdGrade);
            SetStat("CRIT", e.CritGrade,    $"{CritPercentOf(e):0}%",                         GradeMax, e.CritGrade);
            SetStat("RNG",  e.RangeGrade,   $"{RangeOf(e):0.#}",                              GradeMax, e.RangeGrade);
            // 소수 두 자리(`0.67`)면 `RATE` 라벨과 1px 겹친다. 한 자리로 충분하다.
            SetStat("RATE", e.RateGrade,    $"{_config.RateOfGrade(e.RateGrade):0.0}",        GradeMax, e.RateGrade);

            // ⚠ 두 잠금은 다른 것이다.
            //   `unlocked`  이 몸을 쓸 수 있는가 (진행도)
            //   `sealed`    이 몸의 **스킬이** 봉인돼 있는가 (숙련도 0)
            //   봉인이어도 몸은 쓴다 — 잠기는 것은 스킬 칸뿐이다.
            bool skillSealed = _player.IsSkillSealed(e.HostKey);

            if (!_labelColorRead)
            {
                var lbl = _ui.Get<TMPro.TextMeshProUGUI>("ActiveSkillLabel");
                if (lbl != null) _labelColor = lbl.color;
                _labelColorRead = true;
            }

            if (e.IsGhost)
            {
                // 유령은 몸이 아니다 — 능력치·스킬·숙련도를 그대로 쓰면 전부 0 으로 보인다.
                SetGhostCard();
                // 유령에게 있는 수치는 HP 하나뿐이다. 나머지를 `0` 으로 적으면
                // **약한 몸**처럼 읽힌다 — 없는 값은 없다고 적는다.
                SetStatText("HP", (_config != null ? _config.GhostHpMax : 100).ToString());
                foreach (var n in new[] { "ATK", "DEF", "SPD", "CRIT", "RNG", "RATE" }) SetStatText(n, "—");
                SetStartCost(e, true);
                var pb = _ui.Get<Button>("PossessStartButton");
                if (pb != null) pb.interactable = true;
                var ub = _ui.Get<Button>("HostUpgradeButton");
                if (ub != null) ub.interactable = false;   // 유령은 파편으로 안 큰다
                return;
            }

            SetJobCard(e, unlocked);

            // ⚠ **잠겼다고 내용을 가리지 않는다.** 이 카드는 정보 화면이다 —
            //   무슨 스킬인지 모르면 어느 몸을 키울지 고를 수가 없다.
            //   잠김은 자물쇠와 흐린 색으로만 알린다. 글자는 언제나 그대로 둔다.
            var skill = _player.GetActiveSkill(e.ActiveSkillKey);
            _ui.SetText("ActiveSkillLabel", "ACTIVE SKILL");
            // 한글 이름을 쓴다 — 영문은 카드 맨 위 호스트 이름이 이미 맡고 있다.
            _ui.SetText("ActiveSkillNameText",
                skill != null ? (string.IsNullOrEmpty(skill.DisplayName) ? skill.NameEn : skill.DisplayName) : "—");
            _ui.SetText("ActiveSkillDescText", skill?.DisplayDescription ?? string.Empty);
            // ⚠ 전용 자물쇠 그림이 아직 없다. 초상용(`hostdetaillockicon`, 80×96)을
            //   42px 칸에 눌러 넣으면 뭉개진 덩어리로 보인다 — 그림이 올 때까지 끈다.
            //   봉인은 아이콘 흐림으로 이미 읽힌다.
            _ui.SetActive("ActiveSkillSealIcon", false);

            var icon = _ui.Get<Image>("ActiveSkillIcon");
            if (icon != null)
            {
                icon.sprite = GetSprite($"ultimateicon_{e.HostKey}");
                // 아직 아이콘이 없는 호스트가 있다. 스프라이트가 비면 Image 는 흰 사각형을
                // 그리므로 그대로 두면 빈칸이 아니라 **덜 만든 티**가 난다.
                icon.enabled = icon.sprite != null;
                icon.color = skillSealed ? SealedTint : Color.white;
            }

            SetPassiveCard(e, unlocked, skillSealed);
            SetMastery(e, unlocked);

            SetStartCost(e, unlocked);

            var possess = _ui.Get<Button>("PossessStartButton");
            if (possess != null) possess.interactable = unlocked;
            var upgrade = _ui.Get<Button>("HostUpgradeButton");
            if (upgrade != null) upgrade.interactable = unlocked;
        }

        /// <summary>
        /// 봉인된 스킬 아이콘 색. **끄지 않고 어둡게만** 한다 —
        /// 무엇인지는 보여야 키울지 말지를 고를 수 있다.
        /// </summary>
        private static readonly Color SealedTint = new(0.42f, 0.44f, 0.52f, 1f);

        /// <summary>라벨 기본색. 첫 갱신 때 프리팹 값을 그대로 기억해 둔다.</summary>
        private Color _labelColor = Color.white;
        private bool _labelColorRead;

        // ── HOST 강화 ────────────────────────────────────────────
        //
        // 화면을 따로 만들지 않는다. 카드에 이미 `파편 18 / 26` 이 보이므로
        // **누를 자리 하나**만 있으면 무엇을 사는지가 읽힌다.
        //
        // 파는 것은 다음 숙련도 한 단계뿐이라 고를 것이 없다 — 확인 팝업 한 번으로 끝난다.
        //
        // ⚠ 예전에는 Lv0 이 "봉인 해제" 였다. 지금은 **몸이 열리면 스킬도 함께 열려**
        //   (`PlayerDataService.UnsealUnlocked`) 여기 오는 몸은 언제나 Lv1 이상이다.

        private void OnUpgrade()
        {
            var e = _player.GetHost(_selectedKey);
            if (e == null) return;

            int lv = _player.GetMastery(e.HostKey);
            if (lv >= _player.MasteryMax)
            {
                SystemPopup.Show(Localize.Format("ui.hostselect.mastery.maxed", e.DisplayName), null, Localize.Get("ui.common.ok"), null);
                return;
            }

            int have = _player.GetShards(e.HostKey);
            // 단계 값은 같은 표(`_shardCurve`)에서 나온다 — 갈래를 둘로 두지 않는다.
            // 값·등급 배수는 `GameConfig` 에 있고 서비스가 곱해 준다.
            int need = _player.MasteryCost(e.HostKey);

            if (have < need)
            {
                // 얼마나 모자란지를 숫자로 말해 준다. "부족합니다" 만으로는
                // 몇 판을 더 돌아야 하는지 알 수 없다.
                SystemPopup.Show(
                    Localize.Format("ui.hostselect.mastery.short", e.DisplayName, have, need, need - have),
                    null, Localize.Get("ui.common.ok"), null);
                return;
            }

            string title = lv < 1 ? Localize.Get("ui.hostselect.mastery.unseal") : Localize.Format("ui.hostselect.mastery.step", lv, lv + 1);
            SystemPopup.Show(
                Localize.Format("ui.hostselect.mastery.confirm", e.DisplayName, title, need),
                () => DoUpgrade(e.HostKey, need));
        }

        private void DoUpgrade(string hostKey, int cost)
        {
            if (!_player.SpendShards(hostKey, cost)) return;
            _player.SaveAsync().Forget();   // fire-and-forget: 저장 실패해도 화면은 이미 갱신됐다
            RefreshDetail(hostKey);
            // 목록 칸도 잠금 표시가 바뀔 수 있다
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(_slots[i].HostKey == _selectedKey);
        }

        /// <summary>
        /// 직업 배지와 상시 규칙.
        ///
        /// **문구는 3벌뿐이다.** 같은 직업 여섯 명 카드에 같은 문장을 따로 적지 않는다.
        /// 직업은 테이블에 저장하지 않고 평타 방식 + 사거리에서 뽑아내므로
        /// (`BattleDirector.JobOf`), 여기서도 **그릴 때 계산**한다 — 어긋날 자리가 없다.
        /// </summary>
        private void SetJobCard(HostEntry e, bool unlocked)
        {
            var job = JobOf(e);
            _ui.SetText("JobBadgeText", JobNameOf(job));
            // 액티브·패시브와 나란한 틀 안에 라벨을 달아 준다.
            // 라벨이 없으면 떠 있는 글자가 되어 "이게 스킬인가 설명인가" 를 알 수 없다.
            // 배지가 이미 직업 이름을 말한다 — 라벨에 또 붙이지 않는다.
            _ui.SetText("JobSkillLabel", "JOB TRAIT");
            _ui.SetText("JobTraitNameText", JobTraitNameOf(job));
            _ui.SetText("JobRuleText", JobRuleOf(job));

            var badge = _ui.Get<Image>("JobBadge");
            if (badge == null) return;
            badge.sprite = GetSprite($"jobbadge_{JobSpriteOf(job)}");
            badge.enabled = badge.sprite != null;
        }

        /// <summary>
        /// 패시브 카드.
        ///
        /// 패시브가 없는 몸(23명 중 12명)도 **카드를 끄지 않는다.**
        /// 끄면 카드마다 높이가 달라져 아래 요소가 출렁인다 — 빈 문구로 채운다.
        /// </summary>
        private void SetPassiveCard(HostEntry e, bool unlocked, bool skillSealed)
        {
            var p = e.HasPassiveSkill ? _player.GetPassiveSkill(e.PassiveSkillKey) : null;
            bool empty = p == null;
            LayoutBottom(empty);
            if (empty) return;   // 칸이 꺼졌으니 채울 것도 없다

            _ui.SetText("PassiveSkillLabel", "PASSIVE");
            // 확률형만 숫자를 띄운다. 상시형에 "100%" 를 적으면 확률처럼 읽힌다.
            _ui.SetText("PassiveSkillChanceText",
                p.ChancePercent > 0 ? $"{p.ChancePercent}%" : string.Empty);
            _ui.SetText("PassiveSkillNameText", p.DisplayName);
            _ui.SetText("PassiveSkillDescText", p.DisplayDescription);

            // ⚠ 패시브 아이콘은 뺐다. 좁은 칸에서 글자와 겹쳐 문장을 가렸다 —
            //   무엇인지는 이름과 설명이 이미 말한다.
            _ui.SetActive("PassiveSkillIcon", false);
            _ui.SetActive("PassiveSkillSealIcon", false);
        }

        // ── 카드 아래쪽 높이 ────────────────────────────────────
        //
        // 패시브가 없으면 칸이 줄고, 숙련도가 그만큼 올라온다.
        // 카드 총 높이(737)는 고정이라 **두 상태가 모두 들어가야** 한다 —
        // 패시브를 늘 큰 칸으로 두면 없는 몸(12명)에서 빈 구멍이 생기고,
        // 늘 작은 칸으로 두면 있는 몸에서 글자가 칸 밖으로 흘러나온다.
        /// <summary>패시브 칸 높이. 설명이 두 줄인 몸(설녀·청룡 등)까지 들어간다.</summary>
        private const float PassiveHeight = 90f;
        private const float PassiveTop = 584f;   // 능력치가 일곱 칸이 되며 23 px 내려갔다
        private const float BlockGap = 8f;

        /// <summary>
        /// 패시브가 없으면 **칸을 통째로 끈다.** 23명 중 12명이 없는데
        /// 빈 칸을 남겨 두면 카드 절반이 "없음" 을 설명하는 자리가 된다.
        /// 숙련도가 그 자리로 올라와 아래가 비지 않는다.
        /// </summary>
        private void LayoutBottom(bool passiveEmpty)
        {
            _ui.SetActive("PassiveSkillCard", !passiveEmpty);

            if (_ui.Find("MasteryGroup") is RectTransform mastery)
                mastery.anchoredPosition = new Vector2(mastery.anchoredPosition.x,
                    -(passiveEmpty ? PassiveTop : PassiveTop + PassiveHeight + BlockGap));
        }

        /// <summary>
        /// 숙련도와 파편.
        ///
        /// 봉인(Lv 0)이면 목표가 **봉인 해제 비용**이고, 그 뒤로는 다음 레벨 비용이다.
        /// 무엇을 향해 모으는 중인지가 안 보이면 파편이 왜 쌓이는지 알 수 없다.
        /// </summary>
        private void SetMastery(HostEntry e, bool unlocked)
        {
            _ui.SetText("MasteryLabel", Localize.Get("ui.hostselect.mastery"));

            int lv = _player.GetMastery(e.HostKey);
            int have = _player.GetShards(e.HostKey);
            // 단계 값은 같은 표(`_shardCurve`)에서 나온다 — 갈래를 둘로 두지 않는다.
            // 값·등급 배수는 `GameConfig` 에 있고 서비스가 곱해 준다.
            int need = _player.MasteryCost(e.HostKey);

            // 숙련도는 **몇 단계 중 몇인지**가 먼저다. 파편은 그 아래 진행이다.
            _ui.SetText("MasteryLabel", Localize.Get("ui.hostselect.mastery"));
            _ui.SetText("MasteryValueText", $"Lv {lv} / {_player.MasteryMax}");
            _ui.SetText("ShardText", Localize.Get("ui.hostselect.shard"));
            _ui.SetText("ShardCountText", need <= 0 ? "MAX" : $"{have} / {need}");

            // 빈 슬롯 처리로 죽어 있던 색을 되돌린다
            var mframe = _ui.Get<Image>("MasteryGroup");
            if (mframe != null) mframe.color = Color.white;
            foreach (var n in new[] { "MasteryLabel", "MasteryValueText" })
            {
                var t = _ui.Get<TMPro.TextMeshProUGUI>(n);
                if (t != null) t.color = _labelColor;
            }

            var fill = _ui.Find("ShardBarFill") as RectTransform;
            if (fill == null) return;
            float r = need <= 0 ? 1f : Mathf.Clamp01((float)have / need);
            fill.sizeDelta = new Vector2(ShardBarWidth * r, fill.sizeDelta.y);
        }

        /// <summary>파편 바 전체 폭. 프리팹 `ShardBarBg` 와 같아야 한다.</summary>
        private const float ShardBarWidth = 239f;

        /// <summary>
        /// 시작 버튼 두 개의 값 표기.
        ///
        /// 몸을 데려가면 등급값을 내고, 유령은 공짜다. 두 값이 **나란히 보여야**
        /// "싸게 갈까 편하게 갈까" 가 선택이 된다 — 한쪽만 보이면 그냥 세금이다.
        /// </summary>
        private void SetStartCost(HostEntry e, bool unlocked)
        {
            _ui.SetText("PossessTitleText", e.IsGhost ? Localize.Get("ui.hostselect.start_ghost") : Localize.Get("ui.hostselect.possess_start"));
            // ⚠ 버튼 밑줄은 껐다. 보라 바탕 위 보라 글자라 읽히지 않았다.
            //   여기 있던 입장 골드 표기(`B급 -300 G`)는 **아직 갈 곳이 없다** —
            //   어디에 둘지 정해지면 그때 다시 붙인다.
            _ui.SetActive("PossessSubText", false);
        }

        private const string GameConfigAddress = "TableData/GameConfig";
        private Game.Character.GameConfig _config;

        /// <summary>
        /// 유령 칸의 정보창.
        ///
        /// 유령은 스킬도 패시브도 숙련도도 없다. 그 자리를 비워 두면
        /// **"이걸로 뭘 하라는 거지"** 가 된다 — 유령이 실제로 하는 일을 그 칸에 넣는다.
        ///
        ///   직업 자리 → 유령이 무엇인가 (몸이 없다 · 시계가 돈다)
        ///   액티브 자리 → **빙의** (사거리 · 연출 · 무적)
        ///   패시브 자리 → **소멸** (초당 감소 · 몸을 잃는 값)
        ///   숙련도 자리 → **고스트 Lv** — 유령의 성장은 이쪽이다
        ///
        /// 수치는 `GameConfig` 에서 그대로 읽는다. 손으로 적으면 곧 어긋난다.
        /// </summary>
        private void SetGhostCard()
        {
            float drain = _config != null ? _config.GhostDrainPerSecond : 6.7f;
            int hp = _config != null ? _config.GhostHpMax : 100;
            float life = drain > 0f ? hp / drain : 0f;

            var gb = _ui.Get<Image>("GradeBadge");
            if (gb != null) gb.enabled = false;   // 유령은 등급이 없다

            _ui.SetText("JobBadgeText", Localize.Get("ui.hostselect.ghost.badge"));
            var badge = _ui.Get<Image>("JobBadge");
            if (badge != null) badge.enabled = false;   // 유령은 직업 배지 그림이 없다

            _ui.SetText("JobSkillLabel", "JOB TRAIT");
            _ui.SetText("JobTraitNameText", Localize.Get("ui.hostselect.ghost.trait"));
            // ⚠ 초·퍼센트를 늘어놓지 않는다. 0.7초·1.25초는 **플레이하면 몸으로 아는 값**이다.
            //   화면에는 "무엇을 조심해야 하는가" 한 줄만 남긴다.
            _ui.SetText("JobRuleText",
                Localize.Get("ui.hostselect.ghost.rule"));

            _ui.SetText("ActiveSkillLabel", "ACTIVE SKILL");
            _ui.SetText("ActiveSkillNameText", Localize.Get("ui.hostselect.ghost.skill"));
            _ui.SetText("ActiveSkillDescText",
                _config != null
                    ? Localize.Format("ui.hostselect.ghost.skill_desc", _config.PossessRange / 72f)
                    : Localize.Get("ui.hostselect.ghost.skill_desc_near"));

            // 유령은 패시브가 없다 — 빈 슬롯으로 둔다.
            MarkPassiveEmpty();

            // 유령은 파편으로 크지 않는다. 아래 칸도 비운다.
            MarkMasteryEmpty();

            // 스킬 아이콘은 없다. 흰 사각형이 되지 않게 끈다.
            var ghostIcon = _ui.Get<Image>("ActiveSkillIcon");
            if (ghostIcon != null) ghostIcon.enabled = false;
            _ui.SetActive("ActiveSkillSealIcon", false);
            _ui.SetActive("PassiveSkillSealIcon", false);
        }

        /// <summary>유령은 패시브가 없다 — 칸을 끈다.</summary>
        private void MarkPassiveEmpty() => LayoutBottom(true);

        /// <summary>숙련도 칸을 빈 슬롯으로 죽인다 (유령 전용).</summary>
        private void MarkMasteryEmpty()
        {
            // 유령은 파편으로 크지 않는다 — 이 칸은 **고스트 Lv** 을 보여주는 자리다.
            _ui.SetText("MasteryLabel", Localize.Get("ui.hostselect.ghost.level"));
            _ui.SetText("MasteryValueText", $"Lv {_player.GhostLevel} / {_player.GhostLevelMax}");
            _ui.SetText("ShardText", Localize.Get("ui.hostselect.ghost.level_by_gold"));
            _ui.SetText("ShardCountText", $"{_player.GhostLevelCost(_player.GhostLevel + 1):N0} G");
            var fill = _ui.Find("ShardBarFill") as RectTransform;
            if (fill != null)
                fill.sizeDelta = new Vector2(
                    ShardBarWidth * Mathf.Clamp01((float)_player.GhostLevel / _player.GhostLevelMax),
                    fill.sizeDelta.y);
        }

        // ── 직업 ──────────────────────────────────────────────
        //
        // `BattleDirector.JobOf` 와 **같은 규칙**이다. 저 쪽은 전투용이라
        // 여기서 부를 수 없어 규칙만 옮겨 적는다 — 바뀌면 두 곳을 함께 고친다.
        // ⚠ 직업은 **셋**이다(확정본 2026-09-14) — 근거리 6 · 중거리 3 · 원거리 14.
        //   예전의 「관통」은 직업에서 뺐다. 적을 뚫는 것은 직업이 아니라
        //   그 몸이 든 무기의 성질(`AttackKind.Pierce`)이다.
        private enum HostJob { Melee, Mid, Ranged }

        private const float MidRangeMeters = 6.0f;

        /// <summary>
        /// `GANGSTER — GUN` → `GANGSTER`. 표의 영문명에는 무기 구분이 붙어 있는데
        /// 이름줄에 그대로 넣으면 두 줄로 접히거나 잘린다.
        /// </summary>
        private static string ShortNameEn(string nameEn)
        {
            if (string.IsNullOrEmpty(nameEn)) return string.Empty;
            int cut = nameEn.IndexOf('—');            // em dash
            if (cut < 0) cut = nameEn.IndexOf(" - ", System.StringComparison.Ordinal);
            if (cut < 0) cut = nameEn.IndexOf('(');
            return (cut > 0 ? nameEn.Substring(0, cut) : nameEn).Trim();
        }

        private static HostJob JobOf(HostEntry e)
        {
            if (e == null) return HostJob.Ranged;
            if (e.Kind == AttackKind.Melee || e.Kind == AttackKind.Pulse) return HostJob.Melee;
            return e.CanonHostRange > 0f && e.CanonHostRange <= MidRangeMeters
                 ? HostJob.Mid : HostJob.Ranged;
        }

        private static string JobNameOf(HostJob j) => j switch
        {
            HostJob.Melee => Localize.Get("ui.job.melee"),
            HostJob.Mid   => Localize.Get("ui.job.mid"),
            _             => Localize.Get("ui.job.ranged"),
        };

        private static string JobSpriteOf(HostJob j) => j switch
        {
            HostJob.Melee => "melee",
            HostJob.Mid   => "midrange",
            _             => "ranged",
        };

        /// <summary>
        /// 직업 특성의 **이름**. 규칙 한 줄만 있으면 "이게 이 몸의 무엇인지" 가
        /// 안 잡힌다 — 액티브·패시브처럼 이름이 붙어야 같은 층으로 읽힌다.
        /// 세 벌뿐이며 직업에서 바로 나온다.
        /// </summary>
        private static string JobTraitNameOf(HostJob j) => j switch
        {
            HostJob.Melee => Localize.Get("ui.job.melee.trait"),
            HostJob.Mid   => Localize.Get("ui.job.mid.trait"),
            _             => Localize.Get("ui.job.ranged.trait"),
        };

        /// <summary>
        /// 직업 상시 규칙.
        ///
        /// ⚠ **상시 규칙이 있는 직업은 근거리 하나뿐이다**(확정본 2026-09-14).
        ///   중거리의 착탄 범위(`SplashAround`)는 코드에서 걷어냈는데 이 문구만 남아
        ///   "한 칸이 함께 터진다" 고 **없는 규칙을 알리고 있었다.**
        ///   나머지 둘은 규칙 대신 무엇이 그 자리를 대신하는지를 적는다.
        /// </summary>
        private static string JobRuleOf(HostJob j) => j switch
        {
            HostJob.Melee => Localize.Get("ui.job.melee.rule"),
            HostJob.Mid   => Localize.Get("ui.job.mid.rule"),
            _             => Localize.Get("ui.job.ranged.rule"),
        };

        /// <summary>숫자가 아닌 것을 능력치 칸에 적는다 (유령의 `—` 등).</summary>
        private void SetStatText(string stat, string text) => SetStatCell(stat, text, 0f);

        /// <summary>
        /// 화면에 적는 한 방 피해. **`BattleDirector.HostAtkOf` 와 같은 식이어야 한다** —
        /// 여기만 탄 수로 안 나누면 두 발씩 쏘는 몸이 카드에는 7, 판에서는 3 으로 뜬다.
        /// </summary>
        private int AtkOf(HostEntry e)
            => Mathf.Max(1, Mathf.RoundToInt(
                   _config.AtkOfGrade(e.AtkGrade, e.RateGrade) / Mathf.Max(1, e.ShotCount)));

        /// <summary>이 몸의 지금 치명타 확률(%). 고스트 Lv 이 얹힌 값이다.</summary>
        private float CritPercentOf(HostEntry e)
            => HostStats.CritPercent(_config, e, _player.GhostLevel, _player.GhostLevelMax);

        /// <summary>이 몸의 지금 사거리(m). 직업 밴드 상한에서 잘린 값이다.</summary>
        private float RangeOf(HostEntry e)
            => HostStats.Range(_config, e, (int)JobOf(e), _player.GhostLevel, _player.GhostLevelMax);

        private void SetStat(string stat, int value)
            => SetStat(stat, value, value.ToString(), MaxStat, value);

        /// <summary>
        /// 능력치 한 칸. 글자와 바를 따로 받는다 — 단위가 칸마다 다르기 때문이다
        /// (0~100 · m · 초당 횟수 · %).
        /// </summary>
        private void SetStat(string stat, int value, string text, float barMax, float barValue = -1f)
        {
            if (barValue < 0f) barValue = value;
            SetStatCell(stat, text, barMax <= 0f ? 0f : Mathf.Clamp01(barValue / barMax));
        }

        private void SetStatCell(string stat, string text, float ratio)
        {
            var row = _ui.Find($"StatRow_{stat}");
            if (row == null) return;
            var label = _ui.Find(row, "StatLabelText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = stat;
            var valueText = _ui.Find(row, "StatValueText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (valueText != null) valueText.text = text;
            var fill = _ui.Find(row, "StatBarFill") as RectTransform;
            if (fill != null) fill.sizeDelta = new Vector2(StatBarWidth * ratio, fill.sizeDelta.y);
        }


        private void RefreshFooter()
        {
            _ui.SetText("TipText",
                Localize.Get("ui.hostselect.tip"));
            _ui.SetText("OwnedHostCountText", Localize.Format("ui.hostselect.owned", _player.OwnedHostCount, _player.PlayableHosts.Count));
            _ui.SetText("HostListTitleText", "HOST LIST");
        }

        private void OnPossessStart()
        {
            if (!_isChapterStart) { Close(); return; }

            var e = _player.GetHost(_selectedKey);
            if (e == null || !_player.IsHostUnlocked(e)) return;

            // 유령 칸을 골랐으면 몸 없이 들어간다. 골드도 안 낸다.
            if (e.IsGhost)
            {
                _player.StartAsGhost = true;
                EnterGameAsync().Forget(); // fire-and-forget: 씬 전환 완료 대기 불필요
                return;
            }

            // 몸값을 낸다. 모자라면 들어가지 않고 얼마가 모자란지 알린다 —
            // 조용히 유령으로 바꿔 넣으면 "왜 내 캐릭터가 아니지" 가 된다.
            int cost = PlayerDataService.EntryCostOf(e);
            if (!_player.PayHostEntry(e))
            {
                SystemPopup.Show(
                    Localize.Format("ui.hostselect.gold_short", e.DisplayName, e.Grade, _player.Gold, cost),
                    null, Localize.Get("ui.common.ok"), null);
                return;
            }

            _player.StartAsGhost = false;
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

        /// <summary>이미 있으면 건너뛴다. 실패해도 화면은 뜬다 — 그 부분만 빠진다.</summary>
        private static async UniTask LoadOrWarnAsync<T>(IResourceManager res, string address,
                                                        T current, Action<T> assign, string label)
            where T : UnityEngine.Object
        {
            if (current != null) return;
            try { assign(await res.LoadAsync<T>(address)); }
            catch (Exception e) { Debug.LogWarning($"[HostSelect] {label} 로드 실패 — {e.Message}"); }
        }

        private Sprite GetSprite(string name)
            => _atlas != null ? _atlas.GetSprite(name) : null;
    }
}
