using System.Xml;
using Reach.Module.SoapSearch.Extraction;

namespace Reach.Module.SoapSearch.Tests;

internal sealed class SoapEnvelopeFormatterTests
{
    private readonly ISoapEnvelopeFormatter _formatter = new SoapEnvelopeFormatter();

    [Test]
    public async Task UsesXmlToolsNamespaceLayoutWithoutRewritingTags()
    {
        var input =
            "<root xmlns='urn:root' xmlns:a='urn:a' xmlns:b=\"urn:b\"><a:item b:code = 'a&gt;b' /><item xmlns=\"\">&#65;&amp;</item></root>";
        var expected =
            "<root xmlns='urn:root'\n    xmlns:a='urn:a'\n    xmlns:b=\"urn:b\">\n    <a:item b:code = 'a&gt;b' />\n    <item xmlns=\"\">&#65;&amp;</item>\n</root>";
        await Assert.That(_formatter.Format(input)).IsEqualTo(expected);
        await Assert.That(_formatter.Format(expected)).IsEqualTo(expected);
    }

    [Test]
    public async Task PreservesMixedContentSubtreesVerbatim()
    {
        var mixed = "<p> leading <b attr='a > b'>bold</b>\r\n tail <i/> ! </p>";
        await Assert
            .That(_formatter.Format($"<root>{mixed}</root>"))
            .IsEqualTo($"<root>\r\n    {mixed}\r\n</root>");
    }

    [Test]
    public async Task PreservesXmlSpaceSubtreeAndWhitespaceOnlyValues()
    {
        var preserved =
            "<p xml:space='preserve'>  <b>\r\n  </b> <c xml:space='default'><d/></c> </p>";
        var input = $"<root>{preserved}<blank> \t </blank></root>";
        var output = _formatter.Format(input);
        await Assert.That(output.Contains(preserved, StringComparison.Ordinal)).IsTrue();
        await Assert
            .That(output.Contains("<blank> \t </blank>", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task PreservesCdataCommentsProcessingInstructionsAndEntities()
    {
        var input =
            "<root><data><![CDATA[<a>\r\n  x & y</a>]]></data><!-- < > keep  spaces --><?test a='>'?><value>  &#x41; &amp; ü  </value></root>";
        var output = _formatter.Format(input);
        await Assert
            .That(output.Contains("<![CDATA[<a>\r\n  x & y</a>]]>", StringComparison.Ordinal))
            .IsTrue();
        await Assert
            .That(output.Contains("<!-- < > keep  spaces -->", StringComparison.Ordinal))
            .IsTrue();
        await Assert.That(output.Contains("<?test a='>'?>", StringComparison.Ordinal)).IsTrue();
        await Assert
            .That(output.Contains("<value>  &#x41; &amp; ü  </value>", StringComparison.Ordinal))
            .IsTrue();
    }

    [Test]
    public async Task RejectsMalformedXmlInsteadOfRepairingIt()
    {
        await Assert.That(() => _formatter.Format("<root><missing></root>")).Throws<XmlException>();
    }
}
