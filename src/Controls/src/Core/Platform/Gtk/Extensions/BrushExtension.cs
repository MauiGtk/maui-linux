using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls.Platform;

public static class BrushExtension
{
	public static string? ToCss(this Brush? brush)
	{
		if (brush is null)
			return null;

		return brush switch
		{
			SolidColorBrush solid => solid.ToCss(),
			LinearGradientBrush linear => linear.ToCss(),
			RadialGradientBrush radial => radial.ToCssBackground(),
			_ => null
		};
	}

	public static string? ToCssBackgroundDeclaration(this Brush? brush)
	{
		var value = brush.ToCss();
		return value is null ? null : $"background: {value};";
	}

	public static string? ToCss(this SolidColorBrush brush)
	{
		return brush.Color.ToCssColor();
	}

	public static string? ToCss(this LinearGradientBrush brush)
	{
		if (brush.GradientStops == null || brush.GradientStops.Count == 0)
			return null;

		// CSS: linear-gradient(<angle>, color1 offset1, color2 offset2, ...)
		var angle = ComputeCssAngleFromPoints(brush.StartPoint, brush.EndPoint);

		var stops = string.Join(", ",
			brush.GradientStops
				 .OrderBy(s => s.Offset)
				 .Select(s =>
				 {
					 var col = s.Color.ToCssColor();
					 var offsetPercent = (int)(s.Offset * 100);
					 return $"{col} {offsetPercent}%";
				 }));

		return $"linear-gradient({angle}deg, {stops})";
	}

	public static string? ToCssBackground(this RadialGradientBrush brush)
	{
		if (brush.GradientStops == null || brush.GradientStops.Count == 0)
			return null;

		// CSS: radial-gradient(circle at x% y%, color1 offset1, ...)
		var centerX = (int)(brush.Center.X * 100);
		var centerY = (int)(brush.Center.Y * 100);

		var stops = string.Join(", ",
			brush.GradientStops
				 .OrderBy(s => s.Offset)
				 .Select(s =>
				 {
					 var col = s.Color.ToCssColor();
					 var offsetPercent = (int)(s.Offset * 100);
					 return $"{col} {offsetPercent}%";
				 }));

		return $"radial-gradient(circle at {centerX}% {centerY}%, {stops})";
	}

	static string? ToCss(this ImageBrush brush, MauiContext mauiContext)
	{
		var task = brush.ImageSource.GetPlatformImageAsync(mauiContext);
		task.Wait();

		var pixBuf = task.Result?.Value;
		if (pixBuf != null)
		{
			return $"url('{pixBuf.ToBase64PngDataUrl()}')";
		}

		return null;
	}

	public static string ToBase64PngDataUrl(this Gdk.Pixbuf pixbuf)
	{
		byte[] buffer = pixbuf.SaveToBuffer("png");
		string base64 = Convert.ToBase64String(buffer);

		return $"data:image/png;base64,{base64}";
	}
	
	private static double ComputeCssAngleFromPoints(Point start, Point end)
	{
		// MAUI: Point in 0..1 relativ zur Fläche
		// Wir rechnen den Vektor (start -> end) in einen Winkel um.
		// CSS: 0deg = nach oben; 90deg = nach rechts.
		// Wir nehmen hier eine einfache Umrechnung und müssen nicht zu 100 % exakt sein.

		var dx = end.X - start.X;
		var dy = end.Y - start.Y;

		if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001)
		{
			// Degenerierter Fall: einfach 180deg (nach unten)
			return 180.0;
		}

		// Atan2: Angle in Radiant (x: rechts, y: nach unten, aber CSS will 0deg = nach oben)
		var rad = Math.Atan2(dy, dx); // 0 = rechts

		// 0deg in CSS = nach oben, also drehen wir das entsprechend um:
		var deg = (rad * 180.0 / Math.PI) + 90.0;

		// Normalisieren auf 0..360
		if (deg < 0)
			deg += 360.0;

		return Math.Round(deg, 2);
	}
}
