using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class TextArea : TextBase, ITextArea
    {
        public TextArea()
        {
            base.Multiline = true;
            Height = (int)(UI.Font.PreferredTextBoxFont.GetLineHeight() * 4);
        }
    }
}