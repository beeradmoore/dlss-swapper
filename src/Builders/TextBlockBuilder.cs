using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;
using Windows.UI.Text;
using HtmlAgilityPack;

namespace DLSS_Swapper.Builders
{
    class TextBlockBuilder
    {
        public TextBlockBuilder(string expression)
        {
            _expression = expression;
        }

        public TextBlock Build()
        {
            TextBlock content = new TextBlock { TextWrapping = TextWrapping.Wrap };
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(_expression);

            foreach (HtmlNode? node in htmlDoc.DocumentNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "#text":
                        content.Inlines.Add(new Run { Text = node.InnerText });
                        break;
                    case "i":
                        content.Inlines.Add(new Run { Text = node.InnerText, FontStyle = FontStyle.Italic });
                        break;
                    case "br":
                        content.Inlines.Add(new Run { Text = "\n" });
                        break;
                    case "b":
                        content.Inlines.Add(new Run { Text = node.InnerText, FontWeight = FontWeights.Bold });
                        break;
                }
            }
            return content;
        }

        private readonly string _expression;
    }
}
