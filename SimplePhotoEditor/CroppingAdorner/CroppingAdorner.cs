using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Point = System.Drawing.Point;
using System;

namespace SimplePhotoEditor.CroppingAdorner
{
	public class CroppingAdorner : Adorner
	{
		#region Private variables
		// Width of the thumbs.  I know these really aren't "pixels", but px
		// is still a good mnemonic.
		private const int _cpxThumbWidth = 6;

		// PuncturedRect to hold the "Cropping" portion of the adorner
		private PuncturedRect _prCropMask;

		// Canvas to hold the thumbs so they can be moved in response to the user
		private Canvas _cnvThumbs;

		// Cropping adorner uses Thumbs for visual elements.
		// The Thumbs have built-in mouse input handling.
		private CropThumb _crtTopLeft, _crtTopRight, _crtBottomLeft, _crtBottomRight;
		private CropThumb _crtTop, _crtLeft, _crtBottom, _crtRight;

		// To store and manage the adorner's visual children.
		private VisualCollection _vc;

		// DPI for screen
		private static double s_dpiX, s_dpiY;

		private bool isDragging = false;
		private bool isMoving = false;
		private System.Windows.Point startPoint;
		private bool isInitialized = false;
		#endregion

		#region Properties
		public Rect ClippingRectangle
		{
			get
			{
				return _prCropMask.RectInterior;
			}
		}
		#endregion

		#region Routed Events
		public static readonly RoutedEvent CropChangedEvent = EventManager.RegisterRoutedEvent(
			"CropChanged",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(CroppingAdorner));

		public event RoutedEventHandler CropChanged
		{
			add
			{
				base.AddHandler(CroppingAdorner.CropChangedEvent, value);
			}
			remove
			{
				base.RemoveHandler(CroppingAdorner.CropChangedEvent, value);
			}
		}
		#endregion

		#region Dependency Properties
		static public DependencyProperty FillProperty = Shape.FillProperty.AddOwner(typeof(CroppingAdorner));

		public Brush Fill
		{
			get { return (Brush)GetValue(FillProperty); }
			set { SetValue(FillProperty, value); }
		}

		private static void FillPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			CroppingAdorner crp = d as CroppingAdorner;

			if (crp != null)
			{
				crp._prCropMask.Fill = (Brush)args.NewValue;
			}
		}
		#endregion

		#region Constructor
		static CroppingAdorner()
		{
			Color clr = Colors.Gray;
			System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd((IntPtr)0);

			s_dpiX = g.DpiX;
			s_dpiY = g.DpiY;
			clr.A = 80;
			FillProperty.OverrideMetadata(typeof(CroppingAdorner),
				new PropertyMetadata(
					new SolidColorBrush(clr),
					new PropertyChangedCallback(FillPropChanged)));
		}

		public CroppingAdorner(UIElement adornedElement, Rect rcInit)
			: base(adornedElement)
		{
			_vc = new VisualCollection(this);
			_prCropMask = new PuncturedRect();
			_prCropMask.IsHitTestVisible = true;
			_prCropMask.RectInterior = rcInit;
			_prCropMask.Fill = Fill;
			_vc.Add(_prCropMask);
			_cnvThumbs = new Canvas();
			_cnvThumbs.HorizontalAlignment = HorizontalAlignment.Stretch;
			_cnvThumbs.VerticalAlignment = VerticalAlignment.Stretch;
			_cnvThumbs.IsHitTestVisible = true;

			_vc.Add(_cnvThumbs);
			BuildCorner(ref _crtTop, Cursors.SizeNS);
			BuildCorner(ref _crtBottom, Cursors.SizeNS);
			BuildCorner(ref _crtLeft, Cursors.SizeWE);
			BuildCorner(ref _crtRight, Cursors.SizeWE);
			BuildCorner(ref _crtTopLeft, Cursors.SizeNWSE);
			BuildCorner(ref _crtTopRight, Cursors.SizeNESW);
			BuildCorner(ref _crtBottomLeft, Cursors.SizeNESW);
			BuildCorner(ref _crtBottomRight, Cursors.SizeNWSE);

			// Add handlers for Cropping.
			_crtBottomLeft.DragDelta += new DragDeltaEventHandler(HandleBottomLeft);
			_crtBottomRight.DragDelta += new DragDeltaEventHandler(HandleBottomRight);
			_crtTopLeft.DragDelta += new DragDeltaEventHandler(HandleTopLeft);
			_crtTopRight.DragDelta += new DragDeltaEventHandler(HandleTopRight);
			_crtTop.DragDelta += new DragDeltaEventHandler(HandleTop);
			_crtBottom.DragDelta += new DragDeltaEventHandler(HandleBottom);
			_crtRight.DragDelta += new DragDeltaEventHandler(HandleRight);
			_crtLeft.DragDelta += new DragDeltaEventHandler(HandleLeft);

			// Add mouse event handlers for click-and-drag
			this.MouseLeftButtonDown += CroppingAdorner_MouseLeftButtonDown;
			this.MouseLeftButtonUp += CroppingAdorner_MouseLeftButtonUp;
			this.MouseMove += CroppingAdorner_MouseMove;

			// Make sure the adorner can receive mouse events
			this.IsHitTestVisible = true;
			this.Focusable = true;

			FrameworkElement fel = adornedElement as FrameworkElement;
			if (fel != null)
			{
				fel.SizeChanged += new SizeChangedEventHandler(AdornedElement_SizeChanged);
			}
		}
		#endregion

