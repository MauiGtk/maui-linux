#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using Cairo;
using Gdk;
using Gtk;
using Microsoft.Maui.Controls.Handlers.Items.Platform;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Point = Microsoft.Maui.Graphics.Point;

namespace Microsoft.Maui.Controls.Platform
{
	// ported from https://github.com/xamarin/Xamarin.Forms/blob/5.0.0/Xamarin.Forms.Platform.GTK/VisualElementTracker.cs
	class GesturePlatformManager : IDisposable
	{
		readonly IPlatformViewHandler _handler;
		readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;
		Widget? _container;
		Widget? _control;
		VisualElement? _element;

		bool _isDisposed;

		DateTime _lastPressTime;
		double _pressX, _pressY, _pressXRoot, _pressYRoot, _dragMotionX, _dragMotionY;
		Gdk.EventType _pressEventType;
		bool _isPanning, _isSwiping;
		bool _wasPanGestureStartedSent;
		GestureZoom? _gestureZoom;
		double _pinchCumulativeScale;
		Point _pinchInitialScalePoint;

		static DataPackage DragDataPackage = new();

		static readonly Dictionary<Type, List<Gdk.EventMask>> RecognizerEventMapping = new()
		{
			{ 
				typeof(TapGestureRecognizer),
				new()
				{
					Gdk.EventMask.ButtonPressMask,
					Gdk.EventMask.ButtonReleaseMask
				}
			},
			{ 
				typeof(PointerGestureRecognizer), 
				new()
				{
					Gdk.EventMask.PointerMotionMask, 
					Gdk.EventMask.EnterNotifyMask, 
					Gdk.EventMask.LeaveNotifyMask,
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask 
				}
			},
			{ 
				typeof(ChildGestureRecognizer), 
				new()
				{ 
					Gdk.EventMask.PointerMotionMask, 
					Gdk.EventMask.EnterNotifyMask,
					Gdk.EventMask.LeaveNotifyMask,
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask 
				}
			},
			{ 
#pragma warning disable CS0618
				typeof(ClickGestureRecognizer), 
#pragma warning restore CS0618
				new()
				{
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask 
				} 
			},
			{ 
				typeof(DragGestureRecognizer), 
				new()
				{ 
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask,
					Gdk.EventMask.PointerMotionMask,
					Gdk.EventMask.LeaveNotifyMask
				}
			},
			{ 
				typeof(DropGestureRecognizer),
				new()
				{
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask,
					Gdk.EventMask.PointerMotionMask, 
					Gdk.EventMask.LeaveNotifyMask 
				}
			},
			{ 
				typeof(PanGestureRecognizer),
				new()
				{ 
					Gdk.EventMask.ButtonPressMask, 
					Gdk.EventMask.ButtonReleaseMask,
					Gdk.EventMask.PointerMotionMask 
				}
			},
			{ 
				typeof(PinchGestureRecognizer),
				new()
				{
					Gdk.EventMask.ButtonPressMask,
					Gdk.EventMask.ButtonReleaseMask,
					Gdk.EventMask.PointerMotionMask,
					Gdk.EventMask.TouchMask,
					Gdk.EventMask.ScrollMask 
				}
			},
			{ 
				typeof(SwipeGestureRecognizer), 
				new()
				{ 
					Gdk.EventMask.ButtonPressMask,
					Gdk.EventMask.ButtonReleaseMask,
					Gdk.EventMask.PointerMotionMask,
					Gdk.EventMask.LeaveNotifyMask 
				}
			},
		};

		public Widget? Control
		{
			get { return _control; }
			set
			{
				if (_control == value)
					return;

				_control = value;

				if (PreventGestureBubbling)
				{
					UpdatingGestureRecognizers();
				}
			}
		}

		public VisualElement? Element
		{
			get { return _element; }
			set
			{
				if (_element == value)
					return;

				var view = _element as View;

				if (view != null)
				{
					var oldRecognizers = (ObservableCollection<IGestureRecognizer>)view.GestureRecognizers;
					oldRecognizers.CollectionChanged -= _collectionChangedHandler;
				}
				
				_element = value;
				
				if (view != null)
				{
					var newRecognizers = (ObservableCollection<IGestureRecognizer>)view.GestureRecognizers;
					newRecognizers.CollectionChanged += _collectionChangedHandler;
				}
			}
		}

