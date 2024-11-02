

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public enum DPState
    {
        Idle,
        TextCoordX,
        TextCoordY,
        TextCol1,
        TextCol2,
        Text, // remains in this state until a null (0) byte is recieved
        FillCoordX,
        FillCoordY,
        FontSize,
        FillWidth,
        FillHeight,
        FillCol1,
        FillCol2,
        SignalMode,
        SignalLevel,
        NoiseLevel
    }

    public class Display : SKCanvasView, IByteProcessor
    {
        private readonly SKBitmap bitmap = new(512, 512, true);
        public static DPState State { get; set; } = DPState.Idle;
        private int x, y, col1, col2, width, height, font, sigMode, sigLev;
        private string text = string.Empty;
        private readonly SKTypeface typeface = GetMonospaced("Consolas", "monospaced", "Menlo", "Courier");
        private bool displayUpdate = false;

        private static readonly SKRect bmRect = new(0, 0, 512, 512);
        private static readonly SKRect signalRect = new(0, 0, 512, 22);
        private static readonly SKRect noiseRect = new(0, 26, 512, 34);
        private static SKRect signalBar = new(0, 0, 4, 22);
        private static readonly ushort[] upArrow = [0, 384, 960, 2016, 4080, 8184, 16380, 32766, 960, 960, 960, 960, 960, 960, 960, 0];
        private static readonly ushort[] downArrow = [0, 960, 960, 960, 960, 960, 960, 960, 32766, 16380, 8184, 4080, 2016, 960, 384, 0];
        private static readonly ushort[] AB = [0, 124, 254, 198, 198, 16382, 32766, 26310, 26310, 16070, 15872, 26112, 26112, 32512, 16128, 0];
        private static readonly ushort[] stopHand = [0, 64, 432, 684, 682, 682, 682, 682, 12970, 19082, 9730, 4098, 2050, 1538, 260, 0];
        private static readonly ushort[] musicNote = [0, 32736, 16416, 32736, 16416, 16416, 16440, 16444, 16446, 28734, 30748, 31744, 31744, 14336, 0, 0];
        private static readonly ushort[] speechBubble = [0, 2016, 16380, 32766, 32766, 32766, 32766, 16380, 2016, 960, 480, 112, 24, 4, 0, 0];
        private static readonly ushort[] scanSymbol = [0, 3136, 7360, 14816, 29680, 25080, 24796, 24654, 29190, 15110, 8070, 4046, 1948, 824, 560, 0];
        private static readonly ushort[] keySymbol = [0, 0, 0, 0, 56, 124, 32750, 32710, 27886, 27772, 56, 0, 0, 0, 0, 0];
        private static readonly SKColor sigDim = new(0x30, 0x30, 0x30);

        public Display() : base()
        {
            bitmap.Erase(SKColors.Black);
            _ = UpdateDisplayLoop();
            BT.DataTarget = this;
            BT.Dispatcher = Dispatcher;
        }

        private static SKTypeface GetMonospaced(params string[] tfOptions)
        {
            SKTypeface? monospaced = null;
            foreach (string family in tfOptions)
            {
                using (monospaced) { }
                monospaced = SKTypeface.FromFamilyName(family,
                    SKFontStyleWeight.Bold,
                    SKFontStyleWidth.Expanded,
                    SKFontStyleSlant.Upright);
                if (monospaced != null && monospaced.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase))
                    break;
            }
            return monospaced ?? SKTypeface.Default;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.DrawBitmap(bitmap, bmRect, e.Info.Rect);
        }

        private void SpecialCharacter(ushort[] data, int x, int y, SKColor color)
        {
            foreach (ushort d in data)
            {
                int xx = x;
                for (ushort m = 1; m > 0; m <<= 1)
                {
                    SKColor col = (d & m) != 0 ? color : SKColors.Black;
                    bitmap.SetPixel(xx, y, col);
                    bitmap.SetPixel(xx + 1, y, col);
                    bitmap.SetPixel(xx, y + 1, col);
                    bitmap.SetPixel(xx + 1, y + 1, col);
                    xx += 2;
                }
                y += 2;
            }
        }

        private SKColor FwColor()
        {
            // c1 = GGGRRRRR
            // c2 = BBBBBGGG
            int r = (col1 & 0b00011111) << 3;
            int g = ((col2 & 0b00000111) << 5) | ((col1 & 0b11100000) >> 3);
            int b = col2 & 0b11111000;
            if(VM.Instance.BlueBoost)
            {
                int bb = b >> 1;
                g = (g + bb).Clamp(0, 255);
                bb >>= 1;
                r = (r + bb).Clamp(0, 255);
            }
            return new(
                    (byte)r,
                    (byte)g,
                    (byte)b
                );
        }

        private void DrawRect()
        {
            SKRect rect = new(x, y, x + width, y + height);
            using (SKCanvas canvas = new(bitmap))
            {
                using SKPaint paint = new();
                paint.Color = FwColor();
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(rect, paint);
            }
            displayUpdate = true;
        }

        private static int FontSize(int fontNumber)
        {
            return fontNumber switch
            {
                0 => VM.Instance.FontSize0,
                1 => VM.Instance.FontSize1,
                _ => VM.Instance.FontSize2,
            };
        }

        private void DrawText()
        {
            using (SKCanvas canvas = new(bitmap))
            {
                using SKPaint paint = new();
                int fw = font == 0 ? 24 : font == 1 ? 32 : 48;
                int fh = font < 2 ? 32 : 64;
                var col = FwColor();
                paint.Typeface = typeface;
                paint.TextSize = Display.FontSize(font);
                paint.TextAlign = SKTextAlign.Left;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = true;
                foreach (char ch in text)
                {
                    string? c = null;
                    switch (ch)
                    {
                        case '^':
                            SpecialCharacter(upArrow, x, y, col);
                            break;
                        case '_':
                            SpecialCharacter(downArrow, x, y, col);
                            break;
                        case '!':
                            SpecialCharacter(AB, x, y, col);
                            break;
                        case '$':
                            SpecialCharacter(stopHand, x, y, col);
                            break;
                        case '&':
                            SpecialCharacter(musicNote, x, y, col);
                            break;
                        case '{':
                            SpecialCharacter(speechBubble, x, y, col);
                            break;
                        case '~':
                            SpecialCharacter(scanSymbol, x, y, col);
                            break;
                        case '}':
                            SpecialCharacter(keySymbol, x, y, col);
                            break;
                        case '@':
                            c = " ";
                            break;
                        default:
                            c = ch.ToString();
                            break;
                    }
                    if (c != null)
                    {
                        var textBox = new SKRect(x, y, x + fw, y + fh);
                        paint.Color = SKColors.Black;
                        canvas.DrawRect(textBox, paint);
                        paint.Color = col;
                        canvas.DrawText(c, x, y + fh - 1, paint);
                    }
                    x += fw;
                }
            }
            displayUpdate = true;
        }

        public void DrawNoise()
        {
            using (SKCanvas canvas = new(bitmap))
            {
                using SKPaint paint = new();
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = true;
                canvas.DrawRect(noiseRect, paint);
                paint.Color = sigMode == 1 ? SKColors.LimeGreen : SKColors.Blue;
                SKRect rect = new(16, 26, (sigLev * 4) + 16, 34);
                canvas.DrawRect(rect, paint);
            }
            displayUpdate = true;
        }


        public void DrawSignal()
        {
            using (SKCanvas canvas = new(bitmap))
            {
                using SKPaint paint = new();
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = true;
                canvas.DrawRect(signalRect, paint);
                for (int i = 0; i <= 120; i += 2)
                {
                    if (i > sigLev)
                    {
                        paint.Color = sigDim;
                    }
                    else
                    switch (sigMode)
                    {
                        case 0:
                            if (i < 60)
                                paint.Color = SKColors.LimeGreen;
                            else if (i < 90)
                                paint.Color = SKColors.Yellow;
                            else
                                paint.Color = SKColors.Orange;
                            break;
                        case 1:
                            paint.Color = SKColors.Orange;
                            break;
                        case 2:
                            paint.Color = SKColors.DeepSkyBlue;
                            break;
                        case 3:
                            paint.Color = SKColors.LimeGreen;
                            break;
                    }
                    int h = (i * 4) + 16;
                    signalBar.Location = new(h, 0);
                    canvas.DrawRect(signalBar, paint);
                }
            }
            displayUpdate = true;
        }

        public void ProcessByte(int byt)
        {
            switch(State)
            {
                case DPState.Idle:
                    switch(byt)
                    {
                        case 0x4a: // remote mode ACK
                            break;
                        case 0x4b: // end remote mode ACK
                            break;
                        case 0x60: // left green led off
                            VM.Instance.LedGreenLeft = false;
                            break;
                        case 0x61: // left green led on
                            VM.Instance.LedGreenLeft = true;
                            break;
                        case 0x62: // left red led off
                            VM.Instance.LedRedLeft = false;
                            break;
                        case 0x63: // left red led on
                            VM.Instance.LedRedLeft = true;
                            break;
                        case 0x64: // right green led off
                            VM.Instance.LedGreenRight = false;
                            break;
                        case 0x65: // right green led on
                            VM.Instance.LedGreenRight = true;
                            break;
                        case 0x66: // right red led off
                            VM.Instance.LedRedRight = false;
                            break;
                        case 0x67: // right red led on
                            VM.Instance.LedRedRight = true;
                            break;
                        case 0x77: // draw text
                            text = string.Empty;
                            State = DPState.TextCoordX;
                            break;
                        case 0x78: // draw rect
                            State = DPState.FillCoordX;
                            break;
                        case 0x79: // s-meter/power meter
                            State = DPState.SignalMode;
                            break;
                        case 0x7a: // noise meter/tx modulation
                            State = DPState.NoiseLevel;
                            break;
                    }
                    break;
                case DPState.TextCoordX:
                    x = byt << 2;
                    State = DPState.TextCoordY;
                    break;
                case DPState.TextCoordY:
                    y = byt << 2;
                    State = DPState.FontSize;
                    break;
                case DPState.FontSize:
                    font = byt;
                    State = DPState.TextCol1;
                    break;
                case DPState.TextCol1:
                    col1 = byt;
                    State = DPState.TextCol2;
                    break;
                case DPState.TextCol2:
                    col2 = byt;
                    State = DPState.Text;
                    break;
                case DPState.Text:
                    if (byt == 0)
                    {
                        DrawText();
                        State = DPState.Idle;
                    }
                    else
                        text += (char)byt;
                    break;

                case DPState.FillCoordX:
                    x = byt << 2;
                    State = DPState.FillCoordY;
                    break;
                case DPState.FillCoordY:
                    y = byt << 2;
                    State = DPState.FillWidth;
                    break;
                case DPState.FillWidth:
                    width = byt << 2;
                    State = DPState.FillHeight;
                    break;
                case DPState.FillHeight:
                    height = byt << 2;
                    State = DPState.FillCol1;
                    break;
                case DPState.FillCol1:
                    col1 = byt;
                    State = DPState.FillCol2;
                    break;
                case DPState.FillCol2:
                    col2 = byt;
                    State = DPState.Idle;
                    DrawRect();
                    break;

                case DPState.SignalMode:
                    sigMode = byt;
                    State = DPState.SignalLevel;
                    break;
                case DPState.SignalLevel:
                    sigLev = byt;
                    State = DPState.Idle;
                    DrawSignal();
                    break;

                case DPState.NoiseLevel:
                    sigLev = byt;
                    if (sigLev == 255) sigLev = 0;
                    State = DPState.Idle;
                    DrawNoise();
                    break;
            }
        }

        private async Task UpdateDisplayLoop()
        {
            while (true)
            {                
                if(displayUpdate)
                {
                    try { InvalidateSurface(); } catch { }
                    displayUpdate = false;
                }
                using var task = Task.Delay(50);
                await task;
            }
        }

    }
}