		#region Thumb handlers
		// Generic handler for Cropping
		private void HandleThumb(
			double drcL,
			double drcT,
			double drcW,
			double drcH,
			double dx,
			double dy)
		{
			Rect rcInterior = _prCropMask.RectInterior;

			if (rcInterior.Width + drcW * dx < 0)
			{
				dx = -rcInterior.Width / drcW;
			}

			if (rcInterior.Height + drcH * dy < 0)
			{
				dy = -rcInterior.Height / drcH;
			}

			rcInterior = new Rect(
				rcInterior.Left + drcL * dx,
				rcInterior.Top + drcT * dy,
				rcInterior.Width + drcW * dx,
				rcInterior.Height + drcH * dy);

			_prCropMask.RectInterior = rcInterior;
			SetThumbs(_prCropMask.RectInterior);
			RaiseEvent(new RoutedEventArgs(CropChangedEvent, this));
		}

		// Handler for Cropping from the bottom-left.
		private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					1, 0, -1, 1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the bottom-right.
		private void HandleBottomRight(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					0, 0, 1, 1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the top-right.
		private void HandleTopRight(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					0, 1, 1, -1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the top-left.
		private void HandleTopLeft(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					1, 1, -1, -1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the top.
		private void HandleTop(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					0, 1, 0, -1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the left.
		private void HandleLeft(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					1, 0, -1, 0,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the right.
		private void HandleRight(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					0, 0, 1, 0,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}

		// Handler for Cropping from the bottom.
		private void HandleBottom(object sender, DragDeltaEventArgs args)
		{
			if (sender is CropThumb)
			{
				HandleThumb(
					0, 0, 0, 1,
					args.HorizontalChange,
					args.VerticalChange);
			}
		}
		#endregion

		#region Other handlers
		private void AdornedElement_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			FrameworkElement fel = sender as FrameworkElement;
			Rect rcInterior = _prCropMask.RectInterior;
			bool fFixupRequired = false;
			double
				intLeft = rcInterior.Left,
				intTop = rcInterior.Top,
				intWidth = rcInterior.Width,
				intHeight = rcInterior.Height;

			if (rcInterior.Left > fel.RenderSize.Width)
			{
				intLeft = fel.RenderSize.Width;
				intWidth = 0;
				fFixupRequired = true;
			}

			if (rcInterior.Top > fel.RenderSize.Height)
			{
				intTop = fel.RenderSize.Height;
				intHeight = 0;
				fFixupRequired = true;
			}

			if (rcInterior.Right > fel.RenderSize.Width)
			{
				intWidth = Math.Max(0, fel.RenderSize.Width - intLeft);
				fFixupRequired = true;
			}

			if (rcInterior.Bottom > fel.RenderSize.Height)
			{
				intHeight = Math.Max(0, fel.RenderSize.Height - intTop);
				fFixupRequired = true;
			}
			if (fFixupRequired)
			{
				_prCropMask.RectInterior = new Rect(intLeft, intTop, intWidth, intHeight);
			}
		}

		private void CroppingAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Check if we're clicking on a thumb
			if (e.Source is CropThumb)
			{
				return;
			}

			// Check if we're clicking inside the crop area
			var rect = _prCropMask.RectInterior;
			var clickPoint = e.GetPosition(this);
			
			if (rect.Contains(clickPoint))
			{
				// Moving existing crop area
				isMoving = true;
				isDragging = false;
				startPoint = clickPoint;
				this.CaptureMouse();
			}
			else
			{
				// Creating new crop area
				isMoving = false;
				isDragging = true;
				startPoint = clickPoint;
				_prCropMask.RectInterior = new Rect(startPoint.X, startPoint.Y, 0, 0);
				SetThumbs(_prCropMask.RectInterior);
				this.CaptureMouse();
			}
			e.Handled = true;
		}

		private void CroppingAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (isDragging || isMoving)
			{
				this.ReleaseMouseCapture();
				isDragging = false;
				isMoving = false;
				
				if (isDragging)
				{
					// Ensure minimum size for new crop area
					var rect = _prCropMask.RectInterior;
					if (rect.Width < 10 || rect.Height < 10)
					{
						_prCropMask.RectInterior = new Rect(
							rect.X,
							rect.Y,
							Math.Max(10, rect.Width),
							Math.Max(10, rect.Height));
						SetThumbs(_prCropMask.RectInterior);
					}
				}
				e.Handled = true;
			}
		}

		private void CroppingAdorner_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (isDragging)
				{
					// Creating new crop area
					System.Windows.Point currentPoint = e.GetPosition(this);
					double x = Math.Min(startPoint.X, currentPoint.X);
					double y = Math.Min(startPoint.Y, currentPoint.Y);
					double width = Math.Abs(currentPoint.X - startPoint.X);
					double height = Math.Abs(currentPoint.Y - startPoint.Y);

					_prCropMask.RectInterior = new Rect(x, y, width, height);
					SetThumbs(_prCropMask.RectInterior);
				}
				else if (isMoving)
				{
					// Moving existing crop area
					System.Windows.Point currentPoint = e.GetPosition(this);
					double deltaX = currentPoint.X - startPoint.X;
					double deltaY = currentPoint.Y - startPoint.Y;

					var rect = _prCropMask.RectInterior;
					double newX = rect.X + deltaX;
					double newY = rect.Y + deltaY;

					// Keep the crop area within bounds
					newX = Math.Max(0, Math.Min(newX, AdornedElement.RenderSize.Width - rect.Width));
					newY = Math.Max(0, Math.Min(newY, AdornedElement.RenderSize.Height - rect.Height));

					_prCropMask.RectInterior = new Rect(newX, newY, rect.Width, rect.Height);
					SetThumbs(_prCropMask.RectInterior);
					startPoint = currentPoint;
				}
				RaiseEvent(new RoutedEventArgs(CropChangedEvent, this));
				e.Handled = true;
			}
		}
		#endregion