		public View? ViewElement => Element as View;

		bool PreventGestureBubbling
		{
			get
			{
				return Element switch
				{
					Button => true,
					CheckBox => true,
					DatePicker => true,
					Stepper => true,
					Slider => true,
					Switch => true,
					TimePicker => true,
					ImageButton => true,
					RadioButton => true,
					_ => false,
				};
			}
		}

		public Widget? Container
		{
			get { return _container; }
			set
			{
				if (_container == value)
					return;

				_container = value;

				UpdatingGestureRecognizers();
			}
		}

		public GesturePlatformManager(IViewHandler handler)
		{
			_handler = (IPlatformViewHandler)handler;
			_collectionChangedHandler = ModelGestureRecognizersOnCollectionChanged;

			if (_handler.VirtualView == null)
				throw new ArgumentNullException(nameof(handler.VirtualView));

			if (_handler.PlatformView == null)
				throw new ArgumentNullException(nameof(handler.PlatformView));

			Control = _handler.PlatformView;
			Element = _handler.VirtualView as VisualElement;

			if (_handler.ContainerView != null)
				Container = _handler.ContainerView;
			else
				Container = _handler.PlatformView;

			InitGestureZoomForPinchGesture();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (!disposing)
				return;

			if (_container != null)
			{
				RemoveGestureEvents();
			}

			if (_element != null)
			{
				var view = _element as View;

				if (ViewElement != null)
				{
					var oldRecognizers = (ObservableCollection<IGestureRecognizer>)ViewElement.GestureRecognizers;
					oldRecognizers.CollectionChanged -= _collectionChangedHandler;
				}
			}

			_container = null;
			_control = null;
			_element = null;
		}

		private void ModelGestureRecognizersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			UpdatingGestureRecognizers();
		}

		private void UpdatingGestureRecognizers()
		{
			var gestures = ViewElement?.GestureRecognizers;

			if (Container == null || gestures == null)
				return;

			Container.ButtonPressEvent -= OnContainerButtonPressEvent;

			foreach (var gesture in gestures)
			{
				EnsureEventFlagsAreSet(gesture.GetType());
			}

			RemoveGestureEvents();
			AttachGestureEvents(gestures);
		}

		private void EnsureEventFlagsAreSet(Type recognizer)
		{
			if (Container == null)
				return;

			if (RecognizerEventMapping.TryGetValue(recognizer, out var mapping))
			{
				foreach (var eventFlag in mapping)
				{
					if (!Container.Events.HasFlag(eventFlag))
					{
						Container.AddEvents((int)eventFlag);
					}
				}
			}
		}

		private void RemoveGestureEvents()
		{
			if (Container == null)
				return;
			
			Container.ButtonPressEvent -= OnContainerButtonPressEvent;
			Container.ButtonReleaseEvent -= OnContainerButtonReleaseEvent;
			Container.MotionNotifyEvent -= OnContainerMotionEvent;
			Container.EnterNotifyEvent -= OnContainerEnterEvent;
			Container.LeaveNotifyEvent -= OnContainerLeaveEvent;

			Container.DragBegin -= OnContainerDragBegin;
			Container.DragDataGet -= OnContainerDragDataGet;
			Container.DragEnd -= OnContainerDragEnd;
			Container.DragLeave += OnContainerDragLeave;

			Container.DragDrop -= OnContainerDrop;
			Container.DragMotion -= OnContainerDragMotion;
			Container.DragDataReceived -= OnContainerDragDataReceived;
		}

