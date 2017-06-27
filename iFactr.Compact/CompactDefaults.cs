using iFactr.UI;

namespace iFactr.Compact
{
    public class CompactDefaults : IPlatformDefaults
    {
        public double LargeHorizontalSpacing
        {
            get { return 10; }
        }

        public double LeftMargin
        {
            get { return 8; }
        }

        public double RightMargin
        {
            get { return 8; }
        }

        public double SmallHorizontalSpacing
        {
            get { return 4; }
        }

        public double BottomMargin
        {
            get { return 12; }
        }

        public double LargeVerticalSpacing
        {
            get { return 16; }
        }

        public double SmallVerticalSpacing
        {
            get { return 8; }
        }

        public double TopMargin
        {
            get { return 12; }
        }

        public double CellHeight
        {
            get
            {
                return 44;
            }
        }

        public Font ButtonFont
        {
            get { return _normalFont; }
        }

        public Font DateTimePickerFont
        {
            get { return _normalFont; }
        }

        public Font HeaderFont
        {
            get { return _normalHeavyFont; }
        }

        public Font LabelFont
        {
            get { return _normalHeavyFont; }
        }

        public Font MessageTitleFont
        {
            get { return _normalFont; }
        }

        public Font MessageBodyFont
        {
            get { return _smallFont; }
        }

        public Font SectionHeaderFont
        {
            get { return _largeHeavyFont; }
        }

        public Font SectionFooterFont
        {
            get { return _smallFont; }
        }

        public Font SelectListFont
        {
            get { return _largeFont; }
        }

        public Font SmallFont
        {
            get { return _smallFont; }
        }

        public Font TabFont
        {
            get { return _normalFont; }
        }

        public Font TextBoxFont
        {
            get { return _normalFont; }
        }

        public Font ValueFont
        {
            get { return _smallFont; }
        }

        private readonly Font _largeFont = new Font("Roboto", 12);
        private readonly Font _largeHeavyFont = new Font("Roboto", 12) { Formatting = FontFormatting.Bold };
        private readonly Font _normalFont = new Font("Roboto", 10);
        private readonly Font _normalHeavyFont = new Font("Roboto", 10) { Formatting = FontFormatting.Bold };
        private readonly Font _smallFont = new Font("Roboto", 8);
    }
}