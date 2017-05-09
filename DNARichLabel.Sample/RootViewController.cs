using System;

using CoreGraphics;
using Foundation;
using UIKit;

namespace DNARichLabel
{
	public partial class RootViewController : UIViewController, IRichLabelDelegate
	{
		public ModelController ModelController
		{
			get; private set;
		}

		public UIPageViewController PageViewController
		{
			get; private set;
		}

		protected RootViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			ModelController = new ModelController
			{
				RichLabelDelegate = this
			};

			// Configure the page view controller and add it as a child view controller.
			PageViewController = new UIPageViewController(UIPageViewControllerTransitionStyle.Scroll, UIPageViewControllerNavigationOrientation.Horizontal, UIPageViewControllerSpineLocation.None);
			PageViewController.WeakDelegate = this;

			var startingViewController = ModelController.GetViewController(0, Storyboard);
			var viewControllers = new UIViewController[] { startingViewController };
			PageViewController.SetViewControllers(viewControllers, UIPageViewControllerNavigationDirection.Forward, false, null);

			PageViewController.WeakDataSource = ModelController;

			AddChildViewController(PageViewController);
			View.AddSubview(PageViewController.View);

			// Set the page view controller's bounds using an inset rect so that self's view is visible around the edges of the pages.
			var pageViewRect = View.Bounds;
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
				pageViewRect = new CGRect(pageViewRect.X + 20, pageViewRect.Y + 20, pageViewRect.Width - 40, pageViewRect.Height - 40);
			PageViewController.View.Frame = pageViewRect;

			PageViewController.DidMoveToParentViewController(this);

			// Add the page view controller's gesture recognizers to the book view controller's view so that the gestures are started more easily.
			View.GestureRecognizers = PageViewController.GestureRecognizers;
		}

		void IRichLabelDelegate.OnRichLabelRangeTapped(RichLabelEventArgs args)
		{
			switch (args.TouchedRange.LinkType)
			{
				case RichLabelLinkType.Action:
                    ShowDialog("", $"Type: {args.TouchedRange.LinkType.ToString()} \nParameter received: {args.TouchedRange.Link}");
					break;
				case RichLabelLinkType.Hashtag:
                    ShowDialog("", $"Type: {args.TouchedRange.LinkType.ToString()} \nParameter received: {args.TouchedRange.Link}");
					break;
				case RichLabelLinkType.URL:
                    ShowDialog("", $"Type: {args.TouchedRange.LinkType.ToString()} \nParameter received: {args.TouchedRange.Link}");
					break;
				case RichLabelLinkType.UserHandle:
					ShowDialog("", $"Type: {args.TouchedRange.LinkType.ToString()} \nParameter received: {args.TouchedRange.Link}");
					break;
			}
		}

		public void ShowDialog(string title, string message)
		{
			var alertController = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

			var goodAction = UIAlertAction.Create("Good", UIAlertActionStyle.Default, null);
			alertController.AddAction(goodAction);

			PresentViewController(alertController, true, null);
		}
	}
}
