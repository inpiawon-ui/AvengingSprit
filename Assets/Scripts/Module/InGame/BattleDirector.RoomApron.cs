using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 아래 남는 자리에 「방 밖 바닥」을 깐다 (2026-09-11).
    ///
    /// ── 왜 자리가 남나 ──────────────────────────────────────
    /// 하단 조작판을 걷고 필드를 화면 끝까지 내렸다(`FieldFit`). 그런데 방은 936 px 로
    /// 정해져 있어서 화면이 길면 방 아래가 빈다 — 16:9 에서 114, 20:9 에서 434 칸.
    /// 그 위에 D패드·빙의 버튼이 떠 있으므로 까맣게 두면 조작이 허공에 뜬 것처럼 보인다.
    ///
    /// ── 어떻게 까나 ─────────────────────────────────────────
    /// 무대마다 720 × 540 한 장(`roomfloor/roomapron_{무대}`). **위쪽부터** 쓰고 아래는
    /// 화면 밖으로 흘린다 — 그림 윗변이 방 울타리 바로 밑에 붙어야 이어져 보인다.
    /// 그림은 아래로 갈수록 어두워지게 받아서, 어디서 잘려도 끊겨 보이지 않는다.
    /// 좌우 벽(`RoomSide`)과 같은 톤으로 눌러서 방 안(플레이 영역)이 밝게 읽히게 한다.
    ///
    /// ⚠ 그림이 아직 없는 무대는 비워 둔다(어두운 바탕). 다른 무대 그림으로 대신 깔면
    ///   쓰레기장 밑에 연구소 바닥이 붙는다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>납품 규격. 폰마다 위 114 ~ 520 칸만 보인다.</summary>
        private static readonly Vector2 RoomApronSize = new(720f, 540f);

        // ⚠ 주소는 **소문자 그룹 이름**으로 시작한다 (`02_addressables` 규약).
        private const string RoomApronPrefix = "roomfloor/roomapron_";

        private RectTransform _apron;
        private Image _apronImage;

        /// <summary>지금 걸려 있는 무대. 같은 무대가 이어지면 다시 읽지 않는다.</summary>
        private string _apronEnv = string.Empty;

        /// <summary>지금 들고 있는 주소. 새 것을 건 뒤에 이것을 놓는다.</summary>
        private string _apronHeld;

        /// <summary>무대 이름으로 방 아래 바닥을 건다. 좌우 벽과 같은 자리(`ApplyRoomFloor`)에서 부른다.</summary>
        private void ApplyRoomApron(string env, int chapter)
        {
            if (string.IsNullOrEmpty(env)) env = EnvOfChapter(chapter);

            EnsureRoomApron();
            if (_apron == null) return;

            LayoutRoomApron();
            if (env == _apronEnv) return;
            _apronEnv = env;
            LoadRoomApronAsync(env).Forget();   // fire-and-forget: 바닥은 한 프레임 늦어도 된다
        }

        private void EnsureRoomApron()
        {
            if (_apron != null || _field == null) return;
            if (_field.parent is not RectTransform parent) return;

            var go = new GameObject("RoomApron", typeof(RectTransform), typeof(Image));
            _apron = (RectTransform)go.transform;
            _apron.SetParent(parent, false);
            // 필드 **바로 앞**에 끼운다. 조작 버튼·D패드보다 먼저 그려야 그 밑에 깔린다.
            _apron.SetSiblingIndex(_field.GetSiblingIndex());
            _apron.anchorMin = _apron.anchorMax = new Vector2(0.5f, 1f);
            _apron.pivot = new Vector2(0.5f, 1f);
            _apron.sizeDelta = RoomApronSize;

            _apronImage = go.GetComponent<Image>();
            _apronImage.color = RoomSideTint;    // 좌우 벽과 같은 톤 — 방 밖이다
            _apronImage.raycastTarget = false;   // 조작을 가로채면 안 된다
            _apronImage.enabled = false;         // 그림이 올 때까지 비워 둔다
        }

        /// <summary>필드 아랫변에 윗변을 붙인다. 방이 창보다 길어 남는 자리가 없으면 끈다.</summary>
        private void LayoutRoomApron()
        {
            if (_apron == null || _field == null) return;
            if (_field.parent is not RectTransform parent) return;

            float fieldBottom = _fieldTopOffset + _field.rect.height;
            bool gap = parent.rect.height - fieldBottom > 0.5f;
            _apron.gameObject.SetActive(gap);
            if (!gap) return;

            float fieldCenterX = _field.anchoredPosition.x + _field.rect.width * (0.5f - _field.pivot.x);
            _apron.anchoredPosition = new Vector2(fieldCenterX, -fieldBottom);
        }

        private async UniTaskVoid LoadRoomApronAsync(string env)
        {
            var address = RoomApronPrefix + env;
            Sprite art = null;
            try { art = await CoreModule.Get<IResourceManager>().LoadAsync<Sprite>(address); }
            catch (Exception) { /* 아직 안 온 무대다. 비워 둔다. */ }

            // 기다리는 사이에 방이 또 바뀌었으면 이 결과는 버린다.
            if (_apronEnv != env || _apronImage == null)
            {
                if (art != null && address != _apronHeld)
                    CoreModule.Get<IResourceManager>().Release(address);
                return;
            }

            _apronImage.sprite = art;
            _apronImage.enabled = art != null;

            // 새 그림이 걸린 **뒤에** 지난 것을 놓는다. 먼저 놓으면 한 프레임 빈다.
            var old = _apronHeld;
            _apronHeld = art != null ? address : null;
            if (old != null && old != _apronHeld) CoreModule.Get<IResourceManager>().Release(old);
        }
    }
}
