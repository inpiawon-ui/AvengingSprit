using UnityEngine;
using GameSound = Game.Module.Common.GameSound;

namespace Game.Module.InGame
{
    /// <summary>
    /// 시험판 — **사신의 수확** 규칙 (2026-10-07).
    ///
    /// 추가 모드 기획(`Projects/AVSR/AVSR_ExtraModes.html`)의 1순위를 개발 전에 손으로 느껴 보려고 만든 판이다.
    /// 화면 · 보상 · 기록은 만들지 않는다. 메인 판 위에서 규칙만 바꾼다.
    ///
    ///   · 보스를 뺀 **모든 적**에 경직이 쌓인다 — 짧게 몰아치면 찬다(빙의 표식과 같은 게이지).
    ///   · 차면 **혼이 드러난다.** 그동안은 맞지도 움직이지도 않고, 시계가 다 돌면 그 체력 그대로 일어난다.
    ///   · 몸을 버리고 **유령으로 닿으면 거둔다** — 적이 쓰러지고 유령 에너지가 조금 찬다.
    ///   · 드러난 숙주는 지금처럼 빙의 버튼으로 빼앗을 수도 있다 → 「빼앗을까 거둘까」.
    ///
    /// **지우는 법**: 이 파일을 지우고 `ReapRule` · `IsSoulExposed` · `TickReap` · `DrawReapHud` · `ReapAllowsLeave` 를 검색해 나오는 줄을 지운다
    ///   (`Unit.cs` 경직 · 피해 · 색 / `BattleDirector.cs` 겨누기 2곳 · 매 프레임 1곳 / `BattleDirector.TestGui.cs` 1곳 / `BattleDirector.SoulFx.cs` 나가기 조건 1곳 /
    ///   `Editor/TestSwitches.cs` 메뉴).
    /// </summary>
    public static class ReapRule
    {
        /// <summary>
        /// 에디터 메뉴 `Tools/Game/시험판 — 사신의 수확` 으로 켜고 끈다. **기본은 꺼짐, 빌드에서는 늘 꺼짐.**
        /// </summary>
        public static bool Enabled
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetBool("AVSR.ReapMode", false);
            set => UnityEditor.EditorPrefs.SetBool("AVSR.ReapMode", value);
#else
            get => false;
            set { }
#endif
        }

        /// <summary>혼이 드러나는 데 필요한 누적 피해 = 최대 체력의 이 비율. 본 규칙(35%)보다 낮춰 잡몹이 먼저 죽지 않게.</summary>
        public const float StaggerNeedRatio = 0.25f;

        /// <summary>혼이 드러나 있는 시간(초). 기획 3~5초의 가운데.</summary>
        public const float ExposeSeconds = 4f;

        /// <summary>유령이 이만큼(미터) 안에 들면 거둔다. 빙의 사거리(5m)보다 훨씬 짧다 — 날아 들어가야 거둔다.</summary>
        public const float HarvestReachMeters = 0.9f;

        /// <summary>한 번 거둘 때 돌려받는 유령 에너지(최대치 대비 %). 스스로 나오는 값(8%)보다 작다 — 둘 이상 거둬야 남는다.</summary>
        public const int RefundPercent = 5;

