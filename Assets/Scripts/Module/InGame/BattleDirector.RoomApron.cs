using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 아래 바깥에 「방 밖 바닥」을 깐다.
    ///
    /// ── 왜 필요한가 ─────────────────────────────────────────
    /// 카메라가 캐릭터를 따라가서(`WantScroll`) 캐릭터가 방 아래쪽에 서면 방이 위로 밀려 올라간다.
    /// 그 밑에 드러나는 자리 — 조작 버튼이 떠 있는 곳 — 를 까맣게 두면 조작이 허공에 뜬다.
    ///
    /// ── 어떻게 까나 ─────────────────────────────────────────
    /// 720 × 540 한 장을 **방 아랫변 바로 밑**에 붙여 방과 함께 스크롤한다(`ApplyScroll`).
    /// 필드 안(마스크 안)에 두고, 방 바닥 바로 위·유닛 아래에 그린다.
    /// 좌우 벽(`RoomSide`)과 같은 톤으로 눌러서 방 안(플레이 영역)이 밝게 읽히게 한다.
    ///
    /// ── 어느 그림을 쓰나 ────────────────────────────────────
    /// 무대 전용(`roomfloor/roomapron_{무대}`)이 있으면 그것, 없으면 **공용**(`roomapron_common`).
    /// 기획(2026-09-11) — 공용 한 장으로 먼저 보고, 어색한 무대만 전용을 더한다.
    ///
    /// ⚠ 무대 전용 5장(미사일·거리·옥상·연구소·정제소)은 윗부분에 방 아랫벽을 한 번 더 그려
    ///   벽이 두 겹으로 보여서 지웠다. 쓰레기장 전용은 샘플로 통과한 것이라 남긴다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>납품 규격. 카메라가 방 아래로 내려다볼 수 있는 한계이기도 하다(`WantScroll`).</summary>
        private static readonly Vector2 RoomApronSize = new(720f, 540f);

        // ⚠ 주소는 **소문자 그룹 이름**으로 시작한다 (`02_addressables` 규약).
        private const string RoomApronPrefix = "roomfloor/roomapron_";
        private const string RoomApronCommon = "roomfloor/roomapron_common";

        private RectTransform _apron;
        private Image _apronImage;

        /// <summary>지금 걸려 있는 무대. 같은 무대가 이어지면 다시 읽지 않는다.</summary>
        private string _apronEnv = string.Empty;

        /// <summary>지금 들고 있는 주소. 새 것을 건 뒤에 이것을 놓는다.</summary>
        private string _apronHeld;

        /// <summary>무대 이름으로 방 아래 바닥을 건다. 좌우 벽과 같은 자리(`ApplyRoomFloor`)에서 부른다.</summary>
        /// <param name="preferKey">먼저 찾아볼 이름(보스방 `boss_crusher`). 없으면 무대 것으로 떨어진다.</param>
        private void ApplyRoomApron(string env, int chapter, string preferKey = null)
        {
            if (string.IsNullOrEmpty(env)) env = EnvOfChapter(chapter);

            EnsureRoomApron();
            if (_apron == null) return;

            string key = string.IsNullOrEmpty(preferKey) ? env : preferKey;
            if (key == _apronEnv) return;
            _apronEnv = key;
            LoadRoomApronAsync(key, env).Forget();   // fire-and-forget: 바닥은 한 프레임 늦어도 된다
        }

        private void EnsureRoomApron()
        {
            if (_apron != null || _field == null || _floor == null) return;

            var go = new GameObject("RoomApron", typeof(RectTransform), typeof(Image));
            _apron = (RectTransform)go.transform;
            _apron.SetParent(_field, false);
            // 방 바닥 **바로 다음** — 유닛·탄·글자보다 먼저 그려야 그 밑에 깔린다.
            _apron.SetSiblingIndex(_floor.GetSiblingIndex() + 1);
            // 방 좌표와 같은 기준(왼쪽 위). 자리는 `ApplyScroll` 이 방 아랫변 밑으로 옮긴다.
            _apron.anchorMin = _apron.anchorMax = new Vector2(0f, 1f);
            _apron.pivot = new Vector2(0f, 1f);
            _apron.sizeDelta = RoomApronSize;

            _apronImage = go.GetComponent<Image>();
            // ⚠ 색을 곱하지 않는다 (2026-09-17). 새 무대 배경은 방 바닥과 아래 영역을
            //   **한 장으로 이어 그려** 잘라 쓴다. 예전처럼 좌우 벽 톤(`RoomSideTint`)을 곱하면
            //   경계에서 아래 영역만 한 톤 어두워져 한 장면이 두 장으로 갈라져 보인다.
            _apronImage.color = Color.white;
            _apronImage.raycastTarget = false;   // 조작을 가로채면 안 된다
            _apronImage.enabled = false;         // 그림이 올 때까지 비워 둔다
            ApplyScroll();
        }

        private async UniTaskVoid LoadRoomApronAsync(string key, string env)
        {
            // 보스 전용 → 무대 전용 → 공용 순.
            var address = RoomApronPrefix + key;
            var art = await LoadOptionalAsync<Sprite>(address);   // 전용이 없는 방이다
            if (art == null && key != env)
            {
                address = RoomApronPrefix + env;
                art = await LoadOptionalAsync<Sprite>(address);
            }
            if (art == null)
            {
                address = RoomApronCommon;
                art = await LoadOptionalAsync<Sprite>(address);   // 공용도 아직 없으면 비워 둔다
            }

            // 기다리는 사이에 방이 또 바뀌었으면 이 결과는 버린다.
            if (_apronEnv != key || _apronImage == null)
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
