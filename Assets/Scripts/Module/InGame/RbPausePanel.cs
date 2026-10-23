using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 일시정지(시안 Screens/pause · abandon) — 「1-07 /16」 · 지금 빌드(카드 · 몸 · 스킬) · 설정 · 포기하기 · 계속하기.
    /// 포기하기는 확인 창을 한 번 더 띄운다(되돌릴 수 없다). 열린 동안 판을 멈춘다(Time.timeScale 0 — 닫을 때 원래 값으로).
    /// 노드는 빌더(RbInGameBuilder)가 세운다.
    /// </summary>
    public sealed class RbPausePanel : MonoBehaviour
    {
        private const int CardSlots = 6;

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

            // 카드 — 레벨 높은 것부터 최대 여섯
            _cards.Clear();
            if (battle != null) foreach (var kv in battle.CardLevels) if (kv.Value > 0) _cards.Add(kv);
            _cards.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < CardSlots; i++)
            {
                bool on = i < _cards.Count;
                _ui.SetActive($"RbPauseCard{i}", on);
                if (!on) continue;
                var slot = _ui.Find($"RbPauseCard{i}");
                if (_ui.Find(slot, $"RbPauseCard{i}Icon")?.GetComponent<Image>() is Image icon)
                {
                    icon.sprite = _main.CardIconOf(_cards[i].Key);
                    icon.enabled = icon.sprite != null;
                }
                if (_ui.Find(slot, $"RbPauseCard{i}Lv")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI lv) lv.text = $"Lv {_cards[i].Value}";
            }
            _ui.SetActive("RbPauseNoCard", _cards.Count == 0);

            // 몸 · 스킬(유령이면 「몸 없음」)
            string host = _main.ShownHostKey;
            bool has = !string.IsNullOrEmpty(host) && battle != null;
            SetIcon("RbPauseHostFace", has ? _main.FaceOf(host) : null);
            string hostName = has && GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var player)
                              && player.GetHost(host) is Game.Character.HostEntry he
                ? he.DisplayName : Localize.FromTable("ui.rb.pause.no_host", "몸 없음 · 유령");
            _ui.SetText("RbPauseHostName", hostName);
            SetIcon("RbPauseSkill0", has ? battle.ActiveSkillIcon(host) : null);
            SetIcon("RbPauseSkill1", has ? battle.UltimateIcon(host) : null);
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
