using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls.PlatformConfiguration.GTKSpecific;

namespace Microsoft.Maui.Controls
{
	public partial class TabbedPage
	{
		MauiTabbedPage PlatformView => Handler?.PlatformView as MauiTabbedPage ?? throw new InvalidOperationException("Native View not set");

		MauiTabbedPage CreatePlatformView(TabbedPage tabbedPage)
		{
			var mauiTabbedPage = new MauiTabbedPage();
			
			tabbedPage.PropertyChanged += (s, e) => 
			{
				if (e.PropertyName == PlatformConfiguration.GTKSpecific.TabbedPage.TabPositionProperty.PropertyName)
				{
					UpdateTabPosition(tabbedPage, mauiTabbedPage);
				}
			};

			UpdateChildPages(tabbedPage, mauiTabbedPage);

			mauiTabbedPage.SwitchPage += MauiTabbedPage_SwitchPage;

			return mauiTabbedPage;
		}

		private void UpdateTabPosition(TabbedPage tabbedPage, MauiTabbedPage mauiTabbedPage)
		{
			var pos = PlatformConfiguration.GTKSpecific.TabbedPage.GetTabPosition(tabbedPage);

			mauiTabbedPage.TabPos = pos switch
			{
				TabPosition.Top => Gtk.PositionType.Top,
				TabPosition.Bottom => Gtk.PositionType.Bottom,
				_ => Gtk.PositionType.Top
			};

			mauiTabbedPage.QueueResize();
			mauiTabbedPage.QueueDraw();
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

		private static void UpdateChildPages(TabbedPage tabbedPage, MauiTabbedPage mauiTabbedPage)
		{
			var mauiContext = tabbedPage.Handler?.MauiContext;
			if (mauiContext == null)
				return;

			var navigationItems = mauiTabbedPage.NavigationItems;

			var toRemove = new List<int>();

			for (int i = 0; i < navigationItems.Count; i++)
			{
				var navItem = navigationItems[i];
				var hasItem = tabbedPage.Children.Any(x => x.Id == navItem.Id);

				if (!hasItem)
				{
					toRemove.Add(i);
				}
			}

			foreach (var idx in toRemove)
			{
				navigationItems.RemoveAt(idx);
				mauiTabbedPage.RemovePage(idx);
			}

			for (int i = 0; i < tabbedPage.Children.Count; i++)
			{
				var page = tabbedPage.Children[i];
				var existingNavItem = navigationItems.FirstOrDefault(x => x.Id == page.Id);

				if (existingNavItem != default)
				{
					existingNavItem.Order = i;
					existingNavItem.TabPageLabel.TextLabel.Text = page.Title;
					if (page.IconImageSource != null)
					{
						var newIconSourceHash = page.IconImageSource.GetHashCode();
						if (existingNavItem.TabPageLabel.IconSourceHash != page.IconImageSource.GetHashCode())
						{
							existingNavItem.TabPageLabel.Icon = GetImage(page.IconImageSource, mauiContext);
							existingNavItem.TabPageLabel.IconSourceHash = newIconSourceHash;
						}
					}
					else
					{
						existingNavItem.TabPageLabel.Icon = null;
						existingNavItem.TabPageLabel.IconSourceHash = 0;
					}
				}
				else
				{
					var platformChild = page.ToPlatform(mauiContext);

					var image = GetImage(page.IconImageSource, mauiContext);
					var iconSourceHash = page.IconImageSource?.GetHashCode() ?? 0;

					var navItem = new MauiTabbedPage.MauiTabbedPageNaviagtionItem(platformChild, new MauiTabbedPage.MauiTabPageLabel(page.Title, image, iconSourceHash), page.Id)
					{
						Order = i
					};

					navigationItems.Add(navItem);
					navItem.TabPageLabel.Show();
				}
			}

			mauiTabbedPage.UpdateTabPages();

			mauiTabbedPage.BarBackgroundColor = tabbedPage.BarBackgroundColor;
			mauiTabbedPage.SelectedItemTextColor = tabbedPage.SelectedTabColor;
			mauiTabbedPage.UnselectedItemTextColor = tabbedPage.UnselectedTabColor;

			int index = tabbedPage.Children.IndexOf(tabbedPage.CurrentPage);
			mauiTabbedPage.CurrentPage = index;
		}

		private static Gtk.Image? GetImage(ImageSource imageSource, IMauiContext mauiContext)
		{
			Gtk.Image? image = null;

			if (imageSource != null)
			{
				var task = imageSource.GetPlatformImageAsync(mauiContext);
				task.Wait();

				var pixBuf = task.Result?.Value;
				if (pixBuf != null)
				{
					image = new Gtk.Image(pixBuf.ScaleSimple(16, 16, InterpType.Bilinear));
				}
			}
			return image;
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
			view.PlatformView.BarTextColor = view.BarTextColor;
		}

		internal static void MapUnselectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.UnselectedItemTextColor = view.UnselectedTabColor;
		}

		internal static void MapSelectedTabColor(ITabbedViewHandler handler, TabbedPage view)
		{
			view.PlatformView.SelectedItemTextColor = view.SelectedTabColor;
		}

		internal static void MapItemsSource(ITabbedViewHandler handler, TabbedPage tabbedPage)
		{
			if (tabbedPage?.Handler?.MauiContext == null)
				return;

			var mauiTabbedPage = tabbedPage.PlatformView;

			UpdateChildPages(tabbedPage, mauiTabbedPage);
			
			handler.UpdateValue(nameof(TabbedPage.CurrentPage));
		}

		internal static void MapItemTemplate(ITabbedViewHandler handler, TabbedPage tabbedPage)
		{
			if (tabbedPage?.Handler?.MauiContext == null)
				return;

			var mauiTabbedPage = tabbedPage.PlatformView;

			UpdateChildPages(tabbedPage, mauiTabbedPage);

			handler.UpdateValue(nameof(TabbedPage.CurrentPage));
		}

		internal static void MapSelectedItem(ITabbedViewHandler handler, TabbedPage tabbedPage)
		{
			if (tabbedPage?.Handler?.MauiContext == null)
				return;

			var mauiTabbedPage = tabbedPage.PlatformView;

			UpdateChildPages(tabbedPage, mauiTabbedPage);

			handler.UpdateValue(nameof(TabbedPage.CurrentPage));
		}

		internal static void MapCurrentPage(ITabbedViewHandler handler, TabbedPage tabbedPage)
		{
			int index = tabbedPage.Children.IndexOf(tabbedPage.CurrentPage);
			tabbedPage.PlatformView.CurrentPage = index;
		}
	}
}