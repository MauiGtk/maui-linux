using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Pango;
using static Microsoft.Maui.GtkInterop.DllImportFontConfig;

namespace Microsoft.Maui
{ 
	public class FontManager : IFontManager
	{

		readonly IFontRegistrar _fontRegistrar;

		static Pango.Context? _systemContext;

		Pango.Context SystemContext => _systemContext ??= Gdk.PangoHelper.ContextGet();

		public FontManager(IFontRegistrar fontRegistrar, IServiceProvider? serviceProvider = null)
		{
			_fontRegistrar = fontRegistrar;
		}

		FontDescription? _defaultFontFamily;

		public FontDescription DefaultFontFamily
		{
			get => _defaultFontFamily ??= GetFontFamily(default);
		}

		double? _defaultFontSize;

		public double DefaultFontSize => _defaultFontSize ??= DefaultFontFamily?.GetSize() ?? 0;

		public FontDescription GetFontFamily(Font font)
		{
			if (font != default)
			{
				if(!string.IsNullOrWhiteSpace(font.Family)) _fontRegistrar.GetFont(font.Family);
				return font.ToFontDescription();
			}
			else
			{
				return SystemContext.FontDescription;
			}
		}

		public double GetFontSize(Font font)
		{
			return font.Size;
		}

		private IEnumerable<(Pango.FontFamily family, Pango.FontDescription description)> GetAvailableFamilyFaces(Pango.FontFamily family)
		{

			if (family != default)
			{
				foreach (var face in family.Faces)
					yield return (family, face.Describe());
			}

			yield break;
		}

		private FontDescription[] GetAvailableFontStyles()
		{
			var fontFamilies = SystemContext.FontMap?.Families.ToArray();

			var styles = new List<FontDescription>();

			if (fontFamilies != null)
			{
				styles.AddRange(fontFamilies.SelectMany(GetAvailableFamilyFaces).Select(font => font.description)
				   .OrderBy(d => d.Family));
			}

			return styles.ToArray();
		}

		internal static bool AddFontFile(string fontPath)
		{
			// Try to add font file to the current fontconfig configuration
			var result = FcConfigAppFontAddFile(System.IntPtr.Zero, fontPath);

			if (result)
			{
				_systemContext = null;
			}

			return result;
		}

	}

}