using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary>
    /// 범용 UI 검증 하네스 실행기 — 프로필(<see cref="IUiHarnessProfile"/>)의 상태를 차례로 만들고, 상태마다 해상도를 바꿔 가며
    /// 찍고 <see cref="UiLayoutChecker"/> 로 잰다. 결과는 <c>Temp/UiHarness/&lt;프로필&gt;_&lt;시각&gt;/</c>
    /// (캡처 · report.json · report.md). 비교판은 <c>Tools/uiharness/contact_sheet.py</c>.
    ///
    /// <para>못 도달한 상태는 찍지 않고 실패로 남긴다 — 엉뚱한 화면을 찍고 통과하는 일을 막는다.</para>
    /// <para>한계: 실시간 시계(연출·회복 타이머)는 고정하지 않는다 — 찍기 전에 안정 대기만 한다. 저장 데이터는 실제 것을 쓴다.</para>
    /// </summary>
    [InitializeOnLoad]
    public static class UiHarnessRunner
    {
        private const string PendingKey = "uiharness.pending";

        public static string LastOutput { get; private set; }
        public static bool IsRunning { get; private set; }

        static UiHarnessRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary> 플레이 중이 아니면 프로필의 시작 씬을 열고 플레이에 들어간 뒤 돈다. </summary>
        public static void Run(IUiHarnessProfile profile)
        {
            if (EditorApplication.isPlaying)
            {
                Start(profile);
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(profile.BootScene);
            SessionState.SetString(PendingKey, profile.GetType().AssemblyQualifiedName);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            string pending = SessionState.GetString(PendingKey, string.Empty);
            if (change != PlayModeStateChange.EnteredPlayMode || pending.Length == 0) return;

            SessionState.SetString(PendingKey, string.Empty);
            var type = Type.GetType(pending);
            if (type == null) { Debug.LogError("[UiHarness] 프로필을 찾지 못했다: " + pending); return; }
            Start((IUiHarnessProfile)Activator.CreateInstance(type));
        }

        private static void Start(IUiHarnessProfile profile)
        {
            if (IsRunning) return;

            RunAsync(profile).Forget();   // fire-and-forget: 플레이 모드 안에서 끝까지 돈다 — 결과는 파일, 실패는 콘솔에 남는다
        }

        private static async UniTaskVoid RunAsync(IUiHarnessProfile profile)
        {
            IsRunning = true;
            string output = Path.Combine("Temp", "UiHarness", profile.Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(output);
            LastOutput = output;
            var report = new Report(profile, output);
            var driver = new UiHarnessDriver();
            int originalSize = GameViewSize.Current;

            try
            {
                await profile.GotoStart(driver);
                foreach (var scenario in profile.Scenarios)
                {
                    var state = new StateResult { Name = scenario.Name, Mock = scenario.Mock };
                    report.States.Add(state);
                    try
                    {
                        await scenario.Enter(driver);
                        state.Error = scenario.Expect?.Invoke();
                    }
                    catch (Exception e)
                    {
                        state.Error = e.Message;
                    }

                    if (state.Error != null)
                    {
                        Debug.LogWarning($"[UiHarness] {scenario.Name} 에 도달하지 못했다 — {state.Error}");
                        GameViewSize.SelectIndex(originalSize);
                        await profile.GotoStart(driver);
                        continue;
                    }

                    foreach (var (viewName, tag) in profile.Resolutions)
                    {
                        if (!GameViewSize.Select(viewName)) { state.Error = "게임 뷰 크기가 등록돼 있지 않다: " + viewName; break; }
                        await UiHarnessDriver.Frames(4);
                        scenario.Settle?.Invoke(driver);
                        await UiHarnessDriver.Frames(2);
                        Canvas.ForceUpdateCanvases();

                        var shot = new ShotResult { Resolution = tag, Width = Screen.width, Height = Screen.height, File = $"{scenario.Name}_{tag}.png" };
                        var root = profile.Root();
                        if (root == null)
                        {
                            // 빈 위반 목록으로 통과 처리하지 않는다 — 캡처도 하지 않고 상태 오류로 남긴다
                            state.Error = "검사할 UI 뿌리가 없다 (" + tag + ")";
                            continue;
                        }

                        // 해상도를 바꾸면 화면이 다시 짜인다 — 그 해상도에서도 같은 상태인지 다시 확인한다
                        try { shot.Unreached = scenario.Expect?.Invoke(); }
                        catch (Exception e) { shot.Unreached = e.Message; }

                        if (shot.Unreached == null) shot.Violations = UiLayoutChecker.Check(root, profile.Rules);
                        ScreenCapture.CaptureScreenshot(Path.Combine(output, shot.File));
                        state.Shots.Add(shot);
                        await UiHarnessDriver.Frames(3);
                    }

                    GameViewSize.SelectIndex(originalSize);
                    await UiHarnessDriver.Frames(4);
                    try
                    {
                        if (scenario.Leave != null) await scenario.Leave(driver);
                        else await profile.GotoStart(driver);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[UiHarness] {scenario.Name} 에서 나오지 못했다 — {e.Message} · 시작 화면부터 다시");
                        await profile.GotoStart(driver);
                    }
                }
            }
            catch (Exception e)
            {
                report.Error = e.ToString();
                Debug.LogException(e);
            }
            finally
            {
                GameViewSize.SelectIndex(originalSize);
                await UiHarnessDriver.Frames(3);
                report.Write();
                IsRunning = false;
                Debug.Log($"[UiHarness] 끝 — {output} · 실패 {report.Count(UiSeverity.Fail)} · 경고 {report.Count(UiSeverity.Warn)} · 못 간 상태 {report.Unreached}");
            }
        }

        // ─────────────────────────────────────────
        // 결과
        // ─────────────────────────────────────────

        private sealed class ShotResult
        {
            public string Resolution;
            public int Width;
            public int Height;
            public string File;
            public string Unreached;   // 이 해상도에서 상태가 유지되지 않았다 — 도달 실패
            public List<UiLayoutChecker.Violation> Violations = new List<UiLayoutChecker.Violation>();
        }

        private sealed class StateResult
        {
            public string Name;
            public string Mock;
            public string Error;
            public readonly List<ShotResult> Shots = new List<ShotResult>();
        }

        private sealed class Report
        {
            private readonly IUiHarnessProfile _profile;
            private readonly string _output;
            public string Error;
            public readonly List<StateResult> States = new List<StateResult>();

            public Report(IUiHarnessProfile profile, string output)
            {
                _profile = profile;
                _output = output;
            }

            public int Unreached
            {
                get
                {
                    int count = 0;
                    foreach (var s in States) if (s.Error != null) count++;
                    return count;
                }
            }

            public int Count(UiSeverity severity)
            {
                int count = 0;
                foreach (var s in States)
                    foreach (var shot in s.Shots)
                        foreach (var v in shot.Violations)
                            if (v.Severity == severity) count++;
                return count;
            }

            /// <summary> 통과 판정 — 실패 0 + 도달 실패 0 + 실행 오류 없음 + 모든 상태·해상도 샷 존재일 때만 통과. </summary>
            private string Verdict()
            {
                var reasons = new List<string>();
                if (Error != null) reasons.Add("실행 오류");
                int fails = Count(UiSeverity.Fail);
                if (fails > 0) reasons.Add($"실패 {fails}건");
                int unreached = Unreached;
                int shots = 0;
                foreach (var s in States)
                {
                    shots += s.Shots.Count;
                    foreach (var shot in s.Shots) if (shot.Unreached != null) unreached++;
                }

                if (unreached > 0) reasons.Add($"도달 실패 {unreached}건");
                int expected = _profile.Scenarios.Count * _profile.Resolutions.Count;
                if (shots != expected) reasons.Add($"샷 누락 ({shots}/{expected})");
                return reasons.Count == 0 ? "**기계 검사 통과**" : "**기계 검사 실패: " + string.Join(" · ", reasons) + "**";
            }

            public void Write()
            {
                File.WriteAllText(Path.Combine(_output, "report.json"), ToJson(), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(_output, "report.md"), ToMarkdown(), new UTF8Encoding(false));
            }

            private string ToMarkdown()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# UI 하네스 — {_profile.Name}");
                sb.AppendLine();
                sb.AppendLine(Verdict());
                sb.AppendLine();
                sb.AppendLine($"폴더 `{_output}` · 상태 {States.Count}(못 감 {Unreached}) · **실패 {Count(UiSeverity.Fail)}** · 경고 {Count(UiSeverity.Warn)}");
                if (Error != null) sb.AppendLine().AppendLine("**실행 오류**: " + Error);
                sb.AppendLine();
                sb.AppendLine("규칙: C1 판 밖(C1? 추정 판) · C2 마스크에 잘림 · C3 화면 밖 · C4 글자 넘침 · C5 글자 겹침 · R1 비율 찌그러짐 · R2 9분할 없음 · R3 확대 흐림 · R4 원본 과대");
                sb.AppendLine();
                foreach (var severity in new[] { UiSeverity.Fail, UiSeverity.Warn })
                {
                    sb.AppendLine(severity == UiSeverity.Fail ? "## 실패" : "## 경고");
                    sb.AppendLine();
                    sb.AppendLine("| 상태 | 해상도 | 규칙 | 부품 | 기준 | 넘침 | 메모 |");
                    sb.AppendLine("|---|---|---|---|---|---|---|");
                    foreach (var s in States)
                    {
                        if (severity == UiSeverity.Fail && s.Error != null) sb.AppendLine($"| {s.Name} | - | 도달 실패 | {s.Error} | | | |");
                        if (severity == UiSeverity.Fail)
                            foreach (var shot in s.Shots)
                                if (shot.Unreached != null) sb.AppendLine($"| {s.Name} | {shot.Resolution} | 도달 실패 | {shot.Unreached} | | | |");
                        foreach (var shot in s.Shots)
                            foreach (var v in shot.Violations)
                                if (v.Severity == severity && v.Rule != "R3" && v.Rule != "R4")
                                    sb.AppendLine($"| {s.Name} | {shot.Resolution} | {v.Rule} | `{v.Path}` | `{v.Against}` | {v.Over:0.#} | {v.Note} |");
                    }

                    sb.AppendLine();
                }

                // 리소스 경고는 그림 하나에 상태·해상도마다 쌓이므로 그림별로 묶는다(가장 큰 배율).
                sb.AppendLine("## 리소스 경고 (그림별)");
                sb.AppendLine();
                sb.AppendLine("| 규칙 | 그림 | 최대 배율 | 예 | 메모 |");
                sb.AppendLine("|---|---|---|---|---|");
                var worst = new Dictionary<string, UiLayoutChecker.Violation>();
                foreach (var s in States)
                    foreach (var shot in s.Shots)
                        foreach (var v in shot.Violations)
                        {
                            if (v.Rule != "R3" && v.Rule != "R4") continue;
                            string key = v.Rule + "|" + v.Against;
                            if (!worst.TryGetValue(key, out var w) || v.Over > w.Over) worst[key] = v;
                        }

                foreach (var v in worst.Values)
                    sb.AppendLine($"| {v.Rule} | `{v.Against}` | {v.Over:0.0} | `{v.Path}` | {v.Note} |");
                sb.AppendLine();

                sb.AppendLine("## 예외 적용");
                sb.AppendLine();
                sb.AppendLine("| 규칙 | 경로 | 이유 | 적용 횟수 |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var a in _profile.Rules.Allowed)
                    sb.AppendLine($"| {a.Rule} | `{a.Path}` | {a.Why} | {(a.Hits == 0 ? "**0 — 안 쓰임**" : a.Hits.ToString())} |");

                if (_profile.Rules.Modals.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## 모달 규칙");
                    sb.AppendLine();
                    sb.AppendLine("모달이 열린 검사에서는 모달 밖 화면의 레이아웃 검사(C1~C6)를 하지 않는다(리소스 검사는 한다).");
                    sb.AppendLine();
                    sb.AppendLine("| 경로 | 적용 횟수 |");
                    sb.AppendLine("|---|---|");
                    foreach (var m in _profile.Rules.Modals)
                        sb.AppendLine($"| `{m.Path}` | {(m.Hits == 0 ? "**0 — 안 열림**" : m.Hits.ToString())} |");
                }

                return sb.ToString();
            }

            private string ToJson()
            {
                var sb = new StringBuilder();
                sb.Append("{\"profile\":").Append(Str(_profile.Name)).Append(",\"output\":").Append(Str(_output))
                  .Append(",\"mockFolder\":").Append(Str(_profile.MockFolder)).Append(",\"error\":").Append(Str(Error)).Append(",\"states\":[");
                for (int i = 0; i < States.Count; i++)
                {
                    var s = States[i];
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"name\":").Append(Str(s.Name)).Append(",\"mock\":").Append(Str(s.Mock)).Append(",\"error\":").Append(Str(s.Error)).Append(",\"shots\":[");
                    for (int j = 0; j < s.Shots.Count; j++)
                    {
                        var shot = s.Shots[j];
                        if (j > 0) sb.Append(',');
                        sb.Append("{\"resolution\":").Append(Str(shot.Resolution)).Append(",\"width\":").Append(shot.Width).Append(",\"height\":").Append(shot.Height)
                          .Append(",\"file\":").Append(Str(shot.File)).Append(",\"unreached\":").Append(Str(shot.Unreached)).Append(",\"violations\":[");
                        for (int k = 0; k < shot.Violations.Count; k++)
                        {
                            var v = shot.Violations[k];
                            if (k > 0) sb.Append(',');
                            sb.Append("{\"rule\":").Append(Str(v.Rule)).Append(",\"severity\":").Append(Str(v.Severity.ToString()))
                              .Append(",\"path\":").Append(Str(v.Path)).Append(",\"against\":").Append(Str(v.Against)).Append(",\"over\":").Append(F(v.Over))
                              .Append(",\"note\":").Append(Str(v.Note)).Append(",\"box\":").Append(RectJson(v.Box)).Append(",\"limit\":").Append(RectJson(v.Limit)).Append('}');
                        }

                        sb.Append("]}");
                    }

                    sb.Append("]}");
                }

                sb.Append("],\"allowed\":[");
                for (int i = 0; i < _profile.Rules.Allowed.Count; i++)
                {
                    var a = _profile.Rules.Allowed[i];
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"rule\":").Append(Str(a.Rule)).Append(",\"path\":").Append(Str(a.Path.ToString())).Append(",\"why\":").Append(Str(a.Why))
                      .Append(",\"hits\":").Append(a.Hits).Append('}');
                }

                sb.Append("],\"modals\":[");
                for (int i = 0; i < _profile.Rules.Modals.Count; i++)
                {
                    var m = _profile.Rules.Modals[i];
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"path\":").Append(Str(m.Path.ToString())).Append(",\"hits\":").Append(m.Hits).Append('}');
                }

                sb.Append("]}");
                return sb.ToString();
            }

            private static string RectJson(Rect r) => $"[{F(r.xMin)},{F(r.yMin)},{F(r.xMax)},{F(r.yMax)}]";

            private static string F(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);

            private static string Str(string s)
            {
                if (s == null) return "null";
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
            }
        }

        // ─────────────────────────────────────────
        // 게임 뷰 크기 — 에디터 내부 API(리플렉션). 이름으로 고른다.
        // ─────────────────────────────────────────

        private static class GameViewSize
        {
            private const System.Reflection.BindingFlags Any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                                                              System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            private static Type ViewType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");

            private static EditorWindow View => EditorWindow.GetWindow(ViewType, false, null, false);

            public static int Current => (int)ViewType.GetProperty("selectedSizeIndex", Any).GetValue(View);

            public static bool Select(string name)
            {
                int index = IndexOf(name);
                if (index < 0) return false;
                SelectIndex(index);
                return true;
            }

            public static void SelectIndex(int index) => ViewType.GetMethod("SizeSelectionCallback", Any).Invoke(View, new object[] { index, null });

            private static int IndexOf(string name)
            {
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
                var groupType = sizesType.GetProperty("currentGroupType", Any).GetValue(instance);
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
                var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
                for (int i = 0; i < texts.Length; i++)
                    if (texts[i] == name || texts[i].StartsWith(name + " ", StringComparison.Ordinal)) return i;
                return -1;
            }
        }
    }
}
