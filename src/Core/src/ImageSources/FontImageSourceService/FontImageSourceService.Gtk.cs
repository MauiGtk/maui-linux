#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Cairo;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Pango;
using NativeImage = Gdk.Pixbuf;


namespace Microsoft.Maui
{
	public partial class FontImageSourceService
	{
		public override Task<IImageSourceServiceResult<NativeImage>?> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default) =>
			GetImageAsync((IFontImageSource)imageSource, scale, cancellationToken);

		public Task<IImageSourceServiceResult<NativeImage>?> GetImageAsync(IFontImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
		{
			if (imageSource.IsEmpty)
				return FromResult(null);

			try
			{
				// TODO: use a cached way
				var image = RenderImage(imageSource, scale);

				if (image == null)
					throw new InvalidOperationException("Unable to generate font image.");

				var result = new ImageSourceServiceResult(image, true, () => image.Dispose());

				return FromResult(result);
			}
			catch (Exception ex)
			{
				Logger?.LogWarning(ex, "Unable to generate font image '{Glyph}'.", imageSource.Glyph);
				throw;
			}
		}

		static Task<IImageSourceServiceResult<NativeImage>?> FromResult(IImageSourceServiceResult<NativeImage>? result) =>
			Task.FromResult(result);

		internal NativeImage RenderImage(IFontImageSource imageSource, float scale)
		{
			var fontManager = FontManager;

			var fontDescription = fontManager.GetFontFamily(imageSource.Font);
			
			var fontSize = fontManager.GetFontSize(imageSource.Font);

			var color = imageSource.Color ?? Colors.White;

			var width = (int)(fontSize * scale);
			var height = (int)(fontSize * scale);

			using (var surface = new ImageSurface(Format.ARGB32, width, height))
			using (var context = new Cairo.Context(surface))
			{
				context.SetSourceRGBA(1, 1, 1, 0);
				context.Paint();
				
				var layout = CairoHelper.CreateLayout(context);

				layout.FontDescription = fontDescription;
				
				layout.SetText(imageSource.Glyph);
				
				layout.GetPixelSize(out int textWidth, out int textHeight);
				double x = (width - textWidth) / 2.0;
				double y = (height - textHeight) / 2.0;
				context.MoveTo(x, y);

				context.SetSourceRGBA(color.Red, color.Green, color.Blue, color.Alpha);
				
				CairoHelper.ShowLayout(context, layout);

				return new NativeImage(surface, 0, 0, width, height);
			}
		}

	}
}