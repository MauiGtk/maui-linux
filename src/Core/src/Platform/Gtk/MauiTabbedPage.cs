using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace Microsoft.Maui.Platform;

public class MauiTabbedPage : Notebook
{
	public List<MauiTabbedPageLabel> TabPageLabels = new();
	public int SelectedPageIndex { get; set; }

	private Graphics.Color? _selectedColor;
	public Graphics.Color? SelectedColor 
	{
		get => _selectedColor;
		set
		{
			if (_selectedColor != value)
			{
				_selectedColor = value;
				UpdateSelectedItemColor();
			}
		}
	}

	private Graphics.Color? _unselectedColor;
	public Graphics.Color? UnselectedColor 
	{
		get => _unselectedColor;
		set
		{
			if (_unselectedColor != value)
			{
				_unselectedColor = value;
				UpdateUnselectedItemColor();
			}
		}
	}

	private Graphics.Color? _barBackgroundColor;
	public Graphics.Color? BarBackgroundColor
	{
		get => _barBackgroundColor;
		set
		{
			if (_barBackgroundColor != value)
			{
				_barBackgroundColor = value;
				UpdateBarBackgroundColor(_barBackgroundColor);
			}
		}
	}

	public CssProvider CssProvider { get; set; } = new();

	string _tabedPageCss = @"
		.maui-tabs > header > tabs > tab {
			padding-left: 0;
			padding-right: 0;
		}
	";

	public MauiTabbedPage()
	{
		this.SwitchPage += MauiTabbedPage_SwitchPage;

		CssProvider.LoadFromData(_tabedPageCss);

		Gtk.StyleContext.AddProviderForScreen(
			Gdk.Screen.Default,
			CssProvider,
			Gtk.StyleProviderPriority.User
		);

		this.StyleContext.AddClass("maui-tabs");
	}

	private void MauiTabbedPage_SwitchPage(object o, SwitchPageArgs args)
	{
		SelectedPageIndex = (int)args.PageNum;
		UpdateUnselectedItemColor();
		UpdateSelectedItemColor();
	}

	public int AppendPage(Widget child, MauiTabbedPageLabel label)
	{
		TabPageLabels.Add(label);
		label.Show();
		return base.AppendPage(child, label);
	}

	public void SetLabelTextColor(Graphics.Color color)
	{
		foreach (var label in TabPageLabels)
		{
			label.TextLabel.UpdateTextColor(color);
		}
	}

	private void UpdateSelectedItemColor()
	{
		var label = TabPageLabels[SelectedPageIndex];
		label.SetBackgroundColor(SelectedColor);
	}

	private void UpdateUnselectedItemColor()
	{
		foreach (var label in TabPageLabels)
		{
			label.SetBackgroundColor(UnselectedColor);
		}
	}

	private void UpdateBarBackgroundColor(Graphics.Color? color)
	{
		string fullCSS = _tabedPageCss +
		@"
		.maui-tabs > header {
			background-color: " + color.ToCssColor() + @"
		}
		";

		CssProvider.LoadFromData(fullCSS);
	}

	public void UpdateBarBackground(string? css)
	{
		string fullCSS = _tabedPageCss;

		if (!string.IsNullOrWhiteSpace(css))
		{
			fullCSS +=
			@"
			.maui-tabs > header {
				background: " + css + @"
			}
			";
		}

		CssProvider.LoadFromData(fullCSS);
	}

	public class MauiTabbedPageLabel : Box
	{
		public Label TextLabel { get; set; } = new Label();
		public Image? Icon { get; set; } = null;

		string css = @"
			.maui-tab {
				padding-left: 3px;
				padding-right: 3px;
			}
		";

		public MauiTabbedPageLabel(string text, Image? icon = null) : base(Orientation.Horizontal, 0)
		{
			TextLabel.Text = text;
			Icon = icon;

			var provider = new Gtk.CssProvider();
			provider.LoadFromData(css);

			Gtk.StyleContext.AddProviderForScreen(
				Gdk.Screen.Default,
				provider,
				Gtk.StyleProviderPriority.User
			);

			this.StyleContext.AddClass("maui-tab");

			if (Icon != null) PackStart(Icon, false, false, 3);
			PackEnd(TextLabel, false, false, 3);

			ShowAll();
		}
	}
}
