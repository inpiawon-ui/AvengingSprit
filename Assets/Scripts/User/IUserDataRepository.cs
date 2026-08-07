using Cysharp.Threading.Tasks;

namespace Game.User
{
    /// <summary>
    /// 유저 데이터 저장소 계약.
    ///
    /// 게임 로직은 **반드시 이 인터페이스만 경유**한다. 로컬 파일을 직접 읽지 않는다.
    /// 서버 전환 시 구현체만 교체하면 되도록 하기 위함이다 (constants.md 6절 제약 1·2).
    /// </summary>
    public interface IUserDataRepository
    {
        UniTask<UserData> LoadAsync();
        UniTask<bool> SaveAsync(UserData data);
    }
}
