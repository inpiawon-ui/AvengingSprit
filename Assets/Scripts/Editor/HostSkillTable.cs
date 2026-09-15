using System.Collections.Generic;

namespace Game.EditorTools
{
    /// <summary>
    /// 호스트 23명의 **액티브 스킬 성장 두 축**과 쿨다운.
    ///
    /// 단일 출처는 `_exchange/out/42_jobs/AVSR_HostSkills.js` 이고, 이 파일은 그것을
    /// 옮겨 적은 것이다. 기획이 바뀌면 .js 를 받아 다시 옮긴다 —
    /// ⚠ **이 파일을 손으로 고치지 않는다.**
    ///
    /// 23명이 **전부 같은 모양**이다. 그래서 강화 화면이 호스트마다 다른 글을 쓸 필요가 없다.
    ///   base → baseMax   Lv1 → 4 에 자라는 축
    ///   Lv5              해제 하나. 값이 없는 불리언이다
    ///   spec → specMax   Lv6 → 10 에 자라는 축
    ///
    /// ⚠ **호스트별 분기를 만들지 않는다.** 문구는 "{축 이름} {시작} → {끝}" 하나로 찍힌다.
    ///   분기를 넣는 순간 이 구조를 버리는 것이다.
    ///
    /// ⚠ 사거리·반경·각도·판정 폭은 **레벨과 무관한 고정값**이라 여기 없다.
    ///   자라는 것은 위 두 축뿐이다. 공간값이 자라면 방 설계가 통째로 흔들린다.
    ///   (문서에 명시된 예외 둘 — 사신 Lv5 반경 계단, 닌자(사슬) 견인 반경 — 은
    ///    스킬 구현 쪽에서 따로 다룬다.)
    /// </summary>
    public static class HostSkillTable
    {
        public sealed class Entry
        {
            public string HostKey;
            public string NameKr;
            public string SkillNameKr;
            public float Cooldown;     // 8 · 14 · 20 · 28 네 티어뿐이다
            public float Base, BaseMax, Spec, SpecMax;
            public bool Confirmed;     // 플레이로 확인했는가. 나머지는 책상 초안이다
        }

