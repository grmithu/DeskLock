using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskLock.Models;
using FormsScreen = System.Windows.Forms.Screen;

namespace DeskLock.Services;

public sealed class LockScreenImageRenderer
{
    private const double PixelsPerDip = 1.0;

    public string Render(AwayInfo info)
    {
        Directory.CreateDirectory(AppSettings.ImageFolder);
        var path = Path.Combine(AppSettings.ImageFolder, "lock.png");
        RenderToFile(info, path);
        return path;
    }

    public string[] RenderThemePreviews()
    {
        var previewFolder = Path.Combine(AppSettings.ImageFolder, "previews");
        Directory.CreateDirectory(previewFolder);
        var paths = new string[AppSettings.Statuses.Length];

        for (var i = 0; i < AppSettings.Statuses.Length; i++)
        {
            var status = AppSettings.Statuses[i];
            var info = new AwayInfo(status.Name, DateTime.Now.AddMinutes(30), status);
            paths[i] = Path.Combine(previewFolder, $"{status.IconKey}.png");
            RenderToFile(info, paths[i]);
        }

        return paths;
    }

    private static void RenderToFile(AwayInfo info, string path)
    {
        var screen = FormsScreen.PrimaryScreen
            ?? throw new InvalidOperationException("The primary display could not be determined.");
        var width = screen.Bounds.Width;
        var height = screen.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The primary display has invalid dimensions.");
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            Draw(context, info, width, height);
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void Draw(DrawingContext context, AwayInfo info, int width, int height)
    {
        var screenHeight = (double)height;
        var iconSize = screenHeight * 0.09;
        var iconLabelGap = screenHeight * 0.02;
        var labelSize = screenHeight * 0.05;
        var labelReturnGap = screenHeight * 0.012;
        var returnSize = screenHeight * 0.03;

        var labelWidth = width * 0.7;
        var label = MakeLabel(info.Message, labelWidth, labelSize);
        FormattedText? returnLine = null;
        if (info.ReturnTime is DateTime returnTime)
        {
            var returnText = returnTime <= DateTime.Now
                ? "Should be back soon"
                : "Back at " + returnTime.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);
            returnLine = MakeText(
                returnText,
                "Segoe UI",
                FontStyles.Normal,
                FontWeights.Normal,
                returnSize,
                Solid(info.Theme.LockSubColor));
        }

        var contentHeight = iconSize + iconLabelGap + label.Height;
        if (returnLine is not null)
        {
            contentHeight += labelReturnGap + returnLine.Height;
        }

        var top = (height - contentHeight) / 2;
        var iconScale = iconSize / 24;
        var iconTransform = new MatrixTransform(new Matrix(
            iconScale,
            0,
            0,
            iconScale,
            (width - iconSize) / 2,
            top));
        iconTransform.Freeze();

        context.DrawRectangle(Solid(info.Theme.LockColor), null, new Rect(0, 0, width, height));

        var iconPen = new Pen(Solid(Colors.White), 2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        iconPen.Freeze();
        context.PushTransform(iconTransform);
        context.DrawGeometry(null, iconPen, info.Theme.IconGeometry);
        context.Pop();

        var labelY = top + iconSize + iconLabelGap;
        context.DrawText(label, new Point((width - labelWidth) / 2, labelY));

        if (returnLine is not null)
        {
            var returnY = labelY + label.Height + labelReturnGap;
            context.DrawText(returnLine, new Point((width - returnLine.Width) / 2, returnY));
        }
    }

    private static FormattedText MakeLabel(string text, double maxWidth, double fontSize)
    {
        var minimumSize = fontSize * 0.7;
        FormattedText formatted;
        while (true)
        {
            formatted = MakeText(
                text,
                "Segoe UI",
                FontStyles.Normal,
                FontWeights.SemiBold,
                fontSize,
                Solid(Colors.White));
            formatted.MaxTextWidth = maxWidth;
            formatted.TextAlignment = TextAlignment.Center;
            formatted.LineHeight = fontSize * 1.2;
            if (formatted.Height <= formatted.LineHeight * 2 || fontSize <= minimumSize)
            {
                break;
            }

            fontSize = Math.Max(minimumSize, fontSize - 1);
        }

        formatted.MaxLineCount = 2;
        formatted.Trimming = TextTrimming.CharacterEllipsis;
        return formatted;
    }

    private static FormattedText MakeText(
        string text,
        string family,
        FontStyle style,
        FontWeight weight,
        double size,
        Brush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(family), style, weight, FontStretches.Normal),
            size,
            brush,
            PixelsPerDip);
    }

    private static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
