using System;
using System.Collections.Generic;
using Gdk;
using Gtk;

namespace Microsoft.Maui.Platform;

public static class MenuItemExtensions
{
	public static void UpdateKeyboardAccelerators(this MauiMenuItem platformView, AccelGroup accelGroup, IReadOnlyList<IKeyboardAccelerator>? viewKeyboardAccelerators)
	{
		if (viewKeyboardAccelerators == null)
			return;

		foreach (var keyboardAccelerator in viewKeyboardAccelerators)
		{
			var (key, mods) = MapAccelerator(keyboardAccelerator);

			platformView.AddAccelerator(
				"activate",
				accelGroup,
				key,
				mods,
				AccelFlags.Visible
			);

			platformView.KeybordAccelerators.AddRange(MapAcceleratorToText(keyboardAccelerator));
		}
		
		platformView.ArrangeControls();
	}

	private static (uint key, Gdk.ModifierType mods) MapAccelerator(IKeyboardAccelerator acc)
	{
		uint key = 0;
		
		if (acc.Key != null)
		{
			var ekey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), acc.Key, ignoreCase: true);
			key = (uint)ekey;
		}

		Gdk.ModifierType mods = 0;
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Ctrl))
			mods |= Gdk.ModifierType.ControlMask;
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Shift))
			mods |= Gdk.ModifierType.ShiftMask;
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Alt))
			mods |= Gdk.ModifierType.Mod1Mask;
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Windows))
			mods |= Gdk.ModifierType.SuperMask;
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Cmd))
			mods |= Gdk.ModifierType.MetaMask;

		return (key, mods);
	}

	private static List<string> MapAcceleratorToText(IKeyboardAccelerator acc)
	{
		var textList = new List<string>();
		
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Ctrl))
			textList.Add($"{KeyboardAcceleratorModifiers.Ctrl} + {acc.Key}");

		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Shift))
			textList.Add($"{KeyboardAcceleratorModifiers.Shift} + {acc.Key}");
		
		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Alt))
			textList.Add($"{KeyboardAcceleratorModifiers.Alt} + {acc.Key}");

		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Windows))
			textList.Add($"{KeyboardAcceleratorModifiers.Windows} + {acc.Key}");

		if (acc.Modifiers.HasFlag(KeyboardAcceleratorModifiers.Cmd))
			textList.Add($"{KeyboardAcceleratorModifiers.Cmd} + {acc.Key}");

		return textList;
	}

	public static void UpdateImageSource(this MauiMenuItem platformView, IImageSource? viewSource, IMauiContext mauiContext)
	{
		var task = viewSource.GetPlatformImageAsync(mauiContext);
		task.Wait();

		if (task.Result?.Value != null)
		{
			platformView.IconPixBuf = task.Result.Value;
		}

		platformView.ArrangeControls();
	}

	public static void UpdateText(this MauiMenuItem platformView, string text)
	{
		platformView.Label.Text = text;
		platformView.ArrangeControls();
	}
}