        /// <summary>이 시간(초) 안에 다음 혼을 거두면 콤보가 이어진다.</summary>
        public const float ComboSeconds = 3f;
    }

    public sealed partial class BattleDirector
    {
        private int _reapCount;
        private int _reapCombo;
        private int _reapBestCombo;
        private float _reapComboTimer;
        private float _reapFxTimer;

        /// <summary>혼 위로 반짝이를 띄우는 간격(초). 매 프레임 뿌리면 파티클 풀이 바닥난다.</summary>
        private const float ReapFxInterval = 0.3f;

        private void TickReap(float dt)
        {
            if (!ReapRule.Enabled) return;

            if (_reapComboTimer > 0f)
            {
                _reapComboTimer -= dt;
                if (_reapComboTimer <= 0f) _reapCombo = 0;
            }

            _reapFxTimer -= dt;
            bool fx = _reapFxTimer <= 0f;
            if (fx) _reapFxTimer = ReapFxInterval;

            // 드러난 혼은 제자리에 붙든다. 피격 경직을 다시 걸어 두면 적 판단이 «서 있기» 로 빠진다.
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsSoulExposed) continue;
                // 중간보스 대장은 못 뺏는 몸이다 — 거두기로 한 번에 끝나면 그 방의 싸움이 사라진다.
                if (e == _midBoss) { e.ClearStagger(); continue; }
                e.HoldHit(0.12f);
                if (fx && _pfx != null)
                {
                    // 머리 위 혼 구슬 — 간격보다 조금 길게 살려 끊기지 않고 떠 있게 한다.
                    // **남은 시간만큼 작아진다** — 따로 시계를 그리지 않고 구슬 크기로 «곧 일어난다»를 읽힌다(코덱스 검수 2026-10-07).
                    // ⚠ 반짝이(Star4)는 저주색 그림이 없어 주황으로 나온다. 빛 · 고리는 보라 그림이 있다.
                    float orb = 24f + 26f * e.SoulExposedRatio;
                    _pfx.Burst(ParticleFxKind.Glow, ParticleElement.Curse, e.Position + new Vector2(0f, Meters(0.75f)),
                               count: 1, speed: 0f, size: orb, life: ReapFxInterval + 0.12f, spin: 0f);
                    _pfx.Ring(e.Position, ParticleElement.Curse, 80f);
                }
            }

            // 몸이 있으면 못 거둔다 — 「언제 몸을 버리고 나갈까」 가 이 모드의 판단이다.
            if (_host != null || _ghost == null || IsChanneling) return;

            float reach = Meters(ReapRule.HarvestReachMeters);
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsSoulExposed) continue;
                float d = Vector2.Distance(_ghost.Position, e.Position) - BodyExcess(e);
                if (d > reach) continue;
                Harvest(e);
            }
        }

        /// <summary>
        /// 거둘 혼이 있으면 옮겨 탈 몸이 없어도 나갈 수 있다.
        /// 본 규칙(`CanLeaveHost`)은 «탈 몸이 없으면 못 나간다» 인데, 그러면 몸이 하나뿐인 방에서는
        /// 거두러 나갈 수가 없다. 거둔 뒤 몸이 없으면 구조 신호(`TickRescue`)가 몸을 들여보낸다.
        /// </summary>
        private bool ReapAllowsLeave()
        {
            if (!ReapRule.Enabled) return false;
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && _enemies[i].IsAlive && _enemies[i].IsSoulExposed) return true;
            return false;
        }

        private void Harvest(Unit e)
        {
            _reapCount++;
            _reapCombo = _reapComboTimer > 0f ? _reapCombo + 1 : 1;
            _reapComboTimer = ReapRule.ComboSeconds;
            if (_reapCombo > _reapBestCombo) _reapBestCombo = _reapCombo;

            int gain = Mathf.Max(1, GhostHpMax * ReapRule.RefundPercent / 100);
            int before = _ghostHp;
            _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + gain);
            PublishHp();
            ShowHeal(_ghost.Position, _ghostHp - before);

            if (_pfx != null)
            {
                // 큰 「빛」은 그림 가장자리가 네모로 드러난다(코덱스 검수) — 고리와 불티만 쓴다.
                _pfx.Ring(e.Position, ParticleElement.Curse, 120f);
                _pfx.Burst(ParticleFxKind.Spark, ParticleElement.Curse, e.Position,
                           count: 12, speed: 280f, size: 26f, life: 0.35f, spin: 0f);
            }
            GameSound.Cue("event.possess");

            e.ClearStagger();   // 혼이 닫혀야 KillEnemy 의 피해·표식 처리가 평소대로 돈다
            KillEnemy(e);
        }

#if UNITY_EDITOR
        private GUIStyle _reapHudStyle;

        /// <summary>좌상단 글자. 화면을 만들지 않는 시험판이라 OnGUI 로 띄운다(`BattleDirector.TestGui.cs` 가 부른다).</summary>
        private void DrawReapHud()
        {
            if (!ReapRule.Enabled) return;
            _reapHudStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.82f, 0.62f, 1f) },
            };
            string combo = _reapCombo > 1 ? $"   콤보 ×{_reapCombo}" : "";
            // 위쪽 HUD(유령 · 호스트 판) 아래, 방 바닥 첫 줄에 띄운다 — 판 위에 겹치면 둘 다 안 읽힌다.
            float y = Screen.height * 0.19f;
            GUI.Label(new Rect(16f, y, 600f, 32f), $"[시험판] 사신의 수확   거둔 혼 {_reapCount}{combo}", _reapHudStyle);
            GUI.Label(new Rect(16f, y + 30f, 600f, 32f), $"최고 콤보 {_reapBestCombo}", _reapHudStyle);
        }
#endif
    }
}
