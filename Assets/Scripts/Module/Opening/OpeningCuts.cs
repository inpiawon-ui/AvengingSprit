using System;

namespace Game.Module.Opening
{
    /// <summary>
    /// 오프닝이 넘기는 컷 하나.
    ///
    /// 그림과 대사를 **한 줄에 같이** 둔다. 따로 두면 번호가 어긋났을 때
    /// 비명이 밤거리 컷에 붙는 식으로 조용히 틀어진다 — 실제로 발주 단계에서
    /// 한 번 어긋났고, 그래서 기획이 짝을 표로 못 박아 보냈다.
    /// </summary>
    [Serializable]
    public struct OpeningCut
    {
        /// <summary>Addressable 주소의 뒷부분. `cutscene/{Key}` 로 불러온다. 빈 값이면 검은 화면.</summary>
        public string Key;

        /// <summary>아래 글상자에 찍을 한 줄. 빈 값이면 글상자를 숨긴다.</summary>
        public string Line;

        /// <summary>
        /// 이 컷을 자동으로 넘기기까지의 시간. 0 이면 탭할 때까지 기다린다.
        ///
        /// 연속 프레임(발포 5장)은 탭을 기다리면 애니메이션이 아니라 슬라이드가 된다.
        /// </summary>
        public float AutoSeconds;

        /// <summary>
        /// 관 안에 유령을 얹는가. 켜면 <see cref="Energy"/> 만큼 진한 유령이
        /// 그림 위에서 네 프레임으로 돈다.
        /// </summary>
        public bool Ghost;

        /// <summary>남은 에너지(0~1). 유령의 진하기가 된다.</summary>
        public float Energy;

        /// <summary>
        /// 세피아 단색으로 깔 것인가. **회상이라는 뜻이다.**
        ///
        /// 원작 시작 컷신에서 납치 장면만 단색이다 — 지금 벌어지는 일이 아니라
        /// 노인이 들려주는 지난 일이기 때문이다. 색을 빼는 것이 곧 시제 표시다.
        ///
        /// ⚠ 이 한 칸 덕분에 **그림을 새로 안 받는다.** 같은 장면이 프롤로그에
        ///   이미 통과본으로 있고, 원작도 같은 그림을 색만 빼서 다시 쓴다.
        ///   따로 그리면 두 장이 서로 조금씩 달라지고, 그 차이는 아무도 못 잡는다.
        /// </summary>
        public bool Sepia;

        public bool HasArt => !string.IsNullOrEmpty(Key);
        public bool HasLine => !string.IsNullOrEmpty(Line);
        public bool IsAuto => AutoSeconds > 0f;
    }

    /// <summary>
    /// 오프닝 전체. **프롤로그 → 시작 컷신** 두 묶음이다.
    ///
    /// ── 왜 이 순서인가 ──────────────────────────────────────────
    /// 컷신은 장식이 아니라 **튜토리얼의 앞 두 걸음**이다.
    ///   1 프롤로그    왜 유령인가            ← 이 게임의 전제
    ///   2 시작 컷신   왜 싸우는가 · 에너지    ← 이 게임의 목표와 HP
    /// 「공격이 안 된다」를 먼저 겪게 하려면 그 전에 유령이 된 이유가 있어야 한다.
    ///
    /// ── 대사는 원작 그대로다 ────────────────────────────────────
    /// 「자네의 에너지는 한정돼 있네」 한 줄이 곧 고스트 HP 설명이다.
    /// 관이 파랑에서 초록으로 비는 것을 한 번 보여 주면 본편에서 게이지를
    /// 따로 설명할 필요가 없다 — 원작 대사가 우리 시스템을 이미 설명한다.
    /// </summary>
    public static class OpeningCuts
    {
        /// <summary>발포 5프레임이 한 장씩 머무는 시간. 원작 기판 체감에 맞춘 값이다.</summary>
        private const float GunFrame = 0.13f;

        /// <summary>관이 파랑에서 초록으로 비는 데 걸리는 시간.</summary>
        private const float TubeDrain = 1.1f;

        /// <summary>유령이 관 안에서 도는 한 프레임.</summary>
        private const float GhostFrame = 0.22f;

        public static readonly OpeningCut[] Prologue =
        {
            new() { Key = "cut_prologue_1", Line = "어느 날, 여자친구와 함께 걷고 있었다." },
            new() { Key = "cut_prologue_2", Line = "그런데 갑자기, 습격당했다!" },
            new() { Key = "cut_prologue_3", Line = "으아아아아아악……" },
            new() { Key = "cut_prologue_4" },

            // ── 발포 5프레임 — 탭을 기다리지 않는다 ────────────────
            // 한 장씩 넘기면 애니메이션이 아니라 슬라이드가 된다.
            // 인물·배경이 픽셀까지 같고 팔·총·불꽃만 움직이도록 받은 다섯 장이다.
            new() { Key = "cut_prologue_5", AutoSeconds = GunFrame },
            new() { Key = "cut_prologue_6", AutoSeconds = GunFrame },
            new() { Key = "cut_prologue_7", Line = "탕!", AutoSeconds = GunFrame },
            new() { Key = "cut_prologue_8", AutoSeconds = GunFrame },
            new() { Key = "cut_prologue_9", AutoSeconds = GunFrame * 3f },

            // 그림 없는 검은 화면. 여기서 유령이 된다.
            new() { Line = "그리고, 나는 죽었다." },
        };