		private void AttachGestureEvents(IList<IGestureRecognizer> gestures)
		{
			if (Container == null)
				return;

			if (gestures.HasAnyGesturesFor<TapGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;
				Container.ButtonReleaseEvent += OnContainerButtonReleaseEvent;
			}

#pragma warning disable CS0618
			if (gestures.HasAnyGesturesFor<ClickGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;
				Container.ButtonReleaseEvent += OnContainerButtonReleaseEvent;
			}
#pragma warning restore CS0618

			if (gestures.HasAnyGesturesFor<PointerGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;
				Container.ButtonReleaseEvent += OnContainerButtonReleaseEvent;
				Container.MotionNotifyEvent += OnContainerMotionEvent;
				Container.EnterNotifyEvent += OnContainerEnterEvent;
				Container.LeaveNotifyEvent += OnContainerLeaveEvent;
			}

			if (gestures.HasAnyGesturesFor<PanGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;
				Container.ButtonReleaseEvent += OnContainerButtonReleaseEvent;
				Container.MotionNotifyEvent += OnContainerMotionEvent;
				Container.LeaveNotifyEvent += OnContainerLeaveEvent;
			}

			if (gestures.HasAnyGesturesFor<SwipeGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;
				Container.MotionNotifyEvent += OnContainerMotionEvent;
				Container.ButtonReleaseEvent += OnContainerButtonReleaseEvent;
				Container.LeaveNotifyEvent += OnContainerLeaveEvent;
			}

			if (gestures.HasAnyGesturesFor<DragGestureRecognizer>())
			{
				Container.ButtonPressEvent += OnContainerButtonPressEvent;

				Container.DragBegin += OnContainerDragBegin;
				Container.DragDataGet += OnContainerDragDataGet;
				Container.DragEnd += OnContainerDragEnd;
				Container.DragLeave += OnContainerDragLeave;
				
				Gtk.Drag.SourceSet(_container, Gdk.ModifierType.Button1Mask, new TargetEntry[]
				{
					new TargetEntry("text/plain", TargetFlags.App, 0)
				},
				DragAction.Copy);
			}

			if (gestures.HasAnyGesturesFor<DropGestureRecognizer>())
			{
				Container.DragDrop += OnContainerDrop;
				Container.DragLeave += OnContainerDragLeave;
				Container.DragMotion += OnContainerDragMotion;
				Container.DragDataReceived += OnContainerDragDataReceived;

				Gtk.Drag.DestSet(_container, DestDefaults.All, new TargetEntry[]
				{
					new TargetEntry("text/plain", TargetFlags.App, 0)
				}, DragAction.Copy);
			}
		}

		#region Drag & Drop
		private void OnContainerDragDataReceived(object o, DragDataReceivedArgs args) { /* Maybe needed for File Drop */	}

		private void OnContainerDragMotion(object sender, DragMotionArgs args)
		{
			_dragMotionX = args.X;
			_dragMotionY = args.Y;

			Gdk.Drag.Status(args.Context, Gdk.DragAction.Copy, args.Time);
			args.RetVal = true;

			Console.WriteLine("DragMotion called");
			
			if (ViewElement == null)
				return;

			var gestures = ViewElement.GestureRecognizers.GetGesturesFor<DropGestureRecognizer>();
			foreach (var recognizer in gestures)
			{
				recognizer.SendDragOver(new DragEventArgs(DragDataPackage, (relativeTo) => GetPosition(relativeTo, args.X, args.Y), new PlatformDragEventArgs(sender, args)));
			}
		}

		private void OnContainerDragEnd(object sender, DragEndArgs args)
		{
			if (ViewElement == null)
				return;

			var success = args.Context.Actions != 0 && args.Context.Actions != DragAction.Default;

			var gestures = ViewElement.GestureRecognizers.GetGesturesFor<DragGestureRecognizer>();
			foreach (var recognizer in gestures)
			{
				recognizer.SendDropCompleted(new DropCompletedEventArgs(new PlatformDropCompletedEventArgs(sender, args)));
			}
		}

		private void OnContainerDragDataGet(object o, DragDataGetArgs args)
		{
			if (ViewElement == null)
				return;

			var gestures = ViewElement.GestureRecognizers.GetGesturesFor<DragGestureRecognizer>();
			foreach (var recognizer in gestures)
			{
				args.SelectionData.Text = DateTime.UtcNow.ToString();
			}
		}

		private void OnContainerDragLeave(object sender, DragLeaveArgs args)
		{
			if (ViewElement == null)
				return;

			var dropGestures = ViewElement.GestureRecognizers.GetGesturesFor<DropGestureRecognizer>();
			foreach (DropGestureRecognizer recognizer in dropGestures)
			{
				recognizer.SendDragLeave(new DragEventArgs(DragDataPackage, (relativeTo) => GetPosition(relativeTo, _dragMotionX, _dragMotionY), new PlatformDragEventArgs(sender, args)));
			}
		}

