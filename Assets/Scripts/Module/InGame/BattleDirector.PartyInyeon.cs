using System.Collections.Generic;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 인연 (PD 2026-10-29 · 표 `Projects/AVSR/_exchange/plan_party_inyeon.md` + 스킬 개편으로 고친 줄 `plan_party_skills.md`).
    ///   · 호스트마다 인연 하나 — 그 호스트가 **메인일 때만** 켜지는 **액티브 변화**.
    ///   · 짝이 보조에 있으면 켜진다. 짝 수 R · SR 1명 · SSR 2명(둘 중 하나면 된다) — 바뀌는 내용은 같다.
    ///   · 지금 메인의 짝은 호스트 자리 출현이 약 2배(`PartyRandomHost`).
    ///   · 태그 시너지(스탯)와 겹치지 않는다 — 인연은 스킬만 바꾼다.
    /// 액티브가 실제로 어떻게 바뀌는지는 각 스킬 함수가 `InyeonOn(me)` 을 보고 정한다.
    ///
    /// 이름 · 설명은 언어팩 `inyeon.{키}.name` · `inyeon.{키}.desc` 로 번역한다(표의 한국어가 원문).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private sealed class Inyeon
        {
            public string Name;
            public string Desc;
            public string[] Partners;
        }

        private static readonly Dictionary<string, Inyeon> s_inyeon = new()
        {
            ["gangster"] = new() { Name = "청부 표식", Partners = new[] { "thug" }, Desc = "표식 붙은 적을 보조가 우선 노리고 보조 피해 +50%" },
            ["thug"] = new() { Name = "뒷골목 난사", Partners = new[] { "gangster" }, Desc = "난사 탄이 가까운 적 쪽으로 살짝 휜다" },
            ["amazon"] = new() { Name = "번개 도약", Partners = new[] { "ninja" }, Desc = "한 번 더 도약 · 착지마다 번개 고리" },
            ["amazon_elite"] = new() { Name = "불굴의 함성", Partners = new[] { "amazon", "baseball" }, Desc = "함성 범위 1.5배 · 기절 2.5초" },
            ["hopper"] = new() { Name = "쌍권총", Partners = new[] { "hopper_smg" }, Desc = "조준 사격이 6발" },
            ["hopper_smg"] = new() { Name = "백스텝 탄막", Partners = new[] { "hopper" }, Desc = "백스텝 연사가 적을 꿰뚫는다" },
            ["commando_mg"] = new() { Name = "엄호 화망", Partners = new[] { "commando_grenade" }, Desc = "제압 사격에 맞은 적 발밑에 수류탄" },
            ["commando_laser"] = new() { Name = "과부하", Partners = new[] { "robot" }, Desc = "충전 레이저가 두 줄" },
            ["commando_grenade"] = new() { Name = "융단 폭격", Partners = new[] { "commando_missile" }, Desc = "수류탄 투하가 한 바퀴 더" },
            ["commando_missile"] = new() { Name = "집속탄", Partners = new[] { "commando_grenade" }, Desc = "미사일 한 발마다 작은 폭발" },
            ["salamander"] = new() { Name = "불독", Partners = new[] { "dragoon" }, Desc = "중독된 적이 쓰러지면 불꽃으로 터진다" },
            ["dragoon"] = new() { Name = "업화", Partners = new[] { "salamander", "dragon_blue" }, Desc = "불바다가 1.5배 커지고 밟은 적이 둔해진다" },
            ["dragon_blue"] = new() { Name = "번개 사슬", Partners = new[] { "ninja_chain" }, Desc = "묶인 적은 낙뢰를 두 번 맞는다" },
            ["guru"] = new() { Name = "결계 강령", Partners = new[] { "medium" }, Desc = "묶인 적 곁에 해골 2기" },
            ["white_wizard"] = new() { Name = "빛과 얼음", Partners = new[] { "snowwoman" }, Desc = "끌어모은 적이 언 뒤 폭발(피해 +30%)" },
            ["medium"] = new() { Name = "쌍골렘", Partners = new[] { "guru", "white_wizard" }, Desc = "미니 골렘이 두 기" },
            ["ninja"] = new() { Name = "그림자 분신", Partners = new[] { "amazon" }, Desc = "베기 끝에 분신 하나가 4초 남아 싸운다" },
            ["ninja_chain"] = new() { Name = "뇌전 사슬", Partners = new[] { "dragon_blue" }, Desc = "끌어올 때 사슬에 번개가 흐른다" },
            ["robot"] = new() { Name = "레이저 포탑", Partners = new[] { "commando_laser" }, Desc = "포탑이 꿰뚫는 레이저를 쏜다" },
            ["baseball"] = new() { Name = "얼음 장타", Partners = new[] { "snowwoman", "amazon_elite" }, Desc = "라인 드라이브에 맞은 적이 언다" },
            ["snowwoman"] = new() { Name = "얼음 진", Partners = new[] { "white_wizard" }, Desc = "눈보라 범위 2배" },
            ["vampire"] = new() { Name = "피의 연회", Partners = new[] { "salamander", "medium" }, Desc = "박쥐가 두 배 · 중독된 적에게서 더 빨아 온다" },
        };

        /// <summary>지금 켜진 인연의 메인 키. 없으면 null.</summary>
        private string _inyeonKey;

        private static Inyeon InyeonOf(string key) => key != null && s_inyeon.TryGetValue(key, out var y) ? y : null;

        /// <summary>그 호스트의 인연 이름(없으면 null) — 화면이 묻는다.</summary>
        public static string InyeonName(string key)
        {
            var y = InyeonOf(key);
            // 번역은 언어팩 `inyeon.{키}.name`(5언어) — 표의 한국어가 원문
            return y == null ? null : Game.Module.Common.Localize.FromTable($"inyeon.{key}.name", y.Name);
        }

        public static string InyeonDesc(string key)
        {
            var y = InyeonOf(key);
            return y == null ? null : Game.Module.Common.Localize.FromTable($"inyeon.{key}.desc", y.Desc);
        }

        /// <summary>그 호스트의 짝 키들(없으면 빈 배열).</summary>
        public static IReadOnlyList<string> InyeonPartners(string key)
            => InyeonOf(key)?.Partners ?? System.Array.Empty<string>();

        /// <summary>이 몸이 지금 인연이 켜진 메인인가 — 스킬 함수가 이것으로 인연형을 고른다.</summary>
        private bool InyeonOn(Unit me) => PartyOn && me != null && me == _host && _inyeonKey == me.Key;

        public bool InyeonActive => _inyeonKey != null;
        public string InyeonActiveKey => _inyeonKey;

        /// <summary>지금 메인의 짝인가 — 출현 2배 · 빙의 가능 말풍선에 쓴다.</summary>
        private bool IsMainPartner(string key)
        {
            if (_host == null || key == null) return false;
            var y = InyeonOf(_host.Key);
            if (y == null) return false;
            for (int i = 0; i < y.Partners.Length; i++) if (y.Partners[i] == key) return true;
            return false;
        }

        /// <summary>보조 중 메인의 짝이 있으면 인연을 켠다. 켜고 꺼지는 순간을 알린다(연출 · 화면).</summary>
        private void RecountInyeon()
        {
            string on = null;
            if (PartyOn && _host != null && InyeonOf(_host.Key) != null)
                for (int i = 0; i < _subs.Count; i++)
                    if (_subs[i].U != null && IsMainPartner(_subs[i].U.Key)) { on = _host.Key; break; }
            if (on == _inyeonKey) return;
            string was = _inyeonKey;
            _inyeonKey = on;
            OnInyeonChanged(on != null, on ?? was);
        }

        /// <summary>인연이 켜지거나 꺼졌다. 연출은 `BattleDirector.PartyFx` 가 붙인다.</summary>
        private void OnInyeonChanged(bool on, string mainKey)
        {
            UnityEngine.Debug.Log($"[빙의 파티] 인연 {(on ? "켜짐" : "꺼짐")} — {HostName(mainKey)} 「{InyeonName(mainKey)}」");
            PlayInyeonFx(on);
        }

        /// <summary>
        /// 빙의 가능한 적 머리 위에 띄울 한 줄(PD 「시너지에 포함되는지 알려 줘」).
        /// 인연이 먼저 — 데려오면 인연이 켜지면 「인연 · 업화」, 아니면 새로 켜질 태그 시너지, 둘 다 없으면 빈 줄(「시너지 없음」).
        /// </summary>
        public string PartyHintFor(string enemyKey, out bool inyeon)
        {
            inyeon = false;
            if (!PartyOn || _host == null || enemyKey == null) return null;
            if (_inyeonKey == null && IsMainPartner(enemyKey))
            {
                inyeon = true;
                return Tf("ui.party.inyeon.title", InyeonName(_host.Key));
            }
            return NewSynergyIfAdded(enemyKey);
        }

        /// <summary>이 호스트를 보조로 더하면 새로 켜지는 태그 시너지 한 줄(없으면 null).</summary>
        private string NewSynergyIfAdded(string key)
        {
            _hintKeys.Clear();
            foreach (var k in Unit.PartyKeys) _hintKeys.Add(k);
            _hintKeys.Add(key);
            var probe = default(PartySynergy);
            CountSynergy(_hintKeys, ref probe, _hintLines);
            for (int i = 0; i < _hintLines.Count; i++)
                if (!_synLines.Contains(_hintLines[i])) return _hintLines[i];
            return null;
        }

        // 말풍선 계산용 재사용 버퍼 — 적마다 · 프레임마다 새로 만들지 않는다
        private readonly HashSet<string> _hintKeys = new();
        private readonly List<string> _hintLines = new();
    }
}
