using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace TodayIconsGenerator
{
    // FM.2016.12.12 - utility to generate today button icons. change the culture info accordingly (here is fr-FR).

    class Program
    {
        static void Main(string[] args)
        {
            const string path = @"D:\Users\matro\Documents\Visual Studio 2012\Projects\wp7\weekc\weekc\Icons\";

            DateTime date = new DateTime(2012, 01, 01);

            while (date.Year == 2012)
            {
                string filename = date.ToString("MMdd");

                {
                    Bitmap b = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(b);
                    Font fd = new Font("Segoe WP", 15, FontStyle.Bold);
                    Font fm = new Font("Segoe WP", 8, FontStyle.Bold);
                    SolidBrush sb = new SolidBrush(Color.White);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    var width = g.MeasureString(date.Day.ToString(), fd);
                    g.DrawString(date.Day.ToString(), fd, sb, (48 - width.Width) / 2, 3);

                    width = g.MeasureString(date.ToString("MMM", CultureInfo.GetCultureInfo("fr-FR")), fm);

                    g.DrawString(date.ToString("MMM", CultureInfo.GetCultureInfo("fr-FR")), fm, sb, (48 - width.Width) / 2, 26);

                    b.Save(path + filename + ".fr-FR.png", ImageFormat.Png);
                    b.Dispose();
                }

                date = date.AddDays(1);
            }
        }
    }
}
