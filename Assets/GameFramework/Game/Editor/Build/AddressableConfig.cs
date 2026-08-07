using UnityEngine;

namespace GameFramework.Game.Editor.Build
{
    /// <summary>
    /// Addressables CDN 업로드 설정 ScriptableObject.
    /// Assets/ConfigData/AddressableConfig.asset 경로에 생성하세요.
    ///
    /// 사용처: AddressableUploadHandler (rclone 업로드 시 참조)
    /// </summary>
    [CreateAssetMenu(fileName = "AddressableConfig", menuName = "Game/Addressable Config")]
    public class AddressableConfig : ScriptableObject
    {
        [Header("Cloudflare R2")]
        [Space(4)]

        [Tooltip("rclone remote 이름 (예: r2)")]
        [SerializeField] public string remoteName = "";

        [Tooltip("Cloudflare R2 버킷 이름 (예: my-game-cdn)")]
        [SerializeField] public string bucketName = "";
    }
}
