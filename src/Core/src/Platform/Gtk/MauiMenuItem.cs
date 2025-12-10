using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;

namespace Microsoft.Maui.Platform;

// https://docs.gtk.org/gtk3/class.MenuItem.html

public class MauiMenuItem : MenuItem
{
	Menu EnsureSubMenu => (Menu)(Submenu ??= new Menu());

	private Image Icon = new();

	public Box HBox { get; set; } = new Box(Orientation.Horizontal, 6);
	public Box IconBox { get; set; } = new Box(Orientation.Horizontal, 0);

	public new Label Label { get; set; } = new();
	public Label KeyboardAcceleratorLabel { get; set; } = new();

	public Pixbuf IconPixBuf
	{
		get => Icon.Pixbuf;
		set
		{
			Icon.Pixbuf = (value == null) ? value : value.ScaleSimple(20, 20, Gdk.InterpType.Bilinear);
			HasIcon = value != null;
		}
	}

	public bool HasIcon { get; private set; }
	public bool NeedsIconPlaceholder { get; set; }

	public List<string> KeybordAccelerators { get; internal set; } = new();

	public MauiMenuItem() 
	{
		Icon.SetSizeRequest(20, 20);

		IconBox.SetSizeRequest(25, 25);
	}

	public void ArrangeControls()
	{
		foreach (var child in Children)
		{
			Remove(child);
		}

		foreach (var child in HBox.Children)
		{
			HBox.Remove(child);
		}

		foreach (var child in IconBox.Children)
		{
			IconBox.Remove(child);
		}

		if (HasIcon)
		{
			IconBox.PackStart(Icon, false, false, 0);
		}

		if (NeedsIconPlaceholder || HasIcon)
		{
			HBox.PackStart(IconBox, false, false, 0);
		}

		HBox.PackStart(Label, false, false, 0);

		KeyboardAcceleratorLabel.Text = string.Join(", ", KeybordAccelerators);

		HBox.PackEnd(KeyboardAcceleratorLabel, false, false, 0);

		Add(HBox);

		Show();
		ShowAll();
	}

	public void AppendSubItem(MenuItem subItem)
	{
		EnsureSubMenu.Append(subItem);
	}

	public void RemoveSubItem(MenuItem subItem)
	{
		if (Submenu is not { })
			return;
		EnsureSubMenu.Remove(subItem);
	}

	public void InsertSubItem(MenuItem subItem, int index)
	{
		EnsureSubMenu.Insert(subItem, index);
	}

	public void ClearSubItems()
	{
		if (Submenu is not { })
			return;

		foreach (var m in EnsureSubMenu.Children.ToArray())
		{
			EnsureSubMenu.Remove(m);
		}
	}


}