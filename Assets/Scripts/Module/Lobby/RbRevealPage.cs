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
    /// 각성 줄은 그 몸 액티브의 각성 옵션(askill.{몸}.opt) — 6성에 열리고 10성에 효과 2배(SkillScaling.SpecLevel).
    /// </summary>
    public sealed class RbRevealPage : MonoBehaviour
    {
        public const int AwakenStart = 6, AwakenFull = 10;

        [SerializeField] private Sprite _rankGold, _rankSilver;   // 성급 별 — 큰 금별(5성마다) · 작은 은별(RbHostPage.Stars)

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
            _ui.SetText("RvlTitle", fresh ? Localize.FromTable("ui.rb.reveal.new", "새 호스트!") : Localize.FromTable("ui.rb.reveal.awaken_title", "각성"));
            string head = fresh ? Localize.FromTable("ui.rb.reveal.new", "새 호스트!")
                        : rank >= AwakenFull ? Localize.Get("ui.growth6.opt_max")   // 「10성 각성 강화 · 효과 2배」(옛 성장 화면과 같은 말)
                        : string.Format(Localize.FromTable("ui.rb.growth.opt_open", "{0}성 각성 옵션 열림"), rank);
            _ui.SetText("RvlHead", head);
            if (_ui.Get<TextMeshProUGUI>("RvlHead") is TextMeshProUGUI h) h.color = rank >= AwakenFull ? Gold : Teal;
            _ui.SetText("RvlName", e.DisplayName);
            _ui.SetActive("RvlChip", fresh);
            _ui.SetActive("RvlStars", !fresh);
            if (fresh) _ui.SetText("RvlChipText", TagLine(e));
            else RbHostPage.Stars(_ui.Find("RvlStars"), "RvlStar", rank, _rankGold, _rankSilver);
            // 각성 — 그 몸 액티브의 각성 옵션(askill.{몸}.opt, 6성에 열리고 10성에 2배)
            string opt = Localize.Get($"askill.{hostKey}.opt");
            if (opt == $"askill.{hostKey}.opt") opt = Localize.FromTable("ui.rb.growth.star_gain", "체력·공격 +7%");
            _ui.SetText("RvlDesc", fresh ? Localize.Get($"host.{hostKey}.desc") : opt);
            if (_ui.Get<Image>("RvlArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.Show(hostKey, Color.white);
                // 몸 뒤 연출 — 새 몸 시안 빛살 · 6성 시안 링 · 10성 금 링 + 빛살(시안 new_body_badge · awaken_6 · awaken_10)
                if (_shell != null && _shell.Fx != null) _shell.Fx.Reveal(art.rectTransform, rank);
            }
            GameSound.Cue("run.card");
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Done()
        {
            if (_shell != null && _shell.Fx != null && _ui.Get<Image>("RvlArt") is Image art) _shell.Fx.Stop(art.rectTransform);
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
