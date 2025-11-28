using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cairo;
using Pango;

// ReSharper disable CA2211

// ReSharper disable InconsistentNaming

namespace Microsoft.Maui.GtkInterop
{

	public static class DllImportFontConfig
	{

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate bool d_FcConfigAppFontAddFile(System.IntPtr config, string fontPath);

		public static d_FcConfigAppFontAddFile FcConfigAppFontAddFile = FuncLoader.LoadFunction<d_FcConfigAppFontAddFile>(FuncLoader.GetProcAddress(GLibrary.Load(Library.Fontconfig), "FcConfigAppFontAddFile"));
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_FcInitLoadConfigAndFonts();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_FcObjectSetCreate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate bool d_FcObjectSetAdd(IntPtr os, string @object);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void d_FcObjectSetDestroy(IntPtr os);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_FcFontList(IntPtr config, IntPtr pattern, IntPtr objectSet);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int d_FcPatternGetString(IntPtr pattern, string @object, int n, out IntPtr s);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void d_FcFontSetDestroy(IntPtr fontset);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_FcConfigGetCurrent();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate bool d_FcConfigSetCurrent(IntPtr config);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr d_FcPatternCreate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void d_FcPatternDestroy(IntPtr pattern);

		

		private static readonly IntPtr s_fcHandle =	GLibrary.Load(Library.Fontconfig);

		public static readonly d_FcInitLoadConfigAndFonts FcInitLoadConfigAndFonts = FuncLoader.LoadFunction<d_FcInitLoadConfigAndFonts>(FuncLoader.GetProcAddress(s_fcHandle, "FcInitLoadConfigAndFonts"));

		public static readonly d_FcObjectSetCreate FcObjectSetCreate = FuncLoader.LoadFunction<d_FcObjectSetCreate>(FuncLoader.GetProcAddress(s_fcHandle, "FcObjectSetCreate"));

		public static readonly d_FcObjectSetAdd FcObjectSetAdd = FuncLoader.LoadFunction<d_FcObjectSetAdd>(FuncLoader.GetProcAddress(s_fcHandle, "FcObjectSetAdd"));

		public static readonly d_FcObjectSetDestroy FcObjectSetDestroy = FuncLoader.LoadFunction<d_FcObjectSetDestroy>(FuncLoader.GetProcAddress(s_fcHandle, "FcObjectSetDestroy"));

		public static readonly d_FcFontList FcFontList = FuncLoader.LoadFunction<d_FcFontList>(FuncLoader.GetProcAddress(s_fcHandle, "FcFontList"));

		public static readonly d_FcPatternGetString FcPatternGetString = FuncLoader.LoadFunction<d_FcPatternGetString>(FuncLoader.GetProcAddress(s_fcHandle, "FcPatternGetString"));

		public static readonly d_FcFontSetDestroy FcFontSetDestroy = FuncLoader.LoadFunction<d_FcFontSetDestroy>(FuncLoader.GetProcAddress(s_fcHandle, "FcFontSetDestroy"));

		public static readonly d_FcConfigGetCurrent FcConfigGetCurrent = FuncLoader.LoadFunction<d_FcConfigGetCurrent>(FuncLoader.GetProcAddress(s_fcHandle, "FcConfigGetCurrent"));

		public static readonly d_FcConfigSetCurrent FcConfigSetCurrent = FuncLoader.LoadFunction<d_FcConfigSetCurrent>(FuncLoader.GetProcAddress(s_fcHandle, "FcConfigSetCurrent"));

		public static readonly d_FcPatternCreate FcPatternCreate = FuncLoader.LoadFunction<d_FcPatternCreate>(FuncLoader.GetProcAddress(s_fcHandle, "FcPatternCreate"));
		public static readonly d_FcPatternDestroy FcPatternDestroy = FuncLoader.LoadFunction<d_FcPatternDestroy>(FuncLoader.GetProcAddress(s_fcHandle, "FcPatternDestroy"));

		[StructLayout(LayoutKind.Sequential)]
		private struct FcFontSet
		{
			public int nfont;
			public int sfont;
			public IntPtr fonts; // pointer to array of FcPattern*
		}

		public record FontInfo(string Family, string Style, string File);

		public static List<(string family, string style, string file)> ListActiveFonts()
		{
			var cfg = FcConfigGetCurrent();
			if (cfg == IntPtr.Zero)
				cfg = FcInitLoadConfigAndFonts();

			var pattern = FcPatternCreate();
			if (pattern == IntPtr.Zero)
				throw new InvalidOperationException("FcPatternCreate failed.");

			var os = FcObjectSetCreate();
			if (os == IntPtr.Zero)
			{
				FcPatternDestroy(pattern);
				throw new InvalidOperationException("FcObjectSetCreate failed.");
			}
			if (!FcObjectSetAdd(os, "family"))
			{ FcObjectSetDestroy(os); FcPatternDestroy(pattern); throw new InvalidOperationException("FcObjectSetAdd(family) failed."); }
			if (!FcObjectSetAdd(os, "style"))
			{ FcObjectSetDestroy(os); FcPatternDestroy(pattern); throw new InvalidOperationException("FcObjectSetAdd(style) failed."); }
			if (!FcObjectSetAdd(os, "file"))
			{ FcObjectSetDestroy(os); FcPatternDestroy(pattern); throw new InvalidOperationException("FcObjectSetAdd(file) failed."); }

			var fsPtr = FcFontList(cfg, pattern, os);

			FcObjectSetDestroy(os);
			FcPatternDestroy(pattern);

			var list = new List<(string, string, string)>();
			if (fsPtr == IntPtr.Zero)
				return list;

			var fs = Marshal.PtrToStructure<FcFontSet>(fsPtr);
			for (int i = 0; i < fs.nfont; i++)
			{
				var pat = Marshal.ReadIntPtr(fs.fonts, i * IntPtr.Size);
				string fam = TryGetStr(pat, "family") ?? "?";
				string sty = TryGetStr(pat, "style") ?? "?";
				string file = TryGetStr(pat, "file") ?? "?";
				list.Add((fam, sty, file));
			}

			FcFontSetDestroy(fsPtr);
			return list;
		}

		static string? TryGetStr(IntPtr pat, string key)
		{
			return (FcPatternGetString(pat, key, 0, out var p) == 0 && p != IntPtr.Zero)
				? Marshal.PtrToStringUTF8(p)
				: null;
		}

		#region Pango
		static readonly IntPtr s_PangoFtHandle = GLibrary.Load(Library.PangoFt);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void d_pango_fc_font_map_config_changed(IntPtr fontmap);

		static d_pango_fc_font_map_config_changed pango_fc_font_map_config_changed = FuncLoader.LoadFunction<d_pango_fc_font_map_config_changed>(FuncLoader.GetProcAddress(s_PangoFtHandle, "pango_fc_font_map_config_changed"));

		/// <summary>
		/// Notifies the Pango font configuration system that the font map configuration has changed.
		/// </summary>
		/// <remarks>Can be removed after implementation in PangoSharp</remarks>
		public static void PangoFcFontMapConfigChanged()
		{
			using var surf = new ImageSurface(Format.ARGB32, 1, 1);
			using var cr = new Cairo.Context(surf);
			using var layout = CairoHelper.CreateLayout(cr);

			var fontMap = layout.Context.FontMap;
			var handleProp = fontMap.GetType().GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

			var ptr = (IntPtr)(handleProp?.GetValue(fontMap) ?? IntPtr.Zero);
			if (ptr != IntPtr.Zero)
				pango_fc_font_map_config_changed(ptr);
		}
		#endregion Pango
	}

}