        public static readonly OpeningCut[] Start =
        {
            new() { Key = "cut_start_1", Line = "내가 자네를 불렀네." },
            new() { Key = "cut_start_2", Line = "그자들은 악의 비밀결사였어." },
            // ⚠ 3번은 **프롤로그 4번과 같은 그림**이다. 원작이 그렇다 —
            //   같은 납치 장면을 색만 빼서 회상으로 다시 쓴다.
            //   따로 그린 `cut_start_3` 을 받아 봤더니 자세도 인물도 조금씩 달라졌다.
            //   같은 그림을 가리키면 그 어긋남이 **생길 수가 없다.**
            new() { Key = "cut_prologue_4", Sepia = true,
                    Line = "내 유령 에너지 연구를 캐내려고\n딸을 납치해 몸값을 요구했다네." },

            // ── 관이 비는 것을 보여 준다 ───────────────────────────
            //
            // ⚠ 이 두 장이 **이 게임의 HP 설명**이다. 원작은 패널 하나다 —
            //   검은 배경 + 초록 후광 + 관, 그리고 **그 안에 유령**.
            //   한때 관(4·5)과 유령(6~9)을 갈라 두어 관이 비어 보였다.
            //   유령은 컷이 아니라 **관 위에 얹는 겹**이다.
            //
            // 파랑에서 초록으로 넘어가며 유령이 옅어진다.
            // 흐려지는 것은 코드가 알파로 만든다 — 본편 HUD 게이지가 같은 함수를 쓴다.
            new() { Key = "cut_start_4", Line = "자네의 에너지는 한정돼 있네.",
                    Ghost = true, Energy = 1f, AutoSeconds = TubeDrain },
            new() { Key = "cut_start_5", Line = "자네의 에너지는 한정돼 있네.",
                    Ghost = true, Energy = 0.25f },

            new() { Key = "cut_start_10", Line = "부탁하네. 내 딸을 구해 주게!" },
        };

        /// <summary>
        /// 관 안에서 도는 유령 세 장. **본편 유령 스프라이트 그대로다.**
        ///
        /// ⚠ 한때 컷신용으로 따로 받은 네 장(`cut_start_6~9`)을 썼는데
        ///   윤곽선이 없어 뿌옇고 관보다 커서 유리 밖으로 삐져나왔다.
        ///   본편 `unit_ghost_s` 는 검은 윤곽선에 주황 입까지 또렷하고,
        ///   무엇보다 **플레이어가 곧 조종할 그 유령**이다 —
        ///   오프닝에서 본 것이 그대로 게임에 나오는 것이 맞다.
        ///
        /// ⚠ **걷기 프레임은 안 쓴다.** `_walk1`·`_walk2` 는 걸을 때 꼬리가 좌우로
        ///   크게 흔들리도록 그린 것이라, 관 안에 갇힌 유령에 붙이면 꼬리가
        ///   옆으로 삐져나가 펄럭이는 것처럼 보인다.
        ///   **떠 있는 느낌은 위아래로 살짝 흔드는 것**으로 만든다 — 그림 한 장이면 된다.
        /// </summary>
        public static readonly string[] GhostFrames = { "unit_ghost_s" };

        /// <summary>관 안 유령이 위아래로 흔들리는 폭(px)과 한 번 오가는 시간.</summary>
        public const float GhostBobPixels = 7f;
        public const float GhostBobSeconds = 1.6f;

        /// <summary>유령 한 프레임이 머무는 시간.</summary>
        public const float GhostFrameSeconds = GhostFrame;

        /// <summary>프롤로그 → 시작 컷신을 이어 붙인 전체 순서.</summary>
        public static OpeningCut[] All()
        {
            var all = new OpeningCut[Prologue.Length + Start.Length];
            Prologue.CopyTo(all, 0);
            Start.CopyTo(all, Prologue.Length);
            return all;
        }

        /// <summary>
        /// 관 안 유령이 흐려지는 네 단계. 오프닝에서는 마지막 네 컷에 걸쳐 옅어지고,
        /// 본편 HUD 에서는 남은 에너지 비율이 그대로 이 값이 된다.
        ///
        /// ⚠ **두 곳이 같은 함수를 쓴다.** 오프닝에서 「에너지가 줄면 유령이 옅어진다」를
        ///   한 번 본 사람은 HUD 의 그 유령을 바로 읽는다. 값이 갈라지면 그 연결이 끊긴다.
        /// </summary>
        public static float GhostAlpha(float energyRatio)
            => UnityEngine.Mathf.Lerp(0.25f, 1f, UnityEngine.Mathf.Clamp01(energyRatio));
    }
}
