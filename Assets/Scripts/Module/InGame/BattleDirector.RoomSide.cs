using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 넓은 화면에서 플레이 필드 좌우에 남는 자리를 벽으로 채운다 (2026-09-10).
    ///
    /// ── 왜 자리가 남나 ──────────────────────────────────────
    /// 방은 10 m × 13 m 고정이고 1 m = 72 px 이라 **필드는 언제나 720 px** 이다.
    /// 그런데 태블릿 4:3 에서는 화면에 보이는 가로 칸이 960 이 된다(`SafeArea` 의 contain 규칙).
    /// 필드를 늘리면 없는 방을 보여주는 셈이라 늘릴 수 없다 — 남는 120 칸씩이
    /// 그냥 어두워서 「화면이 잘렸다」로 읽혔다.
    ///
    /// 거기에 **세로로 선 벽 띠**를 깔아 필드가 액자에 담긴 것처럼 만든다.
    ///
    /// ── 왜 필드 밖에 두나 ───────────────────────────────────
    /// 필드(`RoomField`)에는 `RectMask2D` 가 걸려 있어서 그 안에 넣으면 잘려 나간다.
    /// 그래서 **필드의 형제**로 두고, 그리는 순서만 필드보다 앞에 둔다.
    ///
    /// ⚠ 그림은 **왼쪽 벽 한 장만** 있다. 오른쪽은 좌우를 뒤집어 쓴다 —
    ///   두 장을 따로 그리면 두 배로 발주해야 하고, 대칭이 안 맞으면 그게 더 눈에 띈다.
    ///
    /// ⚠ 폭을 화면에 맞춰 줄이지 않는다. 그림 폭(160) 그대로 두고 **오른쪽 변을
    ///   필드에 붙인 뒤 왼쪽을 화면 밖으로 흘린다.** 폭을 줄이면 `Image.Type.Tiled` 가
    ///   왼쪽 끝부터 잘라 내서, 정작 보여야 할 「필드와 맞닿는 앞면」이 사라진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>납품 규격. 세로로 이어 붙는 그림이라 높이는 반복 단위다.</summary>
        private const float RoomSideWidth = 160f;

        /// <summary>
        /// 벽을 이만큼 눌러 뒤로 물린다.
        ///
        /// ⚠ **원본 밝기로 두면 안 된다.** 벽 그림이 방 바닥과 같은 무대·같은 화풍이라
        ///   그대로 깔면 필드와 이어져 보여서 **어디까지 걸어갈 수 있는지가 안 읽힌다.**
        ///   실제로 붙여 보니 액자가 아니라 방이 넓어진 것처럼 보였다.
        ///   한 겹 눌러 두면 밝은 쪽이 플레이 영역이라는 것이 저절로 읽힌다.
        /// </summary>
        private static readonly Color RoomSideTint = new Color(0.62f, 0.62f, 0.70f, 1f);

        // ⚠ 주소는 **소문자 그룹 이름**으로 시작한다 (`02_addressables` 규약).
        //   `RoomFloor/` 로 적으면 못 찾고 조용히 벽 없이 지나간다.
        private const string RoomSidePrefix = "roomfloor/roomside_";

        private RectTransform _sideLeft;
        private RectTransform _sideRight;
        private Image _sideLeftImage;
        private Image _sideRightImage;

        /// <summary>지금 걸려 있는 무대. 같은 무대가 이어지면 다시 읽지 않는다.</summary>
        private string _sideEnv = string.Empty;

        /// <summary>지금 들고 있는 주소. 새 것을 건 뒤에 이것을 놓는다.</summary>
        private string _sideHeld;

        /// <summary>
        /// 무대 이름(`junkyard` 등)으로 좌우 벽을 건다.
        /// 방 바닥을 정하는 자리(`ApplyRoomFloor`)에서 부른다 — 벽은 바닥과 한 무대다.
        /// </summary>
        private void ApplyRoomSides(string env, int chapter)
        {
            // 정본 층 배치(`roomfloor_ch1_*`)를 쓰는 방은 무대 이름이 비어 있다.
            // 그때는 챕터로 고른다 — 벽이 없는 방과 있는 방이 섞이면 그게 더 튄다.
            if (string.IsNullOrEmpty(env)) env = EnvOfChapter(chapter);

            EnsureRoomSides();
            if (_sideLeft == null) return;

            // 화면이 기준 폭이면 벽이 통째로 화면 밖이다. 그릴 이유가 없다.
            bool wide = _field != null && _field.parent is RectTransform parent
                        && parent.rect.width > _field.rect.width + 1f;
            _sideLeft.gameObject.SetActive(wide);
            _sideRight.gameObject.SetActive(wide);
            if (!wide) return;

            LayoutRoomSides();
            if (env == _sideEnv) return;
            _sideEnv = env;
            LoadRoomSideAsync(env).Forget();   // fire-and-forget: 벽은 한 프레임 늦어도 된다
        }

        private static string EnvOfChapter(int chapter) => chapter switch
        {
            1 => "junkyard",
            2 => "missile",
            3 => "street",
            4 => "rooftop",
            5 => "lab",
            _ => "refinery",
        };

        private void EnsureRoomSides()
        {
            if (_sideLeft != null || _field == null) return;
            if (_field.parent is not RectTransform parent) return;

            _sideLeft = MakeSide(parent, "RoomSideLeft", flip: false, out _sideLeftImage);
            _sideRight = MakeSide(parent, "RoomSideRight", flip: true, out _sideRightImage);
        }

        private RectTransform MakeSide(RectTransform parent, string name, bool flip, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            // 필드 **바로 앞**에 끼운다. 뒤에 두면 벽이 필드를 덮는다.
            rt.SetSiblingIndex(_field.GetSiblingIndex());

            image = go.GetComponent<Image>();
            image.type = Image.Type.Tiled;
            image.color = RoomSideTint;
            image.raycastTarget = false;   // 조작을 가로채면 안 된다

            // 좌우 뒤집기는 **가운데를 축으로** 한다(pivot 0.5). 끝을 축으로 뒤집으면
            // 그림이 통째로 반대편으로 넘어가 필드를 덮는다.
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (flip) rt.localScale = new Vector3(-1f, 1f, 1f);
            return rt;
        }

        /// <summary>필드의 세로 자리를 그대로 따르고, 가로는 필드 양옆에 붙인다.</summary>
        private void LayoutRoomSides()
        {
            if (_sideLeft == null || _field == null) return;

            float half = _field.rect.width * 0.5f;
            // 필드가 부모 가운데에 있다는 전제 — 그렇지 않으면 필드 중심을 기준으로 잡는다.
            float fieldCenterX = _field.anchoredPosition.x + _field.rect.width * (0.5f - _field.pivot.x);

            foreach (var (rt, sign) in new[] { (_sideLeft, -1f), (_sideRight, 1f) })
            {
                rt.anchorMin = new Vector2(0.5f, _field.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, _field.anchorMax.y);
                rt.pivot = new Vector2(0.5f, _field.pivot.y);
                rt.sizeDelta = new Vector2(RoomSideWidth, _field.sizeDelta.y);
                rt.anchoredPosition = new Vector2(
                    fieldCenterX + sign * (half + RoomSideWidth * 0.5f),
                    _field.anchoredPosition.y);
            }
        }

        private async UniTaskVoid LoadRoomSideAsync(string env)
        {
            var address = RoomSidePrefix + env;
            Sprite art = null;
            try { art = await CoreModule.Get<IResourceManager>().LoadAsync<Sprite>(address); }
            catch (Exception) { /* 아직 안 온 무대다. 벽 없이 간다. */ }

            // 기다리는 사이에 방이 또 바뀌었으면 이 결과는 버린다.
            if (_sideEnv != env || _sideLeftImage == null)
            {
                if (art != null && address != _sideHeld)
                    CoreModule.Get<IResourceManager>().Release(address);
                return;
            }

            _sideLeftImage.sprite = art;
            _sideRightImage.sprite = art;
            _sideLeftImage.enabled = art != null;
            _sideRightImage.enabled = art != null;

            // 새 벽이 걸린 **뒤에** 지난 것을 놓는다. 먼저 놓으면 한 프레임 빈다.
            var old = _sideHeld;
            _sideHeld = art != null ? address : null;
            if (old != null && old != _sideHeld) CoreModule.Get<IResourceManager>().Release(old);
        }
    }
}
