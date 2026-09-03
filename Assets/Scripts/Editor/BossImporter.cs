using System;
using Game.Character;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// <see cref="BossDefTable"/> 의 보스 6종 · 패턴 24개를 `BossTable.asset` 에 굽는다.
    ///
    /// ── 무엇이 어긋나 있었나 ────────────────────────────────────
    /// 엔트리가 3개뿐이었고 `ForChapter(chapter)` 가 챕터당 하나만 돌려줘서
    /// **CH1 006 과 012 에 같은 보스가 섰다.** 방은 6개인데 보스는 3명이었다.
    /// 그림 배정도 정본과 어긋나 있었다 —
    ///   robot_snakes 가 CH1(정본 CH3) · crusher 가 CH2(정본 CH1) · python 이 CH3(정본 CH2).
    /// guardian · kingpin · sludge 는 그림 21장씩 들어와 있는데 아무 데서도 안 썼다.
    ///
    /// ⚠ 이 툴은 `_entries` 를 **통째로 다시 쓴다.** 손으로 고친 값이 있으면 날아간다 —
    ///   고칠 것이 있으면 `.js` 를 고쳐 다시 뽑는 것이 맞다.
    /// </summary>
    public static class BossImporter
    {
        private const string BossPath = "Assets/BundleResource/TableData/BossTable.asset";

        [MenuItem("Tools/Game/보스 임포트 (6종 · 패턴 24)")]
        public static void Import()
        {
            var table = AssetDatabase.LoadAssetAtPath<BossTable>(BossPath);
            if (table == null) { Debug.LogError($"[보스] {BossPath} 를 못 찾았다."); return; }

            var so = new SerializedObject(table);
            var arr = so.FindProperty("_entries");
            arr.arraySize = BossDefTable.All.Length;

            for (int i = 0; i < BossDefTable.All.Length; i++)
            {
                var d = BossDefTable.All[i];
                var e = arr.GetArrayElementAtIndex(i);

                Set(e, "_bossKey", d.Key);
                SetInt(e, "_chapter", d.Chapter);
                SetInt(e, "_roomNo", d.RoomNo);
                Set(e, "_nameEn", d.NameEn);
                Set(e, "_nameKr", d.NameKr);
                Set(e, "_spriteName", d.Sprite);
                SetInt(e, "_canonHp", d.Hp);
                SetInt(e, "_canonAtk", d.Atk);
                SetEnum(e, "_state", StateOf(d.State));
                var ch = e.FindPropertyRelative("_chases");
                if (ch != null) ch.boolValue = d.Chases;
                SetFloat(e, "_breakSeconds", d.BreakSeconds);
                Set(e, "_breakCause", d.BreakCause);

                var moves = e.FindPropertyRelative("_moves");
                moves.arraySize = d.Moves.Length;
                for (int k = 0; k < d.Moves.Length; k++)
                {
                    var m = d.Moves[k];
                    var mp = moves.GetArrayElementAtIndex(k);

                    SetInt(mp, "_fromPhase", m.Phase);
                    SetFloat(mp, "_cooldown", m.Cooldown);
                    SetFloat(mp, "_damageMul", m.DamageMul);
                    SetFloat(mp, "_telegraphSeconds", m.Telegraph);
                    Set(mp, "_nameKr", m.NameKr);
                    Set(mp, "_nameEn", m.NameEn);

                    SetEnum(mp, "_shape", ShapeOf(m.Shape));
                    SetEnum(mp, "_draw", DrawOf(m.Draw));
                    SetEnum(mp, "_dodge", DodgeOf(m.Dodge));

                    SetFloat(mp, "_degrees", m.Degrees);
                    SetFloat(mp, "_radiusMeters", m.Radius);
                    SetFloat(mp, "_widthMeters", m.Width);
                    SetFloat(mp, "_lengthMeters", m.Length);
                    SetFloat(mp, "_innerRadiusMeters", m.InnerRadius);
                    SetFloat(mp, "_gapDegrees", m.GapDegrees);
                    SetInt(mp, "_lanes", m.Lanes);
                    var safe = mp.FindPropertyRelative("_safeAtMeters");
                    if (safe != null) safe.vector2Value = new Vector2(m.SafeX, m.SafeY);

                    SetEnum(mp, "_range", RangeOf(m.Range));
                    SetFloat(mp, "_rangeMeters", m.RangeMeters);
                    SetInt(mp, "_group", m.Group);

                    // 옛 8패턴 칸도 채워 둔다 — 새 도형을 아직 안 그리는 자리가 이것을 읽는다.
                    SetEnum(mp, "_pattern", (int)LegacyOf(m.Draw));
                    SetInt(mp, "_shotCount", ShotsOf(m));
                    SetFloat(mp, "_spreadDegrees", m.Degrees);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            var sb = new System.Text.StringBuilder();
            sb.Append($"[보스] {BossDefTable.All.Length}종 · 패턴 ");
            int n = 0;
            foreach (var d in BossDefTable.All) n += d.Moves.Length;
            sb.AppendLine($"{n}개");
            foreach (var d in BossDefTable.All)
                sb.AppendLine($"  CH{d.Chapter} {d.RoomNo:000}  {d.NameKr}({d.Key}) "
                            + $"· {d.Gate} · HP {d.Hp} · 브레이크 {d.BreakSeconds}초"
                            + (string.IsNullOrEmpty(d.State) ? "" : $" · {d.State}"));
            Debug.Log(sb.ToString());
        }

        // ── 문자열 → 열거형 ─────────────────────────────────────
        //
        // ⚠ 못 알아본 이름은 **조용히 넘기지 않는다.** 조용히 0(첫 값)이 되면
        //   부채꼴이어야 할 것이 직선으로 돌아가는데 아무도 모른다.

        private static int ShapeOf(string s) => s switch
        {
            "Arc" => (int)BossShape.Arc,
            "Line" => (int)BossShape.Line,
            "Lane" => (int)BossShape.Lane,
            "Zone" => (int)BossShape.Zone,
            "Dash" => (int)BossShape.Dash,
            "Mark" => (int)BossShape.Mark,
            _ => throw new Exception($"[보스] 모르는 shape: {s}"),
        };

        /// <summary>
        /// <summary>거리 조건 이름 → <see cref="MoveRange"/>. 빈 값이면 조건 없음.</summary>
        private static int RangeOf(string s) => s switch
        {
            "NEAR" => (int)MoveRange.Near,
            "FAR"  => (int)MoveRange.Far,
            _      => (int)MoveRange.Any,
        };

        /// 패턴 이름 → <see cref="BossDraw"/>.
        ///
        /// 표(`BossDefTable`)의 `Draw` 는 열거형 이름과 **글자까지 같게** 적는다.
        /// 그래서 손으로 24줄을 옮겨 적지 않는다 — 옮겨 적으면 한 줄이 틀려도 안 보인다.
        /// 못 알아본 이름은 던진다. 조용히 0(`None`)이 되면 예고가 통째로 안 그려진다.
        /// </summary>
        private static int DrawOf(string s)
            => Enum.TryParse<BossDraw>(s, out var d) && d != BossDraw.None
             ? (int)d
             : throw new Exception($"[보스] 모르는 draw: {s}");

        private static int DodgeOf(string s) => s switch
        {
            "BACK" => (int)DodgeHint.Back,
            "SIDE" => (int)DodgeHint.Side,
            "GAP" => (int)DodgeHint.Gap,
            "PERP" => (int)DodgeHint.Perp,
            "ZONE" => (int)DodgeHint.Zone,
            "CLOSE" => (int)DodgeHint.Close,
            "SWAP" => (int)DodgeHint.Swap,
            "HOLD" => (int)DodgeHint.Hold,
            _ => throw new Exception($"[보스] 모르는 dodge: {s}"),
        };

        private static int StateOf(string s)
            => Enum.TryParse<BossState>(s, out var v) ? (int)v : (int)BossState.None;

        /// <summary>
        /// 도형이 아직 안 붙은 패턴이 떨어질 옛 거동.
        ///
        /// `StrikeDanger` 가 도형으로 때리지 못했을 때만 여기로 온다.
        /// 24개 전부 도형을 갖게 되면 이 표는 지운다 — 그때까지의 대역이다.
        /// </summary>
        private static BossPattern LegacyOf(string draw) => draw switch
        {
            "Crush" or "SegmentThrust" or "HeadBite" or "StrafingRun" or "WallBurst"
                => BossPattern.Charge,
            "HatchOpen" or "RailLaser" or "BodyCross" or "Conveyor"
                => BossPattern.PopupLaser,
            "VenomCloud" or "Spit" or "CeilingSpread"
                => BossPattern.VenomCloud,
            "WreckingBall" or "CoilWall" or "BoosterDrop" or "Emerge" or "FullEmergence"
                => BossPattern.Ring,
            "TripleBurst" or "MissileSalvo" or "ExecutionLock" or "DebrisFall"
                or "CeilingCling" or "SegmentLaunch"
                => BossPattern.AimedBurst,
            "ShieldUp" => BossPattern.ShieldCycle,
            _ => BossPattern.Volley,
        };

        /// <summary>
        /// 옛 거동이 쏠 발수. `Lanes` 가 곧 **도형 개수**이므로 그대로 쓴다 —
        /// 미사일 5발·파편 5개·그림자 3개가 전부 이 값이다.
        /// </summary>
        private static int ShotsOf(BossDefTable.Move m) => m.Lanes > 0 ? m.Lanes : 5;

        // ── SerializedProperty 잔손질 ────────────────────────────
        private static void Set(SerializedProperty p, string n, string v)
        { var x = p.FindPropertyRelative(n); if (x != null) x.stringValue = v ?? string.Empty; }

        private static void SetInt(SerializedProperty p, string n, int v)
        { var x = p.FindPropertyRelative(n); if (x != null) x.intValue = v; }

        private static void SetFloat(SerializedProperty p, string n, float v)
        { var x = p.FindPropertyRelative(n); if (x != null) x.floatValue = v; }

        private static void SetEnum(SerializedProperty p, string n, int v)
        { var x = p.FindPropertyRelative(n); if (x != null) x.enumValueIndex = v; }
    }
}
