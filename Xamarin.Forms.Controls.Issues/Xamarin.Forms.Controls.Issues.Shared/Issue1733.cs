using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
#if UITEST
using Xamarin.Forms.Core.UITests;
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls
{
#if UITEST
	[Category(UITestCategories.Editor)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 1733, "Autoresizable Editor")]
	public class Issue1733 : TestContentPage
	{
		protected override void Init()
		{
			StackLayout container = new StackLayout()
			{
				BackgroundColor = Color.Purple
			};

			StackLayout layout = new StackLayout()
			{
				BackgroundColor = Color.Pink,
				HeightRequest = 200
			};

			var editor = new Editor()
			{
				BackgroundColor = Color.Green,
				MinimumHeightRequest = 10,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				AutomationId = "editor1"
			};

			var editor2 = new Editor()
			{
				BackgroundColor = Color.Green,
				MinimumHeightRequest = 200,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				AutomationId = "editor2"
			};


			layout.Children.Add(editor);
			layout.Children.Add(editor2);

			StackLayout layoutHorizontal = new StackLayout()
			{
				BackgroundColor = Color.Yellow,
				Orientation = StackOrientation.Horizontal
			};

			var editor3 = new Editor()
			{
				BackgroundColor = Color.Green,
				MinimumWidthRequest = 10,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				AutomationId = "editor3"
			};

			var editor4 = new Editor()
			{
				BackgroundColor = Color.Green,
				MinimumWidthRequest = 200,
				SizeOption = EditorSizeOption.AutoSizeToTextChanges,
				AutomationId = "editor4"
			};


			layoutHorizontal.Children.Add(editor3);
			layoutHorizontal.Children.Add(editor4);

			container.Children.Add(layout);
			container.Children.Add(layoutHorizontal);


			List<Editor> editors = new List<Editor>();
			editors.Add(editor);
			editors.Add(editor2);
			editors.Add(editor3);
			editors.Add(editor4);

			Button buttonChangeFont = new Button()
			{
				Text = "Change the Font",
				AutomationId = "buttonChangeFont"
			};


			Button buttonAddText = new Button()
			{
				Text = "Change the text",
				AutomationId = "buttonAddText"
			};

			Button buttonChangeSizeOption = new Button()
			{
				Text = "Change the size Option option",
				AutomationId = "buttonChangeSizeOption"
			};

			double fontSizeInitial = editor.FontSize;
			buttonChangeFont.Clicked += (x, y) =>
			{
				editors.ForEach(e =>
				{
					if (e.FontSize == fontSizeInitial)
						e.FontSize = 40;
					else
						e.FontSize = fontSizeInitial;
				});
			};

			buttonAddText.Clicked += (_, __) =>
			{
				editors.ForEach(e =>
				{
					if (String.IsNullOrWhiteSpace(e.Text))
						e.Text = String.Join(" ", Enumerable.Range(0, 1000).Select(x => "f").ToArray());
					else
						e.Text = String.Empty;
				});
			};

			buttonChangeSizeOption.Clicked += (_, __) =>
			{
				editors.ForEach(e =>
				{
					EditorSizeOption option = EditorSizeOption.AutoSizeToTextChanges;
					if (e.SizeOption == option)
						option = EditorSizeOption.Default;

					e.SizeOption = option;
				});
			};

			container.Children.Add(buttonChangeFont);
			container.Children.Add(buttonAddText);
			container.Children.Add(buttonChangeSizeOption);


			editors.ForEach(e =>
			{
				Label automationLabelId = new Label();
				automationLabelId.SetBinding(Label.TextProperty, new Binding(nameof(Editor.ClassId), source: e));

				Label width = new Label();
				width.SetBinding(Label.TextProperty, new Binding(nameof(Editor.Width), source: e));

				Label height = new Label();
				height.SetBinding(Label.TextProperty, new Binding(nameof(Editor.Height), source: e));


				container.Children.Add(automationLabelId);
				container.Children.Add(width);
				container.Children.Add(height);
			});

			Content = new ScrollView() { Content = container };
		}

#if UITEST
		[Test]
		public void Issue1733Test()
		{
			RunningApp.WaitForElement(q => q.Marked("editor1"));
			RunningApp.Tap(q => q.Marked("buttonChangeFont"));

		}

#endif
	}
}
