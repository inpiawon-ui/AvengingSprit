using GameFramework.Core.Module.Network;
using UnityEngine;

namespace Game.Module.Common
{
    [CreateAssetMenu(menuName = "Game/NetworkConfig", fileName = "NetworkConfig")]
    public sealed class NetworkConfig : ScriptableObject, INetworkSettings
    {
        [SerializeField] private bool   _useLocal = true;
        [SerializeField] private string _baseUrl  = "";

        public bool   UseLocal => _useLocal;
        public string BaseUrl  => _baseUrl;
    }
}
