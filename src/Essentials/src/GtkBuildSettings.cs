using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Maui
{
	/// <summary>
	/// Provides configuration settings for GTK builds in a .NET MAUI application.
	/// </summary>
	/// <remarks>This class offers static properties to access various build settings related to GTK resources, such
	/// as embedded resource namespaces and resource behaviors for images, fonts, splash screens, and assets.</remarks>
	public static class GtkBuildSettings
	{
		static Assembly entryAssembly = null;
		/// <summary>
		/// Gets the entry assembly for the current application domain.
		/// </summary>
		public static Assembly EntryAssembly
		{
			get
			{
				if (entryAssembly == null)
				{
					entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
				}
				return entryAssembly; 
			}
		}


		static Type gtkBuildSettingsType = null;
		/// <summary>
		/// Gets the <see cref="Type"/> representing the GTK build settings.
		/// </summary>
		public static Type GtkBuildSettingsType
		{
			get 
			{
				if (gtkBuildSettingsType == null)
				{
					gtkBuildSettingsType = EntryAssembly?.GetType("Microsoft.Maui.GtkBuildSettingsContainer");
				}
				
				return gtkBuildSettingsType; 
			}
		}


		static string? _mauiEmbededResourceNamespace = null;
		/// <summary>
		/// Gets the default namespace used for MauiAssets/MauiImages/MauiFonts in a Maui application.
		/// </summary>
		public static string MauiEmbededResourceNamespace
		{
			get
			{
				if (_mauiEmbededResourceNamespace != null)
					return _mauiEmbededResourceNamespace;

				var mauiDefaultEmbededResourceNamespace = GtkBuildSettingsType?.GetField("MauiEmbededResourceNamespace")?.GetValue(null)?.ToString();

				if (mauiDefaultEmbededResourceNamespace != null)
				{
					_mauiEmbededResourceNamespace = mauiDefaultEmbededResourceNamespace;
				}
				else
				{
					_mauiEmbededResourceNamespace = EntryAssembly.GetName().Name + ".MauiGTK";
				}

				return _mauiEmbededResourceNamespace;
			}
		}

		static string? _mauiGTKNamespace = null;
		/// <summary>
		/// Gets the default namespace used for MauiAssets/MauiImages/MauiFonts in a Maui application.
		/// </summary>
		public static string MauiGTKNamespace
		{
			get
			{
				if (_mauiGTKNamespace != null)
					return _mauiGTKNamespace;

				var mauiGTKNamespace = GtkBuildSettingsType?.GetField("MauiEmbededResourceNamespace")?.GetValue(null)?.ToString();

				if (mauiGTKNamespace != null)
				{
					_mauiGTKNamespace = mauiGTKNamespace;
				}
				else
				{
					_mauiGTKNamespace = EntryAssembly.GetName().Name + ".MauiGTK";
				}

				return _mauiGTKNamespace;
			}
		}

		static string? _mauiGTKDefaultDirectory = null;
		/// <summary>
		/// Gets the default directory path for MauiAssets/MauiImages/MauiFonts in a Maui GTK application.
		/// </summary>
		public static string MauiGTKDefaultDirectory
		{
			get
			{
				if (_mauiGTKDefaultDirectory != null)
					return _mauiGTKDefaultDirectory;

				var mauiGTKDefaultDirectory = GtkBuildSettingsType?.GetField("MauiGTKDefaultDirectory")?.GetValue(null)?.ToString();

				if (mauiGTKDefaultDirectory != null)
				{
					_mauiGTKDefaultDirectory = Path.Combine(AppContext.BaseDirectory, mauiGTKDefaultDirectory);
				}
				else
				{
					_mauiGTKDefaultDirectory = Path.Combine(AppContext.BaseDirectory, "MauiGTK");
				}

				return _mauiGTKDefaultDirectory;
			}
		}

		static MauiResourceBehavior? _mauiImageBehavior = null;
		/// <summary>
		/// Gets the behavior for handling Maui image resources.
		/// </summary>
		public static MauiResourceBehavior MauiImageBehavior
		{
			get
			{
				if (_mauiImageBehavior != null)
					return (MauiResourceBehavior)_mauiImageBehavior;

				var mauiImageBehavior = GtkBuildSettingsType?.GetField("MauiImageBehavior")?.GetValue(null)?.ToString();

				if (mauiImageBehavior != null)
				{
					_mauiImageBehavior = (MauiResourceBehavior)Enum.Parse(typeof(MauiResourceBehavior), mauiImageBehavior);
				}
				else
				{
					_mauiImageBehavior = MauiResourceBehavior.EmbedFiles;
				}

				return (MauiResourceBehavior)_mauiImageBehavior;
			}
		}

		static MauiResourceBehavior? _mauiFontBehavior = null;
		/// <summary>
		/// Gets the behavior for handling Maui fonts in the application.
		/// </summary>
		public static MauiResourceBehavior MauiFontBehavior
		{
			get
			{
				if (_mauiFontBehavior != null)
					return (MauiResourceBehavior)_mauiFontBehavior;

				var mauiFontBehavior = GtkBuildSettingsType?.GetField("MauiFontBehavior")?.GetValue(null)?.ToString();

				if (mauiFontBehavior != null)
				{
					_mauiFontBehavior = (MauiResourceBehavior)Enum.Parse(typeof(MauiResourceBehavior), mauiFontBehavior);
				}
				else
				{
					_mauiFontBehavior = MauiResourceBehavior.EmbedFiles;
				}

				return (MauiResourceBehavior)_mauiFontBehavior;
			}
		}

		static MauiResourceBehavior? _mauiSplashBehavior = null;
		/// <summary>
		/// Gets the behavior for handling Maui splash resources.
		/// </summary>
		public static MauiResourceBehavior MauiSplashBehavior
		{
			get
			{
				if (_mauiSplashBehavior != null)
					return (MauiResourceBehavior)_mauiSplashBehavior;

				var mauiSplashBehavior = GtkBuildSettingsType?.GetField("MauiSplashBehavior")?.GetValue(null)?.ToString();

				if (mauiSplashBehavior != null)
				{
					_mauiSplashBehavior = (MauiResourceBehavior)Enum.Parse(typeof(MauiResourceBehavior), mauiSplashBehavior);
				}
				else
				{
					_mauiSplashBehavior = MauiResourceBehavior.EmbedFiles;
				}

				return (MauiResourceBehavior)_mauiSplashBehavior;
			}
		}

		static MauiResourceBehavior? _mauiAssetBehavior = null;
		/// <summary>
		/// Gets the behavior for handling Maui assets in the application.
		/// </summary>
		public static MauiResourceBehavior MauiAssetBehavior
		{
			get
			{
				if (_mauiAssetBehavior != null)
					return (MauiResourceBehavior)_mauiAssetBehavior;

				var mauiAssetBehavior = GtkBuildSettingsType?.GetField("MauiAssetBehavior")?.GetValue(null)?.ToString();

				if (mauiAssetBehavior != null)
				{
					_mauiAssetBehavior = (MauiResourceBehavior)Enum.Parse(typeof(MauiResourceBehavior), mauiAssetBehavior);
				}
				else
				{
					_mauiAssetBehavior = MauiResourceBehavior.EmbedFiles;
				}

				return (MauiResourceBehavior)_mauiAssetBehavior;
			}
		}

		/// <summary>
		/// Specifies the behavior for handling Maui resources in a project.
		/// </summary>
		public enum MauiResourceBehavior
		{
			/// <summary>
			/// Embeds the specified files into the current project.
			/// </summary>
			EmbedFiles,

			/// <summary>
			/// Copies the specified files to the output directory.
			/// </summary>
			CopyFiles
		}
	}
}
