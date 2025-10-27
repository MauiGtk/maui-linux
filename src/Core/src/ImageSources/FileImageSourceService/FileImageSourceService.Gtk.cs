#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using NativeImage = Gdk.Pixbuf;

namespace Microsoft.Maui
{

	public partial class FileImageSourceService
	{

		public override Task<IImageSourceServiceResult<NativeImage>?> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) =>
			GetImageAsync((IFileImageSource)imageSource, scale, cancellationToken);

		public Task<IImageSourceServiceResult<NativeImage>?> GetImageAsync(IFileImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
		{
			if (imageSource.IsEmpty)
				return FromResult(null);

			var filename = imageSource.File;
			var pureFilename = Path.GetFileNameWithoutExtension(imageSource.File);
			var ext = Path.GetExtension(imageSource.File);

			int scaleInt = (int)Math.Round(scale * 100);
			string scaledFilename = $"{pureFilename}.scale-{scaleInt}{ext}";

			var rgxMatchScaling = new Regex(@"scale-(\d+)");

			NativeImage? TryLoadFile()
			{
				if (File.Exists(filename))
					return new NativeImage(filename);

				if (File.Exists(scaledFilename))
					return new(scaledFilename);

				var files = Directory.GetFiles(AppContext.BaseDirectory, "*"+ext).Select(x => Path.GetFileName(x)).ToArray();
				files = files.Where(x => x.StartsWith(pureFilename) && rgxMatchScaling.IsMatch(x)).ToArray();

				if (files.Length == 0) return null;

				var scaling = files.Where(x => rgxMatchScaling.IsMatch(x)).ToDictionary(x => int.Parse(rgxMatchScaling.Match(x).Groups[1].Value), y => y);
				var closestScale = scaling.Keys.OrderBy(s => Math.Abs(s - scaleInt)).FirstOrDefault();
				var res = scaling[closestScale];

				if (res != null) return new(Path.Combine(AppContext.BaseDirectory, res));

				return null;
			}

			NativeImage? TryLoadEmbededMauiImage()
			{
				var baseName = $"MauiGTK.MauiImages.{pureFilename}";
				
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					var names = assembly.GetManifestResourceNames();
					names = names.Where(x => x.Contains(baseName, StringComparison.InvariantCulture) && x.EndsWith(ext)).ToArray();

					if (names.Length == 0) continue;
					
					var scaledFullqualifiedName = names.FirstOrDefault(x => x.Contains(scaledFilename, StringComparison.InvariantCulture));

					if (scaledFullqualifiedName != null)
					{
						return new(assembly, scaledFullqualifiedName);
					}

					var scaling = names.Where(x => rgxMatchScaling.IsMatch(x)).ToDictionary(x => int.Parse(rgxMatchScaling.Match(x).Groups[1].Value), y => y);
					var closestScale = scaling.Keys.OrderBy(s => Math.Abs(s - scaleInt)).FirstOrDefault();
					var res = scaling[closestScale];

					if (res != null) return new(assembly, res);
				}

				return default;
			}

			try
			{
				var image = TryLoadFile();

				if (image == null)
				{
					image = TryLoadEmbededMauiImage();
				}

				if (image == null)
					throw new InvalidOperationException("Unable to load image file.");

				var result = new ImageSourceServiceResult(image, () => image.Dispose()) { };

				return FromResult(result);
			}
			catch (Exception ex)
			{
				Logger?.LogWarning(ex, "Unable to load image file '{File}'.", filename);

				throw;
			}
		}

		static Task<IImageSourceServiceResult<NativeImage>?> FromResult(IImageSourceServiceResult<NativeImage>? result) =>
			Task.FromResult(result);

		
	}

}