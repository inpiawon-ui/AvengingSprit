#if UNITY_EDITOR
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 보스 스킬을 손으로 눌러 보는 시험용 버튼.
    ///
    /// ⚠ **확인이 끝나면 이 파일만 지우면 된다.** 다른 파일에 아무것도 안 남는다 —
    ///   `#if UNITY_EDITOR` 안에 통째로 들어 있어 빌드에도 안 따라간다.
    ///
    /// 쿨다운·페이즈·거리 조건을 **전부 무시하고** 그 자리에서 예고를 시작한다.
    /// 확인하려는 것이 "언제 나오는가" 가 아니라 "나오면 어떻게 생겼는가" 이기 때문이다.
    /// 조건대로 도는 것을 보려면 버튼을 안 누르면 된다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float TestBtnW = 128f;
        private const float TestBtnH = 44f;
        private const float TestBtnGap = 6f;

        private GUIStyle _testBtnStyle;

        private void OnGUI()
        {
            var def = _brain != null ? _brain.Entry : null;
            if (_boss == null || !_boss.IsAlive || def == null || def.Moves.Count == 0) return;

            if (_testBtnStyle == null)
                _testBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true };

            int n = def.Moves.Count;
            float totalH = n * TestBtnH + (n - 1) * TestBtnGap;
            float x = Screen.width - TestBtnW - 10f;
            float y = (Screen.height - totalH) * 0.5f;

            GUI.Label(new Rect(x, y - 22f, TestBtnW, 20f), $"{def.NameKr} 스킬");

            for (int i = 0; i < n; i++)
            {
                var m = def.Moves[i];
                var r = new Rect(x, y + i * (TestBtnH + TestBtnGap), TestBtnW, TestBtnH);
                if (GUI.Button(r, $"{i + 1}. {m.NameKr}", _testBtnStyle))
                    TestForceBossMove(i);
            }
        }

        /// <summary>
        /// 그 보스의 i번째 스킬을 지금 쓰게 한다. 쿨·페이즈·거리를 안 본다.
        ///
        /// 돌던 예고·돌진·물기를 먼저 끊는다 — 겹치면 무엇을 보고 있는지 알 수 없다.
        /// </summary>
        public void TestForceBossMove(int index)
        {
            var def = _brain != null ? _brain.Entry : null;
            if (_boss == null || def == null || index < 0 || index >= def.Moves.Count) return;

            _brain.BeginCharge(Vector2.zero, 0f);
            ClearFollowUp();
            ClearDanger();

            _brain.TestBeginTelegraph(def.Moves[index]);
            Debug.Log($"[시험] {def.NameKr} — {def.Moves[index].NameKr} 강제 시전");
        }
    }
}
#endif
