using System;
using System.Collections.Generic;
using Gtk;

namespace Microsoft.Maui.Handlers
{

	public partial class ViewHandler
	{
		[MissingMapper]
		static partial void MappingFrame(IViewHandler handler, IView view)
		{
			var platformView = handler.ToPlatform();
			platformView.Arrange(view.Frame);
		}

		[MissingMapper]
		public static void MapTranslationX(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapTranslationY(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapScale(IViewHandler handler, IView view)
		{
			//handler.PlatformView.UpdateScale(view.Scale);
		}

		[MissingMapper]
		public static void MapScaleX(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapScaleY(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapRotation(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapRotationX(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapRotationY(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapAnchorX(IViewHandler handler, IView view) { }

		[MissingMapper]
		public static void MapAnchorY(IViewHandler handler, IView view) { }

		public static void MapContextFlyout(IViewHandler handler, IView view)
		{
			var mauiContext = handler.MauiContext ?? throw new InvalidOperationException($"The handler's {nameof(handler.MauiContext)} cannot be null.");

			if (view is IContextFlyoutElement contextFlyoutContainer)
			{
				var contextFlyout = contextFlyoutContainer.ContextFlyout;

				if (contextFlyout == null) return;
				
				var contextFlyoutHandler = contextFlyout.ToHandler(handler.MauiContext);
				if (contextFlyoutHandler.PlatformView is MauiMenu contextFlyoutPlatformView)
				{

					var eventHolder = view.ToPlatform(handler.MauiContext);
					if (eventHolder.Parent is WrapperView wrapperView && eventHolder.Events == 0)
					{
						eventHolder = wrapperView;
					}

					eventHolder.AddEvents((int)Gdk.EventMask.ButtonPressMask);
					eventHolder.AddEvents((int)Gdk.EventMask.ButtonReleaseMask);
					eventHolder.AddEvents((int)Gdk.EventMask.AllEventsMask);

					//Currently, can't prevent the original ContextMenu so we clean it and clone our Elements in it
					(eventHolder as Entry)?.PopulatePopup += (o, args) =>
					{
						var defaultMenu = (args.Popup as Gtk.Menu);

						if (defaultMenu != null)
						{
							foreach (Gtk.MenuItem child in defaultMenu.AllChildren)
							{
								defaultMenu.Remove(child);
							}

							var clonedItems = CloneMenuItems(contextFlyoutPlatformView);

							foreach (var item in clonedItems)
							{
								item.ShowAll();
								item.Show();
								defaultMenu.Add(item);
							}
						}

						//This should prevent from original ContextMenu but it does'nt
						//args.RetVal = true;
					};

					eventHolder.ButtonPressEvent += (o, args) =>
					{
						if (args.Event.Button == 3) // Right click
						{
							contextFlyoutPlatformView.ShowAll();
							contextFlyoutPlatformView.Popup();
							args.RetVal = true;
						}
					};

					eventHolder.PopupMenu += (o, args) =>
					{
						contextFlyoutPlatformView!.ShowAll();
						contextFlyoutPlatformView.Popup();
						args.RetVal = true;
					};
				}
			}
		}

		private static List<MenuItem> CloneMenuItems(Menu menu)
		{
			var clonedItems = new List<MenuItem>();

			foreach (var item in menu.Children)
			{
				if (item is MauiMenuItem menuItem)
				{
					var clone = new MauiMenuItem()
					{
						Sensitive = menuItem.Sensitive,
						NeedsIconPlaceholder = menuItem.NeedsIconPlaceholder
					};

					clone.Label.Text = menuItem.Label.Text;
					if (menuItem.IconPixBuf != null)
					{
						clone.IconPixBuf = menuItem.IconPixBuf;
					}

					clone.Activated += (o, args) =>
					{
						menuItem.Activate();
					};

					clone.ArrangeControls();

					if (menuItem.Submenu != null && menuItem.Submenu is Menu subMenu)
					{
						var subItems = CloneMenuItems(subMenu);
						foreach(var subItem in subItems)
						{
							clone.AppendSubItem(subItem);
						}
					}
					clone.Show();
					clonedItems.Add(clone);
				}
				else if (item is Gtk.SeparatorMenuItem seperatorItem)
				{
					var clone = new Gtk.SeparatorMenuItem();
					clone.Show();
					clonedItems.Add(clone);
				}
			}

			return clonedItems;
		}

		internal static void MapContextFlyout(IElementHandler handler, IContextFlyoutElement contextFlyoutContainer)
		{
			

			var contextFlyout = contextFlyoutContainer.ContextFlyout;
			//var eventHolder = contextFlyoutContainer.

			//if (handler.PlatformView is Microsoft.UI.Xaml.UIElement uiElement)
			//{
			if (contextFlyout != null)
			{
				var contextFlyoutHandler = contextFlyout.ToHandler(handler.MauiContext);
				var contextFlyoutPlatformView = contextFlyoutHandler.PlatformView;

				//if (contextFlyoutPlatformView is FlyoutBase flyoutBase)
				//{
				//	uiElement.ContextFlyout = flyoutBase;
				//}
			}
			else
			{
				//uiElement.ClearValue(UIElement.ContextFlyoutProperty);
			}
		}

		public static void MapToolbar(IViewHandler handler, IView view)
		{
			if (view is IToolbarElement tb)
				MapToolbar(handler, tb);
		}

		internal static void MapToolbar(IElementHandler handler, IToolbarElement tb)
		{
			if (handler.MauiContext is not null)
			{
				var toolbarContainer = handler.MauiContext.GetToolBarContainer();
				toolbarContainer?.SetToolbar(tb.Toolbar?.ToPlatform(handler.MauiContext) as MauiToolbar);
			}
		}
	}

}