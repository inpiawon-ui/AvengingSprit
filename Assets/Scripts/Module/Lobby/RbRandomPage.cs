using System;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 랜덤 출전 공개 — 시안 Screens/random-shuffle · random-reveal · hidden-reveal.png.
    /// 박자 1: 「RANDOM · 어떤 몸과 만날까요? · ? · 공개하기」 / 박자 2: 「이번 판의 호스트 · 등급 이름 · 칩 · 전투 시작」(히든이면 HIDDEN!).
    /// 행동력 · 골드는 「전투 시작」 에서 낸다 — 공개만 보고 뒤로 나가면 아무것도 안 쓴다.
    /// 시안의 섞이는 연출(shuffle)은 아직 없다 — 정지 화면 두 장(연출은 fx 단계).
    /// </summary>
    public sealed class RbRandomPage : MonoBehaviour
    {
        private const float ChipPad = 18f, ChipGap = 12f;

        [SerializeField] private Sprite _wordRandom, _wordHidden, _mystery;

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private IPlayerDataService _player;
        private string _key;
        private bool _revealed;
        private Action _onStart;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("RndGo", Go);
        }

        public void Show(IPlayerDataService player, string hostKey, Action onStart)
        {
            Awake();
            _player = player;
            _key = hostKey;
            _onStart = onStart;
            _revealed = false;
            gameObject.SetActive(true);
            SetWord(_wordRandom);
            _ui.SetText("RndSub", Localize.FromTable("ui.rb.rnd.ask", "어떤 호스트와 만날까요?"));
            SetFace(_mystery);
            Phase(false);
            _ui.SetText("RndGoText", Localize.FromTable("ui.rb.rnd.reveal", "공개하기"));
        }

        /// <summary>뒤로 — 내지 않고 닫는다.</summary>
        public void Close() => gameObject.SetActive(false);

        private void Go()
        {
            if (!_revealed) { Reveal(); return; }
            Close();
            _onStart?.Invoke();
        }

        private void Reveal()
        {
            _revealed = true;
            GameSound.Cue("run.card");
            var e = _player.GetHost(_key);
            bool hidden = _key == PlayerDataService.HiddenTrialHostKey;
            var cfg = _player.Config;
            int gold = cfg != null ? cfg.RandomLuckGoldPct : 20;
            SetWord(hidden ? _wordHidden : _wordRandom);
            _ui.SetText("RndSub", hidden ? Localize.FromTable("ui.rb.rnd.hidden_sub", "특별한 호스트를 만났어요!") : Localize.FromTable("ui.rb.rnd.this_run", "이번 판의 호스트"));
            // 받침 위 큰 몸(작업 지시서 4-2) — 히든은 실루엣(얼굴을 숨긴다)
            SetFace(null);
            _ui.SetActive("RndPedestal", true);
            _ui.SetActive("RndSilhouette", hidden);
            _ui.SetActive("RndArt", !hidden);
            if (!hidden && _ui.Get<Image>("RndArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.Show(_key, Color.white);
            }
            _ui.SetText("RndName", hidden ? Localize.FromTable("ui.rb.rnd.hidden_name", "히든 호스트")
                                          : e != null ? $"{HostGradeLabel.Of(e.Grade)} {e.DisplayName}" : _key);
            bool owned = e != null && _player.IsHostOwned(e);
            _ui.SetText("RndLine", hidden ? Localize.FromTable("ui.rb.rnd.hidden_line", "이번 판 한정 체험 · 조각 미지급")
                                 : owned ? Localize.FromTable("ui.rb.rnd.owned_line", "내가 키운 능력치로 출전")
                                 : Localize.FromTable("ui.rb.rnd.trial_line", "이번 판 체험 · 깨면 조각을 받아요"));
            string[] chips = hidden
                ? new[] { string.Format(Localize.FromTable("ui.rb.lobby.chip.gold", "골드 +{0}%"), gold) }
                : new[]
                {
                    string.Format(Localize.FromTable("ui.rb.lobby.chip.shards", "조각 +{0}"), cfg != null ? cfg.RandomLuckShards : 3),
                    string.Format(Localize.FromTable("ui.rb.lobby.chip.gold", "골드 +{0}%"), gold),
                };
            LayoutChips(chips);
            Phase(true);
            _ui.SetText("RndGoText", Localize.FromTable("ui.rb.rnd.start", "전투 시작"));
        }

        private void Phase(bool revealed)
        {
            if (!revealed) { _ui.SetActive("RndPedestal", false); _ui.SetActive("RndArt", false); _ui.SetActive("RndSilhouette", false); }
            _ui.SetActive("RndQuestion", !revealed);
            _ui.SetActive("RndHint", !revealed);
            _ui.SetActive("RndName", revealed);
            _ui.SetActive("RndLine", revealed);
            if (!revealed) for (int i = 0; i < 2; i++) _ui.SetActive($"RndChip{i}", false);
        }

        /// <summary>칩은 글자 폭만큼 늘려 가운데에 나란히(언어마다 길이가 다르다).</summary>
        private void LayoutChips(string[] texts)
        {
            float total = 0f;
            var widths = new float[2];
            for (int i = 0; i < 2; i++)
            {
                bool on = i < texts.Length;
                _ui.SetActive($"RndChip{i}", on);
                if (!on || _ui.Get<TextMeshProUGUI>($"RndChip{i}Text") is not TextMeshProUGUI t) continue;
                t.text = texts[i];
                widths[i] = t.GetPreferredValues(texts[i]).x + ChipPad * 2f;
                total += widths[i];
            }
            total += ChipGap * Mathf.Max(0, texts.Length - 1);
            float x = -total * 0.5f;
            for (int i = 0; i < texts.Length && i < 2; i++)
            {
                if (_ui.Find($"RndChip{i}") is not RectTransform r) continue;
                r.sizeDelta = new Vector2(widths[i], r.sizeDelta.y);
                r.anchoredPosition = new Vector2(x + widths[i] * 0.5f, r.anchoredPosition.y);
                x += widths[i] + ChipGap;
            }
        }

        private void SetWord(Sprite s)
        {
            if (_ui.Get<Image>("RndWord") is Image img && s != null) img.sprite = s;
        }

        private void SetFace(Sprite s)
        {
            if (_ui.Get<Image>("RndFace") is Image img) { img.sprite = s; img.enabled = s != null; }
        }
    }
}
