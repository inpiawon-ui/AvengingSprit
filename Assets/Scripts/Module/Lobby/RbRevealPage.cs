using System;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 새 몸 · 각성 보여주기 — 시안 Screens/host-new · awaken-6 · awaken-10.png.
    /// 머리글 색이 종류를 말한다(새 몸 · 6성 각성 = 시안색, 10성 = 금색). 확인 · 뒤로 → 그 몸의 성장 화면.
    /// 시안의 각성 옵션 문구(「표식 붙은 적이 받는 피해 +15%」)는 게임에 각성 옵션 자료가 없어 성급 효과 줄로 대신한다.
    /// </summary>
    public sealed class RbRevealPage : MonoBehaviour
    {
        public const int AwakenStart = 6, AwakenFull = 10;

        [SerializeField] private Sprite _starOn, _starOff, _starAwaken;

        private static readonly Color Teal = new(0.42f, 0.85f, 0.92f), Gold = new(1f, 0.79f, 0.24f);

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private string _key;

        private void Awake()
        {
            if (_ui != null) return;   // Open 이 먼저 불렀다(꺼진 채로 열 때) — 버튼을 두 번 걸지 않는다
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("RvlBack", Done);
            _ui.OnClick("RvlOk", Done);
        }

        /// <summary>그 몸을 처음 얻었을 때(rank 0) 또는 6 · 10성이 됐을 때(rank = 지금 성급).</summary>
        public void Show(IPlayerDataService player, string hostKey, int rank)
        {
            if (_ui == null) Awake();
            _key = hostKey;
            var e = player.GetHost(hostKey);
            if (e == null) return;
            gameObject.SetActive(true);
            bool fresh = rank < AwakenStart;
            _ui.SetText("RvlTitle", fresh ? Localize.FromTable("ui.rb.reveal.new", "새 몸!") : Localize.FromTable("ui.rb.reveal.awaken_title", "각성"));
            string head = fresh ? Localize.FromTable("ui.rb.reveal.new", "새 몸!")
                        : rank >= AwakenFull ? string.Format(Localize.FromTable("ui.rb.reveal.awaken_full", "{0}성 각성 완성"), rank)
                        : string.Format(Localize.FromTable("ui.rb.reveal.awaken", "{0}성 각성"), rank);
            _ui.SetText("RvlHead", head);
            if (_ui.Get<TextMeshProUGUI>("RvlHead") is TextMeshProUGUI h) h.color = rank >= AwakenFull ? Gold : Teal;
            _ui.SetText("RvlName", e.DisplayName);
            _ui.SetActive("RvlChip", fresh);
            _ui.SetActive("RvlStars", !fresh);
            if (fresh) _ui.SetText("RvlChipText", TagLine(e));
            else RbHostPage.Stars(_ui.Find("RvlStars"), "RvlStar", rank, _starOn, _starOff, _starAwaken);
            _ui.SetText("RvlDesc", fresh ? Localize.Get($"host.{hostKey}.desc") : Localize.FromTable("ui.rb.growth.star_gain", "체력·공격 +7%"));
            if (_ui.Get<Image>("RvlArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.Show(hostKey, Color.white);
            }
            GameSound.Cue("run.card");
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Done()
        {
            gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(_key)) _shell.OpenGrowth(_key);
        }

        /// <summary>「R · 무기 · 원거리」 — 등급 · 상성 · 사거리.</summary>
        public static string TagLine(HostEntry e, bool withGrade = true)
        {
            var kind = AffinityRule.KindOf(e.HostKey);
            string job = HostStats.JobIndex(e) switch
            {
                HostStats.JobMelee => "ui.job.melee",
                HostStats.JobMid => "ui.job.mid",
                _ => "ui.job.ranged",
            };
            string line = Localize.Get(job);
            if (kind != Affinity.None) line = $"{Localize.Get($"ui.affinity.{kind.ToString().ToLowerInvariant()}")} · {line}";
            if (withGrade) line = $"{HostGradeLabel.Of(e.Grade)} · {line}";
            return line;
        }
    }
}
