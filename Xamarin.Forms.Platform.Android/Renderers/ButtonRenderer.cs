using System;
using System.ComponentModel;
using Android.Content.Res;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using static System.String;
using AButton = Android.Widget.Button;
using AView = Android.Views.View;
using AMotionEvent = Android.Views.MotionEvent;
using AMotionEventActions = Android.Views.MotionEventActions;
using Object = Java.Lang.Object;

namespace Xamarin.Forms.Platform.Android
{
	public class ButtonRenderer : ViewRenderer<Button, AButton>, AView.IOnAttachStateChangeListener
	{
		ButtonDrawable _backgroundDrawable;
		TextColorSwitcher _textColorSwitcher;
		Drawable _defaultDrawable;
		float _defaultFontSize;
		Typeface _defaultTypeface;
		bool _drawableEnabled;
		bool _isDisposed;
		int _imageHeight = -1;

		public ButtonRenderer(Context context) : base(context)
		{
			AutoPackage = false;
		}

		[Obsolete("This constructor is obsolete as of version 3.0. Please use ButtonRenderer(Context) instead.")]
		public ButtonRenderer()
		{
			AutoPackage = false;
		}

		AButton NativeButton
		{
			get { return Control; }
		}

		public void OnViewAttachedToWindow(AView attachedView)
		{
			UpdateText();
		}

		public void OnViewDetachedFromWindow(AView detachedView)
		{
		}

		public override SizeRequest GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			UpdateText();
			return base.GetDesiredSize(widthConstraint, heightConstraint);
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			if (_imageHeight > -1)
			{
				// We've got an image (and no text); it's already centered horizontally,
				// we just need to adjust the padding so it centers vertically
				var diff = (b - t - _imageHeight) / 2;
				diff = Math.Max(diff, 0);
				Control?.SetPadding(0, diff, 0, -diff);
			}

			base.OnLayout(changed, l, t, r, b);
		}

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (disposing)
			{
				if (_backgroundDrawable != null)
				{
					_backgroundDrawable.Dispose();
					_backgroundDrawable = null;
				}
			}

