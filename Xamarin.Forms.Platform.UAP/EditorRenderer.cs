using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Internals;
using Xamarin.Forms.PlatformConfiguration.WindowsSpecific;
using Specifics = Xamarin.Forms.PlatformConfiguration.WindowsSpecific.InputView;

namespace Xamarin.Forms.Platform.UWP
{
	public class EditorRenderer : ViewRenderer<Editor, FormsTextBox>
	{
		private static FormsTextBox copyOfTextBox;
		static Windows.Foundation.Size _zeroSize = new Windows.Foundation.Size(0, 0);
		static Windows.Foundation.Size _infiniteSize = new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity);

		bool _fontApplied;
		Brush _backgroundColorFocusedDefaultBrush;
		Brush _textDefaultBrush;
		Brush _defaultTextColorFocusBrush;

		IEditorController ElementController => Element;

		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var textBox = new FormsTextBox
					{
						AcceptsReturn = true,
						TextWrapping = TextWrapping.Wrap,
						Style = Windows.UI.Xaml.Application.Current.Resources["FormsTextBoxStyle"] as Windows.UI.Xaml.Style
					};

					SetNativeControl(textBox);

					textBox.TextChanged += OnNativeTextChanged;
					textBox.LostFocus += OnLostFocus;

					// If the Forms VisualStateManager is in play or the user wants to disable the Forms legacy
					// color stuff, then the underlying textbox should just use the Forms VSM states
					textBox.UseFormsVsm = e.NewElement.HasVisualStateGroups()
						|| !e.NewElement.OnThisPlatform().GetIsLegacyColorModeEnabled();
				}

				UpdateText();
				UpdateInputScope();
				UpdateTextColor();
				UpdateFont();
				UpdateTextAlignment();
				UpdateFlowDirection();
				UpdateMaxLength();
				UpdateDetectReadingOrderFromContent();
			}

			base.OnElementChanged(e);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && Control != null)
			{
				Control.TextChanged -= OnNativeTextChanged;
				Control.LostFocus -= OnLostFocus;
			}

			base.Dispose(disposing);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Editor.TextColorProperty.PropertyName)
			{
				UpdateTextColor();
			}
			else if (e.PropertyName == InputView.KeyboardProperty.PropertyName)
			{
				UpdateInputScope();
			}
			else if (e.PropertyName == InputView.IsSpellCheckEnabledProperty.PropertyName)
			{
				UpdateInputScope();
			}
			else if (e.PropertyName == Editor.FontAttributesProperty.PropertyName)
			{
				UpdateFont();
			}
			else if (e.PropertyName == Editor.FontFamilyProperty.PropertyName)
			{
				UpdateFont();
			}
			else if (e.PropertyName == Editor.FontSizeProperty.PropertyName)
			{
				UpdateFont();
			}
			else if (e.PropertyName == Editor.TextProperty.PropertyName)
			{
				UpdateText();
			}
			else if (e.PropertyName == VisualElement.FlowDirectionProperty.PropertyName)
			{
				UpdateTextAlignment();
				UpdateFlowDirection();
			}
			else if (e.PropertyName == InputView.MaxLengthProperty.PropertyName)
				UpdateMaxLength();
			else if (e.PropertyName == Specifics.DetectReadingOrderFromContentProperty.PropertyName)
				UpdateDetectReadingOrderFromContent();
		}

		void OnLostFocus(object sender, RoutedEventArgs e)
		{
			ElementController.SendCompleted();
		}

		protected override void UpdateBackgroundColor()
		{
			base.UpdateBackgroundColor();

			if (Control == null)
			{
				return;
			}

			// By default some platforms have alternate default background colors when focused
			BrushHelpers.UpdateColor(Element.BackgroundColor, ref _backgroundColorFocusedDefaultBrush,
				() => Control.BackgroundFocusBrush, brush => Control.BackgroundFocusBrush = brush);
		}

		void OnNativeTextChanged(object sender, Windows.UI.Xaml.Controls.TextChangedEventArgs args)
		{
			Element.SetValueCore(Editor.TextProperty, Control.Text);
		}

		/// <summary>
		/// Use cases
		/// - Initializing a text box that's horizontally contrained but not vertically. If I left out the initial measure it'd just break
		/// - Textboxes refuse to grow horizontally once they are part of a layout and have a set desired size and they are set to wordwrap 
		/// </summary>
		/// <param name="control"></param>
		/// <param name="constraint"></param>
		/// <returns></returns>
		Size GetCopyOfSize(FormsTextBox control, Windows.Foundation.Size constraint)
		{
			if (copyOfTextBox == null)
			{
				copyOfTextBox = new FormsTextBox
				{
					AcceptsReturn = true,
					TextWrapping = TextWrapping.Wrap,
					Style = Windows.UI.Xaml.Application.Current.Resources["FormsTextBoxStyle"] as Windows.UI.Xaml.Style
				};

				// This causes the copy to be initially setup correctly. I'm not quite sure why this is needed but I found
				// that if the first measure of this copy occurs with Text then it will just keep defaulting to a measure with no text.
				// My assumption is that the textbox assumes its first measure is in an empty state so it has to calibrate
				// or some variation of that logic

				copyOfTextBox.Measure(_zeroSize);
			}


			copyOfTextBox.Text = control.Text;
			copyOfTextBox.FontSize = control.FontSize;
			copyOfTextBox.FontFamily = control.FontFamily;
			copyOfTextBox.FontStretch = control.FontStretch;
			copyOfTextBox.FontStyle = control.FontStyle;
			copyOfTextBox.FontWeight = control.FontWeight;
			copyOfTextBox.Margin = control.Margin;
			copyOfTextBox.Padding = control.Padding;
			copyOfTextBox.Measure(_zeroSize);
			copyOfTextBox.Measure(constraint);

			Size result = new Size
			(
				Math.Ceiling(copyOfTextBox.DesiredSize.Width),
				Math.Ceiling(copyOfTextBox.DesiredSize.Height)
			);

			return result;
		}


		SizeRequest CalculateDesiredSizes(FormsTextBox control, Windows.Foundation.Size constraint, EditorSizeOption sizeOption)
		{
			if (sizeOption == EditorSizeOption.AutoSizeToTextChanges)
			{
				Size result = GetCopyOfSize(control, constraint);
				control.Measure(constraint);
				return new SizeRequest(result);
			}
			else
			{
				control.Measure(constraint);
				Size result = new Size(Math.Ceiling(control.DesiredSize.Width), Math.Ceiling(control.DesiredSize.Height));
				return new SizeRequest(result);
			}
		}

		public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			FormsTextBox child = Control;

			if (Children.Count == 0 || child == null)
				return new SizeRequest();

			return CalculateDesiredSizes(child, new Windows.Foundation.Size(widthConstraint, heightConstraint), Element.SizeOption);
		}

		void UpdateFont()
		{
			if (Control == null)
				return;

			Editor editor = Element;

			if (editor == null)
				return;

			bool editorIsDefault = editor.FontFamily == null &&
								   editor.FontSize == Device.GetNamedSize(NamedSize.Default, typeof(Editor), true) &&
								   editor.FontAttributes == FontAttributes.None;

			if (editorIsDefault && !_fontApplied)
				return;

			if (editorIsDefault)
			{
				// ReSharper disable AccessToStaticMemberViaDerivedType
				// Resharper wants to simplify 'TextBox' to 'Control', but then it'll conflict with the property 'Control'
				Control.ClearValue(TextBox.FontStyleProperty);
				Control.ClearValue(TextBox.FontSizeProperty);
				Control.ClearValue(TextBox.FontFamilyProperty);
				Control.ClearValue(TextBox.FontWeightProperty);
				Control.ClearValue(TextBox.FontStretchProperty);
				// ReSharper restore AccessToStaticMemberViaDerivedType
			}
			else
			{
				Control.ApplyFont(editor);
			}

			_fontApplied = true;
		}

		void UpdateInputScope()
		{
			Editor editor = Element;
			var custom = editor.Keyboard as CustomKeyboard;
			if (custom != null)
			{
				Control.IsTextPredictionEnabled = (custom.Flags & KeyboardFlags.Suggestions) != 0;
				Control.IsSpellCheckEnabled = (custom.Flags & KeyboardFlags.Spellcheck) != 0;
			}
			else
			{
				Control.ClearValue(TextBox.IsTextPredictionEnabledProperty);
				if (editor.IsSet(InputView.IsSpellCheckEnabledProperty))
					Control.IsSpellCheckEnabled = editor.IsSpellCheckEnabled;
				else
					Control.ClearValue(TextBox.IsSpellCheckEnabledProperty);
			}

			Control.InputScope = editor.Keyboard.ToInputScope();
		}

		void UpdateText()
		{
			string newText = Element.Text ?? "";

			if (Control.Text == newText)
			{
				return;
			}

			Control.Text = newText;
			Control.SelectionStart = Control.Text.Length;
		}

		void UpdateTextAlignment()
		{
			Control.UpdateTextAlignment(Element);
		}

		void UpdateTextColor()
		{
			Color textColor = Element.TextColor;

			BrushHelpers.UpdateColor(textColor, ref _textDefaultBrush,
				() => Control.Foreground, brush => Control.Foreground = brush);

			BrushHelpers.UpdateColor(textColor, ref _defaultTextColorFocusBrush,
				() => Control.ForegroundFocusBrush, brush => Control.ForegroundFocusBrush = brush);
		}

		void UpdateFlowDirection()
		{
			Control.UpdateFlowDirection(Element);
		}

		void UpdateMaxLength()
		{
			Control.MaxLength = Element.MaxLength;

			var currentControlText = Control.Text;

			if (currentControlText.Length > Element.MaxLength)
				Control.Text = currentControlText.Substring(0, Element.MaxLength);
		}

		void UpdateDetectReadingOrderFromContent()
		{
			if (Element.IsSet(Specifics.DetectReadingOrderFromContentProperty))
			{
				if (Element.OnThisPlatform().GetDetectReadingOrderFromContent())
				{
					Control.TextReadingOrder = TextReadingOrder.DetectFromContent;
				}
				else
				{
					Control.TextReadingOrder = TextReadingOrder.UseFlowDirection;
				}
			}
		}
	}
}