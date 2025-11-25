using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gdk;
using GLib;
using Gtk;
using MenuItem = Gtk.MenuItem;

namespace Microsoft.Maui.Platform;

public static class ToolbarExtensions
{
	public static void UpdateTitle(this MauiToolbar platformView, IToolbar toolbar)
	{
		platformView.Title = toolbar.Title ?? string.Empty;
	}

	public static void UpdateIsVisible(this MauiToolbar platformView, IToolbar toolbar)
	{
		var wasVisible = platformView.Visible;
		platformView.Visible = toolbar.IsVisible;
		if (platformView.IsVisible)
			platformView.ShowAll();
	}

	public static void UpdateBackButtonVisibility(this MauiToolbar platformView, IToolbar toolbar)
	{
		if (toolbar.BackButtonVisible)
		{
			platformView.BackButton.Show();
		}
		else
		{
			platformView.BackButton.Hide();
		}
	}

	public static void UpdateBackButtonTitle(this MauiToolbar platformView, string backButtonTitle)
	{
		platformView.BackButton.Label = backButtonTitle;
		platformView.BackButton.AlwaysShowImage = platformView.BackButton.Image != null;
	}

	public static void UpdateTitleIcon(this MauiToolbar toolbar, Pixbuf? pixbuf)
	{
		if (pixbuf == null)
		{
			toolbar.TitleIcon = null;
		}
		else
		{
			if (toolbar.TitleIcon == null)
				toolbar.TitleIcon = new Image();
			toolbar.TitleIcon.Pixbuf = pixbuf;
		}
	}

	public static void UpdateTitleView(this MauiToolbar toolbar, Widget? titleView)
	{
		toolbar.TitleView = titleView;
	}

	public static void UpdateBackButtonEnabled(this MauiToolbar platformView, bool isEnabled)
	{
		platformView.IsBackButtonEnabled = isEnabled;
	}

	public static void UpdateTextColor(this MauiToolbar platformView, Graphics.Color textColor)
	{
		platformView.TitleLabel.SetForegroundColor(textColor);
	}

}