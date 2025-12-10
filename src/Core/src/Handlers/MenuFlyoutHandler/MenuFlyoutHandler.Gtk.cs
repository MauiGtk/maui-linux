using System;
using System.Collections.Generic;
using System.Linq;
using PlatformView = Microsoft.Maui.Platform.MauiMenu;

namespace Microsoft.Maui.Handlers
{
	public partial class MenuFlyoutHandler : GtkMenuShellHandler<IMenuFlyout, PlatformView, IMenuElement, MauiMenuItem>, IMenuFlyoutHandler 
	{
		protected override MauiMenu CreatePlatformElement()
		{
			return new MauiMenu();
		}

		protected override void DisconnectHandler(MauiMenu platformView)
		{
			if (VirtualView is not null)
			{
				foreach (var item in VirtualView)
				{
					item.Handler?.DisconnectHandler();
				}
			}

			base.DisconnectHandler(platformView);
		}

		public override void SetVirtualView(IElement view)
		{
			base.SetVirtualView(view);
			Clear();

			var items = (IMenuFlyout)view;

			foreach (var item in items)
			{
				Add(item);
			}
		}
	}
}