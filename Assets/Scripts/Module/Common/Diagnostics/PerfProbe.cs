#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace Game.Module.Common.Diagnostics
{
    /// <summary>
    /// 에디터 플레이 중 메모리 · 프레임 튐을 재는 측정기(2026-10-30 메모리 최적화 점검). `UNITY_EDITOR` 로 감싸 빌드에 안 들어간다.
    /// (에디터 폴더에 두면 오브젝트에 붙일 수 없어 게임 쪽 폴더에 둔다.)
    /// 씬이 바뀔 때마다 그 순간 메모리를 적고, 50ms 넘는 프레임은 씬 · 시각 · 그 프레임 GC 할당과 함께 적는다.
    /// 쓰는 법: 플레이 중 <see cref="Attach"/> → 돌아다닌 뒤 <see cref="Report"/>.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        private const float SpikeMs = 50f;

        private static PerfProbe s_probe;
        private static readonly StringBuilder s_log = new();
        private static readonly List<string> s_spikes = new();

        private ProfilerRecorder _used, _tex, _gcFrame;
        private string _scene;
        // 측정기가 지워져도 기록은 남게 정적으로 둔다
        private static float s_start = -1f;
        private static int s_frames, s_over33;
        private static long s_gcTotal;

        public static void Attach()
        {
            if (s_probe != null) return;
            if (s_start < 0f)
            {
                s_log.Clear();
                s_spikes.Clear();
                s_start = Time.realtimeSinceStartup;
            }
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            s_probe = go.AddComponent<PerfProbe>();
        }

        /// <summary>새로 재기 시작 — 앞 기록을 지운다.</summary>
        public static void Reset()
        {
            s_start = -1f;
            s_frames = s_over33 = 0;
            s_gcTotal = 0;
            Attach();
        }

        public static string Report()
        {
            var sb = new StringBuilder(s_log.ToString());
            sb.AppendLine($"프레임 {s_frames} · 33ms 넘음 {s_over33} · GC 합계 {s_gcTotal / 1024 / 1024}MB");
            sb.AppendLine($"50ms 넘는 튐 {s_spikes.Count}번:");
            for (int i = 0; i < s_spikes.Count && i < 40; i++) sb.AppendLine(s_spikes[i]);
            return sb.ToString();
        }

        /// <summary>
        /// 측정 순서를 에디터 다음 틱에 시작한다 — 원격 실행(MCP) 안에서 만든 오브젝트는 호출이 끝나면 치워진다.
        /// 타이틀 → (로비 6초 → 인게임 20초) × 3 → 로비 6초. 로비로 돌아올 때마다 메모리가 늘면 누수다.
        /// </summary>
        public static void StartTour(int rounds = 3, float lobbyStay = 6f, float gameStay = 20f)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                Reset();
                s_probe.TourAsync(rounds, lobbyStay, gameStay).Forget();   // fire-and-forget: 끝나면 Report 로 읽는다
            };
        }

        public static bool TourDone { get; private set; }

        private async Cysharp.Threading.Tasks.UniTask TourAsync(int rounds, float lobbyStay, float gameStay)
        {
            TourDone = false;
            var sm = GameFramework.Core.Base.CoreModule.Get<GameFramework.Core.Module.Scene.ISceneManager>();
            async Cysharp.Threading.Tasks.UniTask Go(string scene, float stay)
            {
                while (sm.IsTransitioning) await Cysharp.Threading.Tasks.UniTask.Delay(200, true);
                await sm.LoadAsync(new GameFramework.Core.Module.Scene.SceneLoadRequest
                {
                    SceneName = scene, LoadingStyle = GameFramework.Core.Module.Scene.LoadingStyle.Overlay,
                });
                await Cysharp.Threading.Tasks.UniTask.Delay((int)(stay * 1000), true);
                Mark($"{scene} {stay:0}초 머문 뒤");
            }
            while (sm.IsTransitioning) await Cysharp.Threading.Tasks.UniTask.Delay(200, true);
            await Cysharp.Threading.Tasks.UniTask.Delay(3000, true);
            Mark("시작");
            for (int i = 0; i < rounds; i++)
            {
                await Go("LobbyScene", lobbyStay);
                await Go("GameScene", gameStay);
            }
            await Go("LobbyScene", lobbyStay);
            Mark("끝");
            TourDone = true;
        }

        /// <summary>지금 메모리를 이름표와 함께 적는다(씬 전환 · 시험 단계마다).</summary>
        public static void Mark(string label)
        {
            Attach();
            s_probe.Write(label);
        }

        private void OnEnable()
        {
            _used = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _tex = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            _gcFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDisable()
        {
            _used.Dispose();
            _tex.Dispose();
            _gcFrame.Dispose();
        }

        private void Update()
        {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene != _scene)
            {
                _scene = scene;
                Write("씬 → " + scene);
            }
            float ms = Time.unscaledDeltaTime * 1000f;
            long gc = _gcFrame.LastValue;
            s_frames++;
            s_gcTotal += gc;
            if (ms > 33f) s_over33++;
            if (ms > SpikeMs)
                s_spikes.Add($"  {Time.realtimeSinceStartup - s_start,6:0.0}초 {scene} {ms:0}ms · GC {gc / 1024}KB");
        }

        private void Write(string label)
        {
            s_log.AppendLine($"{Time.realtimeSinceStartup - s_start,6:0.0}초 {label} | 사용 {_used.LastValue / 1024 / 1024}MB · 텍스처 {_tex.LastValue / 1024 / 1024}MB");
        }
    }
}
#endif