		private async void OnContainerDrop(object sender, DragDropArgs args)
		{
			if (ViewElement == null)
				return;

			var dropGestures = ViewElement.GestureRecognizers.GetGesturesFor<DropGestureRecognizer>();
			foreach (DropGestureRecognizer recognizer in dropGestures)
			{
				await recognizer.SendDrop(new DropEventArgs(new DataPackageView(DragDataPackage), (relativeTo) => GetPosition(relativeTo, args.X, args.Y), new PlatformDropEventArgs(sender, args)));
			}
		}

		private void OnContainerDropCompleted(object o, DragEndArgs args)
		{
			if (ViewElement == null)
				return;

			var dragGestures = ViewElement.GestureRecognizers.GetGesturesFor<DragGestureRecognizer>();
			foreach (DragGestureRecognizer recognizer in dragGestures)
			{
				recognizer.SendDropCompleted(new DropCompletedEventArgs());
			}
		}

		private void OnContainerDragBegin(object sender, DragBeginArgs args)
		{
			if (ViewElement == null)
				return;
			
			var child = Container?.GetChildAt<Widget>(0);
			if (child != null)
			{
				Gtk.Drag.SetIconSurface(args.Context, CreateIconSurface(child));
			}
			else
			{
				Gtk.Drag.SetIconName(args.Context, "text-x-generic", 0, 0);
			}

			var dragGestures = ViewElement.GestureRecognizers.GetGesturesFor<DragGestureRecognizer>();
			foreach (DragGestureRecognizer recognizer in dragGestures)
			{
				var platformArgs = new PlatformDragStartingEventArgs(sender, args);
				var startingEventArgs = recognizer.SendDragStarting(ViewElement, (relativeTo) => GetPosition(relativeTo, _pressXRoot, _pressYRoot), platformArgs);
				DragDataPackage = startingEventArgs.Data;
			}
		}

		// Wayland-safe snapshot: render widget to Cairo.ImageSurface, then to Pixbuf
		public static ImageSurface CreateIconSurface(Widget widget, double scale = 1.0)
		{
			// Determine size
			int w = Math.Max(1, widget.AllocatedWidth);
			int h = Math.Max(1, widget.AllocatedHeight);
			int sw = Math.Max(1, (int)Math.Round(w * scale));
			int sh = Math.Max(1, (int)Math.Round(h * scale));

			var surface = new ImageSurface(Format.Argb32, sw, sh);
			using (var cr = new Context(surface))
			{
				if (scale != 1.0)
					cr.Scale(scale, scale);

				cr.SetSourceRGBA(0, 0, 0, 0);
				cr.Paint();
				widget.Draw(cr);

				surface.Flush();
			}
			return surface;
		}
		#endregion

		#region EventHandling
		void OnContainerLeaveEvent(object sender, LeaveNotifyEventArgs args)
		{
			if (ViewElement == null)
				return;

			var pointerGestures = ViewElement.GestureRecognizers.GetGesturesFor<PointerGestureRecognizer>();
			foreach (PointerGestureRecognizer recognizer in pointerGestures)
			{
				recognizer.SendPointerExited(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event.XRoot, args.Event.YRoot), new PlatformPointerEventArgs());
			}

