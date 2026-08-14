using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Module.InGame
{
    /// <summary>
    /// 피해 수치 표시. 맞은 자리에서 떠오르며 사라진다.
    ///
    /// 붉은 점멸과 체력바만으로는 **몇 대 맞았는지**가 안 보인다. 특히 연사·다발탄은
    /// 한 번에 여러 발이 꽂히는데 체력바만 스르륵 줄어 타격이 읽히지 않는다.
    ///
    /// 탄과 같은 풀 방식이다. 교전 중 매 타격마다 GameObject 를 만들면 GC 가 튄다.
    /// </summary>
    public sealed class DamageText : MonoBehaviour
    {
        private const float LifeSeconds = 0.55f;
        private const float RiseDistance = 34f;   // 총 상승 거리
        private const float FadeFrom = 0.55f;     // 수명의 이 지점부터 흐려진다
        private const float PopScale = 1.35f;     // 뜨는 순간 살짝 커졌다 제자리로

        private RectTransform _rect;
        private TextMeshProUGUI _tmp;
        private TextMeshProUGUI _shadow;   // 도트 폰트일 때만 — 아래 12번 주석
        private Vector2 _origin;
        private float _drift;
        private float _life;

        public bool IsActive => _life > 0f;

        public void Init(TMP_FontAsset font)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(120f, 32f);

            // 어두운 던전 바닥 위에 얹히므로 외곽선이 없으면 숫자가 묻힌다.
            //
            // 원작 도트 폰트는 **비트맵**이라 SDF 셰이더가 아니다 — 외곽선 속성이 아예
            // 없다. 대신 검은 글자를 한 칸 어긋나게 깔아 같은 효과를 낸다.
            // UGUI 는 부모를 먼저 그리므로 **뿌리에 그림자**, 자식에 본 글자를 둔다.
            bool bitmap = font != null && font.atlasRenderMode == GlyphRenderMode.RASTER;

            if (bitmap)
            {
                _shadow = Setup(gameObject.AddComponent<TextMeshProUGUI>(), font);
                _shadow.color = Color.black;

                var face = new GameObject("Face", typeof(RectTransform));
                var fr = (RectTransform)face.transform;
                fr.SetParent(transform, false);
                fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
                fr.sizeDelta = _rect.sizeDelta;
                fr.anchoredPosition = new Vector2(-1f, 1f);   // 그림자가 오른쪽 아래로 보이게
                _tmp = Setup(face.AddComponent<TextMeshProUGUI>(), font);
            }
            else
            {
                _tmp = Setup(gameObject.AddComponent<TextMeshProUGUI>(), font);

                // ⚠ `_OutlineWidth` 는 글자 안쪽을 파먹는다. `_FaceDilate` 를 먼저 키워
                //    두께를 확보하지 않으면 획이 가늘어져 오히려 안 읽힌다.
                var mat = _tmp.fontMaterial;   // 인스턴스 — 공유 머티리얼을 건드리면 다른 UI 까지 변한다
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            }

            gameObject.SetActive(false);
        }

        private static TextMeshProUGUI Setup(TextMeshProUGUI tmp, TMP_FontAsset font)
        {
            if (font != null) tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 26f;
            return tmp;
        }

        /// <summary>
        /// <paramref name="at"/> 는 필드 좌표. 같은 자리에 여러 발이 꽂힐 때
        /// 숫자가 완전히 겹치지 않도록 가로로 조금씩 흩어 놓는다.
        /// </summary>
        public void Show(Vector2 at, int damage, Color color)
            => Show(at, damage.ToString(), color);

        /// <summary>피해 말고도 띄울 것이 있다 — 전술 빙의로 나간 Ghost HP 같은 것.</summary>
        public void Show(Vector2 at, string text, Color color)
        {
            _origin = at;
            _drift = Random.Range(-10f, 10f);
            _life = LifeSeconds;
            _tmp.text = text;
            _tmp.color = color;
            if (_shadow != null) _shadow.text = text;
            gameObject.SetActive(true);
            Apply();
        }

        public void Tick(float dt)
        {
            if (_life <= 0f) return;
            _life -= dt;
            if (_life <= 0f) { Despawn(); return; }
            Apply();
        }

        public void Despawn()
        {
            _life = 0f;
            gameObject.SetActive(false);
        }

        private void Apply()
        {
            float t = 1f - _life / LifeSeconds;          // 0 → 1

            // 처음에 빠르게 솟았다가 느려진다. 등속으로 올리면 종이가 날아가는 느낌이 난다.
            float rise = RiseDistance * (1f - (1f - t) * (1f - t));

            // 픽셀아트라 소수 좌표로 두면 글자가 흐릿하게 떨린다. 정수로 스냅한다.
            _rect.anchoredPosition = new Vector2(
                Mathf.Round(_origin.x + _drift), Mathf.Round(_origin.y + rise));

            float pop = t < 0.2f ? Mathf.Lerp(PopScale, 1f, t / 0.2f) : 1f;
            _rect.localScale = new Vector3(pop, pop, 1f);

            var c = _tmp.color;
            c.a = t < FadeFrom ? 1f : 1f - (t - FadeFrom) / (1f - FadeFrom);
            _tmp.color = c;
            if (_shadow != null) _shadow.color = new Color(0f, 0f, 0f, c.a);
        }
    }
}
