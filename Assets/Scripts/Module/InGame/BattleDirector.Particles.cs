using System;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 파티클 배선 (2026-09-20).
    ///
    /// 뿌리개(<see cref="ParticleFxPool"/>)를 **방 안에** 달고, 재질 넉 장을 주소로 받아 꽂는다.
    /// 방 안에 달아야 좌표가 방 좌표 그대로이고 화면이 흔들릴 때 알갱이도 같이 흔들린다.
    ///
    /// ⚠ 재질이 없으면(주소 로드 실패) 뿌리개는 **기본 재질**로 그냥 돈다 —
    ///   흰 네모가 튀지만 판이 멈추지는 않는다. 그림 하나 때문에 전투를 못 하면 안 된다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private ParticleFxPool _pfx;

        /// <summary>
        /// 알갱이 층. **다른 방 레이어와 똑같이 <see cref="Place"/> 로 움직인다** —
        /// 스크롤·줌·흔들림을 한 자로 재지 않으면 알갱이만 제자리에 남는다.
        /// (처음에 앵커 값을 한 번 베껴 놓았더니, 방이 스크롤하는 순간 유령이 서 있는 자리와
        ///  알갱이가 터지는 자리가 화면 한 칸만큼 어긋났다 — 2026-09-20)
        /// </summary>
        private RectTransform _pfxLayer;

        /// <summary>종류 ↔ 주소. `CreateParticleFx` 에디터 툴이 같은 이름으로 만든다.</summary>
        private static readonly (ParticleFxKind Kind, string Address)[] ParticleArt =
        {
            (ParticleFxKind.Spark, "ParticleFx/spark"),
            (ParticleFxKind.Smoke, "ParticleFx/smoke"),
            (ParticleFxKind.Glow,  "ParticleFx/glow"),
            (ParticleFxKind.Shard, "ParticleFx/shard"),
        };

        private void MakeParticleLayer()
        {
            if (_pfx != null || _unitLayer == null) return;

            var go = new GameObject("ParticleLayer", typeof(RectTransform));
            go.transform.SetParent(_unitLayer.parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = _unitLayer.anchorMin;
            rt.anchorMax = _unitLayer.anchorMax;
            rt.pivot = _unitLayer.pivot;
            rt.sizeDelta = _unitLayer.sizeDelta;
            _pfxLayer = rt;                    // 자리는 ApplyScroll 이 매 프레임 잡는다

            _pfx = go.AddComponent<ParticleFxPool>();
            _pfx.SetSortingOrder(InGameMainUI.ParticleOrder);
        }

        private async UniTask LoadParticleArtAsync(IResourceManager res)
        {
            if (_pfx == null) return;
            foreach (var (kind, address) in ParticleArt)
            {
                try { _pfx.SetMaterial(kind, await res.LoadAsync<Material>(address)); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Battle] 파티클 재질 없음({address}) — 기본 재질로 나온다. {e.Message}");
                }
            }
        }
    }
}
