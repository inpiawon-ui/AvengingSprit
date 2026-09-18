using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 「이 글자 칸은 굵은 폰트」 표시. 언어가 바뀌면 `LanguageModule` 이 그 언어의 굵은 폰트
    /// (`LanguageFont.Heavy`)로 갈아 끼운다. 굵은 폰트가 없는 언어는 본문 폰트 + 굵게 모양.
    ///
    /// <see cref="Dilate"/> 는 획 두께 보정이다. 시안 글자마다 굵기가 조금씩 달라(보통 · 중간 · 굵게)
    /// 폰트를 여러 벌 두는 대신 한 폰트의 획을 깎거나 불린다. 언어가 바뀌어도 같은 값을 다시 입힌다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeavyText : MonoBehaviour
    {
        [SerializeField, Range(-0.5f, 0.5f)] private float _dilate;

        public float Dilate => _dilate;
    }
}
