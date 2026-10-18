using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Reach.Module.SoapSearch.Extraction;

// XML Tools v2's text-based layout: indent elements, keep text inline and split
// subsequent xmlns attributes. Never serialize an XML DOM back over the payload.
internal interface ISoapEnvelopeFormatter
{
    string Format(string xml, bool requireSoapEnvelope = false);
}

internal sealed partial class SoapEnvelopeFormatter : ISoapEnvelopeFormatter
{
    public string Format(string xml, bool requireSoapEnvelope = false)
    {
        var preservedElements = Validate(xml, requireSoapEnvelope);
        var roots = new List<Node>();
        var stack = new Stack<Node>();
        var elementIndex = 0;
        foreach (var token in Tokens().Matches(xml).Cast<Match>())
        {
            var text = token.Value;
            if (text.StartsWith("</", StringComparison.Ordinal))
            {
                var node = stack.Pop();
                node.CloseStart = token.Index;
                node.End = token.Index + token.Length;
                continue;
            }

            var element = text[0] == '<' && text[1] is not ('!' or '?');
            var child = new Node(token.Index, token.Index + token.Length, element);
            (stack.TryPeek(out var parent) ? parent.Children : roots).Add(child);
            if (element)
            {
                child.Preserve = preservedElements[elementIndex++];
                if (!text.EndsWith("/>", StringComparison.Ordinal))
                {
                    stack.Push(child);
                }
            }
        }

        var output = new StringBuilder();
        var newline = xml.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        foreach (var root in roots)
        {
            if (IsWhitespace(xml.AsSpan(root.Start, root.End - root.Start)))
            {
                continue;
            }

            if (output.Length > 0)
            {
                output.Append(newline);
            }

            Write(root, xml, output, 0, newline);
        }

        return output.ToString();
    }

    private static List<bool> Validate(string xml, bool requireSoapEnvelope)
    {
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(
            input,
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
        );
        var elements = new List<bool>();
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (
                elements.Count == 0
                && requireSoapEnvelope
                && (
                    reader.LocalName != "Envelope"
                    || reader.NamespaceURI
                        is not (
                            "http://schemas.xmlsoap.org/soap/envelope/"
                            or "http://www.w3.org/2003/05/soap-envelope"
                        )
                )
            )
            {
                throw new XmlException("The log event does not contain a SOAP envelope.");
            }

            elements.Add(reader.XmlSpace == XmlSpace.Preserve);
        }

        return elements;
    }

    private static void Write(
        Node node,
        string xml,
        StringBuilder output,
        int depth,
        string newline
    )
    {
        var hasElements = node.Children.Any(x => x.IsElement);
        var hasText = node.Children.Any(x =>
            !x.IsElement
            && (
                xml.AsSpan(x.Start).StartsWith("<![CDATA[", StringComparison.Ordinal)
                || (xml[x.Start] != '<' && !IsWhitespace(xml.AsSpan(x.Start, x.End - x.Start)))
            )
        );

        if (!node.IsElement || node.Preserve || (hasElements && hasText))
        {
            output.Append(xml.AsSpan(node.Start, node.End - node.Start));
            return;
        }

        var opening = xml[node.Start..node.OpenEnd];
        var attributeIndex = 0;
        output.Append(
            Attributes()
                .Replace(
                    opening,
                    match =>
                    {
                        var split =
                            attributeIndex++ > 0
                            && (
                                match.Groups["name"].Value == "xmlns"
                                || match
                                    .Groups["name"]
                                    .Value.StartsWith("xmlns:", StringComparison.Ordinal)
                            );
                        return (split ? newline + new string(' ', (depth + 1) * 4) : " ")
                            + match.Groups["attribute"].Value;
                    }
                )
        );

        if (node.CloseStart == node.OpenEnd && node.End == node.OpenEnd)
        {
            return;
        }

        if (!hasElements || hasText)
        {
            output.Append(xml.AsSpan(node.OpenEnd, node.End - node.OpenEnd));
            return;
        }

        foreach (var child in node.Children)
        {
            if (!child.IsElement && IsWhitespace(xml.AsSpan(child.Start, child.End - child.Start)))
            {
                continue;
            }

            output.Append(newline).Append(' ', (depth + 1) * 4);
            Write(child, xml, output, depth + 1, newline);
        }

        output
            .Append(newline)
            .Append(' ', depth * 4)
            .Append(xml.AsSpan(node.CloseStart, node.End - node.CloseStart));
    }

    private static bool IsWhitespace(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (character is not (' ' or '\t' or '\r' or '\n'))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class Node
    {
        public Node(int start, int openEnd, bool isElement)
        {
            Start = start;
            OpenEnd = openEnd;
            CloseStart = openEnd;
            End = openEnd;
            IsElement = isElement;
        }

        public int Start { get; }
        public int OpenEnd { get; }
        public int CloseStart { get; set; }
        public int End { get; set; }
        public bool IsElement { get; }
        public bool Preserve { get; set; }
        public List<Node> Children { get; } = [];
    }

    [GeneratedRegex(
        "<!--[\\s\\S]*?-->|<!\\[CDATA\\[[\\s\\S]*?\\]\\]>|<\\?[\\s\\S]*?\\?>|<(?:[^<>\"']|\"[^\"]*\"|'[^']*')*>|[^<]+"
    )]
    private static partial Regex Tokens();

    [GeneratedRegex("\\s+(?<attribute>(?<name>[^\\s=]+)\\s*=\\s*(?:\"[^\"]*\"|'[^']*'))")]
    private static partial Regex Attributes();
}
