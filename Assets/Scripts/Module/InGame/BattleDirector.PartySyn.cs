using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 2단계 — 태그 시너지 · 파티 카드 · 파티 화면(PD 2026-10-29).
    ///
    ///   · 태그 = 속성(파워 · 무기 · 마법, `AffinityRule.KindOf`) + 조직(아래 표). 같은 태그 2명 = 약 · 3명 = 강, 속성 셋이 다 다르면 균형
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

        private static string AttrName(Affinity a) => a switch
        {
            Affinity.Force => "파워", Affinity.Blade => "무기", Affinity.Magic => "마법", _ => null,
        };

        private struct PartySynergy
        {
            public float Dmg, Crit, Def, Cd, SubPow;
        }

        private PartySynergy _syn;
        private readonly List<string> _synLines = new();
        private readonly Dictionary<string, int> _tagCount = new();

        /// <summary>지금 파티(메인 · 보조)로 시너지를 다시 센다. 매 프레임 `SyncPartyKeys` 뒤에 부른다.</summary>
        private void RecountSynergy()
        {
            _syn = default;
            _synLines.Clear();
            if (!PartyOn) return;
            var keys = Unit.PartyKeys;
            if (keys.Count < 2) return;

            _tagCount.Clear();
            int force = 0, blade = 0, magic = 0;
            foreach (var k in keys)
            {
                var a = AffinityRule.KindOf(k);
                if (a == Affinity.Force) force++; else if (a == Affinity.Blade) blade++; else if (a == Affinity.Magic) magic++;
                var o = OrgOf(k);
                if (o != null) _tagCount[o] = _tagCount.TryGetValue(o, out int n) ? n + 1 : 1;
            }

            AddAttr("파워", force); AddAttr("무기", blade); AddAttr("마법", magic);
            if (keys.Count >= 3 && force == 1 && blade == 1 && magic == 1)
            { _syn.Dmg += 0.12f; _syn.Def += 0.10f; _synLines.Add("균형 · 피해 +12% · 받는 피해 −10%"); }

            foreach (var kv in _tagCount)
            {
                if (kv.Value < 2) continue;
                bool strong = kv.Value >= 3;
                string lv = strong ? "3인" : "2인";
                switch (kv.Key)
                {
                    case "갱단": _syn.Crit += strong ? 20f : 10f; _synLines.Add($"갱단 {lv} · 치명 +{(strong ? 20 : 10)}%"); break;
                    case "코만도 부대": _syn.Def += strong ? 0.20f : 0.10f; _synLines.Add($"코만도 {lv} · 받는 피해 −{(strong ? 20 : 10)}%"); break;
                    case "무도가": _syn.Dmg += strong ? 0.20f : 0.10f; _synLines.Add($"무도가 {lv} · 피해 +{(strong ? 20 : 10)}%"); break;
                    case "괴물": _syn.SubPow += strong ? 0.20f : 0.10f; _synLines.Add($"괴물 {lv} · 보조 위력 +{(strong ? 20 : 10)}%"); break;
                    case "마도": _syn.Cd += strong ? 0.30f : 0.15f; _synLines.Add($"마도 {lv} · 스킬 쿨 −{(strong ? 30 : 15)}%"); break;
                }
            }
        }

        private void AddAttr(string name, int n)
        {
            if (n < 2) return;
            bool strong = n >= 3;
            _syn.Dmg += strong ? 0.25f : 0.10f;
            _synLines.Add($"{name} {n}인 · 피해 +{(strong ? 25 : 10)}%");
        }

        // 계산 자리에서 부르는 배수 — 파티가 아니면 전부 1(또는 0)
        private float PartyDamageMul => PartyOn ? 1f + _syn.Dmg : 1f;
        private float PartyCritAdd => PartyOn ? _syn.Crit : 0f;
        private float PartyDefMul => PartyOn ? Mathf.Max(0.3f, 1f - _syn.Def) : 1f;
        private float PartyCdMul => PartyOn ? Mathf.Max(0.3f, 1f - _syn.Cd) : 1f;

        // ─────────────────────────────────────────
        // 파티 카드 — 레벨업 3장에 랜덤으로 섞인다
        // ─────────────────────────────────────────
        private readonly Dictionary<string, int> _subLv = new();
        private float _subPowCard;

        /// <summary>보조 한 명의 공격력 배수 = 30% × (1 + 카드 + 괴물 시너지) × 스킬 Lv(+25%/Lv).</summary>
        private float SubPowerOf(string key)
            => PartyRule.SubPower * (1f + _subPowCard + _syn.SubPow) * (1f + 0.25f * (_subLv.TryGetValue(key, out int lv) ? lv : 0));

        /// <summary>보조 스킬 간격 — 6초 × 마도 시너지(보조는 절반만) × 스킬 Lv(−8%/Lv).</summary>
        private float SubSkillCdOf(string key)
            => PartyRule.SubSkillSeconds * (1f - _syn.Cd * 0.5f) * Mathf.Max(0.5f, 1f - 0.08f * (_subLv.TryGetValue(key, out int lv) ? lv : 0));

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
            chip = "파티";
            if (key.StartsWith("party.sublv."))
            {
                int slot = int.Parse(key.Substring("party.sublv.".Length));
                if (slot >= _subs.Count || _subs[slot].U == null) return false;
                string k = _subs[slot].U.Key;
                int lv = _subLv.TryGetValue(k, out int v) ? v : 0;
                name = $"보조 {slot + 1} 「{HostName(k)}」 스킬 Lv {lv + 1} → {lv + 2}";
                desc = "보조 스킬 · 평타 위력 +25% · 스킬 간격 −8%\n교체해도 이 호스트에 남는다";
                icon = UnitGet(k, "s") ?? UnitGet(k);
                return true;
            }
            if (key == "party.subpow")
            {
                name = "보조 위력 +10%";
                desc = $"보조 둘의 평타 · 스킬이 함께 세진다\n지금 {Mathf.RoundToInt(PartyRule.SubPower * (1f + _subPowCard) * 100)}% → {Mathf.RoundToInt(PartyRule.SubPower * (1f + _subPowCard + 0.1f) * 100)}%";
                icon = _subs.Count > 0 && _subs[0].U != null ? UnitGet(_subs[0].U.Key, "s") : null;
                return true;
            }
            if (key == "party.ghost")
            {
                name = "유령 HP +10";
                desc = $"바로 회복 · 지금 {_ghostHp} → {Mathf.Min(GhostHpMax, _ghostHp + 10)}";
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
                new PartyOption($"메인과 바꾸기 · 유령 HP −{PartyRule.CostMainSwap}  (남은 {left}회)",
                                $"{sub} 이(가) 메인 · {main} 은(는) 보조 {slot + 1} 로 · 새 메인은 체력 가득",
                                left > 0 && _ghostHp > PartyRule.CostMainSwap, () => PartySwapWithSub(slot)),
                new PartyOption("그만두기", string.Empty, true, null),
            };
            ShowPartyPopup($"보조 {slot + 1} 「{sub}」 을(를) 메인으로?", $"남은 유령 HP {_ghostHp}", opts, -1f);
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

        // ─────────────────────────────────────────
        // 파티 화면 — 초상화 M · 1 · 2 · 시너지 · 레시피 (임시 · 코드로 세움)
        // ─────────────────────────────────────────
        private RectTransform _partyHud;
        private readonly Image[] _hudFace = new Image[3];
        private readonly Image[] _hudFrame = new Image[3];
        private readonly TextMeshProUGUI[] _hudName = new TextMeshProUGUI[3];
        private TextMeshProUGUI _hudSyn, _hudRecipe;
        private string _hudSig;

        /// <summary>화면 위쪽 HUD 바로 아래에 둔다(기준 해상도 720 × 1280 의 위에서 이만큼).</summary>
        private const float PartyHudTop = 214f;

        private void EnsurePartyHud()
        {
            if (_partyHud != null || !PartyOn) return;
            var canvas = _unitLayer != null ? _unitLayer.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return;
            var root = canvas.rootCanvas.transform;

            var go = new GameObject("PartyHud", typeof(RectTransform));
            _partyHud = (RectTransform)go.transform;
            _partyHud.SetParent(root, false);
            _partyHud.anchorMin = _partyHud.anchorMax = new Vector2(0f, 1f);
            _partyHud.pivot = new Vector2(0f, 1f);
            _partyHud.anchoredPosition = new Vector2(10f, -PartyHudTop);
            _partyHud.sizeDelta = new Vector2(330f, 250f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.09f, 0.55f);
            bg.raycastTarget = false;

            string[] tags = { "M", "1", "2" };
            for (int i = 0; i < 3; i++)
            {
                var cell = NewRect($"PartyFace{i}", _partyHud, new Vector2(84f, 84f));
                cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0f, 1f);
                cell.anchoredPosition = new Vector2(10f + i * 104f, -10f);
                _hudFrame[i] = cell.gameObject.AddComponent<Image>();
                _hudFrame[i].color = new Color(0.12f, 0.12f, 0.17f, 0.95f);
                var ol = cell.gameObject.AddComponent<Outline>();
                ol.effectColor = i == 0 ? new Color(1f, 0.84f, 0.42f) : new Color(0.73f, 0.55f, 1f);
                ol.effectDistance = new Vector2(3f, -3f);

                var face = NewRect("Face", cell, new Vector2(76f, 76f));
                _hudFace[i] = face.gameObject.AddComponent<Image>();
                _hudFace[i].preserveAspect = true;
                _hudFace[i].raycastTarget = false;

                var tag = NewText(cell, tags[i], 20, i == 0 ? new Color(1f, 0.84f, 0.42f) : new Color(0.85f, 0.75f, 1f),
                                  new Vector2(30f, -30f), new Vector2(30f, 26f), true);
                tag.alignment = TextAlignmentOptions.Center;
                _hudName[i] = NewText(cell, string.Empty, 15, new Color(0.85f, 0.85f, 0.9f), new Vector2(0f, -56f), new Vector2(100f, 22f), false);

                if (i > 0)
                {
                    int slot = i - 1;
                    var trig = cell.gameObject.AddComponent<EventTrigger>();
                    float downAt = -1f;
                    AddPartyTrigger(trig, EventTriggerType.PointerDown, _ => downAt = Time.unscaledTime);
                    AddPartyTrigger(trig, EventTriggerType.PointerUp, _ =>
                    {
                        // 길게 눌러야 바뀐다 — 실수로 누를 일이 없게(기획서 10-3)
                        if (downAt >= 0f && Time.unscaledTime - downAt >= 0.5f) OpenPartyPortraitSwap(slot);
                        downAt = -1f;
                    });
                }
            }

            _hudSyn = NewText(_partyHud, string.Empty, 16, new Color(1f, 0.84f, 0.42f), new Vector2(165f, -150f), new Vector2(310f, 60f), false);
            _hudSyn.alignment = TextAlignmentOptions.TopLeft;
            ((RectTransform)_hudSyn.transform).anchorMin = ((RectTransform)_hudSyn.transform).anchorMax = new Vector2(0f, 1f);
            _hudRecipe = NewText(_partyHud, string.Empty, 15, new Color(0.78f, 0.78f, 0.85f), new Vector2(165f, -212f), new Vector2(310f, 62f), false);
            _hudRecipe.alignment = TextAlignmentOptions.TopLeft;
            ((RectTransform)_hudRecipe.transform).anchorMin = ((RectTransform)_hudRecipe.transform).anchorMax = new Vector2(0f, 1f);

            if (GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady) lang.ApplyFonts(_partyHud);
            _hudSig = null;
        }

        private static void AddPartyTrigger(EventTrigger t, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> fn)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(fn);
            t.triggers.Add(e);
        }

        /// <summary>파티가 바뀐 프레임에만 화면을 다시 채운다(매 프레임 글자를 새로 만들지 않는다).</summary>
        private void TickPartyHud()
        {
            if (!PartyOn) { if (_partyHud != null) _partyHud.gameObject.SetActive(false); return; }
            EnsurePartyHud();
            if (_partyHud == null) return;
            _partyHud.gameObject.SetActive(_running);

            string mk = _host != null ? _host.Key : "-";
            string s0 = _subs.Count > 0 && _subs[0].U != null ? _subs[0].U.Key : "-";
            string s1 = _subs.Count > 1 && _subs[1].U != null ? _subs[1].U.Key : "-";
            string sig = mk + "|" + s0 + "|" + s1 + "|" + _synLines.Count;
            if (sig == _hudSig) return;
            _hudSig = sig;

            SetFace(0, _host != null ? _host.Key : null);
            SetFace(1, s0 == "-" ? null : s0);
            SetFace(2, s1 == "-" ? null : s1);

            _hudSyn.text = _synLines.Count == 0 ? "시너지 없음 — 같은 속성 · 조직 2명부터" : string.Join("\n", _synLines);
            _hudRecipe.text = RecipeText();
        }

        private void SetFace(int i, string key)
        {
            var sp = key != null ? (UnitGet(key, "s") ?? UnitGet(key)) : null;
            _hudFace[i].sprite = sp;
            _hudFace[i].enabled = sp != null;
            _hudName[i].text = key != null ? HostName(key) : "비어 있음";
        }

        /// <summary>
        /// 레시피 — 지금 메인 기준으로 「무엇을 더 모으면 무엇이 켜지나」 를 글자로만(실루엣 없음 — PD).
        /// ● 켜짐 · ○ 아직. 괄호는 모은 수 / 필요한 수.
        /// </summary>
        private string RecipeText()
        {
            if (_host == null) return string.Empty;
            var keys = Unit.PartyKeys;
            string org = OrgOf(_host.Key);
            var attr = AffinityRule.KindOf(_host.Key);
            int nOrg = 0, nAttr = 0;
            var kinds = new HashSet<Affinity>();
            foreach (var k in keys)
            {
                if (org != null && OrgOf(k) == org) nOrg++;
                var a = AffinityRule.KindOf(k);
                if (a == attr) nAttr++;
                if (a != Affinity.None) kinds.Add(a);
            }
            var sb = new System.Text.StringBuilder("레시피 · 메인 " + HostName(_host.Key) + "\n");
            if (org != null) sb.Append($"{(nOrg >= 2 ? "●" : "○")} {org} ({nOrg}/{(nOrg >= 2 ? 3 : 2)})   ");
            if (AttrName(attr) != null) sb.Append($"{(nAttr >= 2 ? "●" : "○")} {AttrName(attr)} ({nAttr}/{(nAttr >= 2 ? 3 : 2)})   ");
            sb.Append($"{(kinds.Count >= 3 && keys.Count >= 3 ? "●" : "○")} 균형 ({kinds.Count}/3)");
            return sb.ToString();
        }
    }
}
