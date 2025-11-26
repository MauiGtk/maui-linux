using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Maui.Storage
{

	partial class FileSystemImplementation : IFileSystem
	{

		static string CleanPath(string path) =>
			string.Join("_", path.Split(Path.GetInvalidFileNameChars()));

		static string AppSpecificPath =>
			Path.Combine(CleanPath(AppInfoImplementation.PublisherName), CleanPath(AppInfo.PackageName));

		string PlatformCacheDirectory
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSpecificPath, "Cache");

		string PlatformAppDataDirectory
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSpecificPath, "Data");

		Task<Stream> PlatformOpenAppPackageFileAsync(string filename)
		{
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));

			if (GtkBuildSettings.MauiAssetBehavior == GtkBuildSettings.MauiResourceBehavior.CopyFiles)
			{
				var file = FileSystemUtils.PlatformGetFullAppPackageFilePath(filename);

				return Task.FromResult((Stream)File.OpenRead(file));
			}
			else
			{
				var (assembly, resourceName) = FileSystemUtils.GetMauiRessource(FileSystemUtils.MauiResourceType.MauiAsset, filename);

				if (assembly == null || string.IsNullOrWhiteSpace(resourceName))
					return null;

				return Task.FromResult(assembly.GetManifestResourceStream(resourceName));
			}

		}

		Task<bool> PlatformAppPackageFileExistsAsync(string filename)
		{
			if (GtkBuildSettings.MauiAssetBehavior == GtkBuildSettings.MauiResourceBehavior.CopyFiles)
			{
				var file = FileSystemUtils.PlatformGetFullAppPackageFilePath(filename);

				return Task.FromResult(File.Exists(file));
			}
			else
			{
				var (assembly, resourceName) = FileSystemUtils.GetMauiRessource(FileSystemUtils.MauiResourceType.MauiAsset, filename);

				return Task.FromResult(assembly != null && !string.IsNullOrWhiteSpace(resourceName));
			}
		}

	}

	static partial class FileSystemUtils
	{

		public static bool AppPackageFileExists(string filename)
		{
			if (GtkBuildSettings.MauiAssetBehavior == GtkBuildSettings.MauiResourceBehavior.CopyFiles)
			{ 
				var file = PlatformGetFullAppPackageFilePath(filename);

				return File.Exists(file);
			}
			else
			{
				var (assembly, ressourceName) = GetMauiRessource(MauiResourceType.MauiAsset, filename);

				return (assembly != null && !string.IsNullOrWhiteSpace(ressourceName));
			}
		}

		public static string PlatformGetFullAppPackageFilePath(string filename)
		{
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));

			return GetFilePath(MauiResourceType.MauiAsset, filename);
		}

		public static string GetFilePath(MauiResourceType mauiResourceType, string filename = "", double scale = 1)
		{
			filename = NormalizePath(filename);

			string filePath = Path.Combine(GtkBuildSettings.MauiGTKDefaultDirectory, mauiResourceType.ToString() + "s");

			if (mauiResourceType == MauiResourceType.MauiImage || mauiResourceType == MauiResourceType.MauiSplashScreen)
			{
				var rgxMatchScaling = new Regex(@"scale-(\d+)", RegexOptions.Compiled);

				if (scale <= 0)
					scale = 1;
				int scaleInt = (int)Math.Round(scale * 100);

				string extension = "";
				var pureFilename = "";
				string scaledFilename = "";

				if (!string.IsNullOrWhiteSpace(filename))
				{
					extension = Path.GetExtension(filename);
					pureFilename = Path.GetFileNameWithoutExtension(filename);
					scaledFilename = $"{pureFilename}.scale-{scaleInt}{extension}";
				}

				if (!string.IsNullOrWhiteSpace(scaledFilename))
				{
					var imageFile = Path.Combine(filePath, scaledFilename);

					if (File.Exists(imageFile))
						return imageFile;
				}

				var files = Directory.GetFiles(filePath).Select(x => Path.GetFileName(x)).ToArray();

				if (mauiResourceType == MauiResourceType.MauiImage)
				{
					files = files.Where(x => x.StartsWith(pureFilename) && x.EndsWith(extension) && rgxMatchScaling.IsMatch(x)).ToArray();

					if (files.Length == 0)
						return default;
				}
				else if (mauiResourceType == MauiResourceType.MauiSplashScreen)
				{
					if (!string.IsNullOrEmpty(pureFilename))
					{
						files = files.Where(x => x.StartsWith(pureFilename) && x.EndsWith(extension) && rgxMatchScaling.IsMatch(x)).ToArray();
					}
					else
					{
						files = files.Where(x => rgxMatchScaling.IsMatch(x)).ToArray();
					}

					if (files.Length == 0)
						return null;

					var scaledFile = files.FirstOrDefault(x => x.Contains($"scale-{scaleInt}", StringComparison.InvariantCulture));
					if (scaledFile != null)
					{
						var imageFile = Path.Combine(filePath, scaledFile);
						if (File.Exists(imageFile))
							return imageFile;
					}
				}

				var scaling = files.Where(x => rgxMatchScaling.IsMatch(x)).Select(filename => (int.Parse(rgxMatchScaling.Match(filename).Groups[1].Value), filename));
				var closestScale = scaling.OrderBy(s => Math.Abs(s.Item1 - scaleInt)).FirstOrDefault();
				
				if (closestScale != default)
					return Path.Combine(filePath, closestScale.filename);

				return null;
			}
			else if (mauiResourceType == MauiResourceType.MauiAsset)
			{
				return Path.Combine(filePath, filename);
			}
			else if (mauiResourceType == MauiResourceType.MauiFont)
			{
				return Path.Combine(filePath, filename);
			}

			return null;
		}

		public static (Assembly, string) GetMauiRessource(MauiResourceType mauiResourceType, string filename = "", double scale = 1)
		{
			var normalizedFilename = filename.Replace('\\', '.').Replace('/', '.');

			var resourcePath = "";
			var resourceTypePath = "." + mauiResourceType.ToString() + "s";

			resourcePath = GtkBuildSettings.MauiEmbededResourceNamespace + resourceTypePath;

			var resourceNames = GtkBuildSettings.EntryAssembly.GetManifestResourceNames();
			var embededResourceName = resourcePath + "." + normalizedFilename;

			if (resourceNames.Contains(embededResourceName))
				return (GtkBuildSettings.EntryAssembly, embededResourceName);

			if (mauiResourceType == MauiResourceType.MauiAsset || mauiResourceType == MauiResourceType.MauiFont)
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					resourceNames = assembly.GetManifestResourceNames();
					embededResourceName = resourceNames.FirstOrDefault(x => x.EndsWith(resourceTypePath + normalizedFilename, StringComparison.InvariantCulture));
					if (!string.IsNullOrEmpty(embededResourceName))
						return (GtkBuildSettings.EntryAssembly, embededResourceName);
				}
			}
			else
			{
				if (mauiResourceType == MauiResourceType.MauiImage && string.IsNullOrWhiteSpace(filename))
					return (null, null);

				var rgxMatchScaling = new Regex(@"scale-(\d+)", RegexOptions.Compiled);

				if (scale <= 0)
					scale = 1;
				int scaleInt = (int)Math.Round(scale * 100);

				string extension = "";
				var pureFilename = "";
				string scaledFilename = "";

				if (!string.IsNullOrWhiteSpace(filename))
				{
					extension = Path.GetExtension(filename);
					pureFilename = Path.GetFileNameWithoutExtension(filename);
					scaledFilename = $"{pureFilename}.scale-{scaleInt}{extension}";

					var embededResourceScaledName = resourcePath + "." + scaledFilename;

					if (resourceNames.Contains(embededResourceScaledName))
						return (GtkBuildSettings.EntryAssembly, embededResourceScaledName);
				}

				resourceTypePath = GtkBuildSettings.MauiGTKNamespace + resourceTypePath;

				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					var names = assembly.GetManifestResourceNames();
					if (mauiResourceType == MauiResourceType.MauiImage)
					{
						names = names.Where(x => x.Contains(resourceTypePath, StringComparison.InvariantCulture) && x.Contains(pureFilename, StringComparison.InvariantCulture) && x.EndsWith(extension)).ToArray();
						
						if (names.Length == 0)
							continue;

						var scaledFullqualifiedName = names.FirstOrDefault(x => x.Contains(scaledFilename, StringComparison.InvariantCulture));

						if (scaledFullqualifiedName != null)
						{
							return new(assembly, scaledFullqualifiedName);
						}
					}
					else
					{
						names = names.Where(x => x.Contains(resourceTypePath, StringComparison.InvariantCulture)).ToArray();
						if (!string.IsNullOrWhiteSpace(pureFilename) && !string.IsNullOrWhiteSpace(extension))
							names = names.Where(x => x.Contains(pureFilename, StringComparison.InvariantCulture) && x.EndsWith(extension)).ToArray();
					}

					if (names.Length == 0)
						continue;

					var scaling = names.Where(x => rgxMatchScaling.IsMatch(x)).Select(resourceName => (int.Parse(rgxMatchScaling.Match(resourceName).Groups[1].Value), resourceName)).ToArray();
					var closestScale = scaling.OrderBy(s => Math.Abs(s.Item1 - scaleInt)).FirstOrDefault();
					
					if (closestScale != default)
						return new(assembly, closestScale.resourceName);
				}

			}

			return (null, null);
		}

		public enum MauiResourceType
		{
			MauiAsset,
			MauiImage,
			MauiFont,
			MauiSplashScreen
		}
	}

	/// <include file="../../docs/Microsoft.Maui.Essentials/FileBase.xml" path="Type[@FullName='Microsoft.Maui.Essentials.FileBase']/Docs" />
	public partial class FileBase
	{

		static string PlatformGetContentType(string extension) =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		internal void Init(FileBase file) =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		internal virtual Task<Stream> PlatformOpenReadAsync()
			=> throw ExceptionUtils.NotSupportedOrImplementedException;

		void PlatformInit(FileBase file)
			=> throw ExceptionUtils.NotSupportedOrImplementedException;

	}

}