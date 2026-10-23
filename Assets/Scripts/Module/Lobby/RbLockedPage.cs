using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.InGame;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 미보유 호스트 — 시안 Screens/host-locked · host-unlock-ready.png.
    /// 실루엣 · 해금 필요 · 능력치 전부 잠김 · 조각 n /m · 해금(조각이 모자라면 「조각 n개 부족」, 누를 수 없음).
    /// 해금하면 새 몸 보여주기(RbRevealPage) → 성장 화면.
    /// </summary>
    public sealed class RbLockedPage : MonoBehaviour
    {
        private static readonly Color Ink = new(0.16f, 0.11f, 0.03f), Dim = new(0.70f, 0.74f, 0.84f);

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private IPlayerDataService _player;
        private string _key;

        private void Awake()
        {
            if (_ui != null) return;   // Open 이 먼저 불렀다(꺼진 채로 열 때) — 버튼을 두 번 걸지 않는다
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("LckBack", () => _shell.SelectTab("Host"));
            _ui.OnClick("LckHelp", () => SystemPopup.Show(Localize.FromTable("ui.rb.lock.help", "조각을 모으면 그 몸을 해금해요.\n해금하면 출전 · 능력치 강화 · 성급 강화가 열려요."), null, Localize.Get("ui.common.ok"), null));
            _ui.OnClick("LckWhere", () => _shell.ShowShardSources(_key));
            _ui.OnClick("LckUnlock", Unlock);
        }

        public string HostKey => _key;

        public void Open(IPlayerDataService player, string hostKey)
        {
            if (_ui == null) Awake();
            _player = player;
            _key = hostKey;
            gameObject.SetActive(true);
            if (_ui.Find("LckBody") is RectTransform view && view.TryGetComponent<ScrollRect>(out var sr)) sr.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public void Refresh()
        {
            if (_player == null || !gameObject.activeSelf) return;
            var e = _player.GetHost(_key);
            if (e == null) return;
            _ui.SetText("LckName", e.DisplayName);
            _ui.SetText("LckChipText", RbRevealPage.TagLine(e, withGrade: false));
            if (_ui.Get<Image>("LckHostArt") is Image art)
            {
                if (!art.TryGetComponent<UnitShowcase>(out var show)) show = art.gameObject.AddComponent<UnitShowcase>();
                show.Show(_key, Color.black);   // 실루엣 — 아직 얻지 않은 몸
            }
            int shards = _player.GetShards(_key), need = Mathf.Max(1, _player.MasteryCost(_key));
            _ui.SetText("LckShardCount", string.Format(Localize.FromTable("ui.rb.lock.shards", "조각 {0} /{1}"), shards, need));
            if (_ui.Find("LckBarFill") is RectTransform fill) fill.anchorMax = new Vector2(Mathf.Clamp01((float)shards / need), 1f);
            bool ready = shards >= need;
            if (_ui.Get<Button>("LckUnlock") is Button b) b.interactable = ready;
            _ui.SetText("LckUnlockText", ready ? Localize.FromTable("ui.rb.lock.unlock", "해금")
                                               : string.Format(Localize.FromTable("ui.rb.lock.short", "조각 {0}개 부족"), need - shards));
            if (_ui.Get<TextMeshProUGUI>("LckUnlockText") is TextMeshProUGUI t) t.color = ready ? Ink : Dim;
        }

        private void Unlock()
        {
            // 봉인 해제도 성급 올리기와 같은 동작이다(0 → 1, 골드 없음)
            if (!_player.TryStarUp(_key)) { Refresh(); return; }
            _player.SaveAsync().Forget();   // fire-and-forget: 얻자마자 남긴다
            gameObject.SetActive(false);
            _shell.RefreshHeader();
            _shell.ShowReveal(_key, 0);
        }
    }
}