        public static readonly Entry[] All =
        {
            new() { HostKey = "amazon", NameKr = "아마존", SkillNameKr = "돌풍 돌진",
                     Cooldown = 8f, Base = 1.5f, BaseMax = 2.2f, Spec = 1f, SpecMax = 2f,
                     Confirmed = true },
            new() { HostKey = "amazon_elite", NameKr = "아마존 정예", SkillNameKr = "불굴",
                     Cooldown = 28f, Base = 2.5f, BaseMax = 4f, Spec = 1f, SpecMax = 2f,
                     Confirmed = false },
            new() { HostKey = "baseball", NameKr = "슬러거", SkillNameKr = "전탄 반사",
                     Cooldown = 14f, Base = 5f, BaseMax = 7f, Spec = 2f, SpecMax = 4f,   // 반사 +1초 (2026-09-15)
                     Confirmed = false },
            new() { HostKey = "death", NameKr = "사신", SkillNameKr = "영혼 수확",
                     Cooldown = 28f, Base = 5f, BaseMax = 7.5f, Spec = 0f, SpecMax = 0.6f,
                     Confirmed = false },
            new() { HostKey = "guru", NameKr = "구루", SkillNameKr = "수호 결계",
                     Cooldown = 20f, Base = 0.5f, BaseMax = 0.7f, Spec = 5f, SpecMax = 8f,
                     Confirmed = false },
            new() { HostKey = "ninja_chain", NameKr = "닌자(사슬)", SkillNameKr = "사슬 견인",
                     Cooldown = 14f, Base = 5f, BaseMax = 7f, Spec = 0.5f, SpecMax = 1.2f,
                     Confirmed = false },
            new() { HostKey = "dragoon", NameKr = "드라군", SkillNameKr = "처형의 숨결",
                     Cooldown = 20f, Base = 3.5f, BaseMax = 5f, Spec = 0.3f, SpecMax = 0.45f,
                     Confirmed = false },
            new() { HostKey = "salamander", NameKr = "샐러맨더", SkillNameKr = "용암 지대",
                     Cooldown = 14f, Base = 0.5f, BaseMax = 0.9f, Spec = 6f, SpecMax = 10f,
                     Confirmed = false },
            new() { HostKey = "dragon_blue", NameKr = "청룡", SkillNameKr = "냉기 브레스",
                     Cooldown = 14f, Base = 1f, BaseMax = 1.6f, Spec = 3f, SpecMax = 6f,
                     Confirmed = false },
            new() { HostKey = "commando_grenade", NameKr = "코만도(수류탄)", SkillNameKr = "융단 폭격",
                     Cooldown = 20f, Base = 1.2f, BaseMax = 1.8f, Spec = 5f, SpecMax = 8f,
                     Confirmed = false },
            // 얼음 감옥 지속 — 2.5~3.8초가 길었다, **반으로**(기획 2026-09-15)
            new() { HostKey = "snowwoman", NameKr = "설녀", SkillNameKr = "빙결 파쇄",
                     Cooldown = 14f, Base = 1.25f, BaseMax = 1.9f, Spec = 1f, SpecMax = 3f,
                     Confirmed = false },
            new() { HostKey = "thug", NameKr = "폭력배", SkillNameKr = "난사",
                     Cooldown = 8f, Base = 3f, BaseMax = 4.5f, Spec = 3f, SpecMax = 7f,
                     Confirmed = false },
            new() { HostKey = "hopper_smg", NameKr = "호퍼(기관단총)", SkillNameKr = "도약 연사",
                     Cooldown = 14f, Base = 2.5f, BaseMax = 4f, Spec = 3f, SpecMax = 5f,
                     Confirmed = false },
            new() { HostKey = "ninja", NameKr = "닌자(표창)", SkillNameKr = "그림자 분신",
                     Cooldown = 20f, Base = 0.5f, BaseMax = 0.8f, Spec = 4f, SpecMax = 8f,
                     Confirmed = false },
            new() { HostKey = "vampire", NameKr = "흡혈귀", SkillNameKr = "혈갈",
                     Cooldown = 20f, Base = 2f, BaseMax = 3f, Spec = 5f, SpecMax = 8f,
                     Confirmed = false },
            new() { HostKey = "commando_mg", NameKr = "코만도(기관총)", SkillNameKr = "오버히트",
                     Cooldown = 14f, Base = 0.5f, BaseMax = 0.65f, Spec = 5f, SpecMax = 8f,
                     Confirmed = false },
            new() { HostKey = "gangster", NameKr = "갱스터", SkillNameKr = "표식 사격",
                     Cooldown = 14f, Base = 6f, BaseMax = 10f, Spec = 0.3f, SpecMax = 0.6f,
                     Confirmed = false },
            new() { HostKey = "hopper", NameKr = "호퍼", SkillNameKr = "도약 강타",
                     Cooldown = 14f, Base = 3f, BaseMax = 4.5f, Spec = 0.4f, SpecMax = 1f,
                     Confirmed = false },
            new() { HostKey = "commando_missile", NameKr = "코만도(미사일)", SkillNameKr = "다중 유도",
                     Cooldown = 20f, Base = 1f, BaseMax = 1.5f, Spec = 5f, SpecMax = 8f,
                     Confirmed = false },
            new() { HostKey = "medium", NameKr = "영매", SkillNameKr = "저주 전파",
                     Cooldown = 14f, Base = 0.2f, BaseMax = 0.35f, Spec = 1f, SpecMax = 2.5f,
                     Confirmed = false },
            new() { HostKey = "white_wizard", NameKr = "화이트 위저드", SkillNameKr = "광휘 파열",
                     Cooldown = 20f, Base = 2.5f, BaseMax = 3.8f, Spec = 0f, SpecMax = 0.8f,
                     Confirmed = false },
            new() { HostKey = "commando_laser", NameKr = "코만도(레이저)", SkillNameKr = "광폭 레이저",
                     Cooldown = 20f, Base = 4f, BaseMax = 6f, Spec = 3f, SpecMax = 5f,
                     Confirmed = false },
            new() { HostKey = "robot", NameKr = "로봇", SkillNameKr = "포탑 전개",
                     Cooldown = 20f, Base = 0.6f, BaseMax = 1f, Spec = 8f, SpecMax = 14f,
                     Confirmed = false },
        };

        private static Dictionary<string, Entry> s_index;

        public static Entry Get(string hostKey)
        {
            if (s_index == null)
            {
                s_index = new Dictionary<string, Entry>(All.Length);
                for (int i = 0; i < All.Length; i++) s_index[All[i].HostKey] = All[i];
            }
            return hostKey != null && s_index.TryGetValue(hostKey, out var e) ? e : null;
        }
    }
}
