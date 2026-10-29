using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 2단계 — 태그 시너지 · 파티 카드 · 파티 화면(PD 2026-10-29).
    ///
    ///   · 태그 = 속성(파워 · 무기 · 마법, `AffinityRule.KindOf`) + 조직(아래 표). 같은 태그 2명 = 약 · 3명 = 강 · 스탯만 준다
    ///   · 레벨업 3장 안에 파티 카드가 랜덤으로 섞인다(따로 창을 두지 않는다 — PD 「3장 나오는 데서 메인과 파티가 랜덤으로」)
    ///   · 화면: 위에 파티 초상화 M · 1 · 2(길게 누르면 메인과 자리 바꾸기) · 켜진 시너지 · 레시피(글자만, 실루엣 없음 — PD)
    ///
    /// ⚠ 초상화 · 시너지 · 레시피 판은 코드로 세운 임시 화면이다 — 시안이 나오면 발주해 갈아 끼운다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ─────────────────────────────────────────
        // 조직 태그 — 원작 설정 기준으로 다섯 묶음(시험값)
        // ─────────────────────────────────────────
        private static string OrgOf(string key) => key switch
        {
            "gangster" or "thug" or "hopper" or "hopper_smg" => "갱단",
            "commando_mg" or "commando_grenade" or "commando_missile" or "commando_laser" or "robot" => "코만도 부대",
            "amazon" or "amazon_elite" or "ninja" or "ninja_chain" or "baseball" => "무도가",
            "salamander" or "dragoon" or "dragon_blue" or "vampire" => "괴물",
            "guru" or "medium" or "snowwoman" or "white_wizard" or "death" => "마도",
            _ => null,
        };

        /// <summary>
        /// 태그(속성 · 조직의 한국어 내부 이름)의 언어팩 id — 화면에 보일 때만 `ui.party.tag.{id}` 로 번역한다.
        /// 내부 이름은 사전 키 · switch 로 쓰이므로 바꾸지 않는다.
        /// </summary>
        private static string TagId(string tag) => tag switch
        {
            "파워" => "force", "무기" => "blade", "마법" => "magic",
            "갱단" => "gang", "코만도 부대" => "commando", "무도가" => "martial",
            "괴물" => "monster", "마도" => "mystic", _ => null,
        };

        private static string AttrName(Affinity a) => a switch
        {
            Affinity.Force => "파워", Affinity.Blade => "무기", Affinity.Magic => "마법", _ => null,
        };

        /// <summary>
        /// 태그 시너지는 **스탯만** 준다(PD 2026-10-29 「속성은 공속 · 회피 · 치명 같은 스탯, 캐릭터별은 스킬」).
        /// 피해 % · 균형 · 보조 위력은 뺐다 — 스킬이 바뀌는 쪽은 인연이 맡는다(`plan_party_inyeon.md`).
        /// 값은 메인에 100% 걸린다. 보조는 맞지 않으므로(무적) 공속만 절반 받는다.
        /// </summary>
        private struct PartySynergy
        {
            /// <summary>받는 피해 감소(0~1) — 파워.</summary>
            public float Def;
            /// <summary>공속 증가(0~1) — 무기.</summary>
            public float AtkSpd;
            /// <summary>스킬 쿨 감소(0~1) — 마법.</summary>
            public float Cd;
            /// <summary>치명(%p) — 갱단.</summary>
            public float Crit;
            /// <summary>최대 체력 증가(0~1) — 코만도.</summary>
            public float Hp;
            /// <summary>회피(%) — 무도가.</summary>
            public int Dodge;
            /// <summary>처치마다 최대 체력의 이만큼 회복(0~1) — 괴물.</summary>
            public float KillHeal;
            /// <summary>보조 스킬 간격 감소(0~1) — 마도.</summary>
            public float SubCd;
        }

        private PartySynergy _syn;
        private readonly List<string> _synLines = new();
        private readonly Dictionary<string, int> _tagCount = new();

        /// <summary>지금 파티(메인 · 보조)로 시너지를 다시 센다. 매 프레임 `SyncPartyKeys` 뒤에 부른다.</summary>
        private void RecountSynergy()
        {
            CountSynergy(Unit.PartyKeys, ref _syn, _synLines);
            ApplySynergyHp();
        }

        /// <summary>
        /// 태그 시너지를 센다 — 값과 줄만 만들고 몸에는 아무것도 걸지 않는다.
        /// 빙의 가능 말풍선이 「데려오면 무엇이 켜지나」를 물을 때도 이것을 쓴다(체력이 흔들리면 안 된다).
        /// </summary>
        private void CountSynergy(HashSet<string> keys, ref PartySynergy syn, List<string> lines)
        {
            syn = default;
            lines.Clear();
            if (PartyOn && keys.Count >= 2)
            {
                // 시너지는 무리(조직)만 센다 — 파워 · 무기 · 마법은 상성(전투 유불리)에만 쓴다(PD 2026-10-29 「무리만으로 잘 묶어」)
                _tagCount.Clear();
                foreach (var k in keys)
                {
                    var o = OrgOf(k);
                    if (o != null) _tagCount[o] = _tagCount.TryGetValue(o, out int n) ? n + 1 : 1;
                }

                foreach (var kv in _tagCount)
                {
                    if (kv.Value < 2) continue;
                    bool st = kv.Value >= 3;
                    int lv = st ? 3 : 2;
                    // 세기는 SynTierPct 한 표(설명 창과 같은 자) — 갱단 2명 치명 +10 · 3명 +15(PD 「이런 식 좋았다」)
                    int pct = SynTierPct(kv.Key, st);
                    switch (kv.Key)
                    {
                        case "갱단": syn.Crit += pct; lines.Add(Tf("ui.party.syn.gang", lv, pct)); break;
                        case "코만도 부대": syn.Hp += pct / 100f; lines.Add(Tf("ui.party.syn.commando", lv, pct)); break;
                        case "무도가": syn.Dodge += pct; lines.Add(Tf("ui.party.syn.martial", lv, pct)); break;
                        case "괴물": syn.KillHeal += pct / 100f; lines.Add(Tf("ui.party.syn.monster", lv, pct)); break;
                        case "마도": syn.SubCd += pct / 100f; lines.Add(Tf("ui.party.syn.mystic", lv, pct)); break;
                    }
                }
            }
        }

        // ── 코만도 — 최대 체력 ─────────────────────────────────
        // 체력은 유닛 값이라 곱셈 자리가 없다. 걸어 둔 배수를 기억해 두고, 배수가 바뀌면 원래 최대치로 되돌려 다시 곱한다.
        // 늘 때는 늘어난 만큼 채워 준다(「켜졌다」가 바로 보이게), 줄 때는 비율을 넘는 몫만 깎인다.
        private Unit _synHpHost;
        private float _synHpApplied = 1f;

        private void ApplySynergyHp()
        {
            if (_host != _synHpHost) { _synHpHost = _host; _synHpApplied = 1f; }
            if (_host == null) return;
            float want = 1f + _syn.Hp;
            if (Mathf.Approximately(want, _synHpApplied)) return;
            int baseMax = Mathf.Max(1, Mathf.RoundToInt(_host.HpMax / _synHpApplied));
            int oldMax = _host.HpMax;
            int newMax = Mathf.Max(1, Mathf.RoundToInt(baseMax * want));
            _host.SetHpMax(newMax);
            if (newMax > oldMax) _host.Heal(newMax - oldMax);
            _synHpApplied = want;
        }

        // 계산 자리에서 부르는 배수 — 파티가 아니면 전부 1(또는 0)
        private float PartyCritAdd => PartyOn ? _syn.Crit : 0f;
        private float PartyDefMul => PartyOn ? Mathf.Max(0.3f, 1f - _syn.Def) : 1f;
        private float PartyCdMul => PartyOn ? Mathf.Max(0.3f, 1f - _syn.Cd) : 1f;
        /// <summary>공격 간격 배수 — 공속 +25% 면 간격은 1/1.25.</summary>
        private float PartyIntervalMul => PartyOn ? 1f / (1f + _syn.AtkSpd) : 1f;
        /// <summary>보조는 공속을 절반만 받는다.</summary>
        private float PartySubIntervalMul => PartyOn ? 1f / (1f + _syn.AtkSpd * 0.5f) : 1f;

        /// <summary>무도가 회피 — 맞을 피해가 없던 일이 된다.</summary>
        private bool PartyDodged() => PartyOn && _syn.Dodge > 0 && Roll(_syn.Dodge);

        /// <summary>괴물 처치 회복 — 적이 쓰러질 때 메인을 채운다.</summary>
        private void PartyKillHeal()
        {
            if (!PartyOn || _syn.KillHeal <= 0f || _host == null) return;
            _host.Heal(Mathf.Max(1, Mathf.RoundToInt(_host.HpMax * _syn.KillHeal)));
        }

        // ─────────────────────────────────────────
        // 파티 카드 — 레벨업 3장에 랜덤으로 섞인다
        // ─────────────────────────────────────────
        private readonly Dictionary<string, int> _subLv = new();
        private float _subPowCard;

        /// <summary>보조 한 명의 공격력 배수 = 30% × (1 + 카드) × 스킬 Lv(+25%/Lv).</summary>
        private float SubPowerOf(string key)
            => PartyRule.SubPower * (1f + _subPowCard) * (1f + 0.25f * (_subLv.TryGetValue(key, out int lv) ? lv : 0));

        /// <summary>보조 스킬 간격 — 6초 × 마도 시너지 × 스킬 Lv(−8%/Lv).</summary>
        private float SubSkillCdOf(string key)
            => PartyRule.SubSkillSeconds * (1f - _syn.SubCd) * Mathf.Max(0.5f, 1f - 0.08f * (_subLv.TryGetValue(key, out int lv) ? lv : 0));

        /// <summary>카드 3장 중 일부를 파티 카드로 바꾼다(보조가 있을 때만, 한 번에 최대 2장).</summary>
        private void MixPartyCards(string[] keys)
        {
            if (!PartyOn || _host == null || _subs.Count == 0 || keys == null) return;
            _partyCandidates.Clear();
            for (int i = 0; i < _subs.Count; i++) _partyCandidates.Add($"party.sublv.{i}");
            _partyCandidates.Add("party.subpow");
            if (_ghostHp < 80) _partyCandidates.Add("party.ghost");
            int placed = 0;
            for (int i = 0; i < keys.Length && placed < 2 && _partyCandidates.Count > 0; i++)
            {
                if (_rng.NextDouble() > 0.35) continue;
                int pick = _rng.Next(_partyCandidates.Count);
                keys[i] = _partyCandidates[pick];
                _partyCandidates.RemoveAt(pick);
                placed++;
            }
        }

        private readonly List<string> _partyCandidates = new();

        public static bool IsPartyCard(string key) => key != null && key.StartsWith("party.");

        /// <summary>화면이 파티 카드를 그릴 때 묻는다. 이름 · 설명 · 꼬리표 · 그림.</summary>
        public bool PartyCardInfo(string key, out string name, out string desc, out string chip, out Sprite icon)
        {
            name = desc = chip = string.Empty;
            icon = null;
            if (!IsPartyCard(key)) return false;
            chip = Tx("ui.party.card.chip");
            if (key.StartsWith("party.sublv."))
            {
                int slot = int.Parse(key.Substring("party.sublv.".Length));
                if (slot >= _subs.Count || _subs[slot].U == null) return false;
                string k = _subs[slot].U.Key;
                int lv = _subLv.TryGetValue(k, out int v) ? v : 0;
                name = Tf("ui.party.card.sublv.name", slot + 1, HostName(k), lv + 1, lv + 2);
                desc = Tx("ui.party.card.sublv.desc");
                icon = UnitGet(k, "s") ?? UnitGet(k);
                return true;
            }
            if (key == "party.subpow")
            {
                name = Tx("ui.party.card.subpow.name");
                desc = Tf("ui.party.card.subpow.desc", Mathf.RoundToInt(PartyRule.SubPower * (1f + _subPowCard) * 100),
                          Mathf.RoundToInt(PartyRule.SubPower * (1f + _subPowCard + 0.1f) * 100));
                icon = _subs.Count > 0 && _subs[0].U != null ? UnitGet(_subs[0].U.Key, "s") : null;
                return true;
            }
            if (key == "party.ghost")
            {
                name = Tx("ui.party.card.ghost.name");
                desc = Tf("ui.party.card.ghost.desc", _ghostHp, Mathf.Min(GhostHpMax, _ghostHp + 10));
                icon = UnitGet("ghost", "s") ?? UnitGet("ghost");
                return true;
            }
            return false;
        }

        /// <summary>파티 카드를 고른 뒤. ChooseBuff 가 맨 앞에서 넘긴다.</summary>
        private void ApplyPartyCard(string key)
        {
            if (key.StartsWith("party.sublv."))
            {
                int slot = int.Parse(key.Substring("party.sublv.".Length));
                if (slot < _subs.Count && _subs[slot].U != null)
                {
                    string k = _subs[slot].U.Key;
                    _subLv[k] = (_subLv.TryGetValue(k, out int v) ? v : 0) + 1;
                }
            }
            else if (key == "party.subpow") _subPowCard += 0.1f;
            else if (key == "party.ghost") { _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + 10); PublishHp(); }
            RefreshSubAtk();
        }

        /// <summary>보조 공격력을 지금 배수로 다시 맞춘다(카드 · 시너지가 바뀔 때).</summary>
        private void RefreshSubAtk()
        {
            for (int i = 0; i < _subs.Count; i++)
            {
                var s = _subs[i];
                if (s.U == null || s.BaseAtk <= 0) continue;
                int want = Mathf.Max(1, Mathf.RoundToInt(s.BaseAtk * SubPowerOf(s.U.Key)));
                if (want != s.U.Atk) s.U.ScaleAttack(want / (float)s.U.Atk);
            }
        }

        // ─────────────────────────────────────────
        // 자리 바꾸기 — 초상화를 길게 누르면 메인과 보조가 서로 바뀐다(−25, 메인 교체 횟수)
        // ─────────────────────────────────────────
        private void OpenPartyPortraitSwap(int slot)
        {
            if (!PartyOn || _host == null || slot >= _subs.Count || _partyHold || IsChanneling) return;
            int left = PartyRule.MainSwapMax - _mainSwaps;
            string sub = HostName(_subs[slot].U.Key), main = HostName(_host.Key);
            var opts = new List<PartyOption>
            {
                new PartyOption(Tf("ui.party.choice.main_swap", PartyRule.CostMainSwap, left),
                                Tf("ui.party.swap.desc", sub, main, slot + 1),
                                left > 0 && _ghostHp > PartyRule.CostMainSwap, () => PartySwapWithSub(slot)),
                new PartyOption(Tx("ui.party.cancel"), string.Empty, true, null),
            };
            ShowPartyPopup(Tf("ui.party.swap.title", slot + 1, sub), Tf("ui.party.ghost_left", _ghostHp), opts, -1f);
        }

        private void PartySwapWithSub(int slot)
        {
            if (_host == null || slot >= _subs.Count) return;
            var s = _subs[slot];
            string subKey = s.U.Key, mainKey = _host.Key;
            var subAt = s.U.Position;
            var mainAt = _host.Position;
            PartySpend(PartyRule.CostMainSwap, mainAt);
            _mainSwaps++;
            Destroy(s.U.gameObject);
            _subs[slot] = MakeSub(mainKey, subAt);
            var old = _host;
            _host = null;
            Destroy(old.gameObject);   // 쓰러진 것이 아니라 자리를 바꾼 것 — 사망 처리 · 조각은 없다
            var entry = _player != null ? _player.GetHost(subKey) : null;
            EnterHost(entry, subKey, subKey, mainAt, 100);
            RefreshSubAtk();
        }

    }
}
