using UnityEngine;

public static partial class Util
{
    public static class ColorUtil
    {
        public static Color HexToColor(string hex, byte alpha = 255)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color32(r, g, b, alpha);
        }

        /// <summary>
        /// 머티리얼의 메인 컬러를 HSV 기준으로 조정합니다.
        /// </summary>
        /// <param name="mat">대상 머티리얼</param>
        /// <param name="type">0=Hue, 1=Saturation, 2=Value</param>
        /// <param name="addValue">변화량 (정수, +면 증가, -면 감소)</param>
        /// <returns>조정된 머티리얼</returns>
        public static Material AdjustMaterialHSV(Material mat, int type, int addValue)
        {
            if (mat == null)
            {
                Debug.LogWarning("[Util.AdjustMaterialHSV] Material이 null입니다.");
                return null;
            }

            // RGB → HSV
            Color currentColor = mat.color;
            Color.RGBToHSV(currentColor, out float h, out float s, out float v);

            float delta = addValue / 100f;

            switch (type)
            {
                case 0: // Hue (색상)
                    h = Mathf.Repeat(h + delta, 1f);
                    break;

                case 1: // Saturation (채도)
                    s = Mathf.Clamp01(s + delta);
                    break;

                case 2: // Value (밝기)
                    v = Mathf.Clamp01(v + delta);
                    break;

                default:
                    Debug.LogWarning($"[Util.AdjustMaterialHSV] 잘못된 type 값: {type} (0=H, 1=S, 2=V)");
                    break;
            }

            // HSV → RGB 후 머티리얼에 적용
            mat.color = Color.HSVToRGB(h, s, v);

            return mat;
        }

        public static Color GetNormalDamage() => GetColor("#FFFFFF");

        public static Color GetCriticalHit() => GetColor("#FF5555");

        public static Color GetMissOrEvasion() => GetColor("#C0C0C0");

        public static Color GetHeal() => GetColor("#66FF66");

        private static Color GetColor(string htmlColor)
        {
            // # 없으면 추가
            if (htmlColor.StartsWith("#") == false)
                htmlColor = "#" + htmlColor;

            if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
                return color;

            Debug.LogWarning($"색상 값을 파싱하지 못했습니다: {htmlColor}.");
            return Color.magenta;
        }
    }
}
