using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 위 바깥을 구름으로 가린다 (2026-09-11).
    ///
    /// ── 왜 필요한가 ─────────────────────────────────────────
    /// 카메라가 캐릭터를 따라가서(`WantScroll`) 캐릭터가 문 앞(방 위쪽)에 서면 방이 아래로 밀려 내려간다.
    /// 그 위에 드러나는 자리는 그릴 것이 없다 — 궁수의 전설처럼 **구름으로 덮는다.**
    ///
    /// ── 어떻게 까나 ─────────────────────────────────────────
    /// 720 × 640 한 장의 **아랫변을 방 윗변에 붙여** 방과 함께 스크롤한다(`ApplyScroll`).
    /// 아래쪽 가장자리는 뭉게구름이라 방 꼭대기에 살짝 걸친다. 유닛·탄·글자보다 위에 그린다 —
    /// 방 밖에는 아무것도 없어야 한다.
    ///
    /// 무대 전용(`roomfloor/roomcloud_{무대}`)이 있으면 그것, 없으면 **공용**(`roomcloud`) —
    /// 기획(2026-09-11): 구름은 공용 한 장.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>납품 규격.</summary>
        private static readonly Vector2 RoomCloudSize = new(720f, 640f);

        /// <summary>
        /// 구름을 방 윗변보다 이만큼 **내려서** 붙인다.
        ///
        /// ⚠ 납품 그림은 맨 아래 구름 덩이가 y=588 에서 끝나고 그 밑 51 px 가 비어 있다.
        ///   아랫변을 방 윗변에 딱 붙이면 구름과 방 사이에 까만 띠가 뜬다. 내려서 덩이가 벽에 닿게 한다.
        ///   가운데(문 자리)는 일부러 덜 늘어뜨린 그림이라 문은 가리지 않는다.
        /// </summary>
        private const float RoomCloudDrop = 56f;

        /// <summary>
        /// 카메라가 방 위로 올려다볼 수 있는 한계(px). 구름 윗변(640 − 56 = 584)보다 조금 작게 —
        /// 끝까지 올라가면 구름 윗변이 드러난다. 가장 긴 화면(20:9 문 앞)이 467 을 쓴다.
        /// </summary>
        private const float RoomCloudReach = 560f;

        private const string RoomCloudPrefix = "roomfloor/roomcloud_";
        private const string RoomCloudCommon = "roomfloor/roomcloud";

        private RectTransform _cloud;
        private Image _cloudImage;
        private string _cloudEnv = string.Empty;
        private string _cloudHeld;

        /// <summary>무대 이름으로 방 위 구름을 건다. 방 바닥을 까는 자리(`ApplyRoomFloor`)에서 부른다.</summary>
        private void ApplyRoomCloud(string env, int chapter)
        {
            if (string.IsNullOrEmpty(env)) env = EnvOfChapter(chapter);

            EnsureRoomCloud();
            if (_cloud == null) return;

            if (env == _cloudEnv) return;
            _cloudEnv = env;
            LoadRoomCloudAsync(env).Forget();   // fire-and-forget: 구름은 한 프레임 늦어도 된다
        }

        private void EnsureRoomCloud()
        {
            if (_cloud != null || _field == null) return;

            var go = new GameObject("RoomCloud", typeof(RectTransform), typeof(Image));
            _cloud = (RectTransform)go.transform;
            _cloud.SetParent(_field, false);
            // 글자 레이어 **다음** — 방 밖에 걸친 탄·숫자까지 덮는다.
            _cloud.SetSiblingIndex(_textLayer != null ? _textLayer.GetSiblingIndex() + 1 : _field.childCount - 1);
            // 아랫변이 방 윗변(방 좌표 y = 0)에 붙는다. 자리는 `ApplyScroll` 이 옮긴다.
            _cloud.anchorMin = _cloud.anchorMax = new Vector2(0f, 1f);
            _cloud.pivot = new Vector2(0f, 0f);
            _cloud.sizeDelta = RoomCloudSize;

            _cloudImage = go.GetComponent<Image>();
            _cloudImage.raycastTarget = false;   // 조작을 가로채면 안 된다
            _cloudImage.enabled = false;         // 그림이 올 때까지 비워 둔다
            ApplyScroll();
        }

        private async UniTaskVoid LoadRoomCloudAsync(string env)
        {
            var address = RoomCloudPrefix + env;
            Sprite art = null;
            try { art = await CoreModule.Get<IResourceManager>().LoadAsync<Sprite>(address); }
            catch (Exception) { /* 전용이 없는 무대다 — 보통은 이쪽 */ }
            if (art == null)
            {
                address = RoomCloudCommon;
                try { art = await CoreModule.Get<IResourceManager>().LoadAsync<Sprite>(address); }
                catch (Exception) { /* 공용도 아직 없다. 비워 둔다. */ }
            }

            if (_cloudEnv != env || _cloudImage == null)
            {
                if (art != null && address != _cloudHeld)
                    CoreModule.Get<IResourceManager>().Release(address);
                return;
            }

            _cloudImage.sprite = art;
            _cloudImage.enabled = art != null;

            var old = _cloudHeld;
            _cloudHeld = art != null ? address : null;
            if (old != null && old != _cloudHeld) CoreModule.Get<IResourceManager>().Release(old);
        }
    }
}
