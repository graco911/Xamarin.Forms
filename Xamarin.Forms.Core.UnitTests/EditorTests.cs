using System;
using NUnit.Framework;

namespace Xamarin.Forms.Core.UnitTests
{
	[TestFixture]
	public class EditorTests : BaseTestFixture
	{
		[TestCase ("Hi", "My text has changed")]
		[TestCase (null, "My text has changed")]
		[TestCase ("Hi", null)]
		public void EditorTextChangedEventArgs (string initialText, string finalText)
		{
			var editor = new Editor {
				Text = initialText
			};

			Editor editorFromSender = null;
			string oldText = null;
			string newText = null;

			editor.TextChanged += (s, e) => {
				editorFromSender = (Editor)s;
				oldText = e.OldTextValue;
				newText = e.NewTextValue;
			};

			editor.Text = finalText;

			Assert.AreEqual (editor, editorFromSender);
			Assert.AreEqual (initialText, oldText);
			Assert.AreEqual (finalText, newText);
		}

		[Test]
		public void AutoResizeWithMinimumHeightSet()
		{
			StackLayout container = new StackLayout()
			{
				BackgroundColor = Color.Purple,
				Platform = new UnitPlatform(),
				IsPlatformEnabled = true
			};


			StackLayout layout = new StackLayout()
			{
				BackgroundColor = Color.Pink,
				HeightRequest = 200,
				WidthRequest = 200,
				Platform = new UnitPlatform(),
				IsPlatformEnabled = true
			};

			var editor = new Editor()
			{
				BackgroundColor = Color.Green,
				MinimumHeightRequest = 10,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				ClassId = "editor1",
				Platform = new UnitPlatform(getNativeSizeFunc: GetWhatYouReceive),
				IsPlatformEnabled = true
			};

			var editor2 = new Editor()
			{
				BackgroundColor = Color.Green,
				HeightRequest = 2000,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				ClassId = "editor2",
				Platform = new UnitPlatform(getNativeSizeFunc: GetWhatYouReceive),
				IsPlatformEnabled = true
			};

			container.Children.Add(layout);
			layout.Children.Add(editor);
			layout.Children.Add(editor2);


			var result = container.Measure(double.PositiveInfinity, double.PositiveInfinity);

			
		}

		private SizeRequest GetWhatYouReceive(VisualElement arg1, double arg2, double arg3)
		{
			return new SizeRequest(new Size(arg2, arg3));
		}
	}
}
