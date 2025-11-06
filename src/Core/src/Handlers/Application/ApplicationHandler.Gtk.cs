using System;
using GLib;

namespace Microsoft.Maui.Handlers
{
	public partial class ApplicationHandler : ElementHandler<IApplication, Gtk.Application>
	{
		public static partial void MapTerminate(ApplicationHandler handler, IApplication application, object? args) 
		{
			Gtk.Application.Default.Quit();
		}

		public static partial void MapOpenWindow(ApplicationHandler handler, IApplication application, object? args) 
		{
			var platformView = handler.PlatformView;
			if (platformView is null) return;

			var request = (args as OpenWindowRequest);
			platformView.CreatePlatformWindow(application, request);
		}

		public static partial void MapCloseWindow(ApplicationHandler handler, IApplication application, object? args)
		{
			var app = handler.PlatformView;
			if (app is null) return;
			
			if (args is IWindow mauiWindow)
			{
				var platformWin = mauiWindow.Handler?.PlatformView as Gtk.Window;
				if (platformWin != null)
				{
					platformWin.Close();
					return;
				}
			}
		}
	}
}