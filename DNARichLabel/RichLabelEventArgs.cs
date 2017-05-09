namespace RichLabel.iOS
{
    using System;
    using Foundation;

    public class RichLabelEventArgs : EventArgs
    {
        public RichLabelEventArgs(RichLabelRange touchedRange)
        {
            TouchedRange = touchedRange;
        }

        public RichLabelRange TouchedRange { get; set; }

    }
}