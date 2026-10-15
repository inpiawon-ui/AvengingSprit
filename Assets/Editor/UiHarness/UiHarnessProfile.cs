using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary> 하네스가 찍을 화면 상태 하나 — 들어가는 법 · 도달 확인 · 나오는 법. </summary>
    public sealed class UiHarnessScenario
    {
        public string Name;
        public string Mock;                                  // 비교판에 붙일 시안 파일 이름(확장자 없이) — 없으면 null
        public Func<UiHarnessDriver, UniTask> Enter;         // 시작 화면에서 이 상태로
        public Func<string> Expect;                          // 도달했는가 — 문제가 없으면 null, 아니면 이유(못 왔으면 찍지 않고 실패로 남긴다)
        public Action<UiHarnessDriver> Settle;               // 해상도를 바꾼 뒤 다시 세울 것(누름 유지 등)
        public Func<UiHarnessDriver, UniTask> Leave;         // 이 상태에서 시작 화면으로
    }

    /// <summary>
    /// 게임마다 하나 — 하네스가 무엇을 어떻게 찍을지. 범용 실행기(<see cref="UiHarnessRunner"/>)는 이것만 본다.
    /// 파라미터 없는 생성자가 있어야 한다(플레이 진입 뒤 다시 만든다).
    /// </summary>
    public interface IUiHarnessProfile
    {
        string Name { get; }

        /// <summary> 플레이를 시작할 씬(에셋 경로). </summary>
        string BootScene { get; }

        /// <summary> 결과·비교판 폴더 이름에 쓰는 시안 폴더(리포 기준) — 없으면 null. </summary>
        string MockFolder { get; }

        /// <summary> 게임 뷰 크기 이름(에디터에 등록된 것) · 파일 꼬리표. </summary>
        IReadOnlyList<(string ViewName, string Tag)> Resolutions { get; }

        IReadOnlyList<UiHarnessScenario> Scenarios { get; }

        UiHarnessRules Rules { get; }

        /// <summary> 검사할 UI 뿌리(캔버스). 없으면 null. </summary>
        Transform Root();

        /// <summary> 첫 시작 화면까지 간다(씬 로드·준비 대기). 상태마다 실패하면 다시 부른다. </summary>
        UniTask GotoStart(UiHarnessDriver driver);
    }
}
