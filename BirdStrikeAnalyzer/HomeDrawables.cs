using BirdStrikeAnalyzer.Model;
using Microsoft.Maui.Graphics;

namespace BirdStrikeAnalyzer;

/// <summary>Stylized campus map with one circle per building. Circle size and color grow with the report count.</summary>
public sealed class CampusMapDrawable : IDrawable
{
    private readonly IReadOnlyList<BuildingReportCount> _buildings;
    private readonly int _max;

    public CampusMapDrawable(IReadOnlyList<BuildingReportCount> buildings)
    {
        _buildings = buildings;
        _max = Math.Max(1, buildings.Max(b => b.Count));
    }

    public static Color ColorFor(int count, int max)
    {
        var t = (float)count / max;
        return t switch
        {
            < 0.25f => Color.FromArgb("#FFD166"),
            < 0.55f => Color.FromArgb("#F79B3D"),
            _ => Color.FromArgb("#E5484D"),
        };
    }

    public void Draw(ICanvas canvas, RectF r)
    {
        // Land
        canvas.FillColor = Color.FromArgb("#F7F6F2");
        canvas.FillRectangle(r);

        // Green space
        canvas.FillColor = Color.FromArgb("#CDEBD3");
        canvas.FillRectangle(r.Left, r.Top + r.Height * 0.78f, r.Width * 0.22f, r.Height * 0.22f);
        canvas.FillColor = Color.FromArgb("#EFEBDD");
        canvas.FillRectangle(r.Left + r.Width * 0.55f, r.Top, r.Width * 0.25f, r.Height * 0.14f);

        // Roads
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Color.FromArgb("#D3DAE2");
        canvas.StrokeSize = 22;
        canvas.DrawLine(r.Left + r.Width * 0.02f, r.Top + r.Height * 0.12f, r.Left + r.Width * 0.98f, r.Top + r.Height * 0.95f);   // High Ave
        canvas.StrokeSize = 16;
        canvas.DrawLine(r.Left + r.Width * 0.50f, r.Top, r.Left + r.Width * 0.95f, r.Top + r.Height * 0.78f);                       // Algoma Blvd
        canvas.StrokeSize = 6;
        canvas.StrokeColor = Color.FromArgb("#E3E8EE");
        canvas.DrawLine(r.Left, r.Top + r.Height * 0.55f, r.Left + r.Width * 0.45f, r.Top + r.Height * 1.0f);

        canvas.FontColor = Color.FromArgb("#64748B");
        canvas.FontSize = 11;
        canvas.DrawString("High Ave", r.Left + r.Width * 0.30f, r.Top + r.Height * 0.60f, 80, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString("Algoma Blvd", r.Left + r.Width * 0.72f, r.Top + r.Height * 0.40f, 90, 16, HorizontalAlignment.Left, VerticalAlignment.Center);

        // Building circles (draw the biggest first so small ones stay visible)
        float scale = Math.Min(r.Width, r.Height);
        foreach (var b in _buildings.OrderByDescending(b => b.Count))
        {
            var cx = r.Left + b.X * r.Width;
            var cy = r.Top + b.Y * r.Height;
            var radius = scale * (0.035f + 0.085f * MathF.Sqrt((float)b.Count / _max));
            var color = ColorFor(b.Count, _max);

            canvas.FillColor = color.WithAlpha(0.55f);
            canvas.FillCircle(cx, cy, radius);
            canvas.StrokeColor = color;
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(cx, cy, radius);

            canvas.FontColor = Color.FromArgb("#101820");
            canvas.FontSize = 12;
            canvas.DrawString($"{b.Name} ({b.Count})", cx - 60, cy - 8, 120, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Legend
        var lx = r.Left + 12;
        var ly = r.Bottom - 30;
        canvas.FillColor = Colors.White.WithAlpha(0.9f);
        canvas.FillRoundedRectangle(lx - 6, ly - 6, 220, 28, 6);
        (string Label, Color C)[] legend =
        [
            ("Few", Color.FromArgb("#FFD166")),
            ("Some", Color.FromArgb("#F79B3D")),
            ("Many", Color.FromArgb("#E5484D")),
        ];
        canvas.FontSize = 11;
        canvas.FontColor = Color.FromArgb("#101820");
        foreach (var (label, c) in legend)
        {
            canvas.FillColor = c;
            canvas.FillCircle(lx + 6, ly + 8, 6);
            canvas.DrawString(label, lx + 16, ly, 44, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
            lx += 70;
        }
    }
}

/// <summary>Grouped bar chart: months on the x axis, number of reports on the y axis, one color per building.</summary>
public sealed class ReportsBarChartDrawable : IDrawable
{
    private readonly IReadOnlyList<string> _months;
    private readonly IReadOnlyList<MonthlySeries> _series;

    public ReportsBarChartDrawable(IReadOnlyList<string> months, IReadOnlyList<MonthlySeries> series)
    {
        _months = months;
        _series = series;
    }

    public void Draw(ICanvas canvas, RectF r)
    {
        const float left = 38, right = 12, top = 24, bottom = 30;
        var plot = new RectF(r.Left + left, r.Top + top, Math.Max(1, r.Width - left - right), Math.Max(1, r.Height - top - bottom));
        var rawMax = _series.SelectMany(series => series.Values).DefaultIfEmpty(0).Max();
        var max = Math.Max(1, (int)Math.Ceiling(rawMax / 5d) * 5);
        canvas.FontSize = 10;
        for (var value = 0; value <= max; value += Math.Max(1, max / 4))
        {
            var y = plot.Bottom - plot.Height * value / max;
            canvas.StrokeColor = Color.FromArgb("#E2E8F0");
            canvas.StrokeSize = 1;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
            canvas.FontColor = Color.FromArgb("#64748B");
            canvas.DrawString(value.ToString(), r.Left, y - 8, left - 6, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
        }
        if (_months.Count == 0 || _series.Count == 0) return;
        var groupWidth = plot.Width / _months.Count;
        var seriesCount = _series.Count;
        var barWidth = Math.Min(24f, groupWidth * 0.72f / seriesCount);
        for (var month = 0; month < _months.Count; month++)
        {
            var groupLeft = plot.Left + month * groupWidth + (groupWidth - barWidth * seriesCount) / 2;
            for (var series = 0; series < seriesCount; series++)
            {
                var values = _series[series].Values;
                var value = month < values.Length ? values[month] : 0;
                var height = plot.Height * value / max;
                canvas.FillColor = Color.FromArgb(_series[series].Color);
                canvas.FillRoundedRectangle(groupLeft + series * barWidth, plot.Bottom - height, Math.Max(1, barWidth - 2), height, 2);
            }
            canvas.FontColor = Color.FromArgb("#475467");
            canvas.DrawString(_months[month], plot.Left + month * groupWidth, plot.Bottom + 4, groupWidth, 18, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        canvas.FillColor = Color.FromArgb("#1682D4");
        canvas.FillRoundedRectangle(plot.Left, r.Top + 7, 10, 10, 2);
        canvas.FontColor = Color.FromArgb("#101820");
        canvas.DrawString("Confirmed strikes", plot.Left + 14, r.Top + 4, 130, 16, HorizontalAlignment.Left, VerticalAlignment.Center);
    }
}
