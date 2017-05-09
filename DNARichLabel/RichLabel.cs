namespace RichLabel.iOS
{
    using UIKit;
    using Foundation;
    using CoreGraphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    [Register("RichLabel")]
    public class RichLabel : UILabel, INSLayoutManagerDelegate
    {
        private NSLayoutManager _layoutManager;
        private NSTextContainer _textContainer;
        private NSTextStorage _textStorage;

        private List<RichLabelRange> _linkRanges;
        private bool _isTouchMoved;
        private NSRange _selectedRange;

		nfloat _lineHeightMultiple = 1.0f;

		bool _alignTextToTop = true;
		float _topOffset = 4.0f;

        private string _unparsedText = string.Empty;

        private Dictionary<RichLabelLinkType, RichLabelAttributes> _linkTypeAttributes;
        private bool _systemUrlStyle;
        private bool _automaticLinkDetectionEnabled;
        private RichLabelLinkTypeOption _linkDetectionTypes;
        private UIColor _selectedLinkBackgroundColor;

        private List<string> _ignoredKeywords = new List<string>();

        public RichLabel(IntPtr handle) : base(handle)
        {
            SetupTextSystem();
        }
        
        public RichLabel(NSCoder coder) : base(coder)
        {
            SetupTextSystem();
        }
        
        public RichLabel(CGRect frame) : base(frame)
        {
            SetupTextSystem();
        }

		public IRichLabelDelegate Delegate { get; set; }

		public IRichLabelContainerDelegate ContainerDelegate { get; set; }

        public RichLabelLinkTypeOption LinkDetectionTypes
        {
            get { return _linkDetectionTypes; }
            set
            {
                _linkDetectionTypes = value;
                UpdateTextStoreWithText();
            }
        }

        public bool AutomaticLinkDetectionEnabled
        {
            get
            {
                return _automaticLinkDetectionEnabled;
            }
            set
            {
                _automaticLinkDetectionEnabled = value;
                UpdateTextStoreWithText();
            }
        }

		public nfloat LineHeightMultiple
		{
			get
			{
				return _lineHeightMultiple;
			}
			set
			{
				_lineHeightMultiple = value;
				UpdateTextStoreWithText();
			}
		}

        public UIColor SelectedLinkBackgroundColor
        {
            get { return _selectedLinkBackgroundColor; }
            set
            {
                _selectedLinkBackgroundColor = value;
                UpdateTextStoreWithText();
            }
        }

        public NSRange SelectedRange
        {
            get { return _selectedRange; }
            set
            {
                NSRange range = value;

                if (_selectedRange.Length > 0 && _textStorage.Length > _selectedRange.Location && !_selectedRange.Equals(range))
                {
                    _textStorage.RemoveAttribute(UIStringAttributeKey.BackgroundColor, _selectedRange);
                }

                if (range.Length > 0 && _selectedLinkBackgroundColor != null)
                {
                    _textStorage.AddAttribute(UIStringAttributeKey.BackgroundColor, _selectedLinkBackgroundColor, range);
                }

                _selectedRange = range;

                SetNeedsDisplay();
            }
        }

        public bool SystemUrlStyle
        {
            get { return _systemUrlStyle; }

            set {
                _systemUrlStyle = value;

                Text = Text;
            }
        }

        public List<string> IgnoredKeywords
        {
            get { return _ignoredKeywords; }
            set { _ignoredKeywords = value; }
        }

		public void SetUp(nfloat lineHeightMultiple, bool userInteractionEnabled, bool automaticLinkDetectionEnabled, RichLabelLinkTypeOption linkDetectionTypes)
		{
			_lineHeightMultiple = lineHeightMultiple;
			_automaticLinkDetectionEnabled = automaticLinkDetectionEnabled;
			UserInteractionEnabled = userInteractionEnabled;
			_linkDetectionTypes = linkDetectionTypes;

            UpdateTextStoreWithText();
		}

        private void SetupTextSystem()
        {
            _textContainer = new NSTextContainer
            {
                LineFragmentPadding = 0,
                MaximumNumberOfLines = (nuint) Lines,
                LineBreakMode = LineBreakMode,
                Size = Frame.Size
            };

            _layoutManager = new NSLayoutManager {Delegate = this};
            _layoutManager.AddTextContainer(_textContainer);

            _textContainer.LayoutManager = _layoutManager;

            UserInteractionEnabled = true;

            AutomaticLinkDetectionEnabled = false;

            LinkDetectionTypes = RichLabelLinkTypeOption.All;

            _systemUrlStyle = false;

            _selectedLinkBackgroundColor = UIColor.FromWhiteAlpha(.95f, 1.0f);
            
            UpdateTextStoreWithText();

        }

		void UpdateLinkTypeAttributes()
		{
			_linkTypeAttributes = new Dictionary<RichLabelLinkType, RichLabelAttributes>();

			var linkTypes = new RichLabelLinkType[] {
				RichLabelLinkType.Action,
				RichLabelLinkType.Hashtag,
				RichLabelLinkType.URL,
				RichLabelLinkType.UserHandle
			};

			foreach (var linkType in linkTypes)
			{
				var linkAttributes = AttributesFromProperties();

				switch (linkType)
				{
					case RichLabelLinkType.Action:
						linkAttributes.ForegroundColor = UIColor.FromRGB(144, 164, 174);
						break;
					case RichLabelLinkType.Hashtag:
						linkAttributes.ForegroundColor = UIColor.FromRGB(0, 120, 215);
						break;
					case RichLabelLinkType.URL:
						linkAttributes.ForegroundColor = UIColor.FromRGB(0, 120, 215);
						break;
					case RichLabelLinkType.UserHandle:
						linkAttributes.ForegroundColor = UIColor.FromRGB(0, 120, 215);
						break;
				}

				SetAttributesForLinkType(linkType, linkAttributes);

			}

		}

        private RichLabelRange LinkAtPoint(CGPoint location)
        {
            if (_textStorage.ToString().Length == 0)
                return null;

            CGPoint textOffset = CalcGlyphsPositionInView();

            location.X -= textOffset.X;
            location.Y -= textOffset.Y;

            nuint touchedChar = _layoutManager.GlyphIndexForPoint(location, _textContainer);
            
            CGRect lineRect = _layoutManager.LineFragmentRectForGlyphAtIndex(touchedChar);
            if (!lineRect.Contains(location))
                return null;

            foreach (RichLabelRange linkRange in _linkRanges)
            {
                NSRange range = linkRange.Range.RangeValue;

                if ((touchedChar >= (nuint)range.Location) && (touchedChar < (nuint)(range.Location + range.Length)))
                    return linkRange;
            }

            return null;
        }

        public override nint Lines
        {
            get { return base.Lines; }
            set
            {
                base.Lines = value;
                _textContainer.MaximumNumberOfLines = (nuint)base.Lines;
            }
        }
        
        public override string Text
        {
            get { return base.Text; }
            set
            {
                _unparsedText = value;

                if (string.IsNullOrEmpty(_unparsedText))
                {
                    base.Text = "";
                }
                else
                {
                    base.Text = StringExtensions.GetParsedMarkdownString(_unparsedText);
                }

                NSAttributedString attributedText = new NSAttributedString(base.Text, AttributesFromProperties().AsNSDictionary());
                UpdateTextStoreWithAttributedString(attributedText);
            }
        }

        public override NSAttributedString AttributedText
        {
            get { return base.AttributedText; }
            set
            {
				_unparsedText = value.Value;

				if (string.IsNullOrEmpty(_unparsedText))
				{
					base.AttributedText = value;
				}
				else
				{
					var range = new NSRange(0, value.Value.Length);
					var attributes = value.GetAttributes(0, out range);

					base.AttributedText = new NSAttributedString(StringExtensions.GetParsedMarkdownString(_unparsedText), attributes);
				}

                UpdateTextStoreWithAttributedString(base.AttributedText);
            }
        }
        
        private RichLabelAttributes AttributesForLinkType(RichLabelLinkType linkType)
        {
            return _linkTypeAttributes.FirstOrDefault(t => t.Key == linkType).Value ?? RichLabelAttributes.Default;
        }

        private void SetAttributesForLinkType(RichLabelLinkType linkType, RichLabelAttributes attributes)
        {
            if (attributes != null)
            {
                _linkTypeAttributes[linkType] = attributes;
            }
            else
            {
                _linkTypeAttributes.Remove(linkType);
            }

            Text = Text;
        }

        private void UpdateTextStoreWithText()
        {
            var attributes = AttributesFromProperties().AsNSDictionary();

            if (AttributedText != null)
            {
                UpdateTextStoreWithAttributedString(AttributedText);
            }else if (Text != null)
            {
                UpdateTextStoreWithAttributedString(new NSAttributedString(Text, attributes));
            }
            else
            {
                UpdateTextStoreWithAttributedString(new NSAttributedString("", attributes));
            }

			UpdateLinkTypeAttributes();
             
            SetNeedsDisplay();
        }

        private void UpdateTextStoreWithAttributedString(NSAttributedString attributedString)
        {
            if (attributedString.Length != 0)
            {
                attributedString = SanitizeAttributedString(attributedString);
            }

            if (AutomaticLinkDetectionEnabled && attributedString.Length != 0)
            {
                _linkRanges = GetRangesForLinks(attributedString);
                attributedString = AddLinkAttributesToAttributedString(attributedString, _linkRanges);
            }
            else
            {
                _linkRanges = null;
            }

            if (_textStorage != null)
            {
                _textStorage.SetString(attributedString);
            }
            else
            {
                _textStorage = new NSTextStorage();
                _textStorage.SetString(attributedString);
                _textStorage.AddLayoutManager(_layoutManager);
                _layoutManager.TextStorage = _textStorage;
            }
        }

        private RichLabelAttributes AttributesFromProperties()
        {
            NSShadow shadow = ShadowAttributeFromProperties();

            UIColor color = ColorAttributeFromProperties();

            var paragraph = new NSMutableParagraphStyle
			{
				Alignment = TextAlignment,
				LineHeightMultiple = LineHeightMultiple
			};

            return new RichLabelAttributes
            {
                ForegroundColor = color,
                Font = Font,
                Shadow = shadow,
                ParagraphStyle = paragraph
            };
        }

        private NSShadow ShadowAttributeFromProperties()
        {
            NSShadow shadow = new NSShadow();
            if (ShadowColor != null)
            {
                shadow.ShadowColor = ShadowColor;
                shadow.ShadowOffset = ShadowOffset;
            }
            else
            {
                shadow.ShadowOffset = new CGSize(0, -1);
                shadow.ShadowColor = null;
            }

            return shadow;
        }

        private UIColor ColorAttributeFromProperties()
        {
            UIColor color = TextColor;
            if (!Enabled)
            {
                color = UIColor.LightGray;
            }
            else if (Highlighted)
            {
                color = HighlightedTextColor;
            }

            return color;
        }
        
        private List<RichLabelRange> GetRangesForLinks(NSAttributedString attributedString)
        {
            List<RichLabelRange> rangesForLinks = new List<RichLabelRange>();
            
            switch (LinkDetectionTypes)
            {
                case RichLabelLinkTypeOption.UserHandle:
                    rangesForLinks.AddRange(GetRangesForUserHandles(attributedString.Value));
                    break;
                case RichLabelLinkTypeOption.None:
                    break;
                case RichLabelLinkTypeOption.Hashtag:
                    rangesForLinks.AddRange(GetRangesForHashtags(attributedString.Value));
                    break;
                case RichLabelLinkTypeOption.URL:
                    rangesForLinks.AddRange(GetRangesForUrls(attributedString.Value));
                    break;
                case RichLabelLinkTypeOption.Action:
                    rangesForLinks.AddRange(GetRangesForActions(attributedString.Value));
                    break;
                case RichLabelLinkTypeOption.All:
                    rangesForLinks.AddRange(GetRangesForActions(attributedString.Value));
                    rangesForLinks.AddRange(GetRangesForUserHandles(attributedString.Value));
                    rangesForLinks.AddRange(GetRangesForHashtags(attributedString.Value));
                    rangesForLinks.AddRange(GetRangesForUrls(attributedString.Value));
                    break;
            }

            return rangesForLinks;
        }

        private List<RichLabelRange> GetRangesForUserHandles(string text)
        {
            List<RichLabelRange> rangesForUserHandles = new List<RichLabelRange>();

            Regex linkParser = new Regex(@"(?<=\s|^)@(\w*[A-Za-z0-9_]+\w*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in linkParser.Matches(text))
            {
                if (!IgnoreMatch(match.Value))
                {
                    rangesForUserHandles.Add(new RichLabelRange
                    {
                        LinkType = RichLabelLinkType.UserHandle,
                        Range = NSValue.FromRange(new NSRange(match.Index, match.Length)),
                        Link = match.Value
                    });
                }
            }

            return rangesForUserHandles;
        }

        private List<RichLabelRange> GetRangesForHashtags(string text)
        {
            List<RichLabelRange> rangesForHashtags = new List<RichLabelRange>();

            Regex linkParser = new Regex(@"(?<=\s|^)#(\w*[A-Za-z0-9_-]+\w*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in linkParser.Matches(text))
            {
                if (!IgnoreMatch(match.Value))
                {
                    rangesForHashtags.Add(new RichLabelRange
                    {
                        LinkType = RichLabelLinkType.Hashtag,
                        Range = NSValue.FromRange(new NSRange(match.Index, match.Length)),
                        Link = match.Value
                    });
                }
            }

            return rangesForHashtags;
        }

        private List<RichLabelRange> GetRangesForUrls(string text)
        {
            List<RichLabelRange> rangesForUrls = new List<RichLabelRange>();
            
            Regex linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in linkParser.Matches(text))
            {
                if (!IgnoreMatch(match.Value))
                {
                    rangesForUrls.Add(new RichLabelRange
                    {
                        LinkType = RichLabelLinkType.URL,
                        Range = NSValue.FromRange(new NSRange(match.Index, match.Length)),
                        Link = match.Value
                    });
                }
            }

            return rangesForUrls;
        }

        private List<RichLabelRange> GetRangesForActions(string text)
        {
            List<RichLabelRange> rangesForUrls = new List<RichLabelRange>();
            
            Regex linkParser = new Regex(@"\@\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in linkParser.Matches(_unparsedText))
            {
                if (!IgnoreMatch(match.Value))
                {
                    GroupCollection groups = match.Groups;
                    if (groups.Count >= 3)
                    {
						rangesForUrls.Add(new RichLabelRange
						{
							LinkType = RichLabelLinkType.UserHandle,
							Range = NSValue.FromRange(new NSRange(Text.IndexOf(groups[1].Value), groups[1].Length)),
							Link = groups[2].Value
						});
                    }
                }
            }

			linkParser = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			foreach (Match match in linkParser.Matches(_unparsedText))
			{
				if (!IgnoreMatch(match.Value))
				{
					GroupCollection groups = match.Groups;
					if (groups.Count >= 3)
					{
						if (!rangesForUrls.Any(range => range.Link == groups[2].Value))
							rangesForUrls.Add(new RichLabelRange
							{
								LinkType = RichLabelLinkType.Action,
								Range = NSValue.FromRange(new NSRange(Text.IndexOf(groups[1].Value), groups[1].Length)),
								Link = groups[2].Value
							});
					}
				}
			}
            
            return rangesForUrls;
        }

        private bool IgnoreMatch(string stringToMatch)
        {
            return _ignoredKeywords.Contains(stringToMatch.ToLowerInvariant());
        }
        
        private NSAttributedString AddLinkAttributesToAttributedString(NSAttributedString originalAttributedString, List<RichLabelRange> linkRanges)
        {
            NSMutableAttributedString attributedString = new NSMutableAttributedString(originalAttributedString);

			//SetUpLinkTypeAttributes();

            foreach (RichLabelRange linkRange in linkRanges)
            {
                NSRange range = linkRange.Range.RangeValue;
                RichLabelLinkType linkType = linkRange.LinkType;

                RichLabelAttributes attributes = AttributesForLinkType(linkType);

                attributedString.AddAttributes(attributes.AsNSDictionary(), range);

                if (_systemUrlStyle && linkRange.LinkType == RichLabelLinkType.URL)
                {
                    attributedString.AddAttribute(UIStringAttributeKey.Link, new NSString(linkRange.Link), range);
                }

            }

            return attributedString;
        }

        public override CGRect TextRectForBounds(CGRect bounds, nint numberOfLines)
        {
            CGSize savedTextContainerSize = _textContainer.Size;
            nuint savedTextContainerNumberOfLines = _textContainer.MaximumNumberOfLines;

            _textContainer.Size = bounds.Size;
            _textContainer.MaximumNumberOfLines = (nuint) numberOfLines;

            CGRect textBounds = _layoutManager.GetUsedRectForTextContainer(_textContainer);

            textBounds.Location = bounds.Location;
            textBounds.Size = new CGSize(Math.Ceiling(textBounds.Size.Width), Math.Ceiling(textBounds.Size.Height));

			if (textBounds.Size.Height > 17)
			{
				float offsetY;
				if (textBounds.Size.Height < Bounds.Size.Height)
				{
					offsetY = (float)(Bounds.Size.Height - textBounds.Size.Height) / 2.0f;
				}
				else
				{
					offsetY = -_topOffset;
				}
				textBounds.Location = new CGPoint(textBounds.Location.X, textBounds.Location.Y + offsetY);
			}

            _textContainer.Size = savedTextContainerSize;
            _textContainer.MaximumNumberOfLines = savedTextContainerNumberOfLines;

            return textBounds;
        }

        public override void DrawText(CGRect rect)
        {
            NSRange glyphRange = _layoutManager.GetGlyphRange(_textContainer);
            CGPoint glyphsPoint = CalcGlyphsPositionInView();

            _layoutManager.DrawBackgroundForGlyphRange(glyphRange, glyphsPoint);
            _layoutManager.DrawGlyphs(glyphRange, glyphsPoint);
        }

        private CGPoint CalcGlyphsPositionInView()
        {
            CGPoint textOffset = CGPoint.Empty;

            CGRect textBounds = _layoutManager.GetUsedRectForTextContainer(_textContainer);
			textBounds.Size = new CGSize(Math.Ceiling(textBounds.Size.Width), Math.Ceiling(textBounds.Size.Height));

			if (textBounds.Size.Height > Math.Ceiling(Font.PointSize * AttributesForLinkType(RichLabelLinkType.UserHandle).ParagraphStyle.LineHeightMultiple))
			{
				textOffset.Y = -_topOffset;
			}

            return textOffset;
        }
        
        private NSAttributedString SanitizeAttributedString(NSAttributedString attributedString)
        {
            NSRange range;
            NSParagraphStyle paragraphStyle = (NSParagraphStyle)attributedString.GetAttribute(UIStringAttributeKey.ParagraphStyle, 0, out range);

            if (paragraphStyle == null)
            {
                return attributedString;
            }

            NSMutableParagraphStyle mutableParagraphStyle = (NSMutableParagraphStyle) paragraphStyle.MutableCopy();
            mutableParagraphStyle.LineBreakMode = UILineBreakMode.WordWrap;

            NSMutableAttributedString restyled = new NSMutableAttributedString(attributedString);
            restyled.AddAttribute(UIStringAttributeKey.ParagraphStyle, mutableParagraphStyle, new NSRange(0, restyled.Length));

            return restyled;
        }

        public override CGRect Frame
        {
            get { return base.Frame; }
            set
            {
                base.Frame = value;
                _textContainer.Size = Bounds.Size;
            }
        }

        public override CGRect Bounds
        {
            get{ return base.Bounds; }

            set
            {
                base.Bounds = value;
                _textContainer.Size = Bounds.Size;
            }
        }

		public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            _textContainer.Size = Bounds.Size;
        }


        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            _isTouchMoved = false;

            RichLabelRange touchedLink = LinkAtPoint(GetTouchLocation(touches));

            if (touchedLink != null)
            {
                SelectedRange = touchedLink.Range.RangeValue;
            }
            else
            {
                base.TouchesBegan(touches, evt);
            }
        }
        
        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);

            _isTouchMoved = true;
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            if (_isTouchMoved)
            {
                SelectedRange = default(NSRange);
                return;
            }
            
            RichLabelRange touchedLink = LinkAtPoint(GetTouchLocation(touches));

			if (touchedLink != null && touchedLink.LinkType != RichLabelLinkType.Action)
            {
                Delegate?.OnRichLabelRangeTapped(new RichLabelEventArgs(touchedLink));
            }
            else
            {
				ContainerDelegate?.OnRichLabelRangeNotTapped();
            }

            SelectedRange = default(NSRange);
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);

            SelectedRange = default(NSRange);
        }

		public void OverrideNoActionLinksAttributes(UIFont font, UIColor color)
		{
			foreach (var attribute in _linkTypeAttributes)
			{
				if (attribute.Key == RichLabelLinkType.Action)
					continue;

				attribute.Value.ForegroundColor = color;
				attribute.Value.Font = font;
			}
		}

        private CGPoint GetTouchLocation(NSSet touches)
        {
            UITouch touch = (UITouch)touches.AnyObject;
            return touch.LocationInView(this);
        }

        [Export("layoutManager:shouldBreakLineByHyphenatingBeforeCharacterAtIndex:")]
        public bool ShouldBreakLineByHyphenatingBeforeCharacter(NSLayoutManager layoutManager,
            UInt32 charIndex)
        {
            NSRange range;
            NSUrl linkUrl = (NSUrl) layoutManager.TextStorage.GetAttribute(UIStringAttributeKey.Link, (nint) charIndex, out range);
            
            return !(linkUrl != null && (charIndex > range.Location) && (charIndex <= range.Location + range.Length));
        }

    }

}