using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gdk;
using GLib;
using Gtk;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Controls
{
	public partial class Toolbar
	{
		public List<ToolbarItemMap> ToolbarItemMaps { get; } = new();

		MauiToolbar PlatformView => Handler?.PlatformView as MauiToolbar ?? throw new InvalidOperationException("Native View not set");

		public static void MapBarTextColor(ToolbarHandler arg1, Toolbar arg2) =>
			MapBarTextColor((IToolbarHandler)arg1, arg2);

		public static void MapBarBackground(ToolbarHandler arg1, Toolbar arg2) =>
			MapBarBackground((IToolbarHandler)arg1, arg2);

		public static void MapBackButtonTitle(ToolbarHandler arg1, Toolbar arg2) =>
			MapBackButtonTitle((IToolbarHandler)arg1, arg2);

		public static void MapToolbarItems(ToolbarHandler arg1, Toolbar arg2) =>
			MapToolbarItems((IToolbarHandler)arg1, arg2);

		public static void MapTitleView(ToolbarHandler arg1, Toolbar arg2) =>
			MapTitleView((IToolbarHandler)arg1, arg2);

		public static void MapIconColor(ToolbarHandler arg1, Toolbar arg2) =>
			MapIconColor((IToolbarHandler)arg1, arg2);

		public static void MapTitleIcon(ToolbarHandler arg1, Toolbar arg2) =>
			MapTitleIcon((IToolbarHandler)arg1, arg2);

		public static void MapBackButtonVisible(ToolbarHandler arg1, Toolbar arg2) =>
			MapBackButtonVisible((IToolbarHandler)arg1, arg2);

		public static void MapIsVisible(ToolbarHandler arg1, Toolbar arg2) =>
			MapIsVisible((IToolbarHandler)arg1, arg2);

		public static void MapBarTextColor(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView.TextColor = toolbar.BarTextColor;
		}

		public static void MapBackButtonEnabled(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView.UpdateBackButtonEnabled(toolbar.BackButtonEnabled);
		}

		public static void MapIsVisible(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView?.UpdateIsVisible(toolbar);
		}

		public static void MapBackButtonVisible(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView?.UpdateBackButtonVisibility(toolbar);
		}

		public static void MapTitleIcon(IToolbarHandler handler, Toolbar toolbar)
		{
			Pixbuf? pixBuf = null;
			if (handler.MauiContext != null)
			{
				var task = toolbar.TitleIcon.GetPlatformImageAsync(handler.MauiContext);
				task.Wait();

				pixBuf = task.Result?.Value;
			}

			handler.PlatformView.UpdateTitleIcon(pixBuf);
		}

		public static void MapTitleView(IToolbarHandler handler, Toolbar toolbar)
		{
			var platformView = (handler.MauiContext != null) ? toolbar.TitleView?.ToPlatform(handler.MauiContext) : null;
			handler.PlatformView.UpdateTitleView(platformView);
		}

		public static void MapIconColor(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView.UpdateIconColor(toolbar.IconColor);
		}

		public static void MapToolbarItems(IToolbarHandler handler, Toolbar toolbar)
		{
			toolbar.UpdateMenu(handler, toolbar);
		}

		public static void MapBackButtonTitle(IToolbarHandler handler, Toolbar toolbar)
		{
			handler.PlatformView.UpdateBackButtonTitle(toolbar.BackButtonTitle);
		}

		public static void MapBarBackground(IToolbarHandler handler, Toolbar toolbar)
		{
			var css = toolbar.BarBackground.ToCss();
			toolbar.PlatformView.UpdateBarBackground(css);
		}
		
		void UpdateMenu(IToolbarHandler toolbarHandler, Toolbar toolbar)
		{
			toolbar.PlatformView.ClearToolbarItems();
			toolbar.PlatformView.ClearMenuItems();

			foreach (var toolbarItem in ToolbarItems)
			{
				var map = ToolbarItemMaps.FirstOrDefault(x => x.ToolbarItem == toolbarItem);

				if (map == default)
				{
					map = new(toolbarItem);
					ToolbarItemMaps.Add(map);
				}

				if (toolbarItem.Order != ToolbarItemOrder.Secondary)
				{
					map.MauiToolbarItem = UpdateMauiToolbarItem(map.ToolbarItem, map.MauiToolbarItem ?? new(), toolbarHandler);
					map.MauiToolbarItem.UpdateTextColor(toolbar.BarTextColor);
					map.MauiToolbarItem.UpdateIconColor(toolbar.IconColor);
					toolbar.PlatformView.AddToolbarItem(map.MauiToolbarItem);
				}

				map.MenuItem = UpdateMauiToolbarItem(map.ToolbarItem, map.MenuItem ?? new(), toolbarHandler);
				map.MenuItem.UpdateIconColor(toolbar.IconColor);
				toolbar.PlatformView.AddMenuItem(map.MenuItem);
			}
			toolbar.PlatformView.ArrangeToolbarChilds();
			toolbar.PlatformView.ArrangeToolbarItems();
		}

		private MauiToolbarItem UpdateMauiToolbarItem(ToolbarItem toolbarItem, MauiToolbarItem mauiToolbarItem, IToolbarHandler toolbarHandler)
		{
			mauiToolbarItem.Label = toolbarItem.Text;
			mauiToolbarItem.Sensitive = toolbarItem.IsEnabled;
			mauiToolbarItem.Priority = toolbarItem.Priority;
			mauiToolbarItem.Order = (MauiToolbarItem.EOrder)(int)toolbarItem.Order;
			mauiToolbarItem.ID = toolbarItem.Id;

			mauiToolbarItem.Clicked -= MauiToolbarItem_Clicked;
			mauiToolbarItem.Clicked += MauiToolbarItem_Clicked;

			if (toolbarHandler.MauiContext != null)
			{
				if (toolbarItem.IconImageSource == null || toolbarItem.IconImageSource.IsEmpty)
				{
					mauiToolbarItem.Image = null;
				}
				else
				{
					var task = toolbarItem.IconImageSource.GetPlatformImageAsync(toolbarHandler.MauiContext);
					task.Wait();

					var pixBuf = task.Result?.Value;
					if (pixBuf != null)
					{
						mauiToolbarItem.PixBuf = pixBuf;
						mauiToolbarItem.Image = new Gtk.Image(pixBuf.ScaleSimple(16, 16, InterpType.Bilinear));
					}
				}
			}

			return mauiToolbarItem;
		}

		private void MauiToolbarItem_Clicked(object? sender, EventArgs e)
		{
			if(sender is not MauiToolbarItem mauiToolbarItem)
				return;

			var map = ToolbarItemMaps.FirstOrDefault(x => x.MauiToolbarItem == mauiToolbarItem || x.MenuItem == mauiToolbarItem);
			if (map != null && map.ToolbarItem?.Command != null && map.ToolbarItem.Command.CanExecute(map.ToolbarItem.CommandParameter))
			{
				map.ToolbarItem.Command.Execute(map.ToolbarItem.CommandParameter);
			}
		}

		public class ToolbarItemMap
		{
			public ToolbarItem ToolbarItem { get; set; }

			public MauiToolbarItem? MauiToolbarItem { get; set; }

			public MauiToolbarItem? MenuItem { get; set; }

			public ToolbarItemMap(ToolbarItem toolbarItem) 
			{
				ToolbarItem = toolbarItem;
			}
		}
	}

}