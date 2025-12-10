using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Microsoft.Maui.Handlers
{
	// https://docs.gtk.org/gtk3/class.MenuShell.html

	public abstract class GtkMenuShellHandler<TVirtualView, TPlatform, TVirtualItem, TPlatformItem> : ElementHandler<TVirtualView, TPlatform>
		where TPlatform : Gtk.MenuShell, new()
		where TVirtualView : class, IList<TVirtualItem>, IElement
		where TVirtualItem : IElement
		where TPlatformItem : MauiMenuItem
	{
		protected GtkMenuShellHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper, commandMapper) { }

		protected override TPlatform CreatePlatformElement()
		{
			return new();
		}

		public override void SetVirtualView(IElement view)
		{
			base.SetVirtualView(view);
			Clear();

			foreach (var item in (TVirtualView)view)
			{
				Add(item);
			}
		}

		public void Add(TVirtualItem view)
		{
			var platformItem = (MenuItem)view.ToPlatform(MauiContext!);
			PlatformView.Append(platformItem);

			UpdateIconPlaceholder();

			platformItem.Show();
		}

		public void Remove(TVirtualItem view)
		{
			var platformItem = (MenuItem)view.ToPlatform(MauiContext!);
			PlatformView.Remove(platformItem);

			UpdateIconPlaceholder();
		}

		public void Insert(int index, TVirtualItem view)
		{
			var platformItem = (MenuItem)view.ToPlatform(MauiContext!);
			PlatformView.Insert(platformItem, index);

			UpdateIconPlaceholder();

			platformItem.Show();
		}

		public void Clear()
		{
			foreach (var c in PlatformView.Children.ToArray())
			{
				PlatformView.Remove(c);
			}
		}

		private void UpdateIconPlaceholder()
		{
			var needsIconPlaceholder = PlatformView.Children.Any(x => x is TPlatformItem item && item.HasIcon);

			foreach (var child in PlatformView.Children)
			{
				if (child is TPlatformItem item)
				{
					item.NeedsIconPlaceholder = needsIconPlaceholder;
					item.ArrangeControls();
				}
			}
		}
	}

	public partial class MenuBarHandler : GtkMenuShellHandler<IMenuBar, MauiMenuBar, IMenuBarItem, MauiMenuItem> { }
}