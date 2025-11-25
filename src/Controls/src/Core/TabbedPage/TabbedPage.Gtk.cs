using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Gdk;
using GLib;
using Gtk;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Handlers.Items.Platform;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui.Controls
{
	public partial class TabbedPage
	{
		MauiTabbedPage PlatformView => Handler?.PlatformView as MauiTabbedPage ?? throw new InvalidOperationException("Native View not set");

		MauiTabbedPage CreatePlatformView(TabbedPage tabbedPage)
		{
			var mauiTabbedPage = new MauiTabbedPage();

			CreateChildPages(tabbedPage, mauiTabbedPage);

			mauiTabbedPage.SwitchPage += MauiTabbedPage_SwitchPage;

			return mauiTabbedPage;
		}

		private void MauiTabbedPage_SwitchPage(object sender, SwitchPageArgs args)
		{
			var newPage = this.Children[(int)args.PageNum];
			this.CurrentPage = newPage;
		}

		private static MauiTabbedPage? OnCreatePlatformView(ViewHandler<ITabbedView, MauiTabbedPage> handler)
		{
			if (handler.VirtualView is TabbedPage tabbedPage)
			{
				return tabbedPage.CreatePlatformView(tabbedPage);
			}

			return null;
		}

		private void CreateChildPages(TabbedPage tabbedPage, MauiTabbedPage mauiTabbedPage)
		{
			if (tabbedPage?.Handler?.MauiContext == null)
				return;

			foreach (var page in tabbedPage.Children)
			{
				var platformChild = page.ToPlatform(tabbedPage.Handler.MauiContext);

				Gtk.Image? image = null;

				if (page.IconImageSource != null)
				{
					var task = page.IconImageSource.GetPlatformImageAsync(tabbedPage.Handler.MauiContext);
					task.Wait();

					var pixBuf = task.Result?.Value;
					if (pixBuf != null)
					{
						image = new Gtk.Image(pixBuf.ScaleSimple(16, 16, InterpType.Bilinear));
					}
				}

				mauiTabbedPage.AppendPage(platformChild, new MauiTabbedPage.MauiTabbedPageLabel(page.Title, image));
			}
		}

		internal static void MapBarBackground(ITabbedViewHandler handler, TabbedPage view)
		{
			var css = view.BarBackground.ToCss();
			view.PlatformView.UpdateBarBackground(css);
		}

		internal static void MapBarBackgroundColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.BarBackgroundColor = view.BarBackgroundColor;
		}

		internal static void MapBarTextColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.SetLabelTextColor(view.BarTextColor);
		}

		internal static void MapUnselectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.UnselectedColor = view.UnselectedTabColor;
		}

		internal static void MapSelectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.SelectedColor = view.SelectedTabColor;
		}
		
		internal static void MapItemsSource(ITabbedViewHandler handler, TabbedPage view)
		{
			//not needed
		}

		internal static void MapItemTemplate(ITabbedViewHandler handler, TabbedPage view)
		{
			//not needed
		}

		internal static void MapSelectedItem(ITabbedViewHandler handler, TabbedPage view)
		{
			//not needed
		}

		internal static void MapCurrentPage(ITabbedViewHandler handler, TabbedPage view)
		{
			int index = view.Children.IndexOf(view.CurrentPage);
			view.PlatformView.CurrentPage = index;
		}
	}
}