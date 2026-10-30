using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 방 표 읽기 — 이번 챕터 것만(PD 2026-10-30 최적화 · `RoomTable` 머리 주석).
    ///
    ///   본편   이번 챕터 표 한 장
    ///   모드   보스 방 모음 + 시작 방이 든 챕터 표(탑 층 · 밤 무대) — 모드 표를 읽은 뒤에 고른다
    ///
    /// 읽은 표는 판 안의 표 하나(`_rooms`)에 합친다 — 방을 찾는 코드는 그대로 `_rooms.Get` 을 쓴다.
    /// 합친 표는 이 판이 만든 것이라 판이 끝나면 지운다(읽어 온 표 자체는 리소스 관리자 몫).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private async UniTask LoadChapterRoomsAsync(IResourceManager res, int chapter)
        {
            if (chapter <= 0 || (_rooms != null && _rooms.FirstOf(chapter) != null)) return;
            try { MergeRooms(await res.LoadAsync<RoomTable>(RoomTable.ChapterAddress(chapter))); }
            catch (Exception e) { Debug.LogWarning($"[Battle] {chapter}챕터 방 표 없음 — 절차적 생성으로 간다. {e.Message}"); }
        }

        /// <summary>모드 판 — 표를 읽고 층 · 무대를 정한 뒤에 부른다.</summary>
        private async UniTask LoadModeRoomsAsync(IResourceManager res)
        {
            if (!IsModeRun) return;
            try { MergeRooms(await res.LoadAsync<RoomTable>(RoomTable.BossesAddress)); }
            catch (Exception e) { Debug.LogWarning($"[Battle] 보스 방 모음 없음 — {e.Message}"); }
            // 시작 방(탑 보스 층은 위에서 읽은 보스 방이 된다)이 든 챕터 — 보스 방이면 이미 있어도 그 챕터 길이 필요 없다
            string start = ModeStartRoom();
            if (_rooms == null || _rooms.Get(start) == null)
                await LoadChapterRoomsAsync(res, RoomTable.ChapterOfRoomId(start));
        }

        private void MergeRooms(RoomTable table)
        {
            if (table == null) return;
            if (_rooms == null)
            {
                _rooms = ScriptableObject.CreateInstance<RoomTable>();
                _rooms.name = "RunRooms";
            }
            _rooms.AddRooms(table.Rooms, table.ContractVersion);
        }

#if UNITY_EDITOR
        /// <summary>에디터 전용 — 원본 통째 표(편집용, 주소 없음)를 합친다. 방 촬영 도구가 챕터를 건너뛸 때.</summary>
        private void EditorMergeAllRooms()
            => MergeRooms(UnityEditor.AssetDatabase.LoadAssetAtPath<RoomTable>("Assets/BundleResource/TableData/RoomTable.asset"));
#endif

        private void OnDestroy()
        {
            if (_rooms != null) Destroy(_rooms);
            _rooms = null;
        }
    }
}
