using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 큰 그림 한 칸 — 주소로 **보일 때 한 장만** 읽고, 다른 그림으로 바뀌면 옛 것을 놓는다.
    ///
    /// ⚠ 예전에는 화면 프리팹이 큰 그림을 직접 물어 화면이 뜨는 순간 다 올라왔다(2026-10-30 메모리 점검):
    ///   로비 챕터 배경 10장(1080 × 2520, 장당 약 2.7MB) · 인게임 결과 배경 3장 · 이벤트 그림 19장.
    /// 그림은 어드레서블에 한 장씩 따로 있다 — 등록은 에디터 `LazyArtGroups`.
    /// </summary>
    public sealed class LazySpriteSlot
    {
        /// <summary>챕터 배경 — `StageBg/rb_art_bg_stage_chNN`.</summary>
        public const string StageBg = "StageBg/";
        /// <summary>키트 큰 그림(결과 배경 · 이벤트 그림) — `KitArt/{파일 이름}`.</summary>
        public const string KitArt = "KitArt/";

        private string _address;
        private int _ticket;

        /// <summary>그 그림을 img 에 꽂는다. 다 읽은 뒤 꽂으므로 그때까지는 이전 그림이 남는다.</summary>
        /// <param name="hideUntilLoaded">처음 꽂을 때 다 읽을 때까지 칸을 감춘다(빈 칸 · 옛 그림이 보이면 안 되는 곳).</param>
        public void Show(Image img, string address, string scope, bool hideUntilLoaded = false)
        {
            if (img == null || string.IsNullOrEmpty(address)) return;
            if (address == _address) { img.enabled = img.sprite != null; return; }
            if (hideUntilLoaded) img.enabled = false;
            ShowAsync(img, address, scope, ++_ticket).Forget();   // fire-and-forget: 읽히면 꽂는다
        }

        /// <summary>쥐고 있는 그림을 놓는다(화면이 닫힐 때).</summary>
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
                Debug.LogWarning($"[LazySprite] 그림을 못 읽었다({address}) — 지금 그림을 둔다: {e.Message}");
                return;
            }
            // 그사이 다른 그림으로 바뀌었거나 화면이 사라졌으면 방금 읽은 것을 놓는다
            if (ticket != _ticket || img == null)
            {
                if (sprite != null) res.Release(address);
                return;
            }
            if (sprite == null) return;
            img.sprite = sprite;
            img.enabled = true;
            // 새 그림을 꽂은 뒤 옛 것을 놓는다 — 먼저 놓으면 한 프레임 빈 칸이 보인다
            if (_address != null && _address != address) res.Release(_address);
            _address = address;
        }
    }
}
