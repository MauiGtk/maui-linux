#nullable enable

using System.IO;

namespace Microsoft.Maui
{
	public partial class FontRegistrar : IFontRegistrar
	{
		string? LoadNativeAppFont(string font, string filename, string? alias)
		{
			if(GtkBuildSettings.MauiFontBehavior == GtkBuildSettings.MauiResourceBehavior.EmbedFiles)
			{
				var (assembly, resourceName) = Storage.FileSystemUtils.GetMauiRessource(Storage.FileSystemUtils.MauiResourceType.MauiFont, filename);

				var stream = assembly.GetManifestResourceStream(resourceName);

				if(stream == null)
					throw new FileNotFoundException($"Embedded font with the name {filename} was not found.");

				return LoadEmbeddedFont(font, filename, alias, stream);
			}
			else
			{
				using var stream = GetNativeFontStream(filename, alias);

				return LoadEmbeddedFont(font, filename, alias, stream);
			}
		}

		static FileStream GetNativeFontStream(string filename, string? alias)
		{
			var fontPath = Storage.FileSystemUtils.GetFilePath(Storage.FileSystemUtils.MauiResourceType.MauiFont, filename);
			if (File.Exists(fontPath))
				return File.OpenRead(fontPath);

			throw new FileNotFoundException($"Native font with the name {filename} was not found.");
		}
	}
}