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

        private static string ShrineDescOf(ShrineGift g) => g switch
        {
            ShrineGift.FullHeal => "호스트 체력 전부 회복",
            ShrineGift.SoulHeal => "고스트 체력 전부 회복",
            ShrineGift.MaxHpUp  => $"뺏는 몸 최대 체력 +{ShrineMaxHpUpPercent}%",
            ShrineGift.AtkUp    => $"공격력 +{ShrineAtkUpPercent}%",
            ShrineGift.SpeedUp  => $"발사 간격 −{ShrineSpeedUpPercent}%",
            _                   => $"사거리 +{ShrineRangeUpPercent}%",
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
            for (int i = 0; i < _shrineOffer.Count; i++)
            {
                titles[i] = ShrineTitleOf(_shrineOffer[i]);
                descs[i] = ShrineDescOf(_shrineOffer[i]);
            }
            _bus.Publish(new ShrineOpenedEvent { Titles = titles, Descs = descs });
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
                    return "호스트 체력 전부 회복";

                case ShrineGift.SoulHeal:
                {
                    int before = _ghostHp;
                    _ghostHp = GhostHpMax;
                    PublishHp();
                    var at = Avatar != null ? Avatar.Position : Vector2.zero;
                    PlayFx("heal_plus", at, 128f, loop: false);
                    ShowHeal(at, Mathf.Max(1, _ghostHp - before));
                    return "고스트 체력 전부 회복";
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
                    return $"뺏는 몸 최대 체력 +{ShrineMaxHpUpPercent}%";

                case ShrineGift.AtkUp:
                    _buffs.AddShrineAtkPercent(ShrineAtkUpPercent);
                    return $"공격력 +{ShrineAtkUpPercent}%";

                case ShrineGift.SpeedUp:
                    _buffs.AddShrineAttackSpeedPercent(ShrineSpeedUpPercent);
                    return $"발사 간격 −{ShrineSpeedUpPercent}%";

                default:
                    _buffs.AddShrineRangePercent(ShrineRangeUpPercent);
                    return $"사거리 +{ShrineRangeUpPercent}%";
            }
        }
    }
}
