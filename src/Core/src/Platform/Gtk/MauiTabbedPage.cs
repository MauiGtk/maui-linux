using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GLib;
using Gtk;

namespace Microsoft.Maui.Platform;

public class MauiTabbedPage : Notebook
{
	public List<MauiTabbedPageNaviagtionItem> NavigationItems = new();
	public int SelectedPageIndex { get; set; }

	private Graphics.Color? _barTextColor;
	public Graphics.Color? BarTextColor
	{
		get => _barTextColor;
		set
		{
			if (_barTextColor != value)
			{
				_barTextColor = value;
				UpdateBarTextColor();
			}
		}
	}

	private Graphics.Color? _selectedItemTextColor;
	public Graphics.Color? SelectedItemTextColor
	{
		get => _selectedItemTextColor;
		set
		{
			if (_selectedItemTextColor != value)
			{
				_selectedItemTextColor = value;
				UpdateSelectedItemTextColor();
			}
		}
	}

	private Graphics.Color? _unselectedItemTextColor;
	public Graphics.Color? UnselectedItemTextColor
	{
		get => _unselectedItemTextColor;
		set
		{
			if (_unselectedItemTextColor != value)
			{
				_unselectedItemTextColor = value;
				UpdateUnselectedItemTextColor();
			}
		}
	}

	private Graphics.Color? _selectedItemBackgroundColor;
	public Graphics.Color? SelectedItemBackgroundColor
	{
		get => _selectedItemBackgroundColor;
		set
		{
			if (_selectedItemBackgroundColor != value)
			{
				_selectedItemBackgroundColor = value;
				UpdateSelectedItemBackgroundColor();
			}
		}
	}

	private Graphics.Color? _unselectedItemBackgroundColor;
	public Graphics.Color? UnselectedItemBackgroundColor
	{
		get => _unselectedItemBackgroundColor;
		set
		{
			if (_unselectedItemBackgroundColor != value)
			{
				_unselectedItemBackgroundColor = value;
				UpdateUnselectedItemBackgroundColor();
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
		UpdateUnselectedItemBackgroundColor();
		UpdateSelectedItemBackgroundColor();

		var tabPos = TabPos;
	}

	public int AppendPage(MauiTabbedPageNaviagtionItem naviagtionItem)
	{
		NavigationItems.Add(naviagtionItem);
		naviagtionItem.TabPageLabel.Show();
		return base.AppendPage(naviagtionItem.Page, naviagtionItem.TabPageLabel);
	}

	public void UpdateTabPages()
	{
		var selected = SelectedPageIndex;

		foreach (var child in this.Children)
		{
			this.Remove(child);
		}

		foreach (var navItem in NavigationItems.OrderBy(x => x.Order))
		{
			base.AppendPage(navItem.Page, navItem.TabPageLabel);
		}

		SelectedPageIndex = selected;
	}

	public void UpdateBarTextColor()
	{
		foreach (var pair in NavigationItems)
		{
			pair.TabPageLabel.TextLabel.UpdateTextColor(BarTextColor ?? Graphics.Colors.Black);
		}
	}

	private void UpdateSelectedItemBackgroundColor()
	{
		var pair = NavigationItems.ElementAt(SelectedPageIndex);
		pair.TabPageLabel.SetBackgroundColor(SelectedItemBackgroundColor);
	}

	private void UpdateUnselectedItemBackgroundColor()
	{
		foreach (var pair in NavigationItems)
		{
			pair.TabPageLabel.SetBackgroundColor(UnselectedItemBackgroundColor);
		}
	}

	private void UpdateSelectedItemTextColor()
	{
		var pair = NavigationItems.ElementAt(SelectedPageIndex);
		pair.TabPageLabel.TextLabel.UpdateTextColor(SelectedItemTextColor ?? BarTextColor ?? Graphics.Colors.Black);
	}

	private void UpdateUnselectedItemTextColor()
	{
		foreach (var pair in NavigationItems)
		{
			pair.TabPageLabel.TextLabel.UpdateTextColor(UnselectedItemTextColor ?? BarTextColor ?? Graphics.Colors.Black);
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

	public class MauiTabPageLabel : Box
	{
		public Label TextLabel { get; set; } = new Label();

		public Image? Icon { get; set; } = null;
		public int IconSourceHash { get; set; }

		string css = @"
			.maui-tab {
				padding-left: 3px;
				padding-right: 3px;
			}
		";

		public MauiTabPageLabel(string text, Image? icon = null, int iconSourceHash = 0) : base(Orientation.Horizontal, 0)
		{
			TextLabel.Text = text;
			Icon = icon;
			IconSourceHash = iconSourceHash;

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

	public class MauiTabbedPageNaviagtionItem
	{
		public Widget Page { get; set; }
		public MauiTabPageLabel TabPageLabel { get; set; } = new("");
		public Guid Id { get; private set; }
		public int Order { get; internal set; }

		public MauiTabbedPageNaviagtionItem(Widget page)
		{
			Page = page;
		}

		public MauiTabbedPageNaviagtionItem(Widget page, MauiTabPageLabel label, Guid id)
		{
			Page = page;
			TabPageLabel = label;
			Id = id;
		}
	}
}
