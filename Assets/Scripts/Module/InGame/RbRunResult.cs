using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 결과 화면(시안 Screens/result-story · result-fail) — 스토리 판 클리어 · 실패 공통.
    /// 배경 그림 · 「CLEAR / FAILED」 워드아트 · 유령 · 기록 3줄(도달한 방 · 처치 · 플레이 시간) · 보상 3칸(골드 · 가장 많이 모은 조각 · 유령 경험치) · 로비로 / 다음.
    /// 보상은 전투가 이미 넣었다 — 여기는 보여 주기만 한다. 「다음 챕터 · 다시 도전」 은 그 챕터를 골라 둔 채 로비로 간다(출전 값은 로비에서 낸다).
    /// 프리팹은 빌더(RbInGameBuilder)가 세운다 — 노드 이름이 곧 키다.
    /// </summary>
    public sealed partial class RbRunResult : MonoBehaviour
    {
        [SerializeField] private Sprite _bgClear, _bgFail, _wordClear, _wordFailed, _ghostCheer, _ghostSad;

        private UIBinder _ui;
        private IPlayerDataService _player;
        private int _nextChapter;

        public void Show(StageFinishedEvent e, SpriteAtlas faces)
        {
            Bind();
            _mode = ResultMode.Story;
            CoreModule.TryGet(out _player);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui — 열 때 맨 위

            bool clear = e.IsCleared;
            int ch = Mathf.Max(1, e.FinishedChapter);
            int chapters = _player != null ? Mathf.Max(1, _player.ChapterCount) : ch;
            _nextChapter = clear ? Mathf.Min(ch + 1, chapters) : ch;

            SetSprite("RbResultBg", clear ? _bgClear : _bgFail);
            SetSprite("RbResultWord", clear ? _wordClear : _wordFailed);
            _ui.SetActive("RbResultHeadline", false);
            _ui.SetActive("RbResultExpTile", true);
            _ui.SetActive("RbResultGoldTile", true);
            SetSprite("RbResultGhost", clear ? _ghostCheer : _ghostSad);
            _ui.SetText("RbResultChapter", $"CHAPTER {ch}");
            bool unlocked = clear && ch < chapters;
            _ui.SetActive("RbResultNew", unlocked);
            if (unlocked) _ui.SetText("RbResultNewText", string.Format(Localize.FromTable("ui.rb.result.unlocked", "CH {0} 열림!  <color=#FFC93D>NEW</color>"), ch + 1));

            // 기록
            int rooms = Mathf.Max(1, e.ChapterRooms);
            _ui.SetText("RbResultRoomValue", $"{Mathf.Clamp(e.ReachedRoom, 0, rooms)}/{rooms}");
            _ui.SetText("RbResultKillValue", e.Kills.ToString("N0"));
            int sec = Mathf.Max(0, Mathf.RoundToInt(e.PlaySeconds));
            _ui.SetText("RbResultTimeValue", $"{sec / 60:00}:{sec % 60:00}");

            // 보상 — 골드 · 가장 많이 모은 조각(얼굴) · 유령 경험치
            _ui.SetText("RbResultGoldValue", e.RewardGold.ToString("N0"));
            bool shard = e.ShardHostKeys != null && e.ShardHostKeys.Length > 0;
            _ui.SetActive("RbResultShardTile", shard);
            if (shard)
            {
                string key = e.ShardHostKeys[0];
                var host = _player?.GetHost(key);
                _ui.SetText("RbResultShardValue", e.ShardCounts[0].ToString("N0"));
                _ui.SetText("RbResultShardLabel", string.Format(Localize.FromTable("ui.rb.result.shard", "{0} 조각"), host != null ? host.DisplayName : key));
                if (_ui.Get<Image>("RbResultShardFace") is Image face)
                {
                    face.sprite = faces != null ? faces.GetSprite($"face_{key}") : null;
                    face.enabled = face.sprite != null;
                }
            }
            _ui.SetText("RbResultExpValue", e.RewardGhostExp.ToString("N0"));
            _ui.SetText("RbResultNextText", clear
                ? (ch < chapters ? Localize.FromTable("ui.rb.result.next", "다음 챕터 ▶") : Localize.FromTable("ui.rb.result.again", "다시 도전"))
                : Localize.FromTable("ui.rb.result.again", "다시 도전"));
        }

        private void SetSprite(string node, Sprite s)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void Bind()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _ui.OnClick("RbResultLobby", () => Leave(false));
            _ui.OnClick("RbResultNext", () => Leave(true));
        }

        private void Leave(bool next)
        {
            if (_mode != ResultMode.Story) { LeaveMode(next); return; }
            GameSound.Cue("ui.tap");
            if (next && _player != null) _player.SelectedChapter = _nextChapter;
            var main = GetComponentInParent<InGameMainUI>();
            if (main != null) main.LeaveToLobby();
        }
    }
}
