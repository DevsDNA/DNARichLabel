namespace DNARichLabel
{
	using System;
	using UIKit;

	public partial class DataViewController : UIViewController
	{
		public Tuple<RichLabelLinkTypeOption, string> DataObject
		{
			get; set;
		}

		protected DataViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public IRichLabelDelegate RichLabelDelegate { get; set; }

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			richLabel.SetUp(linkDetectionTypes: DataObject.Item1);

			if (RichLabelDelegate != null)
				richLabel.Delegate = RichLabelDelegate;

			richLabel.Text = DataObject.Item2;
		}
	}
}
