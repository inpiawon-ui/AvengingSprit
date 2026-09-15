using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Game.Module.InGame
{
    /// <summary>
    /// **있으면 쓰고 없으면 만다** 식으로 그림을 무는 자리 (2026-09-15).
    ///
    /// ── 왜 필요한가 ─────────────────────────────────────────
    /// 방 배경은 층층이 선택적이다 — 무대 전용이 있으면 그것, 없으면 공용, 그것도 없으면 빈칸.
    /// 「없는 게 정상」인 주소를 그냥 물면 Addressables 가 `InvalidKeyException` 을 **콘솔에 찍는다.**
    /// try/catch 로는 막을 수 없다. 던지는 자리가 우리 쪽이 아니라 Addressables 의 지연 콜백이라
    /// 우리가 잡기 전에 이미 로그가 나간다. 실제로 방을 넘길 때마다
    /// 「No Location found for Key=roomfloor/roomcloud_junkyard」 가 쌓였다.
    ///
    /// ── 어떻게 막나 ─────────────────────────────────────────
    /// **물기 전에 자리부터 묻는다.** `LoadResourceLocationsAsync` 는 없는 키에도 던지지 않고
    /// 빈 목록을 준다. 비어 있으면 로드를 아예 시작하지 않으므로 찍힐 예외도 없다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>
        /// 없을 수도 있는 주소를 조용히 문다. 없으면 `null` — 콘솔에 아무것도 남기지 않는다.
        /// 「반드시 있어야 하는」 리소스에는 쓰지 마라. 그건 없을 때 시끄러워야 맞다.
        /// </summary>
        private static async UniTask<T> LoadOptionalAsync<T>(string address) where T : Object
        {
            if (string.IsNullOrEmpty(address)) return null;

            var probe = Addressables.LoadResourceLocationsAsync(address, typeof(T));
            await probe.ToUniTask();
            bool found = probe.Status == AsyncOperationStatus.Succeeded
                      && probe.Result != null && probe.Result.Count > 0;
            Addressables.Release(probe);
            if (!found) return null;

            return await CoreModule.Get<IResourceManager>().LoadAsync<T>(address);
        }
    }
}