			if (_isSwiping)
			{
				_isSwiping = false;
			}
		}

		void OnContainerEnterEvent(object sender, EnterNotifyEventArgs args)
		{
			if (ViewElement == null)
				return;

			var pointerGestures = ViewElement.GestureRecognizers.GetGesturesFor<PointerGestureRecognizer>();

			foreach (PointerGestureRecognizer recognizer in pointerGestures)
			{
				recognizer.SendPointerEntered(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event.XRoot, args.Event.YRoot), new PlatformPointerEventArgs());
			}
		}

		void OnContainerMotionEvent(object sender, MotionNotifyEventArgs args)
		{
			if (ViewElement == null)
				return;

			var pointerGestures = ViewElement.GestureRecognizers.GetGesturesFor<PointerGestureRecognizer>();

			foreach (PointerGestureRecognizer recognizer in pointerGestures)
			{
				recognizer.SendPointerMoved(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event.XRoot, args.Event.YRoot), new PlatformPointerEventArgs());
			}

			if (_isPanning)
			{
				var panGestures = ViewElement.GestureRecognizers.GetGesturesFor<PanGestureRecognizer>();
				foreach (IPanGestureController recognizer in panGestures)
				{
					recognizer.SendPan(ViewElement, args.Event.X - _pressX, args.Event.Y - _pressY, PanGestureRecognizer.CurrentId.Value);
				}
			}

			if (_isSwiping)
			{
				var swipeGestures = ViewElement.GestureRecognizers.GetGesturesFor<SwipeGestureRecognizer>();
				foreach (ISwipeGestureController recognizer in swipeGestures)
				{
					recognizer.SendSwipe(ViewElement, args.Event.X - _pressX, args.Event.Y - _pressY);
				}
			}
		}

		void OnContainerButtonPressEvent(object sender, ButtonPressEventArgs args)
		{
			_lastPressTime = DateTime.UtcNow;
			_pressX = args.Event.X;
			_pressY = args.Event.Y;
			_pressXRoot = args.Event.XRoot;
			_pressYRoot = args.Event.YRoot;
			_pressEventType = args.Event.Type;

			if (ViewElement == null)
				return;

			var pointerGestures = ViewElement.GestureRecognizers.GetGesturesFor<PointerGestureRecognizer>();

			foreach (var recognizer in pointerGestures)
			{
				recognizer.SendPointerPressed(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event), new PlatformPointerEventArgs());
			}

			var panGestures = ViewElement.GestureRecognizers.GetGesturesFor<PanGestureRecognizer>();
			foreach (IPanGestureController recognizer in panGestures)
			{
				_isPanning = true;
				if (!_wasPanGestureStartedSent)
				{
					recognizer.SendPanStarted(ViewElement, PanGestureRecognizer.CurrentId.Value);
					_wasPanGestureStartedSent = true;
				}
				recognizer.SendPan(ViewElement, args.Event.X - _pressX, args.Event.Y - _pressY, PanGestureRecognizer.CurrentId.Value);
			}

			var swipeGestures = ViewElement.GestureRecognizers.GetGesturesFor<SwipeGestureRecognizer>();
			foreach (ISwipeGestureController recognizer in swipeGestures)
			{
				_isSwiping = true;
				recognizer.SendSwipe(ViewElement, args.Event.X - _pressX, args.Event.Y - _pressY);
			}
		}

		void OnContainerButtonReleaseEvent(object sender, ButtonReleaseEventArgs args)
		{
			var button = args.Event.Button;
			var buttonMask = ButtonsMask.Primary;
			if (button == 3)
			{
				buttonMask = ButtonsMask.Secondary;
			}

			if (ViewElement == null)
				return;

			int numClicks = 0;

			switch (_pressEventType)
			{
				case Gdk.EventType.ThreeButtonPress:
					numClicks = 3;

					break;
				case Gdk.EventType.TwoButtonPress:
					numClicks = 2;

					break;
				case Gdk.EventType.ButtonPress:
					numClicks = 1;

					break;
				default:
					return;
			}

			// Taps or Clicks
			var tapGestures = ViewElement.GestureRecognizers.GetGesturesFor<TapGestureRecognizer>(recognizer => recognizer.NumberOfTapsRequired == numClicks && recognizer.Buttons.HasFlag(buttonMask));
#pragma warning disable CS0618
			var clickGestures = ViewElement.GestureRecognizers.GetGesturesFor<ClickGestureRecognizer>(recognizer => recognizer.NumberOfClicksRequired == numClicks && recognizer.Buttons.HasFlag(buttonMask));
#pragma warning restore CS0618

			if (tapGestures.Count() > 0 || clickGestures.Count() >  0)
			{
				var dt = (DateTime.UtcNow - _lastPressTime).TotalMilliseconds;
				var dx = Math.Abs(args.Event.X - _pressX);
				var dy = Math.Abs(args.Event.Y - _pressY);

				if (dt < 400 && dx < 5 && dy < 5)
				{
					foreach (TapGestureRecognizer recognizer in tapGestures)
					{
						recognizer.SendTapped(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event));
					}

					foreach (var recognizer in clickGestures)
					{
						recognizer.SendClicked(ViewElement, buttonMask);
					}
				}
			}

			var pointerGestures = ViewElement.GestureRecognizers.GetGesturesFor<PointerGestureRecognizer>();

			foreach (PointerGestureRecognizer recognizer in pointerGestures)
			{
				recognizer.SendPointerReleased(ViewElement, (relativeTo) => GetPosition(relativeTo, args.Event), new PlatformPointerEventArgs());
			}

			if (_isPanning)
			{
				var panGestures = ViewElement.GestureRecognizers.GetGesturesFor<PanGestureRecognizer>();
				foreach (IPanGestureController recognizer in panGestures)
				{
					recognizer.SendPanCompleted(ViewElement, PanGestureRecognizer.CurrentId.Value);
				}
				_isPanning = false;
				_wasPanGestureStartedSent = false;
			}

			if (_isSwiping)
			{
				var swipeGestures = ViewElement.GestureRecognizers.GetGesturesFor<SwipeGestureRecognizer>();
				foreach (SwipeGestureRecognizer recognizer in swipeGestures)
				{
					((ISwipeGestureController)recognizer).DetectSwipe(ViewElement, recognizer.Direction);
				}
				_isSwiping = false;
			}
		}
		#endregion Event Handling

		#region Pinch
		void InitGestureZoomForPinchGesture()
		{
			if (ViewElement == null)
				return;

			var pinchRecognizers = ViewElement.GestureRecognizers.GetGesturesFor<PinchGestureRecognizer>();
			if (pinchRecognizers.Count() == 0)
				return;

			_gestureZoom = new(Container);

			_gestureZoom.Begin += (sender, args) =>
			{
				_pinchCumulativeScale = 1.0;

				double cx, cy;
				if (!TryGetGestureCenter(_gestureZoom, out cx, out cy))
					cx = cy = 0;

				_pinchInitialScalePoint = new Point(cx, cy);

				foreach (IPinchGestureController recognizer in pinchRecognizers)
				{
					recognizer.SendPinchStarted(Element, _pinchInitialScalePoint);
				}
			};

			_gestureZoom.ScaleChanged += (sender, args) =>
			{
				// args.Scale is a delta since the last signal → accumulate
				_pinchCumulativeScale *= args.Scale;

				double cx, cy;
				if (!TryGetGestureCenter(_gestureZoom, out cx, out cy))
					cx = cy = 0;

				var currentPoint = new Point(cx, cy);

				foreach (IPinchGestureController recognizer in pinchRecognizers)
				{
					// SendPinch(object sender, double scale, Point currentScalePoint)
					recognizer.SendPinch(Element, _pinchCumulativeScale, currentPoint);
				}
			};

			_gestureZoom.End += (sender, args) =>
			{
				foreach (IPinchGestureController recognizer in pinchRecognizers)
				{
					recognizer.SendPinchEnded(Element);
				}
			};

			_gestureZoom.Cancel += (sender, args) =>
			{
				foreach (IPinchGestureController recognizer in pinchRecognizers)
				{
					recognizer.SendPinchCanceled(Element);
				}
			};
		}

		bool TryGetGestureCenter(Gesture gesture, out double cx, out double cy)
		{
			dynamic dyn = gesture;
			double x = 0, y = 0, w = 0, h = 0;
			if (dyn.GetBoundingBox(out x, out y, out w, out h))
			{
				cx = x + w / 2.0;
				cy = y + h / 2.0;
				return true;
			}

			cx = cy = 0;
			return false;
		}

		#endregion Pinch

		#region Calculate Position
		Point? GetPosition(IElement? relativeTo, EventButton e)
		{

			// The widget that raised the event
			if (e.Window == null)
				return null;

			// If no relative target is given, return local coordinates
			if (relativeTo == null)
				return new(e.XRoot, e.YRoot);

			// Try to convert to coordinates relative to another GTK widget
			if (relativeTo.Handler?.PlatformView is Gtk.Widget target)
			{
				var targetX = target.PositionX();
				var targetY = target.PositionY();

				return new(e.XRoot - targetX, e.YRoot - targetY);
			}

			return new(e.XRoot, e.YRoot);
		}

		Point? GetPosition(IElement? relativeTo, double xRoot, double yRoot)
		{
			// If no relative target is given, return local coordinates
			if (relativeTo == null)
				return new(xRoot, yRoot);

			// Try to convert to coordinates relative to another GTK widget
			if (relativeTo.Handler?.PlatformView is Gtk.Widget target)
			{
				var targetX = target.PositionX();
				var targetY = target.PositionY();

				return new(xRoot - targetX, yRoot - targetY);
			}

			return new(xRoot, yRoot);
		}
		#endregion
	}

}