using System;
using System.IO;
using System.Linq;
using Cairo;
using Microsoft.Maui.GtkInterop;
using Pango;
using Path = System.IO.Path;

namespace Microsoft.Maui
{
	public partial class EmbeddedFontLoader
	{
		public string? LoadFont(EmbeddedFont font)
		{
			var cfg = DllImportFontConfig.FcConfigGetCurrent();
			if (cfg == IntPtr.Zero)
			{
				cfg = DllImportFontConfig.FcInitLoadConfigAndFonts();
				DllImportFontConfig.FcConfigSetCurrent(cfg);
			}

			RegisterFromStream(font.ResourceStream!, font.FontName!, cfg);
			DllImportFontConfig.PangoFcFontMapConfigChanged();

			// For debugging: list all active fonts
			// var fonts = DllImportFontConfig.ListActiveFonts();
			// GetPangoCairoFonts();

			return font.FontName;
		}

		/// <summary>
		/// Registers a font from the specified stream into the font configuration.
		/// </summary>
		/// <remarks>This method reads the font data from the provided stream, writes it to a temporary file, and
		/// registers the file with the specified font configuration. The temporary file is created in the system's temporary
		/// directory and will persist for the duration of the application's execution.</remarks>
		/// <param name="fontStream">The <see cref="Stream"/> containing the font data. The stream must be readable.</param>
		/// <param name="filename">The name of the font file, including its extension. This is used to determine the font type and generate a
		/// temporary file.</param>
		/// <param name="cfg">A handle to the font configuration object where the font will be registered.</param>
		/// <exception cref="InvalidOperationException">Thrown if the font cannot be added to the font configuration.</exception>
		static void RegisterFromStream(Stream fontStream, string filename, nint cfg)
		{
			var extension = Path.GetExtension(filename).ToLowerInvariant();
			var pureFilename = Path.GetFileNameWithoutExtension(filename);

			// Stream → byte[]
			byte[] bytes;
			if (fontStream is MemoryStream ms && ms.TryGetBuffer(out var seg))
			{
				bytes = seg.Array is null ? ms.ToArray() : seg.Array.AsSpan(seg.Offset, seg.Count).ToArray();
			}
			else
			{
				using var tmp = new MemoryStream();
				fontStream.CopyTo(tmp);
				bytes = tmp.ToArray();
			}

			var baseDir = Path.Combine(Path.GetTempPath(), "maui-app-fonts");
			Directory.CreateDirectory(baseDir);
			var tempPath = Path.Combine(baseDir, filename);

			if (!File.Exists(tempPath))
				File.WriteAllBytes(tempPath, bytes);

			bool ok = DllImportFontConfig.FcConfigAppFontAddFile(cfg, tempPath);
			if (!ok)
				throw new InvalidOperationException($"FcConfigAppFontAddFile fehlgeschlagen: {tempPath}");
		}

		static FontFamily[] GetPangoCairoFonts(bool writeOutput = true)
		{
			using var surf = new ImageSurface(Format.ARGB32, 1, 1);
			using var cr = new Cairo.Context(surf);
			using var layout = CairoHelper.CreateLayout(cr);

			var fontMap = layout.Context.FontMap;
			var families = fontMap.Families;

			if (writeOutput) 
			{
				foreach (var family in families)
				{
					Console.WriteLine(family.Name);
					foreach (var face in family.Faces)
						Console.WriteLine($"  - {face.FaceName} ({face.Describe()})");
				}
			}

			return families;
		}
	}
}
