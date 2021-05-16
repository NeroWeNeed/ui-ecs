using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;

namespace NeroWeNeed.UIECS {
    [Serializable]
    public struct UILength {
        private static readonly Regex regex = new Regex("(NAN|[-]?INF(INITY)?|[-]?[0-9]+(?:\\.[0-9]+)?)([a-zA-Z%]*)?", RegexOptions.IgnoreCase);
        public float realValue;
        public float value;
        public UILengthUnit unit;
        public UILength(float value, UILengthUnit unit) {
            this.value = value;
            realValue = default;
            this.unit = unit;
        }
        public static bool TryParse(string s, out UILength result) {
            var match = regex.Match(s);

            if (match.Success) {
                if (!float.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float number)) {
                    number = 0f;
                }
                if (match.Groups.Count <= 3 || !Enum.TryParse<UILengthUnit>(match.Groups[3].Value, true, out UILengthUnit unit)) {
                    unit = UILengthUnit.Px;
                }
                result = new UILength(number, unit);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Normalize<TContextData>(TContextData context) where TContextData : struct, IUILengthContext {
            if (float.IsInfinity(value) || float.IsNaN(value)) {
                return value;
            }
            switch (unit) {
                case UILengthUnit.Auto:
                    break;
                case UILengthUnit.Px:
                    return value * context.PixelScale;
                case UILengthUnit.Cm:
                    return value * context.PixelScale * context.Dpi / 2.54f;
                case UILengthUnit.Mm:
                    return value * context.PixelScale * context.Dpi / 25.4f;
                case UILengthUnit.In:
                    return value * context.PixelScale * context.Dpi;
                case UILengthUnit.Pt:
                    return value * context.PixelScale * context.Dpi * (1f / 72f);
                case UILengthUnit.Pc:
                    return value * context.PixelScale * context.Dpi * (1f / 6f);
                case UILengthUnit.Em:

                    break;
                case UILengthUnit.Ex:
                    break;
                case UILengthUnit.Ch:
                    break;
                case UILengthUnit.Rem:
                    break;
                case UILengthUnit.Vw:
                    return value * context.PixelScale * (context.ViewportSize.x * 0.01f);
                case UILengthUnit.Vh:
                    return value * context.PixelScale * (context.ViewportSize.y * 0.01f);
                case UILengthUnit.Vmin:
                    return value * context.PixelScale * (math.min(context.ViewportSize.x, context.ViewportSize.y) * 0.01f);
                case UILengthUnit.Vmax:
                    return value * context.PixelScale * (math.max(context.ViewportSize.x, context.ViewportSize.y) * 0.01f);
                case UILengthUnit.Percent:
                    return value * context.RelativeTo;
                default:
                    break;
            }
            return 0f;
        }

        public override string ToString() {
            return (float.IsInfinity(value) || float.IsNaN(value)) ? value.ToString() : $"{value}{unit}";
        }

    }
    public struct SimpleUILengthContext : IUILengthContext {
        

        public float Dpi { get; }
        public float PixelScale { get;  }
        public float2 ViewportSize { get; }
        public float RelativeTo { get; set; }
        public SimpleUILengthContext(float dpi, float pixelScale, float2 viewportSize) {
            Dpi = dpi;
            PixelScale = pixelScale;
            ViewportSize = viewportSize;
            RelativeTo = 0f;
        }
    }
    public interface IUILengthContext {
        public float Dpi { get; }
        public float PixelScale { get; }
        public float2 ViewportSize { get; }
        public float RelativeTo { get; set; }

    }

    /// <summary>
    /// First byte denotes whether the value is an absolute or relative length.
    /// </summary>
    public enum UILengthUnit : byte {
        Px = 0b00000000, Cm = 0b00000010, Mm = 0b00000100, In = 0b00000110, Pt = 0b00001000, Pc = 0b00001010,
        Em = 0b00000001, Ex = 0b00000011, Ch = 0b00000101, Rem = 0b00000111, Vw = 0b00001001, Vh = 0b00001011, Vmin = 0b00001101, Vmax = 0b00001111, Percent = 0b00010001, Auto = 0b00010011, Inherit = 0b00010101
    }

}