using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PhotoMosaic.Common.Extensions
{
    public static class FrameworkElementExtensions
    {
        public static string InsertText(this TextBox box, string insertionText)
        {
            string text;
            if (box.SelectionLength == 0)
            {
                text = box.Text.Insert(box.CaretIndex, insertionText);
            }
            else
            {
                text = box.ReplaceSelection(insertionText);
            }
            return text;
        }

        public static string BackSpaceText(this TextBox box)
        {
            if (box.CaretIndex == 0 && box.SelectionLength == 0)
            {
                return box.Text;
            }
            else if (box.SelectionLength != 0)
            {
                return box.ReplaceSelection("");
            }
            else
            {
                return box.Text.ReplaceSelection("", box.CaretIndex - 1, 1);
            }
        }

        public static string DeleteText(this TextBox box)
        {
            if (box.CaretIndex == box.Text.Length)
            {
                return box.Text;
            }
            else if (box.SelectionLength != 0)
            {
                return box.ReplaceSelection("");
            }
            else
            {
                return box.Text.ReplaceSelection("", box.CaretIndex, 1);
            }
        }

        private static string ReplaceSelection(this TextBox box, string insertString)
        {
            if (box.SelectionLength == 0)
                return box.Text;

            return box.Text.ReplaceSelection(insertString, box.SelectionStart, box.SelectionLength);
        }

        private static string ReplaceSelection(this string baseText, string insertText, int selectionStart, int selectionLength)
        {
            var left = baseText.Substring(0, selectionStart);
            var lastIndex = selectionLength + selectionStart;
            var length = baseText.Length - lastIndex;
            var right = baseText.Substring(selectionLength + selectionStart, length);

            return left + insertText + right;
        }
    }
}
