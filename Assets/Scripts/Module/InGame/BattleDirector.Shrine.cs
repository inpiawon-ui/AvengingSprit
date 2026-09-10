using System.Collections.Generic;
using Game.Module.Events;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 회복 제단 — **6개 중 3택1**.
    ///
    /// 예전에는 다가서면 그냥 회복만 했다. 그러면 이 방은 「지나가며 회복하는 자리」라
    /// 고를 것이 없다. 004 는 고스트 체력이 40% 아래일 때만 열리는 방이라
    /// **공짜로 주되 하나만** 고르게 한다 — 값은 포기하는 둘이다(기획 2026-09-08).
    ///
    /// 회복 둘 · 강화 넷으로 잡았다. 체력이 바닥이라 들어온 방이므로 회복이 늘 후보에
    /// 남아야 하지만, 회복만 있으면 「어차피 회복」이 되어 고르는 뜻이 사라진다.
    ///
    /// ⚠ 3~6 은 카드(`c001` 공격 증폭 · `c020` 불굴 …)와 효과가 겹친다. 그래도 둔다 —
    ///   제단은 **공짜**고 카드는 골드나 레벨을 치른다. 바닥일 때만 열리는 방이라
    ///   공짜여도 세지 않는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>제단이 내놓는 것. 표가 아니라 코드에 둔다 — 여섯 개뿐이고 전부 고정값이다.</summary>
        public enum ShrineGift
        {
            /// <summary>지금 몸을 가득 채운다</summary>
            FullHeal,
            /// <summary>유령을 가득 채운다</summary>
            SoulHeal,
            /// <summary>앞으로 빼앗을 몸 전부 최대 체력 +20% (판 지속)</summary>
            MaxHpUp,
            /// <summary>공격력 +25%</summary>
            AtkUp,
            /// <summary>발사 간격 −18%</summary>
            SpeedUp,
            /// <summary>사거리 +25%</summary>
            RangeUp,
        }

        private const int ShrinePickCount = 3;

        private const int ShrineMaxHpUpPercent = 20;
        private const int ShrineAtkUpPercent = 25;
        private const int ShrineSpeedUpPercent = 18;
        private const int ShrineRangeUpPercent = 25;

        private readonly List<ShrineGift> _shrineOffer = new(ShrinePickCount);
        private bool _shrineOpen;

        public bool IsShrineOpen => _shrineOpen;

        /// <summary>제단이 내놓은 셋. UI 가 읽는다.</summary>
        public IReadOnlyList<ShrineGift> ShrineOffer => _shrineOffer;

        private static string ShrineTitleOf(ShrineGift g) => g switch
        {
            ShrineGift.FullHeal => "몸을 아문다",
            ShrineGift.SoulHeal => "영혼을 채운다",
            ShrineGift.MaxHpUp  => "그릇을 넓힌다",
            ShrineGift.AtkUp    => "힘을 받는다",
            ShrineGift.SpeedUp  => "손이 빨라진다",
            _                   => "멀리 닿는다",
        };

        /// <summary>
        /// 무엇을 주는가. **수치는 적지 않는다.**
        ///
        /// 「사거리 +25%」 처럼 숫자를 박아 두면 세 칸이 전부 숫자 비교가 되어
        /// 큰 수만 고르게 된다. 무엇이 좋아지는지만 말하고, 얼마나는 몸으로 안다.
        /// </summary>
        private static string ShrineDescOf(ShrineGift g) => g switch
        {
            ShrineGift.FullHeal => "호스트 체력을 가득 채운다",
            ShrineGift.SoulHeal => "고스트 체력을 가득 채운다",
            ShrineGift.MaxHpUp  => "뺏는 몸이 더 튼튼해진다",
            ShrineGift.AtkUp    => "공격력이 오른다",
            ShrineGift.SpeedUp  => "공격이 빨라진다",
            _                   => "사거리가 늘어난다",
        };

        /// <summary>제단 선물의 아이콘. 뜻이 가장 가까운 카드 그림을 빌려 쓴다.</summary>
        public static string ShrineIconOf(int index) => index switch
        {
            0 => "buffcard_heal",     // 몸을 아문다
            1 => "buffcard_c017",     // 영혼을 채운다 — 생명 회수
            2 => "buffcard_c020",     // 그릇을 넓힌다 — 불굴(최대 체력)
            3 => "buffcard_c001",     // 힘을 받는다 — 공격 증폭
            4 => "buffcard_c006",     // 손이 빨라진다 — 가속
            _ => "buffcard_c011",     // 멀리 닿는다 — 확장
        };

        /// <summary>
        /// 제단을 연다. 여섯 중 셋을 뽑는다.
        ///
        /// ⚠ 몸이 없으면 「몸을 아문다」는 아무 일도 안 한다. 후보에서 뺀다 —
        ///   고를 수는 있는데 아무 일도 안 일어나는 칸은 선택이 아니라 함정이다.
        /// </summary>
        private void OpenShrine()
        {
            _shrineOffer.Clear();

            var pool = new List<ShrineGift>(6)
            {
                ShrineGift.SoulHeal, ShrineGift.MaxHpUp,
                ShrineGift.AtkUp, ShrineGift.SpeedUp, ShrineGift.RangeUp,
            };
            if (_host != null) pool.Insert(0, ShrineGift.FullHeal);

            for (int i = 0; i < ShrinePickCount && pool.Count > 0; i++)
            {
                int at = _rng.Next(pool.Count);
                _shrineOffer.Add(pool[at]);
                pool.RemoveAt(at);
            }
            if (_shrineOffer.Count == 0) { SpawnExit(); return; }

            _shrineOpen = true;
            var titles = new string[_shrineOffer.Count];
            var descs = new string[_shrineOffer.Count];
            var icons = new string[_shrineOffer.Count];
            for (int i = 0; i < _shrineOffer.Count; i++)
            {
                titles[i] = ShrineTitleOf(_shrineOffer[i]);
                descs[i] = ShrineDescOf(_shrineOffer[i]);
                icons[i] = ShrineIconOf((int)_shrineOffer[i]);
            }
            _bus.Publish(new ShrineOpenedEvent { Titles = titles, Descs = descs, Icons = icons });
        }

        /// <summary>
        /// 셋 중 하나를 골랐다. **거절은 없다** — 셋 다 공짜라 안 고를 이유가 없고,
        /// 안 고르는 길을 두면 그 자리가 그냥 통로가 된다(기획 2026-09-08).
        /// </summary>
        public void ChooseShrine(int index)
        {
            if (!_shrineOpen || index < 0 || index >= _shrineOffer.Count) return;
            _shrineOpen = false;
            string line = ApplyShrine(_shrineOffer[index]);
            _shrineOffer.Clear();
            _bus.Publish(new ShrineResolvedEvent { ResultLine = line });
            SpawnExit();
        }


        private string ApplyShrine(ShrineGift g)
        {
            switch (g)
            {
                case ShrineGift.FullHeal:
                    if (_host == null) return "몸이 없어 받을 수 없었다.";
                    _host.Heal(_host.HpMax);
                    PublishHp();
                    ShowHeal(_host.Position, _host.HpMax);
                    return "호스트 체력을 가득 채웠다";

                case ShrineGift.SoulHeal:
                {
                    int before = _ghostHp;
                    _ghostHp = GhostHpMax;
                    PublishHp();
                    var at = Avatar != null ? Avatar.Position : Vector2.zero;
                    PlayFx("heal_plus", at, 128f, loop: false);
                    ShowHeal(at, Mathf.Max(1, _ghostHp - before));
                    return "고스트 체력을 가득 채웠다";
                }

                // ⚠ 아래 셋은 **카드 통로를 그대로 탄다.** 제단 전용 배율을 따로 두면
                //   같은 것을 재는 자가 둘이 되어 나중에 반드시 어긋난다.
                case ShrineGift.MaxHpUp:
                    _buffs.AddShrineHostHpPercent(ShrineMaxHpUpPercent);
                    if (_host != null)
                    {
                        _host.SetHpMax(Mathf.RoundToInt(_host.HpMax * (1f + ShrineMaxHpUpPercent / 100f)));
                        PublishHp();
                    }
                    return "뺏는 몸이 더 튼튼해졌다";

                case ShrineGift.AtkUp:
                    _buffs.AddShrineAtkPercent(ShrineAtkUpPercent);
                    return "공격력이 올랐다";

                case ShrineGift.SpeedUp:
                    _buffs.AddShrineAttackSpeedPercent(ShrineSpeedUpPercent);
                    return "공격이 빨라졌다";

                default:
                    _buffs.AddShrineRangePercent(ShrineRangeUpPercent);
                    return "사거리가 늘어났다";
            }
        }
    }
}
