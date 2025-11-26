using Gtk;
using PlatformView = Microsoft.Maui.Platform.MauiTabbedPage;

namespace Microsoft.Maui.Handlers;

public partial class TabbedViewHandler : ViewHandler<ITabbedView, MauiTabbedPage>
{
	protected override MauiTabbedPage CreatePlatformView()
	{
		return new();
	}
}