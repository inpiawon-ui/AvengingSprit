using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// RE:BORN 안내 창 — 시안 Screens/guide.png. 주제(sortie · growth · possess …)마다 그림 한 장 + 제목 + 두세 줄.
    /// 여러 주제를 넘기며 본다(쪽 점 · 다음 → 마지막은 확인). 로비 「?」 · 처음 들어온 순간 · 인게임 첫 판이 연다.
    /// 글은 문자열 표 `ui.rb.guide.{주제}.title` · `.body`. 그림은 키트 `rb_art_guide_{주제}`.
    /// 인게임에서는 열린 동안 판을 멈춘다(pauseGame).
    /// </summary>
    public sealed class RbGuide : MonoBehaviour
    {
        private const string SeenPrefix = "guide.seen.";

        [SerializeField] private Sprite[] _arts = Array.Empty<Sprite>();

        private UIBinder _ui;
        private readonly List<string> _pages = new();
        private int _index;
        private bool _pause;
        private float _savedScale = -1f;
        private Action _onClosed;

        public bool IsOpen => gameObject.activeSelf;

        private void Init()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _ui.OnClick("RbGuideNext", Next);
            _ui.OnClick("RbGuideClose", Close);
        }

        /// <summary>주제를 차례로 보여 준다.</summary>
        public void Show(bool pauseGame, params string[] topics) => Show(pauseGame, null, topics);

        public void Show(bool pauseGame, Action onClosed, params string[] topics)
        {
            if (topics == null || topics.Length == 0) return;
            Init();
            _pages.Clear();
            _pages.AddRange(topics);
            _index = 0;
            _onClosed = onClosed;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui — 열 때 맨 위
            _pause = pauseGame;
            if (_pause && _savedScale < 0f)
            {
                _savedScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            Fill();
        }

        /// <summary>처음 한 번만(PlayerPrefs `guide.seen.{id}`). 보였으면 true.</summary>
        public bool ShowOnce(string id, bool pauseGame, params string[] topics)
        {
            if (HasSeen(id)) return false;
            PlayerPrefs.SetInt(SeenPrefix + id, 1);
            PlayerPrefs.Save();
            Show(pauseGame, topics);
            return true;
        }

        public static bool HasSeen(string id) => PlayerPrefs.GetInt(SeenPrefix + id, 0) == 1;

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            var done = _onClosed;
            _onClosed = null;
            done?.Invoke();
        }

        private void OnDisable()
        {
            // 판을 멈춰 뒀으면 되돌린다 — 닫기 · 씬 전환 · 비활성 어느 길로 사라져도
            if (_savedScale >= 0f)
            {
                Time.timeScale = _savedScale;
                _savedScale = -1f;
            }
        }

        private void Next()
        {
            GameSound.Cue("ui.tap");
            if (_index < _pages.Count - 1) { _index++; Fill(); }
            else Close();
        }

        private void Fill()
        {
            string topic = _pages[_index];
            _ui.SetText("RbGuideTitle", Localize.Get($"ui.rb.guide.{topic}.title"));
            _ui.SetText("RbGuideBody", Localize.Get($"ui.rb.guide.{topic}.body"));
            if (_ui.Get<Image>("RbGuideArt") is Image art)
            {
                var s = ArtOf(topic);
                art.sprite = s;
                art.enabled = s != null;
            }
            // 쪽 점 — 한 장이면 감춘다
            _ui.SetActive("RbGuideDots", _pages.Count > 1);
            if (_pages.Count > 1)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _pages.Count; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.Append(i == _index ? "<color=#3EE6FF>●</color>" : "○");
                }
                _ui.SetText("RbGuideDots", sb.ToString());
            }
            bool last = _index >= _pages.Count - 1;
            _ui.SetText("RbGuideNextText", last ? Localize.FromTable("ui.common.ok", "확인") : Localize.FromTable("ui.rb.guide.next", "다음"));
        }

        private Sprite ArtOf(string topic)
        {
            string name = $"rb_art_guide_{topic}";
            for (int i = 0; i < _arts.Length; i++)
                if (_arts[i] != null && _arts[i].name == name) return _arts[i];
            return null;
        }
    }
}
