namespace GameFramework.Core.Module.ObjectPool
{
    /// <summary>
    /// 제네릭 ObjectPool&lt;T&gt;를 타입 소거(type-erasure)로 다루기 위한 비제네릭 인터페이스.
    /// ObjectPoolManager가 ClearAll에서 리플렉션 없이 일괄 해제하는 데 사용한다.
    /// </summary>
    internal interface IObjectPool
    {
        void Clear();
    }
}
