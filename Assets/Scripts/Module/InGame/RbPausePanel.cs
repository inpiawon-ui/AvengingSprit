using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 일시정지 — 시안 `_exchange/in/mock_pause_panel_free_peak.png`(PD 2026-10-27 「레벨업 카드랑 현재 호스트 정보를 제대로 정리」).
    ///   「1-07 / 30 · 스토리」 · 지금 빌드(카드 4 × 2 — 등급 칸 · 그림 · 이름 · Lv, 빈 칸은 +) ·
    ///   지금 몸(큰 얼굴 · 이름 · 등급 · 체력 줄 · 속성 꼬리표 · 액티브 / 필살기) · 설정 · 포기하기 · 계속하기.
    /// 포기하기는 확인 창을 한 번 더 띄운다(되돌릴 수 없다). 열린 동안 판을 멈춘다(Time.timeScale 0 — 닫을 때 원래 값으로).
    /// 노드 · 그림은 빌더(RbInGameBuilder)가 세우고 넣는다.
    /// </summary>
    public sealed class RbPausePanel : MonoBehaviour
    {
        private const int CardSlots = 8;
        private static readonly string[] RarityLabel = { "COMMON", "RARE", "EPIC", "LEGENDARY" };
        private static readonly string[] AffinityKey = { null, "ui.affinity.blade", "ui.affinity.force", "ui.affinity.magic" };

        [SerializeField] private Sprite[] _cardFrames;    // CardRarity 순서 — 일반 · 희귀 · 영웅 · 전설
        [SerializeField] private Sprite[] _hostFrames;    // HostGrade 순서 — R · SR · SSR
        [SerializeField] private Sprite _ghostFrame;
        [SerializeField] private Sprite[] _gradeTags;     // HostGrade 순서
        [SerializeField] private Sprite[] _affinityIcons; // Affinity 순서 — 0(없음)은 비워 둔다

        private UIBinder _ui;
        private InGameMainUI _main;
        private float _scaleBefore = 1f;
        private bool _open;
        private readonly List<KeyValuePair<string, int>> _cards = new();

        public void Open(InGameMainUI main)
        {
            _main = main;
            if (_ui == null)
            {
                _ui = new UIBinder(transform);
                _ui.OnClick("RbPauseContinue", Close);
                _ui.OnClick("RbPauseClose", Close);
                _ui.OnClick("RbPauseSettings", () => SystemPopup.Show(Localize.FromTable("ui.rb.soon", "준비 중이에요 · 업데이트로 열려요"), null, Localize.Get("ui.common.ok"), null));
                _ui.OnClick("RbPauseAbandon", () => _ui.SetActive("RbAbandon", true));
                _ui.OnClick("RbAbandonCancel", () => _ui.SetActive("RbAbandon", false));
                _ui.OnClick("RbAbandonClose", () => _ui.SetActive("RbAbandon", false));
                _ui.OnClick("RbAbandonConfirm", GiveUp);
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _ui.SetActive("RbAbandon", false);
            if (!_open) { _scaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f; _open = true; }
            Time.timeScale = 0f;
            Fill();
        }

        private void Fill()
        {
            _ui.SetText("RbPauseStage", _main.StageLine);
            var battle = _main.Battle;
            FillCards(battle);
            FillHost(battle);
        }

        // 카드 — 레벨 높은 것부터 여덟. 남는 칸은 흐린 「+」 칸
        private void FillCards(BattleDirector battle)
        {
            _cards.Clear();
            if (battle != null) foreach (var kv in battle.CardLevels) if (kv.Value > 0) _cards.Add(kv);
            _cards.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < CardSlots; i++)
            {
                bool on = i < _cards.Count;
                var slot = _ui.Find($"RbPauseCard{i}");
                if (slot == null) continue;
                _ui.Find(slot, $"RbPauseCard{i}Full")?.gameObject.SetActive(on);
                _ui.Find(slot, $"RbPauseCard{i}Empty")?.gameObject.SetActive(!on);
                if (!on) continue;
                string key = _cards[i].Key;
                int rarity = (int)_main.CardRarityOf(key);
                if (_ui.Find(slot, $"RbPauseCard{i}Frame")?.GetComponent<Image>() is Image frame && _cardFrames != null
                    && rarity < _cardFrames.Length) frame.sprite = _cardFrames[rarity];
                if (_ui.Find(slot, $"RbPauseCard{i}Icon")?.GetComponent<Image>() is Image icon)
                {
                    icon.sprite = _main.CardIconOf(key);
                    icon.enabled = icon.sprite != null;
                }
                SetText(slot, $"RbPauseCard{i}Rarity", RarityLabel[Mathf.Clamp(rarity, 0, RarityLabel.Length - 1)]);
                SetText(slot, $"RbPauseCard{i}Name", _main.CardNameOf(key));
                SetText(slot, $"RbPauseCard{i}Lv", $"Lv {_cards[i].Value}");
            }
        }

        // 몸 — 유령이면 유령 테두리 · 「몸 없음 · 유령」 · 체력 · 속성 · 스킬 칸은 비운다
        private void FillHost(BattleDirector battle)
        {
            string host = _main.ShownHostKey;
            Game.Character.HostEntry he = null;
            bool has = !string.IsNullOrEmpty(host) && battle != null
                       && GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var player)
                       && (he = player.GetHost(host)) != null;
            int grade = has ? (int)he.Grade : 0;

            SetIcon("RbPauseHostFace", has ? _main.FaceOf(host) : null);
            if (_ui.Get<Image>("RbPauseHostFrame") is Image frame)
                frame.sprite = has && _hostFrames != null && grade < _hostFrames.Length ? _hostFrames[grade] : _ghostFrame;
            _ui.SetText("RbPauseHostName", has ? he.DisplayName : Localize.FromTable("ui.rb.pause.no_host", "몸 없음 · 유령"));
            SetIcon("RbPauseHostGradeSmall", has && _gradeTags != null && grade < _gradeTags.Length ? _gradeTags[grade] : null);

            _ui.SetActive("RbPauseHp", has);
            if (has)
            {
                int hp = _main.ShownHostHp, max = Mathf.Max(1, _main.ShownHostHpMax);
                _ui.SetText("RbPauseHpText", $"{hp} / {max}");
                if (_ui.Get<Image>("RbPauseHpFill") is Image fill) fill.fillAmount = Mathf.Clamp01(hp / (float)max);
            }

            var aff = has ? AffinityRule.KindOf(host) : Affinity.None;
            bool showAff = has && AffinityRule.Enabled && aff != Affinity.None;
            _ui.SetActive("RbPauseAff", showAff);
            SetIcon("RbPauseHostAffCorner", showAff && _affinityIcons != null && (int)aff < _affinityIcons.Length ? _affinityIcons[(int)aff] : null);
            if (showAff)
            {
                SetIcon("RbPauseAffIcon", _affinityIcons != null && (int)aff < _affinityIcons.Length ? _affinityIcons[(int)aff] : null);
                _ui.SetText("RbPauseAffText", Localize.Get(AffinityKey[(int)aff]));
            }

            _ui.SetActive("RbPauseSkillBox0", has);
            _ui.SetActive("RbPauseSkillBox1", has);
            SetIcon("RbPauseSkill0", has ? battle.ActiveSkillIcon(host) : null);
            SetIcon("RbPauseSkill1", has ? battle.UltimateIcon(host) : null);
        }

        private void SetText(Transform parent, string node, string value)
        {
            if (_ui.Find(parent, node)?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI t) t.text = value;
        }

        private void SetIcon(string node, Sprite s)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void Close()
        {
            GameSound.Cue("ui.tap");
            Resume();
            gameObject.SetActive(false);
        }

        private void Resume()
        {
            if (!_open) return;
            _open = false;
            Time.timeScale = _scaleBefore;
        }

        private void GiveUp()
        {
            Resume();   // 씬을 떠나기 전에 시간을 되돌린다 — 0 으로 남으면 로비 연출이 멎는다
            if (_main.Battle != null) _main.Battle.GiveUp();   // 주운 골드는 챙겨서 나간다
            _main.LeaveToLobby();
        }

        private void OnDisable() => Resume();
    }
}
