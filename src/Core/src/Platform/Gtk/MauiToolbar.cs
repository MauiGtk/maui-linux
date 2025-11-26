using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform
{
	public class MauiToolbar : Box
	{
		internal Button BackButton { get; }

		internal bool IsBackButtonEnabled
		{
			get => BackButton.IsSensitive;
			set => BackButton.Sensitive = value;
		}

		private Image? _titleIcon = null;
		internal Image? TitleIcon 
		{
			get => _titleIcon;
			set
			{
				if(value == null && _titleIcon != null)
				{
					Remove(_titleIcon);
				}

				_titleIcon = value;
				ArrangeToolbarChilds();
			}
		}

		private Widget? _titleView;
		internal Widget? TitleView 
		{
			get => _titleView;
			set
			{
				if (value == null && _titleView != null)
				{
					Remove(_titleView);
				}

				_titleView = value;

				ArrangeToolbarChilds();
			} 
		}

		private List<MauiToolbarItem> _toolbarItems = new();
		internal List<MauiToolbarItem> ToolbarItems
		{
			get
			{
				return _toolbarItems;
			}
		}

		private List<MauiToolbarItem> _menuItems = new();
		internal List<MauiToolbarItem> MenuItems
		{
			get
			{
				return _menuItems;
			}
		}

		Label? _titleLabel;

		internal Label TitleLabel
		{
			get
			{
				if (_titleLabel != null)
				{
					return _titleLabel;
				}

				_titleLabel = new Label();
				return _titleLabel;
			}
		}

		public string Title
		{
			get => TitleLabel.Text;
			set => TitleLabel.Text = value;
		}

		private Graphics.Color _textColor = Colors.Black;
		public Graphics.Color TextColor
		{
			get => _textColor;
			set
			{
				if(_textColor != value)
				{
					_textColor = value;
					UpdateTextColor(_textColor);
				}
			}
		}

		public MenuButton MenuButton { get; } = new();
		public Popover MenuPopover { get; }
		
		public Box MenuViewBox = new Gtk.Box(Gtk.Orientation.Vertical, 6);
		public FixedWidthBox ToolbarItemBox = new FixedWidthBox(Orientation.Horizontal, 6, 100);
		
		public delegate void BackButtonClickedEventHandler(object? sender);

		public event BackButtonClickedEventHandler? BackButtonClicked;

		public CssProvider CssProvider { get; set; } = new();

		private string _defaultCss = @"
		.maui-toolbar-menubutton,
		.maui-toolbar-menubutton:hover,
		.maui-toolbar-menubutton:active,
		.maui-toolbar-menubutton:checked,
		.maui-toolbar-menubutton:focus {
			background-color: transparent;
			background-image: none;
			border: none;
			box-shadow: none;
			outline: none;
			border-radius: 0;
		}

		.maui-toolbar-backbutton,
		.maui-toolbar-backbutton:hover,
		.maui-toolbar-backbutton:active,
		.maui-toolbar-backbutton:checked,
		.maui-toolbar-backbutton:focus {
			background-color: transparent;
			background-image: none;
			border: none;
			box-shadow: none;
			outline: none;
			border-radius: 0;
		}

		.maui-toolbar-menuviewbox,
		.maui-toolbar-menuviewbox maui-toolbar-item
		{
			color: #000000;
		}
		";

		public MauiToolbar() : base(Orientation.Horizontal, 0)
		{
			BackButton = new Button();
			BackButton.Image = new Image(Stock.GoBack, IconSize.LargeToolbar);
			BackButton.Relief = ReliefStyle.None;
			BackButton.Clicked += (sender, args) => BackButtonClicked?.Invoke(sender);
			BackButton.StyleContext.AddClass("maui-toolbar-backbutton");

			MenuPopover = new(MenuButton);
			MenuPopover.Add(MenuViewBox);
			MenuButton.Direction = ArrowType.Down;

			MenuButton.Image = Gtk.Image.NewFromIconName("open-menu-symbolic", IconSize.Button);
			MenuButton.Relief = ReliefStyle.None;
			MenuButton.Popover = MenuPopover;
			MenuButton.Visible = true;

			MenuButton.StyleContext.AddClass("maui-toolbar-menubutton");
			MenuViewBox.StyleContext.AddClass("maui-toolbar-menuviewbox");

			Gtk.StyleContext.AddProviderForScreen(
				Gdk.Screen.Default,
				CssProvider,
				Gtk.StyleProviderPriority.User
			);

			this.StyleContext.AddClass("maui-toolbar");

			this.SizeAllocated += MauiToolbar_SizeAllocated;

			ArrangeToolbarChilds();
			NoShowAll = true;
		}

		private void MauiToolbar_SizeAllocated(object o, SizeAllocatedArgs args)
		{
			int titleIconWidth = (TitleIcon != null) ? TitleIcon.AllocatedWidth : 0;
			int titleViewWidth = (TitleView != null) ? TitleView.AllocatedWidth : 0;

			int usedWith = BackButton.AllocatedWidth + titleIconWidth + TitleLabel.AllocatedWidth + titleViewWidth + MenuButton.AllocatedWidth;

			var availableWidth = this.AllocatedWidth - usedWith;
			availableWidth = (availableWidth > 0) ? availableWidth : 0;

			int sumItemWidth = ToolbarItems.Sum(x => x.GetPreferredWidth().Item2);

			ToolbarItemBox.FixedWidth = availableWidth;
			ToolbarItemBox.Show();
			
			ReflowToolbar(availableWidth);
		}

		private void ReflowToolbar(int availableWidth)
		{
			var overflowItems = new List<MauiToolbarItem>();
			var currentToolbarItems = new List<MauiToolbarItem>();
			
			bool hasOverflow = false;
			int currentWidth = 0;

			foreach (var item in ToolbarItems)
			{
				(int itemMin, int itemNat) = item.GetPreferredWidth();
				int width = (itemNat == 0) ? item.LastAllocatedWidth : itemNat;

				currentWidth += width;

				if (currentWidth >= availableWidth || (itemNat == 0 && item.LastAllocatedWidth == 0))
				{
					hasOverflow = true;
				}
				else
				{
					currentToolbarItems.Add(item);
				}

				if (hasOverflow)
				{
					overflowItems.Add(item);
				}
			}

			if (overflowItems.Count > 0 || MenuItems.Any(x => x.Order == MauiToolbarItem.EOrder.Secondary))
			{
				MenuButton.Show();
			}
			else
			{
				MenuButton.Hide();
			}

			foreach (var item in currentToolbarItems)
			{
				item.Show();
				var menuItem = MenuItems.FirstOrDefault(x => x.ID == item.ID);
				menuItem?.Visible = false;
				menuItem?.Hide();
			}

			foreach (var item in overflowItems)
			{
				item.Visible = false;
				item.Hide();
				var menuItem = MenuItems.FirstOrDefault(x => x.ID == item.ID);
				menuItem?.Show();
			}
		}

		public void ArrangeToolbarChilds()
		{
			if(Children.Contains(BackButton))
				this.Remove(BackButton);
			if (Children.Contains(TitleIcon))
				this.Remove(TitleIcon);
			if (Children.Contains(TitleLabel))
				this.Remove(TitleLabel);
			if (Children.Contains(TitleView))
				this.Remove(TitleView);
			if (Children.Contains(MenuButton))
				Remove(MenuButton);
			if (Children.Contains(ToolbarItemBox))
				Remove(ToolbarItemBox);

			PackStart(BackButton, false, false, 0);
			
			int availableSpace = 0;
			int endPosition = 0;
			if (TitleView != null)
			{
				PackStart(TitleView, false, false, 0);

				endPosition = TitleView.Allocation.X + TitleView.Allocation.Width;
				availableSpace = this.AllocatedWidth - endPosition;
			}
			else
			{
				if (TitleIcon != null) PackStart(TitleIcon, false, false, 0);
				PackStart(TitleLabel, false, false, 0);
				
				TitleLabel.Show();
				
				endPosition = TitleLabel.Allocation.X + TitleLabel.Allocation.Width;
				availableSpace = this.AllocatedWidth - endPosition;
			}
			PackEnd(MenuButton, false, false, 0);
			
			PackEnd(ToolbarItemBox, false, false, 0);
		}

		public void ArrangeToolbarItems()
		{
			foreach (var item in ToolbarItems)
			{
				ToolbarItemBox.PackEnd(item, false, false, 0);
			}

			foreach (var item in MenuItems)
			{
				MenuViewBox.PackStart(item, false, false, 0);
			}

			if (MenuItems.Any(x => x.Order == MauiToolbarItem.EOrder.Secondary))
			{
				MenuButton.Visible = true;
			}
			else
			{
				MenuButton.Visible = false;
			}

			MenuViewBox.ShowAll();
		}

		public void ClearToolbarItems()
		{
			foreach (var item in ToolbarItems)
			{
				ToolbarItemBox.Remove(item);
			}

			ToolbarItems.Clear();
		}

		public void ClearMenuItems()
		{
			foreach (var item in MenuViewBox.Children)
			{
				MenuViewBox.Remove(item);
			}

			MenuItems.Clear();
		}

		public void AddToolbarItem(MauiToolbarItem item)
		{
			ToolbarItems.Add(item);
		}

		public void AddMenuItem(MauiToolbarItem item)
		{
			MenuItems.Add(item);
		}

		public void UpdateBarBackground(string? css)
		{
			string fullCSS = _defaultCss;

			if (!string.IsNullOrWhiteSpace(css))
			{
				fullCSS +=
				@"
				.maui-toolbar {
					background: " + css + @"
				}
				";
			}

			CssProvider.LoadFromData(fullCSS);
		}

		private void UpdateTextColor(Graphics.Color textColor)
		{
			TitleLabel.UpdateTextColor(textColor);
			BackButton.UpdateTextColor(textColor);

			foreach (var item in ToolbarItems)
			{
				item.UpdateTextColor(textColor);
			}
		}

		internal void UpdateIconColor(Graphics.Color iconColor)
		{
			var backButtonIcon = this.RenderIcon(Stock.GoBack, IconSize.Button, null);
			BackButton.Image = (iconColor != null) ? new Image(backButtonIcon.TintFlat(iconColor)) : new Image(backButtonIcon);

			var menuButtonIcon = IconTheme.Default.LoadIcon("open-menu-symbolic", 16, IconLookupFlags.UseBuiltin);
			MenuButton.Image = (iconColor != null) ? new Image(menuButtonIcon.TintFlat(iconColor)) : new Image(menuButtonIcon);

			if (TitleIcon != null && iconColor != null)
			{
				TitleIcon.Pixbuf = TitleIcon.Pixbuf.TintFlat(iconColor);
			}

			foreach (var item in ToolbarItems)
			{
				item.UpdateIconColor(iconColor);
			}

			foreach (var item in MenuItems)
			{
				item.UpdateIconColor(iconColor);
			}
		}
	}

	public class MauiToolbarItem : Gtk.Button
	{
		public int Priority { get; internal set; }
		public EOrder Order { get; internal set; }
		public Pixbuf? PixBuf { get; internal set; }
		public Guid ID { get; internal set; }

		public int LastAllocatedWidth {	get; internal set; } = 0;

		public CssProvider CssProvider { get; set; } = new();

		private string _defaultCss = @"
		.maui-toolbar-item,
		.maui-toolbar-item:hover,
		.maui-toolbar-item:active,
		.maui-toolbar-item:checked,
		.maui-toolbar-item:focus {
			background-color: transparent;
			background-image: none;
			border: none;
			box-shadow: none;
			outline: none;
			border-radius: 0;
		}";

		public MauiToolbarItem()
		{
			Relief = ReliefStyle.None;
			AlwaysShowImage = true;
			Visible = true;
			
			CssProvider.LoadFromData(_defaultCss);
			
			StyleContext.AddProviderForScreen(
				Gdk.Screen.Default,
				CssProvider,
				StyleProviderPriority.Application
			);

			this.StyleContext.AddClass("maui-toolbar-item");
		}

		protected override void OnSizeAllocated(Rectangle allocation)
		{
			if(allocation.Width > 1) LastAllocatedWidth = allocation.Width;
			base.OnSizeAllocated(allocation);
		}

		public (int, int) GetPreferredWidth()
		{
			int width = 0;
			int min = 0;
			this.GetPreferredWidth(out min, out width);
			return (min, width);
		}

		internal void UpdateIconColor(Graphics.Color? iconColor)
		{
			if (PixBuf != null && iconColor != null)
			{
				if (iconColor != null)
				{
					Image = new Gtk.Image(PixBuf.ScaleSimple(16, 16, InterpType.Bilinear).TintFlat(iconColor));
				}
				else
				{
					Image = new Gtk.Image(PixBuf.ScaleSimple(16, 16, InterpType.Bilinear));
				}
			}
		}

		public enum EOrder
		{
			Default = 0,
			Primary = 1,
			Secondary = 2
		}
	}

	public class FixedWidthBox : Gtk.Box
	{
		public int FixedWidth { get; set; } = 100;

		public FixedWidthBox(Orientation orientation, int spacing, int fixedWidth)
			: base(orientation, spacing)
		{
			FixedWidth = fixedWidth;
		}

		protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
		{
			base.OnGetPreferredWidth(out minimum_width, out natural_width);

			minimum_width = 0;

			if (FixedWidth > 0)
			{
				//if (minimum_width > FixedWidth)
				//	minimum_width = FixedWidth;
				natural_width = FixedWidth;
			}
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			int sumItemWidth = Children.Sum(x => GetPreferedWidth(x).Item2);
			if (FixedWidth > 0 && allocation.Width > FixedWidth)
			{
				allocation.Width = FixedWidth;
			}
			
			base.OnSizeAllocated(allocation);
		}

		private (int, int) GetPreferedWidth(Widget item)
		{
			int width = 0;
			int min = 0;
			item?.GetPreferredWidth(out min, out width);
			return (min, width);
		}
	}
}