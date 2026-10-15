using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary> 위반 등급 — 실패는 보고 전에 반드시 고친다, 경고는 사람이 본다. </summary>
    public enum UiSeverity
    {
        Warn,
        Fail,
    }

    /// <summary>
    /// 게임마다 다른 검사 설정 — 명시한 판(담기 기준)과 예외. 게임 코드에는 손대지 않고 하네스 데이터로만 둔다.
    /// 경로는 검사 뿌리(캔버스) 아래 <c>이름/이름/…</c>.
    /// </summary>
    public sealed class UiHarnessRules
    {
        /// <summary> 명시한 판 — 이 판 아래 보이는 부품은 판 안쪽(테 두께 <see cref="Container.Inset"/>) 안에 들어야 한다(실패). </summary>
        public sealed class Container
        {
            public Regex Path;
            public float Inset;          // 판 높이 비율 — 금테 등 테 두께
            public string Why;
        }

        /// <summary> 예외 — 규칙 id(C1·C2…·R1…, «*» 는 전부) · 경로 · 이유. 쓰이지 않은 예외는 리포트에 남는다. </summary>
        public sealed class Allow
        {
            public string Rule;
            public Regex Path;
            public string Why;
            public int Hits;
        }

        /// <summary>
        /// 겹쳐 쌓는 화면 묶음 — 부모 아래 이름이 맞는 자식 중 «보이는» 것 가운데 맨 위(마지막) 하나만 검사한다.
        /// 뒤에 가려진 화면(켜져 있지만 위 화면에 덮인)을 검사하지 않게. 이름이 안 맞는 자식(토스트 등)은 그대로 검사한다.
        /// </summary>
        public readonly List<(Regex Parent, Regex Child)> Exclusive = new List<(Regex, Regex)>();

        public readonly List<Container> Containers = new List<Container>();

        public UiHarnessRules ExclusiveChildren(string parentRegex, string childRegex)
        {
            Exclusive.Add((new Regex(parentRegex), new Regex(childRegex)));
            return this;
        }
        public readonly List<Allow> Allowed = new List<Allow>();

        /// <summary> 모달 창 규칙 — 경로가 맞고 보이는 창이 있으면 그 창 밖의 화면은 그 상태에서 레이아웃 검사(C1~C6)를 하지 않는다. </summary>
        public sealed class ModalRule
        {
            public Regex Path;
            public int Hits;   // 모달이 실제로 열려 적용된 검사(상태×해상도) 횟수 — 0 이면 리포트에 «안 쓰임» 으로 남는다
        }

        public readonly List<ModalRule> Modals = new List<ModalRule>();

        /// <summary>
        /// 모달 창(화면을 덮는 어두운 판 + 창) 지정. 경로가 <paramref name="pathRegex"/> 에 맞고 보이는 요소가 있으면, 그 요소 하위가 아닌
        /// 모든 요소는 C1~C6 의 대상과 가리개에서 빠진다(모달 뒤 화면은 모달이 없는 다른 상태가 검사한다).
        /// 맞는 요소가 여럿이면 다른 맞는 요소의 자손은 버리고 가장 위에 그려지는(캔버스 정렬 → 형제 순서) 하나를 기준으로 한다.
        /// 리소스 검사(R1~R4)는 가려진 화면에도 그대로 한다 — 그림 자체의 결함이라 가림과 무관하고, 다른 상태에서 잡히지 않을 수 있다.
        /// </summary>
        public UiHarnessRules Modal(string pathRegex)
        {
            Modals.Add(new ModalRule { Path = new Regex(pathRegex) });
            return this;
        }

        public UiHarnessRules Contain(string pathRegex, float inset, string why)
        {
            Containers.Add(new Container { Path = new Regex(pathRegex), Inset = inset, Why = why });
            return this;
        }

        public UiHarnessRules AllowRule(string rule, string pathRegex, string why)
        {
            Allowed.Add(new Allow { Rule = rule, Path = new Regex(pathRegex), Why = why });
            return this;
        }

        internal bool IsAllowed(string rule, string path)
        {
            for (int i = 0; i < Allowed.Count; i++)
            {
                var a = Allowed[i];
                if ((a.Rule == "*" || a.Rule == rule) && a.Path.IsMatch(path))
                {
                    a.Hits++;
                    return true;
                }
            }

            return false;
        }
    }
}
