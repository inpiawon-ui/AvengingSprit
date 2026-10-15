using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Game.Editor.UiHarness
{
    /// <summary>
    /// 화면의 한 점이 그 Image 의 «칠해진(불투명한) 부분» 인가 — 원본 PNG 를 에디터에서 읽어 알파를 본다(텍스처를 읽기 가능으로 바꾸지 않는다).
    /// 9분할 판은 네 모서리·변·가운데로 나눠 스프라이트 좌표로 되돌린다. 가림 판정(C6)이 둥근 모서리·투명 여백을 가림으로 오판하지 않게.
    /// </summary>
    public static class UiSpriteAlpha
    {
        private sealed class Pixels
        {
            public Color32[] Data;
            public int Width;
            public int Height;
        }

        private static readonly Dictionary<string, Pixels> s_cache = new Dictionary<string, Pixels>();
        private static readonly Vector3[] s_corners = new Vector3[4];

        public static void Clear() => s_cache.Clear();

        /// <summary> 화면 점(<paramref name="world"/>)에서 이 Image 가 불투명한가(알파 &gt; 0.5). 원본을 못 읽으면 상자 안이면 불투명으로 본다. </summary>
        public static bool IsSolidAt(Image image, Vector2 world)
        {
            var rt = image.rectTransform;
            Vector2 local = rt.InverseTransformPoint(world);
            var rect = rt.rect;
            if (!rect.Contains(local)) return false;

            var sprite = image.sprite;
            if (sprite == null) return true;

            var pixels = Load(sprite);
            if (pixels == null) return true;

            if (!TrySpritePoint(image, rect, local, out var sp)) return false;
            var tr = sprite.textureRect;
            int x = Mathf.Clamp(Mathf.FloorToInt(tr.x + sp.x), 0, pixels.Width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(tr.y + sp.y), 0, pixels.Height - 1);
            return pixels.Data[y * pixels.Width + x].a > 128;
        }

        /// <summary> 상자 안 점을 스프라이트 픽셀 좌표로(아래 왼쪽 원점). 비율 유지로 비운 곳·속을 비운 9분할 가운데는 false. </summary>
        private static bool TrySpritePoint(Image image, Rect rect, Vector2 local, out Vector2 point)
        {
            var sprite = image.sprite;
            var size = sprite.rect.size;
            float u = local.x - rect.xMin;
            float v = local.y - rect.yMin;
            point = default;

            if (image.type == Image.Type.Sliced && sprite.border.sqrMagnitude > 0f)
            {
                var border = sprite.border;   // 왼 · 아래 · 오른 · 위 (스프라이트 픽셀)
                float unit = image.pixelsPerUnit;   // 스프라이트 픽셀 → 로컬 단위
                float l = border.x / unit, b = border.y / unit, r = border.z / unit, t = border.w / unit;
                bool isCenterX = u > l && u < rect.width - r;
                bool isCenterY = v > b && v < rect.height - t;
                if (isCenterX && isCenterY && !image.fillCenter) return false;

                point.x = u <= l ? u * unit
                        : u >= rect.width - r ? size.x - (rect.width - u) * unit
                        : border.x + (u - l) / Mathf.Max(0.001f, rect.width - l - r) * (size.x - border.x - border.z);
                point.y = v <= b ? v * unit
                        : v >= rect.height - t ? size.y - (rect.height - v) * unit
                        : border.y + (v - b) / Mathf.Max(0.001f, rect.height - b - t) * (size.y - border.y - border.w);
                return true;
            }

            if (image.preserveAspect)
            {
                float aspect = size.x / size.y;
                float w = Mathf.Min(rect.width, rect.height * aspect);
                float h = w / aspect;
                u -= (rect.width - w) * 0.5f;
                v -= (rect.height - h) * 0.5f;
                if (u < 0f || v < 0f || u > w || v > h) return false;
                point = new Vector2(u / w * size.x, v / h * size.y);
                return true;
            }

            point = new Vector2(u / rect.width * size.x, v / rect.height * size.y);
            return true;
        }

        private static Pixels Load(Sprite sprite)
        {
            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path)) return null;
            if (s_cache.TryGetValue(path, out var cached)) return cached;

            Pixels pixels = null;
            if (File.Exists(path) && (path.EndsWith(".png") || path.EndsWith(".jpg")))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(File.ReadAllBytes(path)))
                {
                    // 임포트에서 줄였을 수 있다(최대 크기) — 스프라이트 좌표는 임포트된 텍스처 기준이라 그 크기로 맞춘다.
                    if (texture.width != sprite.texture.width || texture.height != sprite.texture.height)
                        texture = Resize(texture, sprite.texture.width, sprite.texture.height);
                    pixels = new Pixels { Data = texture.GetPixels32(), Width = texture.width, Height = texture.height };
                }

                Object.DestroyImmediate(texture);
            }

            s_cache[path] = pixels;
            return pixels;
        }

        private static Texture2D Resize(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(source);
            return result;
        }
    }
}
