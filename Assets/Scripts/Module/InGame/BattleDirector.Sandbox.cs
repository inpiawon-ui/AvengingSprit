using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 연출 확인용 **테스트 판** (2026-09-15).
    ///
    /// ── 이 파일은 지우기 위해 있다 ─────────────────────────────
    /// 스킬 연출을 눈으로 보려면 「적이 안 죽고 · 스킬이 계속 나가는」 판이 필요한데,
    /// 그런 상태는 게임이 아니다. 그래서 **한 곳에 몰아 두고 스위치 하나로** 켠다.
    ///
    /// **지우는 법**: 이 파일을 지우고, 본문에서 `Sandbox` 를 검색해 나오는
    /// **네 줄**을 지우면 끝이다. 다른 자리에는 아무것도 안 남는다.
    ///   · `BattleDirector.cs`              — 마릿수 2곳 · 체력 · 게이지 · 채우기 · 표시 · 유령시계
    ///   · `BattleDirector.Cards.cs`        — 암살 차단
    ///   · `BattleDirector.HostPassives.cs` — 표식 처형 차단
    ///   · `BattleDirector.SkillsNew.cs`    — 사신 즉사 차단
    ///   그리고 이 판을 쓰는 에디터 도구 `Assets/Scripts/Editor/SkillFxRecorder.cs`(스킬 연출 영상)도 함께 지운다.
    ///
    /// ⚠ 스위치는 `GameConfig` 에 있다(`_sandboxMode`). **기본은 꺼짐**이고,
    ///   켜져 있으면 인게임 좌상단에 빨간 글씨로 알린다 — 켠 채로 빌드하는 사고를 막는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>테스트 판이 켜져 있는가. 표가 없으면 언제나 꺼짐이다.</summary>
        private bool Sandbox => _config != null && _config.SandboxMode;

        /// <summary>테스트 판에서 세울 적 마릿수.</summary>
        private const int SandboxEnemyCount = 5;

        /// <summary>테스트 판에서 적 체력에 곱하는 값. 안 죽어야 연출을 계속 본다.</summary>
        private const int SandboxHpMul = 200;

        /// <summary>마릿수를 테스트 값으로 바꾼다. 꺼져 있으면 받은 값을 그대로 돌려준다.</summary>
        private int SandboxCount(int normal) => Sandbox ? SandboxEnemyCount : normal;

        /// <summary>체력을 불린다. 꺼져 있으면 받은 값 그대로다.</summary>
        private int SandboxHp(int normal) => Sandbox ? normal * SandboxHpMul : normal;

        /// <summary>게이지를 깎을 차례인가. 테스트 판에서는 안 깎는다 — 계속 쓸 수 있어야 한다.</summary>
        /// 데미지 테스트 모드에서도 안 깎는다 — 스킬 컷인 · 연출을 연달아 보기 위해서다(기획 2026-09-17).
        private bool SandboxKeepsGauge => Sandbox || TestDamageOn;

        /// <summary>
        /// 즉사를 막을 차례인가.
        ///
        /// ⚠ 체력을 200배로 불려도 **즉사는 체력을 안 본다.** 갱스터 표식 처형 ·
        ///   사신의 시간 · 암살 카드는 그냥 죽인다 — 그래서 적이 계속 사라졌다
        ///   (기획 2026-09-15). 테스트 판에서는 세 곳 모두 막는다.
        /// </summary>
        /// <summary>
        /// ⚠ **즉사를 보려면 `GameConfig._sandboxNoExecute` 를 꺼라.**
        ///   기본은 막혀 있다 — 안 막으면 표식 처형만으로 방이 쓸려서 연출을 못 본다.
        /// </summary>
        private bool SandboxBlocksExecute => Sandbox && _config.SandboxNoExecute;

        /// <summary>
        /// 유령 시계를 멈출 차례인가.
        ///
        /// ⚠ 테스트 중에 몸을 갈아타려고 잠깐 유령으로 있으면 시계가 돌아
        ///   「유령이 소멸했습니다」로 로비에 튕겼다. 연출을 보다 말게 된다.
        /// </summary>
        private bool SandboxKeepsGhost => Sandbox;

        /// <summary>
        /// 내 공격 피해를 테스트 값으로 바꾼다(`GameConfig._testPlayerDamage`). 꺼져 있으면 받은 값 그대로.
        /// 스테이지를 한 방씩 넘기며 전체를 둘러보는 용도다.
        /// </summary>
        private int SandboxDamage(int normal)
            => _config != null && _config.TestPlayerDamage > 0 ? _config.TestPlayerDamage : normal;

        private bool TestDamageOn => _config != null && _config.TestPlayerDamage > 0;

