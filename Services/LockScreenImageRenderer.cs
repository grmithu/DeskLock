using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AwayLock.Models;

namespace AwayLock.Services;

/// <summary>1920x1080 lock screen PNG toiri kore (editorial serif design).</summary>
public sealed class LockScreenImageRenderer
{
    private const int W = 1920;
    private const int H = 1080;

    public string Render(AwayInfo info)
    {
        Directory.CreateDirectory(AppSettings.ImageFolder);
        foreach (var old in Directory.EnumerateFiles(AppSettings.ImageFolder, "away_*.png"))
        {
            try { File.Delete(old); } catch { /* ignore */ }
        }

        // Notun file name, jate Windows purono cache na dekhay
        var path = Path.Combine(AppSettings.ImageFolder, $"away_{DateTime.Now:yyyyMMddHHmmssfff}.png");

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            Draw(dc, info);
        }

        var bitmap = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    private static void Draw(DrawingContext dc, AwayInfo info)
    {
        DrawBackground(dc);

        var bangla = ContainsBangla(info.Message + info.SubText + AppSettings.ContactText);
        var mainFont = bangla ? "Nirmala UI" : "Georgia";
        var mainWeight = bangla ? FontWeights.Bold : FontWeights.Normal;
        var subStyle = bangla ? FontStyles.Normal : FontStyles.Italic;

        var white = Solid(Colors.White);
        var soft = Solid(Color.FromArgb(205, 255, 255, 255));
        var dotBrush = Solid(info.Accent);
        var linePen = new Pen(Solid(Color.FromArgb(190, 255, 255, 255)), 2);
        linePen.Freeze();

        // 1) Caption (spaced letters) + choto dot. Windows er ghori/tarikh upore thake, tai shob nicher ongshe.
        var spaced = string.Join(" ", AppSettings.CaptionText.ToUpperInvariant().ToCharArray());
        var caption = MakeText(spaced, "Segoe UI Semibold", FontStyles.Normal, FontWeights.Normal, 35, soft);
        const double dotD = 22, dotGap = 18, capY = 560;
        var groupW = (AppSettings.ShowDot ? dotD + dotGap : 0) + caption.Width;
        var capX = (W - groupW) / 2;

        if (AppSettings.ShowDot)
        {
            dc.DrawEllipse(dotBrush, null, new Point(capX + dotD / 2, capY + caption.Height / 2), dotD / 2, dotD / 2);
            dc.DrawText(caption, new Point(capX + dotD + dotGap, capY));
        }
        else
        {
            dc.DrawText(caption, new Point(capX, capY));
        }

        // 2) Message (boro serif headline). Lomba hole font nijei choto hoy.
        double size = 117;
        FormattedText message;
        do
        {
            message = MakeText(info.Message, mainFont, FontStyles.Normal, mainWeight, size, white);
            size -= 8;
        } while (message.Width > 1500 && size > 40);

        var msgY = capY + caption.Height + 14;
        dc.DrawText(message, new Point((W - message.Width) / 2, msgY));

        // 3) Patla dag + "Back at ..." (italic)
        var nextY = msgY + message.Height + 22;
        if (!string.IsNullOrWhiteSpace(info.SubText))
        {
            dc.DrawLine(linePen, new Point(W / 2.0 - 160, nextY), new Point(W / 2.0 + 160, nextY));
            var sub = MakeText(info.SubText, mainFont, subStyle, FontWeights.Normal, 53, soft);
            dc.DrawText(sub, new Point((W - sub.Width) / 2, nextY + 24));
        }

        // 4) Jaruri jogajog
        if (!string.IsNullOrWhiteSpace(AppSettings.ContactText))
        {
            var contact = MakeText(AppSettings.ContactText, mainFont, subStyle, FontWeights.Normal, 37, soft);
            dc.DrawText(contact, new Point((W - contact.Width) / 2, 930));
        }
    }

    private static void DrawBackground(DrawingContext dc)
    {
        var rect = new Rect(0, 0, W, H);
        try
        {
            if (File.Exists(AppSettings.BackgroundImage))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(AppSettings.BackgroundImage);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                dc.DrawImage(image, rect);
                return;
            }
        }
        catch
        {
            // fallback niche
        }

        dc.DrawRectangle(Solid(Color.FromRgb(0, 90, 158)), null, rect);
    }

    private static FormattedText MakeText(string text, string family, FontStyle style, FontWeight weight, double size, Brush brush)
    {
        var typeface = new Typeface(new FontFamily(family), style, weight, FontStretches.Normal);
        return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, brush, 1.0);
    }

    private static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static bool ContainsBangla(string text) => text.Any(c => c >= '\u0980' && c <= '\u09FF');
}
