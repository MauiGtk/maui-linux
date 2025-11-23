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
			
			NativeImage? TryLoadFile()
			{
				if (File.Exists(filename))
					return new NativeImage(filename);

				if (GtkBuildSettings.MauiImageBehavior !=  GtkBuildSettings.MauiResourceBehavior.CopyFiles) return default;

				var filePath = FileSystemUtils.GetFilePath(FileSystemUtils.MauiResourceType.MauiImage, filename, scale);

				if (filePath != null && File.Exists(filePath))
					return new NativeImage(filePath);

				filePath = FileSystemUtils.GetFilePath(FileSystemUtils.MauiResourceType.MauiImage, filename, scale);
				
				if (filePath != null && File.Exists(filePath))
					return new NativeImage(filePath);
				
				return default;
			}

			NativeImage? TryLoadEmbededMauiImage()
			{
				if (GtkBuildSettings.MauiImageBehavior != GtkBuildSettings.MauiResourceBehavior.EmbedFiles)	return default;

				var (assembly, resourceName) = FileSystemUtils.GetMauiRessource(FileSystemUtils.MauiResourceType.MauiImage, filename, scale);

				if (assembly != null && !string.IsNullOrWhiteSpace(resourceName))
				{
					return new(assembly, resourceName);
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