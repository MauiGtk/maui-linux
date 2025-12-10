using System.Collections.Generic;
using Gtk;

namespace Microsoft.Maui.Platform;

// https://docs.gtk.org/gtk3/class.Menu.html

public class MauiMenu : Menu
{
	public List<MauiMenuItem> Items { get; internal set; } = new();

	public MauiMenu()
	{
		NoShowAll = false;
	}

	public void Append(MauiMenuItem item)
	{
		Items.Add(item);
		base.Append(item);
		item.ShowAll();
	}

	public void Insert(MauiMenuItem item, int index)
	{
		Items.Insert(index, item);
		base.Insert(item, index);
		item.ShowAll();
	}

	public void Remove(MauiMenuItem item)
	{
		Items.Remove(item);
		base.Remove(item);
	}

}