using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace nicFWRemoteBT
{
    public class XButton : Button
    {
        public XButton() : base() 
        {
            SizeChanged += XButton_MeasureInvalidated;
            MeasureInvalidated += XButton_MeasureInvalidated;            
        }

        public void ResizeText()
        {
            MeasureInvalidated -= XButton_MeasureInvalidated;
            SizeChanged -= XButton_MeasureInvalidated;
            this.FontSize = 0.1;

            for (int adj = 16; adj > 0; adj /= 2)
            {
                while (this.FontSize < 1000)
                {
                    this.FontSize += adj;
                    var req = (Size)Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.None);
                    if (req.Width > Width || req.Height > Height)
                    {
                        this.FontSize -= adj;
                        break;
                    }
                }
            }
            SizeChanged += XButton_MeasureInvalidated;
            MeasureInvalidated += XButton_MeasureInvalidated;
        }

        private void XButton_MeasureInvalidated(object? sender, EventArgs e)
        {
            ResizeText();
        }

        public string Tag
        {
            get { return (string)GetValue(TagProperty); }
            set { SetValue(TagProperty, value); }
        }
        public static readonly BindableProperty TagProperty =
            BindableProperty.Create(nameof(Tag), typeof(string), typeof(XButton), defaultValue: string.Empty);

    }
}
