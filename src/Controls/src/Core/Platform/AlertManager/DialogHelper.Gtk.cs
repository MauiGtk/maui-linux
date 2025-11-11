using System;
using System.Linq;
using GLib;
using Gtk;
using Microsoft.Maui.Controls.Internals;


namespace Microsoft.Maui.Controls.Platform
{
	// Ported from https://github.com/xamarin/Xamarin.Forms/blob/5.0.0/Xamarin.Forms.Platform.GTK/Helpers/DialogHelper.cs
	internal static class DialogHelper
	{
		public static void ShowAlert(Gtk.Window window, AlertArguments arguments)
		{
			var messageDialog = new MessageDialog(
				window,
				DialogFlags.DestroyWithParent | DialogFlags.UseHeaderBar,
				MessageType.Other,
				GetAlertButtons(arguments),
				arguments.Message
			);

			SetDialogTitle(arguments.Title, messageDialog);
			SetButtonText(arguments.Accept, ResponseType.Ok, messageDialog);
			SetButtonText(arguments.Cancel, ResponseType.Cancel, messageDialog);

			ResponseType result = (ResponseType)messageDialog.Run();

			if (result == ResponseType.Ok)
			{
				arguments.SetResult(true);
			}
			else
			{
				arguments.SetResult(false);
			}

			messageDialog.Destroy();
		}

		public static void ShowActionSheet(Gtk.Window window, ActionSheetArguments arguments)
		{
			// as title bar is not shown, use dialog body for title text

			var messageDialog = new MessageDialog(
				window,
				DialogFlags.DestroyWithParent | DialogFlags.UseHeaderBar,
				MessageType.Other,
				ButtonsType.Cancel,
				arguments.Title
			);


			//SetDialogTitle(arguments.Title, messageDialog);
			SetButtonText(arguments.Cancel, ResponseType.Cancel, messageDialog);
			SetDestructionButton(arguments.Destruction, messageDialog);
			AddExtraButtons(arguments, messageDialog);

			var result = (ResponseType)messageDialog.Run();

			if (result == ResponseType.Cancel)
			{
				arguments.SetResult(arguments.Cancel);
			}
			else if (result == ResponseType.Reject)
			{
				arguments.SetResult(arguments.Destruction);
			}

			messageDialog.Destroy();
		}

		private static void SetDialogTitle(string title, MessageDialog messageDialog)
		{
			messageDialog.Title = title ?? string.Empty;
		}

		private static void SetButtonText(string text, ResponseType type, MessageDialog messageDialog)
		{
			var button = messageDialog.GetWidgetForResponse((int)type) as Gtk.Button;

			if (button is null)
				return;

			if (string.IsNullOrEmpty(text))
			{
				button.Hide();
			}
			else
			{
				button.Label = text;
			}
		}

		private static void SetDestructionButton(string destruction, MessageDialog messageDialog)
		{
			if (!string.IsNullOrEmpty(destruction))
			{
				var destructionButton =
					messageDialog.AddButton(destruction, ResponseType.Reject) as Gtk.Button;

				if (destructionButton is null)
					return;

				var destructionColor = Microsoft.Maui.Graphics.Colors.Red;
				destructionButton.Child.SetForegroundColor(destructionColor);
			}
		}

		private static void AddExtraButtons(ActionSheetArguments arguments, MessageDialog messageDialog)
		{
			foreach (var buttonText in arguments.Buttons)
			{
				var button = new Gtk.Button();
				button.Label = buttonText;
				button.Clicked += (obj, eventArgs) =>
				{
					arguments.SetResult(button.Label);
					messageDialog.Destroy();
				};
				button.Show();
				messageDialog.AddActionWidget(button, ResponseType.None);
			}
		}

		private static ButtonsType GetAlertButtons(AlertArguments arguments)
		{
			bool hasAccept = !string.IsNullOrEmpty(arguments.Accept);
			bool hasCancel = !string.IsNullOrEmpty(arguments.Cancel);

			ButtonsType type = ButtonsType.None;

			if (hasAccept && hasCancel)
			{
				type = ButtonsType.OkCancel;
			}
			else if (hasAccept && !hasCancel)
			{
				type = ButtonsType.Ok;
			}
			else if (!hasAccept && hasCancel)
			{
				type = ButtonsType.Cancel;
			}

			return type;
		}

		public static void ShowPromptDialog(Gtk.Window platformWindow, PromptArguments promptArguments)
		{
			var dialog = new Gtk.Dialog(
				promptArguments.Title,
				platformWindow,
				Gtk.DialogFlags.Modal
			);

			dialog.AddButton(promptArguments.Cancel ?? "Cancel", ResponseType.Cancel);
			dialog.AddButton(promptArguments.Accept ?? "OK", ResponseType.Ok);

			dialog.DefaultResponse = ResponseType.Ok;

			var box = dialog.ContentArea;
			var label = new Gtk.Label(promptArguments.Message ?? string.Empty) { Xalign = 0f };
			var entry = new Gtk.Entry();

			if (!string.IsNullOrEmpty(promptArguments.Placeholder))
				entry.PlaceholderText = promptArguments.Placeholder;

			if (!string.IsNullOrEmpty(promptArguments.InitialValue))
			{
				entry.Text = promptArguments.InitialValue;
				try
				{
					entry.SelectRegion(0, entry.TextLength);
				}
				catch { /* ignore if not supported */ }
			}

			if (promptArguments.MaxLength > -1)
				entry.MaxLength = promptArguments.MaxLength;

			ApplyKeyboard(entry, promptArguments.Keyboard);
			entry.ActivatesDefault = true;

			// Add widgets to dialog content area
			box.PackStart(label, false, false, 6);
			box.PackStart(entry, false, false, 6);

			// Handle dialog response
			dialog.Response += (o, args) =>
			{
				string? text = null;
				if (args.ResponseId == ResponseType.Ok)
					text = entry.Text;

				promptArguments.SetResult(text);

				dialog.Destroy();
			};

			// Handle closing via window "X" button
			dialog.DeleteEvent += (o, e) =>
			{
				promptArguments.SetResult(null);
				dialog.Destroy();
			};

			dialog.ShowAll();
		}

		private static void ApplyKeyboard(Gtk.Entry entry, Keyboard keyboard)
		{
			keyboard = keyboard ?? Keyboard.Default;

			entry.Visibility = true;
			entry.InputHints = InputHints.None;
			entry.InputPurpose = InputPurpose.FreeForm;
						
			// Map MAUI keyboard types to GTK input purposes
			if (keyboard == Keyboard.Email)
			{
				entry.InputPurpose = InputPurpose.Email;
			}
			else if (keyboard == Keyboard.Telephone)
			{
				entry.InputPurpose = InputPurpose.Phone;
			}
			else if (keyboard == Keyboard.Numeric)
			{
				entry.InputPurpose = InputPurpose.Number;
			}
			else if (keyboard == Keyboard.Url)
			{
				entry.InputPurpose = InputPurpose.Url;
			}
			else if (keyboard == Keyboard.Chat)
			{
				entry.InputPurpose = InputPurpose.FreeForm;
			}
			else if (keyboard == Keyboard.Text || keyboard == Keyboard.Default || keyboard == Keyboard.Plain)
			{
				entry.InputPurpose = InputPurpose.FreeForm;
			}
		}
	}
}
