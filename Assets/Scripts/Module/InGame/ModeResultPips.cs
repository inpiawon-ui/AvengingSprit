using Cysharp.Threading.Tasks;
using Game.Module.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>탑 · 밤 결과 창이 같이 쓰는 것 — 입장 점(켜짐 · 꺼짐 그림은 빌더가 켜짐으로 세운다) · 등장 곡선.</summary>
    internal static class ModeResultPips
    {
        private static readonly Color Off = new(1f, 1f, 1f, 0.22f);

        /// <summary>남은 입장 수만큼 점을 켠다 — 꺼진 점은 흐리게(그림 하나로).</summary>
        public static void Set(UIBinder ui, string prefix, int left)
        {
            for (int i = 0; i < 3; i++)
            {
                var img = ui.Get<Image>($"{prefix}{i}");
                if (img != null) img.color = i < left ? Color.white : Off;
            }
        }

        /// <summary>창이 0.86 → 1 로 OutBack — 챕터 결과 창과 같은 등장.</summary>
        public static async UniTaskVoid PopAsync(Transform content, MonoBehaviour owner)
        {
            var token = owner.GetCancellationTokenOnDestroy();
            const float seconds = 0.38f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (content == null) return;
                content.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, Ease.OutBack(t / seconds));
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (content != null) content.localScale = Vector3.one;
        }
    }
}
