using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 챕터 배경 한 칸 — 주소로 **한 장만** 읽고, 다른 챕터로 바뀌면 옛 것을 놓는다.
    ///
    /// ⚠ 예전에는 로비 · 성장 화면이 챕터 배경 10장(1080 × 2520, 장당 약 2.7MB)을 프리팹에 직접 물어
    ///   로비에 들어가는 순간 10장이 다 올라왔다(2026-10-30 메모리 점검). 챕터가 100개로 늘면 더 커진다.
    /// 그림은 어드레서블 `stagebg` 묶음에 한 장씩 따로 있다 — 주소 `StageBg/{파일 이름}`.
    /// </summary>
    public sealed class StageBgSlot
    {
        public const string Prefix = "StageBg/";

        private string _address;
        private int _ticket;

        /// <summary>그 배경을 img 에 꽂는다. 다 읽은 뒤 꽂으므로 그때까지는 이전 그림이 남는다.</summary>
        public void Show(Image img, string name, string scope)
        {
            if (img == null || string.IsNullOrEmpty(name)) return;
            string address = Prefix + name;
            if (address == _address) return;
            ShowAsync(img, address, scope, ++_ticket).Forget();   // fire-and-forget: 읽히면 꽂는다
        }

        /// <summary>쥐고 있는 배경을 놓는다(화면이 닫힐 때).</summary>
        public void Clear()
        {
            _ticket++;
            if (_address != null && CoreModule.TryGet<IResourceManager>(out var res)) res.Release(_address);
            _address = null;
        }

        private async UniTaskVoid ShowAsync(Image img, string address, string scope, int ticket)
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
            Sprite sprite;
            try { sprite = await res.LoadAsync<Sprite>(address, scope); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageBg] 배경을 못 읽었다({address}) — 지금 그림을 둔다: {e.Message}");
                return;
            }
            // 그사이 다른 챕터로 바뀌었거나 화면이 사라졌으면 방금 읽은 것을 놓는다
            if (ticket != _ticket || img == null)
            {
                if (sprite != null) res.Release(address);
                return;
            }
            if (sprite != null) img.sprite = sprite;
            // 새 그림을 꽂은 뒤 옛 것을 놓는다 — 먼저 놓으면 한 프레임 빈 칸이 보인다
            if (_address != null && _address != address) res.Release(_address);
            _address = sprite != null ? address : _address;
        }
    }
}
