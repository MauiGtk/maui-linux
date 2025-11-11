using System;
using Gtk;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
//using Microsoft.Maui.Platform.Gtk;

namespace Microsoft.Maui.Handlers
{
	public partial class LayoutHandler : ViewHandler<ILayout, LayoutView>
	{
		protected override LayoutView CreatePlatformView()
		{
			if (VirtualView == null)
			{
				throw new InvalidOperationException($"{nameof(VirtualView)} must be set to create a {nameof(LayoutView)}");
			}

			return new LayoutView { CrossPlatformLayout = VirtualView, };
		}

		public override void SetVirtualView(IView view)
		{
			base.SetVirtualView(view);

			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			PlatformView.CrossPlatformLayout = VirtualView;

			PlatformView.ClearChildren();

			foreach (var child in VirtualView)
			{
				if (child.ToPlatform(MauiContext) is { } nativeChild)
				{
					if (nativeChild.Parent is EventBoxWrapperView && nativeChild.Parent != null)
					{
						PlatformView.Add(child, nativeChild.Parent);
					}
					else
					{
						PlatformView.Add(child, nativeChild);
					}
				}
			}

			PlatformView.QueueAllocate();
			PlatformView.QueueResize();
		}

		protected override void DisconnectHandler(LayoutView nativeView)
		{
			base.DisconnectHandler(nativeView);
			PlatformView.ClearChildren();
		}

		public void Add(IView child)
		{
			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			if (child.ToPlatform(MauiContext) is { } nativeChild)
			{
				if (nativeChild.Parent != null && nativeChild.Parent is EventBoxWrapperView)
				{
					PlatformView.Add(child, nativeChild.Parent);
				}
				else
				{
					PlatformView.Add(child, nativeChild);
				}
			}

			PlatformView.QueueAllocate();
		}

		public void Remove(IView child)
		{
			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			if (child.ToPlatform(MauiContext) is { } nativeChild)
			{
				if (nativeChild.Parent != null && nativeChild.Parent is EventBoxWrapperView)
				{
					PlatformView.Remove(nativeChild.Parent);
				}
				else
				{
					PlatformView.Remove(nativeChild);
				}
			}

			PlatformView.QueueAllocate();
		}

		public void Clear()
		{
			PlatformView?.ClearChildren();
		}

		public void Insert(int index, IView child)
		{
			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			if (child.ToPlatform(MauiContext) is { } nativeChild)
			{
				if (nativeChild.Parent != null && nativeChild.Parent is EventBoxWrapperView)
				{
					PlatformView.Insert(child, nativeChild.Parent, index);
				}
				else
				{
					PlatformView.Insert(child, nativeChild, index);
				}
			}

			PlatformView.QueueAllocate();
		}

		public void Update(int index, IView child)
		{
			_ = PlatformView ?? throw new InvalidOperationException($"{nameof(PlatformView)} should have been set by base class.");
			_ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} should have been set by base class.");
			_ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} should have been set by base class.");

			if (child.ToPlatform(MauiContext) is { } nativeChild)
			{
				if (nativeChild.Parent != null && nativeChild.Parent.Parent is EventBoxWrapperView)
				{
					PlatformView.Update(child, nativeChild.Parent, index);
				}
				else
				{
					PlatformView.Update(child, nativeChild, index);
				}
			}

			PlatformView.QueueAllocate();
		}

#if DEBUG_
		public override void PlatformArrange(Rect rect)
		{
			PlatformView?.Arrange(rect);
		}

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			if (PlatformView is not { } platformView)
				return Size.Zero;

			return platformView.GetDesiredSize(widthConstraint, heightConstraint);
			//return base.GetDesiredSize(widthConstraint, heightConstraint);
		}

#endif

		[MissingMapper]
		public void UpdateZIndex(IView view)
		{

		}

		public static partial void MapBackground(ILayoutHandler handler, ILayout layout)
		{
			handler.PlatformView?.UpdateBackground(layout);
		}
	}
}