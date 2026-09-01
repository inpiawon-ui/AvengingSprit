using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 캐릭터별 **총구 위치**. 몸 중심 기준이고 캔버스 크기로 나눠 둬서
    /// 표시 크기가 달라도 따라간다. 순서는 <see cref="Unit.FacingSuffix"/> 와 같다.
    ///
    /// ⚠ **이 파일은 손으로 고치지 않는다.** `Tools/Game/총구 위치 다시 재기` 가 다시 쓴다.
    ///   그림이 바뀌면 그 메뉴를 돌리면 된다.
    /// </summary>
    public static class MuzzleTable
    {
        /// <summary>표에 없는 캐릭터가 쓰는 값. **몸 중심**이다.</summary>
        private static readonly Vector2[] Fallback =
        {
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
        };

        private static readonly Dictionary<string, Vector2[]> Table = new()
        {
            ["actor_enforcer"] = new Vector2[] { new(-0.003f, +0.065f), new(-0.021f, +0.002f), new(+0.013f, +0.089f), new(-0.064f, +0.067f), new(+0.005f, -0.168f) },
            ["amazon"] = new Vector2[] { new(+0.040f, -0.016f), new(+0.023f, -0.049f), new(+0.040f, -0.008f), new(+0.003f, -0.005f), new(-0.044f, -0.071f) },
            ["amazon_elite"] = new Vector2[] { new(+0.094f, +0.090f), new(+0.033f, +0.107f), new(+0.064f, +0.000f), new(+0.056f, -0.063f), new(+0.050f, +0.123f) },
            ["baseball"] = new Vector2[] { new(+0.159f, +0.141f), new(+0.271f, -0.045f), new(+0.276f, +0.095f), new(-0.170f, +0.237f), new(-0.204f, +0.185f) },
            ["bat"] = new Vector2[] { new(+0.010f, +0.206f), new(+0.000f, +0.073f), new(+0.090f, +0.056f), new(+0.094f, -0.028f), new(+0.250f, +0.161f) },
            ["coilwalker"] = new Vector2[] { new(-0.009f, -0.009f), new(+0.091f, +0.143f), new(+0.078f, +0.154f), new(+0.186f, +0.122f), new(+0.094f, +0.301f) },
            ["commando_grenade"] = new Vector2[] { new(-0.041f, +0.157f), new(-0.011f, +0.188f), new(-0.164f, +0.249f), new(+0.065f, +0.210f), new(-0.002f, +0.189f) },
            ["commando_laser"] = new Vector2[] { new(+0.046f, +0.121f), new(+0.129f, +0.152f), new(+0.194f, +0.128f), new(+0.201f, +0.109f), new(+0.237f, +0.161f) },
            ["commando_mg"] = new Vector2[] { new(+0.111f, +0.102f), new(+0.239f, +0.062f), new(+0.208f, +0.077f), new(+0.236f, +0.065f), new(+0.237f, +0.104f) },
            ["commando_missile"] = new Vector2[] { new(+0.003f, +0.093f), new(+0.005f, +0.084f), new(-0.054f, +0.122f), new(-0.022f, +0.141f), new(+0.028f, +0.143f) },
            ["death"] = new Vector2[] { new(+0.006f, +0.182f), new(-0.085f, +0.217f), new(-0.020f, +0.063f), new(+0.007f, +0.129f), new(+0.076f, +0.222f) },
            ["dragon_blue"] = new Vector2[] { new(-0.019f, +0.161f), new(-0.062f, +0.094f), new(+0.067f, +0.051f), new(+0.039f, +0.121f), new(+0.078f, +0.013f) },
            ["dragoon"] = new Vector2[] { new(+0.013f, -0.094f), new(+0.014f, -0.201f), new(+0.210f, -0.069f), new(+0.085f, -0.169f), new(+0.046f, -0.112f) },
            ["gangster"] = new Vector2[] { new(+0.181f, +0.029f), new(+0.107f, -0.019f), new(+0.113f, -0.008f), new(+0.128f, +0.031f), new(+0.192f, +0.023f) },
            ["ghost"] = new Vector2[] { new(+0.060f, +0.057f), new(-0.051f, -0.005f), new(+0.198f, +0.008f), new(+0.140f, -0.015f), new(+0.195f, +0.041f) },
            ["guru"] = new Vector2[] { new(+0.032f, +0.183f), new(-0.072f, +0.180f), new(-0.029f, +0.240f), new(-0.102f, +0.143f), new(-0.068f, +0.100f) },
            ["hopper"] = new Vector2[] { new(-0.037f, +0.153f), new(+0.234f, +0.130f), new(+0.233f, +0.153f), new(+0.273f, +0.169f), new(+0.305f, +0.198f) },
            ["hopper_smg"] = new Vector2[] { new(+0.085f, +0.111f), new(+0.094f, +0.155f), new(+0.281f, +0.149f), new(+0.132f, +0.182f), new(+0.092f, +0.167f) },
            ["medium"] = new Vector2[] { new(+0.031f, +0.020f), new(+0.070f, +0.006f), new(+0.068f, +0.038f), new(+0.052f, +0.042f), new(+0.018f, +0.028f) },
            ["ninja"] = new Vector2[] { new(-0.040f, +0.057f), new(+0.059f, +0.024f), new(+0.019f, -0.060f), new(+0.007f, -0.001f), new(+0.077f, +0.240f) },
            ["ninja_chain"] = new Vector2[] { new(+0.029f, +0.041f), new(+0.056f, +0.012f), new(+0.142f, +0.027f), new(+0.213f, +0.133f), new(+0.149f, +0.065f) },
            ["roadwarden"] = new Vector2[] { new(-0.006f, +0.072f), new(-0.044f, +0.007f), new(-0.047f, +0.194f), new(+0.032f, +0.248f), new(+0.133f, +0.273f) },
            ["robot"] = new Vector2[] { new(+0.123f, +0.097f), new(+0.033f, +0.001f), new(+0.044f, +0.049f), new(+0.151f, +0.026f), new(+0.422f, +0.063f) },
            ["salamander"] = new Vector2[] { new(+0.234f, +0.128f), new(+0.261f, +0.140f), new(+0.222f, +0.056f), new(+0.217f, +0.143f), new(+0.127f, +0.074f) },
            ["skeleton"] = new Vector2[] { new(+0.014f, +0.029f), new(-0.008f, -0.017f), new(+0.005f, +0.017f), new(-0.019f, +0.002f), new(-0.007f, -0.029f) },
            ["snowwoman"] = new Vector2[] { new(+0.099f, -0.064f), new(+0.100f, -0.053f), new(+0.057f, -0.051f), new(+0.110f, -0.029f), new(+0.033f, -0.037f) },
            ["thug"] = new Vector2[] { new(+0.132f, +0.051f), new(+0.196f, -0.014f), new(+0.173f, +0.068f), new(+0.216f, +0.008f), new(+0.290f, +0.074f) },
            ["vampire"] = new Vector2[] { new(-0.006f, +0.075f), new(+0.078f, +0.128f), new(+0.135f, +0.110f), new(+0.135f, +0.170f), new(-0.009f, +0.088f) },
            ["white_wizard"] = new Vector2[] { new(+0.082f, -0.009f), new(+0.008f, -0.042f), new(+0.012f, -0.054f), new(-0.005f, -0.011f), new(+0.096f, +0.019f) },
        };

        /// <summary>
        /// <paramref name="key"/> 의 <paramref name="facingIndex"/> 방향 총구 오프셋.
        /// 몸 중심 기준 비율이다 — 부르는 쪽이 표시 크기를 곱한다.
        /// </summary>
        public static Vector2 Get(string key, int facingIndex)
        {
            if (facingIndex < 0 || facingIndex >= Unit.FacingSuffix.Length) return Vector2.zero;
            if (key != null && Table.TryGetValue(key, out var set)) return set[facingIndex];
            return Fallback[facingIndex];
        }
    }
}
