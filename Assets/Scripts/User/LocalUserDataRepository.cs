using Cysharp.Threading.Tasks;
using GameFramework.Core.Module.Data;
using UnityEngine;

namespace Game.User
{
    /// <summary>
    /// 로컬 저장 구현체. Core `IDataManager` 를 통해 영속화한다.
    ///
    /// 서버 전환 시 이 클래스 대신 `ServerUserDataRepository` 를 등록하면 된다.
    /// 게임 로직은 `IUserDataRepository` 만 보므로 코드 변경이 없다.
    /// </summary>
    public sealed class LocalUserDataRepository : IUserDataRepository
    {
        // ⚠ 0.1.15 에서 이름을 바꿨다 — 덮어 설치해도 **처음 상태로 시작**하게(2026-09-18 지시:
        //   젬 100만 · 상자 없음 · 챕터 1만). 예전 저장(`avsr_userdata`)은 읽지 않는다.
        //   다음에 또 전원 초기화가 필요하면 뒤 번호만 올린다.
        private const string SaveKey = "avsr_userdata_0115";

        private readonly IDataManager _data;

        public LocalUserDataRepository(IDataManager data) => _data = data;

        public async UniTask<UserData> LoadAsync()
        {
            if (!_data.Has(SaveKey))
                return UserData.CreateNew();

            var loaded = await _data.LoadAsync<UserData>(SaveKey);
            if (loaded == null)
                return UserData.CreateNew();

            // 스키마 버전이 낮으면 여기서 마이그레이션한다. 현재는 v1 뿐이라 통과.
            if (loaded.saveVersion < UserData.CurrentSaveVersion)
            {
                Debug.Log($"[UserData] 마이그레이션 v{loaded.saveVersion} → v{UserData.CurrentSaveVersion}");
                loaded.saveVersion = UserData.CurrentSaveVersion;
            }
            return loaded;
        }

        public UniTask<bool> SaveAsync(UserData data) => _data.SaveAsync(SaveKey, data);
    }
}
