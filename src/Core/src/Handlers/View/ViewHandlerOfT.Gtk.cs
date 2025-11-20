using System;
using System.Diagnostics;
using Gtk;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform.Gtk;

namespace Microsoft.Maui.Handlers
{

	public partial class ViewHandler<TVirtualView, TPlatformView> : IPlatformViewHandler
	{

		Gtk.Widget? IPlatformViewHandler.PlatformView => (Gtk.Widget?)base.PlatformView;

		public override void PlatformArrange(Rect rect)
		{
			this.PlatformArrangeHandler(rect);
		}

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
			=> this.GetDesiredSizeFromHandler(widthConstraint, heightConstraint);

		protected override void SetupContainer()
		{
			if (PlatformView == null || ContainerView != null)
				return;

			var oldParent = PlatformView.Parent as Container;
			var oldIndex = GetChildIndex(oldParent, PlatformView);

			oldParent?.Remove(PlatformView);

			var wrapper = new WrapperView();
			wrapper.SetChild(PlatformView);

			ContainerView = wrapper;

			if (oldParent != null)
				InsertChildAt(oldParent, wrapper, oldIndex);
		}

		protected override void RemoveContainer()
		{
			if (PlatformView == null || ContainerView == null || PlatformView.Parent != ContainerView)
			{
				CleanupContainerView(ContainerView);
				ContainerView = null;
				return;
			}

			var wrapper = (WrapperView)ContainerView;
			var oldParent = wrapper.Parent as Container;
			var oldIndex = GetChildIndex(oldParent, wrapper);

			oldParent?.Remove(wrapper);

			CleanupContainerView(wrapper);
			ContainerView = null;

			if (oldParent != null)
				InsertChildAt(oldParent, PlatformView, oldIndex);
		}

		static void CleanupContainerView(Widget? containerView)
		{
			if (containerView is WrapperView wrapper)
			{
				wrapper.SetChild(null);
				wrapper.Dispose();
			}
		}

		// Try to preserve index for common containers (Box/Fixed). For others, we fall back to Add().
		static int? GetChildIndex(Container? parent, Widget child)
		{
			if (parent is Box box)
			{
				var children = box.Children;
				for (int i = 0; i < children.Length; i++)
					if (ReferenceEquals(children[i], child))
						return i;
			}
			else if (parent is Fixed @fixed)
			{
				var children = @fixed.Children;
				for (int i = 0; i < children.Length; i++)
					if (ReferenceEquals(children[i], child))
						return i;
			}
			// Grid/FlowBox etc. don't have a meaningful "index" we can easily restore generically
			return null;
		}

		static void InsertChildAt(Container parent, Widget child, int? index)
		{
			switch (parent)
			{
				case Box box:
					// Pack at end then reorder to target index (if provided)
					box.PackStart(child, expand: false, fill: false, padding: 0);
					if (index is int i)
						box.ReorderChild(child, i);
					child.ShowAll();
					break;

				case Fixed @fixed:
					// Without stored coordinates we place at (0,0). If you track the old allocation, use it here.
					@fixed.Put(child, 0, 0);
					child.ShowAll();
					break;

				case Bin bin:
					// Single-child containers (e.g., EventBox, ScrolledWindow)
					if (bin.Child != null)
						bin.Remove(bin.Child);
					bin.Add(child);
					child.ShowAll();
					break;

				default:
					// Generic fallback: simple Add()
					parent.Add(child);
					child.ShowAll();
					break;
			}
		}

		protected void InvokeEvent(System.Action action)
		{
			Dispatcher.Invoke(action);
		}

		public void MapFont(ITextStyle textStyle)
		{
			MapFont(PlatformView, textStyle);

		}

		public void MapFont(Gtk.Widget? platformView, ITextStyle textStyle)
		{
			if (platformView == null)
				return;

			var fontManager = this.GetRequiredService<IFontManager>();
			platformView.UpdateFont(textStyle, fontManager);

		}

	}
}