#if UNITY_EDITOR
        /// <summary>
        /// 스킬 연출 영상(`SkillFxRecorder`)용 — 지금 몸을 치우고 그 자리에 다른 몸을 세운다(2026-10-06).
        /// 테스트 판에서만 된다. 빙의 연출 · 탈출 비용 없이 바로 바꾼다 — 23종을 한 판에서 연달아 찍으려고.
        /// 영상 도중 맞아 죽지 않게 무적을 길게 건다.
        /// </summary>
        public bool SandboxSwapHost(string key)
        {
            if (!Sandbox || !_running || _player == null) return false;
            var entry = _player.GetHost(key);
            if (entry == null) return false;
            Vector2 pos = _host != null ? _host.Position : _ghost.Position;
            if (_host != null)
            {
                Destroy(_host.gameObject);
                _host = null;
            }
            EnterHost(entry, key, entry.DisplayName, pos, 100);
            _invuln = 999f;
            // ⚠ 적을 채우지 않는다 — 영상 도구가 몸마다 판을 새로 연다. 채우면 그림이 안 올라온 종류의 적이
            //   흰 네모로 섰다(2026-10-06).
            return true;
        }

        /// <summary>영상 도구가 켠다 — 방 입장 때 적 다섯 채우기를 건너뛴다.</summary>
        public static bool SandboxSkipTopUp;

        /// <summary>테스트 판이 돌고 있는가 — 영상 도구가 시작해도 되는지 본다.</summary>
        public bool SandboxRunning => Sandbox && _running;

        /// <summary>
        /// 그 몸의 그림을 올린다. 판은 시작 몸 · 길에 나올 적의 그림만 미리 올려서,
        /// 그 밖의 몸으로 바로 바꾸면 **흰 네모**로 섰다(영상 2026-10-06). 실제 게임은 방 안의 적에게서만 몸을 얻어 문제없다.
        /// </summary>
        public void SandboxPreload(string hostKey)
        {
            var res = GameFramework.Core.Base.CoreModule.Get<GameFramework.Core.Module.Resource.IResourceManager>();
            foreach (var key in SandboxAtlasKeys(hostKey))
                if (!_unitAtlas.ContainsKey(key))
                    LoadOneUnitAtlasAsync(res, key).Forget();   // fire-and-forget: 다 올라왔는지는 SandboxUnitReady 로 본다
        }

        public bool SandboxUnitReady(string hostKey)
        {
            foreach (var key in SandboxAtlasKeys(hostKey))
                if (!_unitAtlas.ContainsKey(key)) return false;
            return true;
        }

        /// <summary>몸 키와 그림 키 — 둘이 다른 몸이 있어 둘 다 올린다.</summary>
        private System.Collections.Generic.IEnumerable<string> SandboxAtlasKeys(string hostKey)
        {
            if (string.IsNullOrEmpty(hostKey)) yield break;
            yield return hostKey;
            var entry = _player != null ? _player.GetHost(hostKey) : null;
            if (entry != null && !string.IsNullOrEmpty(entry.SpriteKey) && entry.SpriteKey != hostKey)
                yield return entry.SpriteKey;
        }
#endif

        /// <summary>
        /// 적 수를 다섯으로 **채운다.** 정본 방은 스폰 목록이 정해져 있어
        /// `SandboxCount` 가 안 닿는다 — 모자란 만큼 뒤에 더 세운다.
        /// 적을 **세운 뒤에** 부른다.
        /// </summary>
        private void SandboxTopUp(int index)
        {
            if (!Sandbox) return;
#if UNITY_EDITOR
            // 스킬 영상을 찍는 동안은 채우지 않는다 — 채우는 적은 아무 몸이나 골라 그림이 안 올라온 종류가 흰 네모로 섰다
            if (SandboxSkipTopUp) return;
#endif
            int have = 0;
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i] != null && _enemies[i].IsAlive) have++;
            int need = SandboxEnemyCount - have;
            if (need > 0) SpawnProcedural(need, elite: false, seed: index * 977 + have);
        }

        // ── 켜져 있다는 표시 ──────────────────────────────────
        //
        // 테스트 판을 켠 채로 잊으면 「적이 안 죽는 게임」이 나간다.
        // 화면에 붙여 두면 잊을 수가 없다.

        private RectTransform _sandboxTag;

        private void EnsureSandboxTag()
        {
            if (!Sandbox && !TestDamageOn)
            {
                if (_sandboxTag != null) _sandboxTag.gameObject.SetActive(false);
                return;
            }
            if (_sandboxTag == null)
            {
                var canvas = _field != null ? _field.root : transform.root;
                var go = new GameObject("SandboxTag", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                _sandboxTag = (RectTransform)go.transform;
                _sandboxTag.anchorMin = _sandboxTag.anchorMax = new Vector2(0f, 1f);
                _sandboxTag.pivot = new Vector2(0f, 1f);
                // ⚠ HUD 위에 올리면 「PLAYER SOUL」 글자와 겹쳐 둘 다 안 읽힌다(실측).
                //   HUD 아래(280px)로 내려 방 왼쪽 위 구석에 붙인다.
                _sandboxTag.anchoredPosition = new Vector2(12f, -292f);
                _sandboxTag.sizeDelta = new Vector2(360f, 34f);   // 「● 데미지 9999 · 스킬 무한」이 200 에서 두 줄로 꺾였다

                var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
                // ⚠ 길게 적었더니 줄바꿈되어 방을 가렸다(실측). 한 줄로 줄인다.
                tmp.text = Sandbox ? "● 테스트 판" : $"● 데미지 {_config.TestPlayerDamage} · 스킬 무한";
                tmp.fontSize = 24f;
                tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;   // 폭이 모자라면 두 줄로 꺾여 방을 가렸다
                tmp.color = new Color(1f, 0.32f, 0.32f, 1f);
                tmp.raycastTarget = false;
            }
            _sandboxTag.SetAsLastSibling();
            if (!_sandboxTag.gameObject.activeSelf) _sandboxTag.gameObject.SetActive(true);
        }
    }
}
