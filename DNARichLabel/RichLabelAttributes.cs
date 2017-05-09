using System.Collections.Generic;

namespace RichLabel.iOS
{
    using Foundation;
    using UIKit;
    
    public class RichLabelAttributes
    {
        public static RichLabelAttributes Default = new RichLabelAttributes();

        private List<object> _keys = new List<object>();
        private List<object> _values = new List<object>();
        
        public RichLabelAttributes()
        {
            ForegroundColor = UIColor.FromRGB(0, 120, 215);
        }
        
        public UIColor ForegroundColor { get; set; }
        public UIFont Font { get; set; }
        public NSShadow Shadow { get; set; }
        public NSMutableParagraphStyle ParagraphStyle { get; set; }

        public NSDictionary AsNSDictionary()
        {
            _keys = new List<object>();
            _values = new List<object>();

            AddAttribute(UIStringAttributeKey.ForegroundColor, ForegroundColor);
            AddAttribute(UIStringAttributeKey.Font, Font);
            AddAttribute(UIStringAttributeKey.Shadow, Shadow);
            AddAttribute(UIStringAttributeKey.ParagraphStyle, ParagraphStyle);
            
            return NSDictionary.FromObjectsAndKeys(_values.ToArray(), _keys.ToArray());
        }

        private void AddAttribute(object attributeKey, object objectToAdd)
        {
            if (objectToAdd != null)
            {
                _keys.Add(attributeKey);
                _values.Add(objectToAdd);
            }
        }
    }
}