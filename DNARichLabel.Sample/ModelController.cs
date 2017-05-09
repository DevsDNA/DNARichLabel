namespace DNARichLabel
{
	using System;
	using System.Collections.Generic;
	using UIKit;

	public class ModelController : UIPageViewControllerDataSource
	{
		readonly List<Tuple<RichLabelLinkTypeOption, string>> pageData;

		public ModelController()
		{
			pageData = new List<Tuple<RichLabelLinkTypeOption, string>>();

			pageData.Add(new Tuple<RichLabelLinkTypeOption, string>(RichLabelLinkTypeOption.UserHandle, "This is an example of a 'UserHandle' type detection: @devsdna. As you can see, hashtags like #devsdna and links like http://www.devsdna.com are not active."));
			pageData.Add(new Tuple<RichLabelLinkTypeOption, string>(RichLabelLinkTypeOption.Hashtag, "This is an example of a 'Hashtag' detection: #devsdna. As you can see, users like @devsdna and links like http://www.devsdna.com are not active."));
			pageData.Add(new Tuple<RichLabelLinkTypeOption, string>(RichLabelLinkTypeOption.URL, "This is an example of a 'URL' type detection: http://www.devsdna.com. As you can see, hashtags like #devsdna and users like @devsdna are not active."));
			pageData.Add(new Tuple<RichLabelLinkTypeOption, string>(RichLabelLinkTypeOption.All, "In this case 'All' types are active, so we can detects users like @devsdna, hashtags like #devsdna and links like http://www.devsdna.com"));
			pageData.Add(new Tuple<RichLabelLinkTypeOption, string>(RichLabelLinkTypeOption.Action, "There is also a 'Action' type that detects some basic markdown links like this: [This is a link to DevsDNA's site](http://www.devsdna.com); or this: [This is a call-to-action](the parameter to pass to call-to-action listener)."));
		}

		public IRichLabelDelegate RichLabelDelegate { get; set; }

		public DataViewController GetViewController(int index, UIStoryboard storyboard)
		{
			if (index >= pageData.Count)
				return null;

			// Create a new view controller and pass suitable data.
			var dataViewController = (DataViewController)storyboard.InstantiateViewController("DataViewController");
			dataViewController.DataObject = pageData[index];

			if(RichLabelDelegate != null)
				dataViewController.RichLabelDelegate = RichLabelDelegate;

			return dataViewController;
		}

		public int IndexOf(DataViewController viewController)
		{
			return pageData.IndexOf(viewController.DataObject);
		}

		public override nint GetPresentationCount(UIPageViewController pageViewController)
		{
			return pageData.Count;
		}

		public override nint GetPresentationIndex(UIPageViewController pageViewController)
		{
			return 0;
		}

		#region Page View Controller Data Source

		public override UIViewController GetNextViewController(UIPageViewController pageViewController, UIViewController referenceViewController)
		{
			int index = IndexOf((DataViewController)referenceViewController);

			if (index == -1 || index == pageData.Count - 1)
				return null;

			return GetViewController(index + 1, referenceViewController.Storyboard);
		}

		public override UIViewController GetPreviousViewController(UIPageViewController pageViewController, UIViewController referenceViewController)
		{
			int index = IndexOf((DataViewController)referenceViewController);

			if (index == -1 || index == 0)
				return null;

			return GetViewController(index - 1, referenceViewController.Storyboard);
		}

		#endregion
	}
}
