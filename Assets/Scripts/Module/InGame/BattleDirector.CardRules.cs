using Game.Character;

namespace Game.Module.InGame
{
    /// <summary>
    /// 카드가 뜨는 조건 · 켜지는 조건 (PD 2026-10-21 1단계 레벨업 카드).
    ///
    ///   몸 조건 — 관통 · 유도 · 벽 반사 · 추가 발사는 원거리 몸, 사거리는 근접 몸, 폭발 메아리는 폭발 몸.
    ///             안 맞는 몸이면 3택1 에 안 뜨고, 먹은 뒤 갈아타면 꺼졌다가 맞는 몸으로 오면 다시 켜진다.
    ///   테크트리 — 앞 카드 중 하나라도 가져야 뜬다(감전 → 번개 사슬 · 화염 각인 → 화상 번짐 등).
    ///
    /// 「폭발 몸」은 전투만 아는 정보(탄 종류 표 `ShotKind`)라 카드 표가 아니라 여기서 판정한다.
    /// </summary>
    public sealed partial class BattleDirector : ICardRule
    {
        public bool BodyMatches(CardBodyRule rule, HostEntry host)
        {
            if (rule == CardBodyRule.Any) return true;
            if (host == null || host.IsGhost) return false;   // 유령일 땐 몸 카드가 쉰다
            bool melee = HostStats.JobIndex(host) == HostStats.JobMelee;
            return rule switch
            {
                CardBodyRule.Melee => melee,
                CardBodyRule.Ranged => !melee,
                CardBodyRule.Blast => IsBlastBody(host),
                _ => true,
            };
        }

        public bool Owns(string cardKey) => _buffs.LevelOf(cardKey) > 0;

        /// <summary>
        /// 평타 탄이 터지는 몸 — 수류탄 · 미사일 탄을 쓰는 몸과 로봇(포탑 · 평타가 폭발).
        /// 폭발 메아리(<see cref="QueueEchoBlast"/>)가 일어나는 곳과 같은 기준이다.
        /// </summary>
        private static bool IsBlastBody(HostEntry host)
        {
            if (host == null) return false;
            if (host.HostKey == DeployHostKey) return true;
            return ShotKind.TryGetValue(host.HostKey, out var k) && (k == "grenade" || k == "missile");
        }
    }
}
