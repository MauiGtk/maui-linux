using System;
using GLib;
using Gtk;
using PlatformView = Microsoft.Maui.Platform.MauiMenuItem;

namespace Microsoft.Maui.Handlers
{
	public partial class MenuFlyoutItemHandler
	{
		protected AccelGroup AccelGroup { get; set; }

		protected override PlatformView CreatePlatformElement()
		{
			return new();
		}

		protected override void ConnectHandler(PlatformView platformView)
		{
			base.ConnectHandler(PlatformView);
			PlatformView.Activated += OnClicked;

			if (MauiContext != null)
			{
				AccelGroup = new AccelGroup();
				var window = MauiContext.GetPlatformWindow();
				window.AddAccelGroup(AccelGroup);
			}
		}

		protected override void DisconnectHandler(PlatformView platformView)
		{
			base.DisconnectHandler(PlatformView);
			PlatformView.Activated -= OnClicked;
		}

		void OnClicked(object? sender, EventArgs e)
		{
			VirtualView.Clicked();
		}

		public static void MapIsEnabled(IMenuFlyoutItemHandler handler, IMenuElement view) =>
			handler.PlatformView.UpdateIsEnabled(view.IsEnabled);

		public static void MapText(IMenuFlyoutItemHandler handler, IMenuElement view)
		{
			handler.PlatformView.UpdateText(view.Text);
		}

		public static void MapIsEnabled(IMenuBarItemHandler handler, IMenuBarItem view) =>
			handler.PlatformView.UpdateIsEnabled(view.IsEnabled);

		public static void MapKeyboardAccelerators(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			handler.PlatformView.UpdateKeyboardAccelerators(((MenuFlyoutItemHandler)handler).AccelGroup, view.KeyboardAccelerators);
		}

		public static void MapSource(IMenuFlyoutItemHandler handler, IMenuFlyoutItem view)
		{
			if (handler.MauiContext == null)
				return;

			handler.PlatformView.UpdateImageSource(view.Source, handler.MauiContext);
		}
	}
}