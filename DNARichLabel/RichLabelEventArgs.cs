namespace DNARichLabel
{
    using System;

    public class RichLabelEventArgs : EventArgs
    {
        public RichLabelEventArgs(RichLabelRange touchedRange)
        {
            TouchedRange = touchedRange;
        }

        public RichLabelRange TouchedRange { get; set; }

    }
}