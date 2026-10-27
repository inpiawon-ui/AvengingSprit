using System.Collections.Generic;
using Game.Character;
using Game.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 레벨업 3택1 — 시안 Screens/levelup(Core/ui.css .choice).
    /// 카드를 누르면 바로 적용한다(PD 2026-10-27 「투스텝은 필요 없어」 — 시안의 「골라서 확정」 버튼은 뺐다).
    /// 카드 아래 초록 줄은 이번에 받을 수치(새 카드 「경험치 +15%」 · 가진 카드 「튕김 횟수 2 → 3」).
    /// </summary>
    public sealed partial class InGameMainUI
    {
        // 시안 색 — 왼쪽 띠(.choice inset 8px) · 등급 글자(.small gold · cyan · muted). 전설은 시안에 없어 금빛 몫(선택 빛 #C29B3F)으로 둔다
        private static readonly Color RbStripCommon = new Color32(140, 149, 166, 255);
        private static readonly Color RbStripRare = new Color32(79, 128, 177, 255);
        private static readonly Color RbStripEpic = new Color32(121, 94, 156, 255);
        private static readonly Color RbStripTop = new Color32(194, 155, 63, 255);
        private static readonly Color RbTextMuted = new Color32(171, 178, 206, 255);
        private static readonly Color RbTextRare = new Color32(84, 220, 235, 255);
        private static readonly Color RbTextEpic = new Color32(239, 198, 103, 255);
        private static readonly Color RbTextTop = new Color32(255, 201, 61, 255);
        // 꼬리표 좌우 여백 · 오른쪽 끝(카드 안 1000 px — 시안)
        private const float RbTagPad = 10f * 2f / 3f, RbTagRight = 1000f * 2f / 3f;

        private readonly float[] _rbCardHomeY = new float[3];
        private bool _rbHomeKnown;

        private static Color RbStripColor(CardRarity r) => r switch
        {
            CardRarity.Rare => RbStripRare,
            CardRarity.Epic => RbStripEpic,
            CardRarity.Legendary => RbStripTop,
            _ => RbStripCommon,
        };

        private static Color RbRarityText(CardRarity r) => r switch
        {
            CardRarity.Rare => RbTextRare,
            CardRarity.Epic => RbTextEpic,
            CardRarity.Legendary => RbTextTop,
            _ => RbTextMuted,
        };

        /// <summary>창을 열 때 — 카드 제자리(빌더가 놓은 y)를 한 번 기억하고 되돌린다. 떠오름 · 포커스 연출이 옮겨 둔 것을 지운다.</summary>
        private void RbCardsHome()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_ui.Find($"BuffCard{i}") is not RectTransform rt) continue;
                if (!_rbHomeKnown) _rbCardHomeY[i] = rt.anchoredPosition.y;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _rbCardHomeY[i]);
            }
            _rbHomeKnown = true;
        }

        /// <summary>카드 한 장의 RE:BORN 칸 — 효과 줄 · 꼬리표 폭.</summary>
        private void RbFillCard(int i, BuffEntry entry, int level)
        {
            string delta = CardDelta(entry, level);
            _ui.SetActive($"BuffCard{i}Delta", !string.IsNullOrEmpty(delta));
            _ui.SetText($"BuffCard{i}Delta", delta);
            RbFitTag(i);
        }

        // 꼬리표는 글 폭 + 좌우 10 px(시안 padding 6 10) · 오른쪽 끝을 카드 안 1000 px 에 맞춘다
        private void RbFitTag(int i)
        {
            if (_ui.Find($"BuffCard{i}Chip") is not RectTransform chip) return;
            if (_ui.Get<TMP_Text>($"BuffCard{i}ChipText") is not TMP_Text t) return;
            float w = t.GetPreferredValues(t.text, 9999f, chip.sizeDelta.y).x + RbTagPad * 2f;
            chip.sizeDelta = new Vector2(w, chip.sizeDelta.y);
            chip.anchoredPosition = new Vector2(RbTagRight - w, chip.anchoredPosition.y);
        }

        // ── 효과 줄 ────────────────────────────────────────────

        // 수치를 어떻게 읽나 — 표의 값(ValueAt)이 무엇인지는 RunBuffs.Recompute 가 정한다(그 쓰임과 같게 적는다)
        private enum DeltaUnit { Percent, PercentDown, Count, Level, Tenths, Seconds }

        private static readonly Dictionary<BuffKind, (string Label, DeltaUnit Unit)> CardStats = new()
        {
            { BuffKind.Attack, ("공격력", DeltaUnit.Percent) },
            { BuffKind.AttackSpeed, ("공격 속도", DeltaUnit.Percent) },
            { BuffKind.Range, ("사거리", DeltaUnit.Percent) },
            { BuffKind.MoveSpeed, ("이동 속도", DeltaUnit.Percent) },
            { BuffKind.ActiveSkillCharge, ("스킬 쿨타임", DeltaUnit.PercentDown) },
            { BuffKind.MultiShot, ("추가 탄", DeltaUnit.Level) },
            { BuffKind.Lifesteal, ("흡혈 확률", DeltaUnit.Percent) },
            { BuffKind.Slow, ("둔화", DeltaUnit.Percent) },
            { BuffKind.Pierce, ("관통", DeltaUnit.Count) },
            { BuffKind.DamageReduction, ("받는 피해", DeltaUnit.PercentDown) },
            { BuffKind.FieldDuration, ("장판 지속", DeltaUnit.Tenths) },
            { BuffKind.Ricochet, ("튕긴 탄 피해", DeltaUnit.Percent) },
            { BuffKind.AoeRadius, ("범위", DeltaUnit.Percent) },
            { BuffKind.Execute, ("마무리 피해", DeltaUnit.Percent) },
            { BuffKind.BossFocus, ("보스 피해", DeltaUnit.Percent) },
            { BuffKind.Regen, ("처치 회복", DeltaUnit.Percent) },
            { BuffKind.FrostImprint, ("빙결 확률", DeltaUnit.Percent) },
            { BuffKind.FlameImprint, ("화상 확률", DeltaUnit.Percent) },
            { BuffKind.CurseImprint, ("저주 확률", DeltaUnit.Percent) },
            { BuffKind.SustainStack, ("연타 피해", DeltaUnit.Percent) },
            { BuffKind.Homing, ("유도", DeltaUnit.Percent) },
            { BuffKind.SpectralEcho, ("복제 피해", DeltaUnit.Percent) },
            { BuffKind.ExplosiveEcho, ("2차 폭발 피해", DeltaUnit.Percent) },
            { BuffKind.GhostTurret, ("포대 피해", DeltaUnit.Percent) },
            { BuffKind.ArmorBreak, ("쌓이는 피해", DeltaUnit.Percent) },
            { BuffKind.StatusChain, ("번짐 확률", DeltaUnit.Percent) },
            { BuffKind.CrisisBarrier, ("방벽", DeltaUnit.Percent) },
            { BuffKind.Overcharge, ("감전 피해", DeltaUnit.Percent) },
            { BuffKind.ExpGain, ("경험치", DeltaUnit.Percent) },
            { BuffKind.OrbitShield, ("방패", DeltaUnit.Count) },
            { BuffKind.Assassinate, ("즉사 확률", DeltaUnit.Percent) },
            { BuffKind.ChainLightning, ("튕김 횟수", DeltaUnit.Count) },
            { BuffKind.GuardInvuln, ("재사용 대기", DeltaUnit.Seconds) },
            { BuffKind.LowHpPower, ("궁지 피해", DeltaUnit.Percent) },
        };

        /// <summary>
        /// 초록 효과 줄 — 새 카드는 1레벨 값(「경험치 +15%」), 가진 카드는 지금 → 다음(「튕김 횟수 2 → 3」).
        /// 수치가 없는 카드(화상 번짐 · 필살기 초기화 — 켜지면 끝)는 빈 줄.
        /// </summary>
        private static string CardDelta(BuffEntry entry, int level)
        {
            if (entry == null || !CardStats.TryGetValue(entry.Kind, out var stat)) return string.Empty;
            string label = Localize.FromTable($"ui.card.stat.{entry.Kind.ToString().ToLowerInvariant()}", stat.Label);
            if (level <= 0) return $"{label} {Amount(entry, 1, stat.Unit, true)}";
            int next = Mathf.Min(level + 1, entry.MaxLevel);
            if (next == level) return $"{label} {Amount(entry, level, stat.Unit, false)}";
            return $"{label} {Amount(entry, level, stat.Unit, false)} → {Amount(entry, next, stat.Unit, false)}";
        }

        private static string Amount(BuffEntry entry, int level, DeltaUnit unit, bool signed)
        {
            int v = unit == DeltaUnit.Level ? level : entry.ValueAt(level);
            return unit switch
            {
                DeltaUnit.Percent => (signed ? "+" : string.Empty) + v + "%",
                DeltaUnit.PercentDown => "-" + v + "%",
                DeltaUnit.Count or DeltaUnit.Level => (signed ? "+" : string.Empty) + v,
                DeltaUnit.Tenths => string.Format(Localize.FromTable("ui.card.unit.sec", "{0}초"), (signed ? "+" : string.Empty) + (v / 10f).ToString("0.#")),
                _ => string.Format(Localize.FromTable("ui.card.unit.sec", "{0}초"), v),
            };
        }
    }
}
