namespace Game.Module.InGame
{
    /// <summary>
    /// 시너지가 켜지는 계기.
    /// 정본은 몸을 갈아탈 때(ON_SWITCH) 말고도 근접 마무리·대시에서 켜지는 것을 둔다.
    /// </summary>
    public enum SynergyTrigger
    {
        OnSwitch,
        OnMeleeFinish,
        OnDash,
    }

    /// <summary>
    /// 시너지가 켜져 있는 동안 무엇이 달라지는가.
    /// 하나하나가 별개 규칙이라 값으로 두고 쓰는 쪽에서 갈라 본다.
    /// </summary>
    public enum SynergyKind
    {
        /// <summary>표식이 순간이동 처형 지점이 된다 (S01)</summary>
        BlinkExecution,
        /// <summary>지뢰가 빙결 룬이 된다 (S02)</summary>
        FreezeRune,
        /// <summary>근접 마무리가 흡혈을 회복으로 바꾼다 (S04)</summary>
        HealFinisher,
        /// <summary>저주가 걸린 적이 죽으면 불기둥이 솟는다 (S05)</summary>
        FirePillarCircuit,
        /// <summary>대시 동안 받는 피해가 준다 (S06)</summary>
        ArmoredDash,
        /// <summary>빙결된 적까지 순간이동 처형이 이어진다 (S07)</summary>
        FrozenBlinkChain,
        /// <summary>포탑이 네이팜을 쏜다 — 쏜 자리에 불장판 (S03)</summary>
        NapalmTurret,
        /// <summary>포탑 탄이 벽에서 튕긴다 (S08)</summary>
        BounceTurret,
    }

    /// <summary>
    /// 시너지 한 줄. 정본 SYNERGY 표를 그대로 옮긴 것이다.
    ///
    /// **이전 몸 → 지금 몸** 의 짝이 핵심이다. 시너지는 한 캐릭터가 잘나서가 아니라
    /// **무엇을 버리고 무엇으로 갈아탔는가**에서 나온다 — 그래야 교체가 선택이 된다.
    /// </summary>
    public readonly struct SynergyRule
    {
        public readonly string Id;
        public readonly string From;
        public readonly string To;
        public readonly SynergyTrigger Trigger;
        public readonly SynergyKind Kind;
        public readonly float Seconds;

        /// <summary>이 버프를 갖고 있어야 켜진다. null 이면 조건 없이 켜진다(정본 ALWAYS_BASE).</summary>
        public readonly string GateBuff;

        public SynergyRule(string id, string from, string to, SynergyTrigger trigger,
                           SynergyKind kind, float seconds, string gateBuff)
        {
            Id = id; From = from; To = to; Trigger = trigger;
            Kind = kind; Seconds = seconds; GateBuff = gateBuff;
        }
    }

    /// <summary>
    /// 정본 SYNERGY 8종. 전부 실제로 동작한다.
    ///
    /// 설치물이 붙으면서 마지막 둘(S03·S08)까지 들어와 **정본 8종이 전부 산다.**
    /// </summary>
    public static class SynergyTable
    {
        public static readonly SynergyRule[] Rules =
        {
            new("S01", "gangster",         "ninja",        SynergyTrigger.OnSwitch,
                SynergyKind.BlinkExecution,    8f,  null),
            new("S02", "commando_grenade",  "white_wizard", SynergyTrigger.OnSwitch,
                SynergyKind.FreezeRune,       12f,  "mine_alchemy"),
            new("S04", "vampire",           "amazon",       SynergyTrigger.OnMeleeFinish,
                SynergyKind.HealFinisher,     10f,  "crimson_combo"),
            new("S05", "medium",            "dragoon",      SynergyTrigger.OnSwitch,
                SynergyKind.FirePillarCircuit, 10f, "curse_inferno"),
            new("S06", "guru",              "amazon",       SynergyTrigger.OnDash,
                SynergyKind.ArmoredDash,       8f,  "guarded_rush"),
            new("S07", "white_wizard",      "ninja",        SynergyTrigger.OnSwitch,
                SynergyKind.FrozenBlinkChain,  8f,  "arcane_execution"),
            new("S03", "robot",             "dragoon",      SynergyTrigger.OnSwitch,
                SynergyKind.NapalmTurret,     12f,  "fire_firmware"),
            new("S08", "baseball",          "robot",        SynergyTrigger.OnSwitch,
                SynergyKind.BounceTurret,     10f,  "bank_shot"),
        };

        public static string NameOf(SynergyKind k) => k switch
        {
            SynergyKind.BlinkExecution    => "표식 처형",
            SynergyKind.FreezeRune        => "빙결 룬",
            SynergyKind.HealFinisher      => "흡혈 마무리",
            SynergyKind.FirePillarCircuit => "저주 화염",
            SynergyKind.ArmoredDash       => "수호 돌진",
            SynergyKind.FrozenBlinkChain  => "빙결 연쇄",
            SynergyKind.NapalmTurret      => "네이팜 터렛",
            SynergyKind.BounceTurret      => "도탄 터렛",
            _                             => "시너지",
        };
    }
}