		#region Arranging/positioning
		private void SetThumbs(Rect rc)
		{
			_crtBottomRight.SetPos(rc.Right, rc.Bottom);
			_crtTopLeft.SetPos(rc.Left, rc.Top);
			_crtTopRight.SetPos(rc.Right, rc.Top);
			_crtBottomLeft.SetPos(rc.Left, rc.Bottom);
			_crtTop.SetPos(rc.Left + rc.Width / 2, rc.Top);
			_crtBottom.SetPos(rc.Left + rc.Width / 2, rc.Bottom);
			_crtLeft.SetPos(rc.Left, rc.Top + rc.Height / 2);
			_crtRight.SetPos(rc.Right, rc.Top + rc.Height / 2);
		}

		// Arrange the Adorners.
		protected override Size ArrangeOverride(Size finalSize)
		{
			Rect rcExterior = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);
			_prCropMask.RectExterior = rcExterior;
			Rect rcInterior = _prCropMask.RectInterior;
			_prCropMask.Arrange(rcExterior);

			SetThumbs(rcInterior);
			_cnvThumbs.Arrange(rcExterior);
			return finalSize;
		}
		#endregion

		#region Public interface
		public BitmapSource BpsCrop()
		{
			// Get the crop rectangle in device-independent pixels
			Rect rcInterior = _prCropMask.RectInterior;
			
			// Get the margins
			Thickness margin = AdornerMargin();
			
			// Convert the crop rectangle to pixels, accounting for DPI and margins
			Point pxFromPos = UnitsToPx(rcInterior.Left + margin.Left, rcInterior.Top + margin.Top);
			Point pxFromSize = UnitsToPx(rcInterior.Width, rcInterior.Height);
			
			// Create a RenderTargetBitmap of the entire adorned element
			RenderTargetBitmap rtb = new RenderTargetBitmap(
				(int)AdornedElement.RenderSize.Width,
				(int)AdornedElement.RenderSize.Height,
				s_dpiX,
				s_dpiY,
				PixelFormats.Default);
			
			rtb.Render(AdornedElement);

			// Calculate the crop rectangle in pixels
			Int32Rect cropRect = new Int32Rect(
				pxFromPos.X,
				pxFromPos.Y,
				pxFromSize.X,
				pxFromSize.Y);

			// Create and return the cropped bitmap
			return new CroppedBitmap(rtb, cropRect);
		}

		public Rect GetCropRect()
		{
			// Get the crop rectangle in device-independent pixels
			Rect rcInterior = _prCropMask.RectInterior;
			
			// Get the margins
			Thickness margin = AdornerMargin();
			
			// Convert the crop rectangle to pixels, accounting for DPI and margins
			Point pxFromPos = UnitsToPx(rcInterior.Left + margin.Left, rcInterior.Top + margin.Top);
			Point pxFromSize = UnitsToPx(rcInterior.Width, rcInterior.Height);
			
			return new Rect(pxFromPos.X, pxFromPos.Y, pxFromSize.X, pxFromSize.Y);
		}
		#endregion

		#region Helper functions
		private Thickness AdornerMargin()
		{
			Thickness thick = new Thickness(0);
			if (AdornedElement is FrameworkElement)
			{
				thick = ((FrameworkElement)AdornedElement).Margin;
			}
			return thick;
		}

		private void BuildCorner(ref CropThumb crt, Cursor crs)
		{
			if (crt != null) return;

			crt = new CropThumb(_cpxThumbWidth);

			// Set some arbitrary visual characteristics.
			crt.Cursor = crs;

			_cnvThumbs.Children.Add(crt);
		}

		private Point UnitsToPx(double x, double y)
		{
			return new Point((int)(x * s_dpiX / 96), (int)(y * s_dpiY / 96));
		}
		#endregion

		#region Visual tree overrides
		// Override the VisualChildrenCount and GetVisualChild properties to interface with
		// the adorner's visual collection.
		protected override int VisualChildrenCount { get { return _vc.Count; } }
		protected override Visual GetVisualChild(int index) { return _vc[index]; }

		protected override void OnRender(DrawingContext drawingContext)
		{
			// Ensure the adorner is rendered with a transparent background
			drawingContext.DrawRectangle(
				Brushes.Transparent,
				null,
				new Rect(0, 0, this.ActualWidth, this.ActualHeight));
		}
		#endregion

		#region Internal Classes
		class CropThumb : Thumb
		{
			#region Private variables
			int _cpx;
			#endregion

			#region Constructor
			internal CropThumb(int cpx)
				: base()
			{
				_cpx = cpx;
			}
			#endregion

			#region Overrides
			protected override Visual GetVisualChild(int index)
			{
				return null;
			}

			protected override void OnRender(DrawingContext drawingContext)
			{
				drawingContext.DrawRoundedRectangle(Brushes.White, new Pen(Brushes.Black, 1), new Rect(new Size(_cpx, _cpx)), 1, 1);
			}
			#endregion

			#region Positioning
			internal void SetPos(double x, double y)
			{
				Canvas.SetTop(this, y - _cpx / 2);
				Canvas.SetLeft(this, x - _cpx / 2);
			}
			#endregion
		}
		#endregion
	}

}
