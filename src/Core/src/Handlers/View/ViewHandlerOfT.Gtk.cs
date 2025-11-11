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

			var wrapper = new EventBoxWrapperView();
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

			var wrapper = (EventBoxWrapperView)ContainerView;
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
			if (containerView is EventBoxWrapperView wrapper)
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

	/// <summary>
	/// Wrapper similar to WinUI's WrapperView, but for GTK:
	/// - Provides an event-capable container (no-window children like Label/Image won't receive events otherwise)
	/// - Central place to attach gestures, clipping, effects, etc.
	/// </summary>
	public class EventBoxWrapperView : EventBox, IDisposable
	{
		public EventBoxWrapperView()
		{
			// Transparent, but receives events
			VisibleWindow = false;
			AboveChild = false;
		}

		public void SetChild(Widget? child)
		{
			if (Child != null)
				Remove(Child);

			if (child != null)
			{
				Add(child);

				// Sichtbar machen – wichtig bei dynamisch gesetzten Kindern
				child.Show();

				// Expand weitergeben (falls Parent das auswertet)
				child.Hexpand = this.Hexpand;
				child.Vexpand = this.Vexpand;

				// optional: Margins spiegeln, wenn du welche nutzt
				child.Margin = this.Margin;
			}

			// Den Wrapper selbst sichtbar halten
			this.Show();
		}

		protected override void OnGetPreferredWidth(out int min, out int nat)
		{
			if (Child != null)
				Child.GetPreferredWidth(out min, out nat);
			else
				min = nat = 0;
		}

		protected override void OnGetPreferredHeight(out int min, out int nat)
		{
			if (Child != null)
				Child.GetPreferredHeight(out min, out nat);
			else
				min = nat = 0;
		}

		protected override void OnGetPreferredHeightForWidth(int width, out int min, out int nat)
		{
			if (Child != null)
				Child.GetPreferredHeightForWidth(width, out min, out nat);
			else
				min = nat = 0;
		}

		protected override void OnGetPreferredWidthForHeight(int height, out int min, out int nat)
		{
			if (Child != null)
				Child.GetPreferredWidthForHeight(height, out min, out nat);
			else
				min = nat = 0;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			Child?.SizeAllocate(new Gdk.Rectangle(allocation.X, allocation.Y, allocation.Width, allocation.Height));
		}

		//protected override bool OnDrawn(Cairo.Context cr)
		//{
		//	// Niemals selbst malen – Child soll sichtbar sein
		//	return false; // false = nichts selbst gezeichnet, Default weiter
		//}

		protected override void OnHierarchyChanged(Widget previous_toplevel)
		{
			base.OnHierarchyChanged(previous_toplevel);
			if (Child != null)
			{
				Child.Hexpand = this.Hexpand;
				Child.Vexpand = this.Vexpand;
			}
		}

		protected override void OnDestroyed()
		{
			if (Child != null)
				Remove(Child);
			base.OnDestroyed();
		}

		public new void Dispose()
		{
			try
			{
				if (Child != null)
					Remove(Child);
			}
			catch { /* ignore */ }
			base.Dispose();
		}
	}

}