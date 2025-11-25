using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Gdk;

namespace Microsoft.Maui
{
	public static class PixBufExtension
	{
		public static Pixbuf Tint(this Pixbuf original, Microsoft.Maui.Graphics.Color color)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			var tinted = new Pixbuf(
				original.Colorspace,
				original.HasAlpha,
				original.BitsPerSample,
				original.Width,
				original.Height);

			original.CopyArea(0, 0, original.Width, original.Height, tinted, 0, 0);

			int width = tinted.Width;
			int height = tinted.Height;
			int rowstride = tinted.Rowstride;
			int channels = tinted.NChannels;

			int bufferSize = rowstride * height;
			var data = new byte[bufferSize];

			Marshal.Copy(tinted.Pixels, data, 0, bufferSize);

			// MAUI Color: 0.0–1.0
			double tr = color.Red;
			double tg = color.Green;
			double tb = color.Blue;

			for (int y = 0; y < height; y++)
			{
				int rowStart = y * rowstride;

				for (int x = 0; x < width; x++)
				{
					int idx = rowStart + x * channels;

					byte r = data[idx + 0];
					byte g = data[idx + 1];
					byte b = data[idx + 2];

					data[idx + 0] = (byte)(r * tr);
					data[idx + 1] = (byte)(g * tg);
					data[idx + 2] = (byte)(b * tb);
				}
			}

			Marshal.Copy(data, 0, tinted.Pixels, bufferSize);

			return tinted;
		}

		public static Pixbuf TintFlat(this Pixbuf original, Microsoft.Maui.Graphics.Color color)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			var tinted = new Pixbuf(
				original.Colorspace,
				original.HasAlpha,
				original.BitsPerSample,
				original.Width,
				original.Height);

			original.CopyArea(0, 0, original.Width, original.Height, tinted, 0, 0);

			int width = tinted.Width;
			int height = tinted.Height;
			int rowstride = tinted.Rowstride;
			int channels = tinted.NChannels;

			int bufferSize = rowstride * height;
			var data = new byte[bufferSize];

			Marshal.Copy(tinted.Pixels, data, 0, bufferSize);

			byte rTarget = (byte)(color.Red * 255);
			byte gTarget = (byte)(color.Green * 255);
			byte bTarget = (byte)(color.Blue * 255);

			for (int y = 0; y < height; y++)
			{
				int rowStart = y * rowstride;

				for (int x = 0; x < width; x++)
				{
					int idx = rowStart + x * channels;

					// wenn Alpha vorhanden → nutzen
					byte a = channels == 4 ? data[idx + 3] : (byte)255;

					if (a == 0)
						continue; // komplett transparent, ignorieren

					data[idx + 0] = rTarget;
					data[idx + 1] = gTarget;
					data[idx + 2] = bTarget;
					// Alpha unverändert
				}
			}

			Marshal.Copy(data, 0, tinted.Pixels, bufferSize);

			return tinted;
		}
	}
}
