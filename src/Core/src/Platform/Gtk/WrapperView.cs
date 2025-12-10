using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace Microsoft.Maui.Platform
{
	public partial class WrapperView : EventBox, IDisposable
	{
		public WrapperView()
		{
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

				child.Show();

				child.Hexpand = this.Hexpand;
				child.Vexpand = this.Vexpand;

				child.Margin = this.Margin;
			}

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