			base.Dispose(disposing);
		}

		protected override AButton CreateNativeControl()
		{
			return new AButton(Context);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement == null)
			{
				AButton button = Control;
				if (button == null)
				{
					button = CreateNativeControl();
					button.SetOnClickListener(ButtonClickListener.Instance.Value);
					button.SetOnTouchListener(ButtonTouchListener.Instance.Value);
					button.Tag = this;
					SetNativeControl(button);
					_textColorSwitcher = new TextColorSwitcher(button.TextColors);
					button.AddOnAttachStateChangeListener(this);
				}
			}
			else
			{
				if (_drawableEnabled)
				{
					_drawableEnabled = false;
					_backgroundDrawable.Reset();
					_backgroundDrawable = null;
				}
			}

			UpdateAll();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Button.TextProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Button.TextColorProperty.PropertyName)
				UpdateTextColor();
			else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
				UpdateEnabled();
			else if (e.PropertyName == Button.FontProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateDrawable();
			else if (e.PropertyName == Button.ImageProperty.PropertyName)
				UpdateBitmap();
			else if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName)
				UpdateText();

			if (_drawableEnabled &&
				(e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName || e.PropertyName == Button.BorderColorProperty.PropertyName || e.PropertyName == Button.BorderRadiusProperty.PropertyName ||
				 e.PropertyName == Button.BorderWidthProperty.PropertyName))
			{
				_backgroundDrawable.Reset();
				Control.Invalidate();
			}

			base.OnElementPropertyChanged(sender, e);
		}

		protected override void UpdateBackgroundColor()
		{
			// Do nothing, the drawable handles this now
		}

		void UpdateAll()
		{
			UpdateFont();
			UpdateText();
			UpdateBitmap();
			UpdateTextColor();
			UpdateEnabled();
			UpdateDrawable();
		}

		void UpdateBitmap()
		{
			var elementImage = Element.Image;
			var imageFile = elementImage?.File;
			_imageHeight = -1;

			if (elementImage == null || IsNullOrEmpty(imageFile))
			{
				Control.SetCompoundDrawablesWithIntrinsicBounds(null, null, null, null);
				return;
			}

			var image = Context.GetDrawable(imageFile);

			if (IsNullOrEmpty(Element.Text))
			{
				// No text, so no need for relative position; just center the image
				// There's no option for just plain-old centering, so we'll use Top 
				// (which handles the horizontal centering) and some tricksy padding (in OnLayout)
				// to handle the vertical centering 

				// Clear any previous padding and set the image as top/center
				Control.SetPadding(0, 0, 0, 0);
				Control.SetCompoundDrawablesWithIntrinsicBounds(null, image, null, null);

				// Keep track of the image height so we can use it in OnLayout
				_imageHeight = image.IntrinsicHeight;

				image?.Dispose();
				return;
			}

			var layout = Element.ContentLayout;

			Control.CompoundDrawablePadding = (int)layout.Spacing;

			switch (layout.Position)
			{
				case Button.ButtonContentLayout.ImagePosition.Top:
					Control.SetCompoundDrawablesWithIntrinsicBounds(null, image, null, null);
					break;
				case Button.ButtonContentLayout.ImagePosition.Bottom:
					Control.SetCompoundDrawablesWithIntrinsicBounds(null, null, null, image);
					break;
				case Button.ButtonContentLayout.ImagePosition.Right:
					Control.SetCompoundDrawablesWithIntrinsicBounds(null, null, image, null);
					break;
				default:
					// Defaults to image on the left
					Control.SetCompoundDrawablesWithIntrinsicBounds(image, null, null, null);
					break;
			}

			image?.Dispose();
		}

		void UpdateDrawable()
		{
			if (Element.BackgroundColor == Color.Default)
			{
				if (!_drawableEnabled)
					return;

				if (_defaultDrawable != null)
					Control.SetBackground(_defaultDrawable);

				_drawableEnabled = false;
			}
			else
			{
				if (_backgroundDrawable == null)
					_backgroundDrawable = new ButtonDrawable(Context.ToPixels);

				_backgroundDrawable.Button = Element;

				if (_drawableEnabled)
					return;

				if (_defaultDrawable == null)
					_defaultDrawable = Control.Background;

				Control.SetBackground(_backgroundDrawable);
				_drawableEnabled = true;
			}

			Control.Invalidate();
		}

		void UpdateEnabled()
		{
			Control.Enabled = Element.IsEnabled;
		}

		void UpdateFont()
		{
			Button button = Element;
			if (button.Font == Font.Default && _defaultFontSize == 0f)
				return;

			if (_defaultFontSize == 0f)
			{
				_defaultTypeface = NativeButton.Typeface;
				_defaultFontSize = NativeButton.TextSize;
			}

			if (button.Font == Font.Default)
			{
				NativeButton.Typeface = _defaultTypeface;
				NativeButton.SetTextSize(ComplexUnitType.Px, _defaultFontSize);
			}
			else
			{
				NativeButton.Typeface = button.Font.ToTypeface();
				NativeButton.SetTextSize(ComplexUnitType.Sp, button.Font.ToScaledPixel());
			}
		}

		void UpdateText()
		{
			var oldText = NativeButton.Text;
			NativeButton.Text = Element.Text;

			// If we went from or to having no text, we need to update the image position
			if (IsNullOrEmpty(oldText) != IsNullOrEmpty(NativeButton.Text))
			{
				UpdateBitmap();
			}
		}

		void UpdateTextColor()
		{
			_textColorSwitcher?.UpdateTextColor(Control, Element.TextColor);
		}

		class ButtonClickListener : Object, IOnClickListener
		{
			public static readonly Lazy<ButtonClickListener> Instance = new Lazy<ButtonClickListener>(() => new ButtonClickListener());

			public void OnClick(AView v)
			{
				var renderer = v.Tag as ButtonRenderer;
				if (renderer != null)
					((IButtonController)renderer.Element).SendClicked();
			}
		}

		class ButtonTouchListener : Object, IOnTouchListener
		{
			public static readonly Lazy<ButtonTouchListener> Instance = new Lazy<ButtonTouchListener>(() => new ButtonTouchListener());

			public bool OnTouch(AView v, AMotionEvent e)
			{
				var renderer = v.Tag as ButtonRenderer;
				if (renderer != null)
				{
					var buttonController = renderer.Element as IButtonController;
					if (e.Action == AMotionEventActions.Down)
					{
						buttonController?.SendPressed();
					}
					else if (e.Action == AMotionEventActions.Up)
					{
						buttonController?.SendReleased();
					}
				}
				return false;
			}
		}
	}
}