using TMPro;
using UnityEngine;

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

            _tmp = gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _tmp.font = font;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.enableWordWrapping = false;
            _tmp.raycastTarget = false;
            _tmp.fontStyle = FontStyles.Bold;
            _tmp.fontSize = 26f;

            // 어두운 던전 바닥 위에 얹히므로 외곽선이 없으면 숫자가 묻힌다.
            // ⚠ `_OutlineWidth` 는 글자 안쪽을 파먹는다. `_FaceDilate` 를 먼저 키워
            //    두께를 확보하지 않으면 획이 가늘어져 오히려 안 읽힌다.
            var mat = _tmp.fontMaterial;   // 인스턴스 — 공유 머티리얼을 건드리면 다른 UI 까지 변한다
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

            gameObject.SetActive(false);
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
        }
    }
}
