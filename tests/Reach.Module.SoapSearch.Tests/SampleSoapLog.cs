namespace Reach.Module.SoapSearch.Tests;

internal static class SampleSoapLog
{
    public const string Siid = "sample-service-call";
    public const string Request = """
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Header>
        <h:context xmlns:h='urn:example:headers' xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type='h:Context'><h:language>tr</h:language><h:correlation>sample-service-call</h:correlation></h:context>
          </soapenv:Header>
          <soapenv:Body>
        <p:SubmitOrder xmlns:p="urn:example:orders" xmlns:v='urn:example:values'><p:description>İıçöğüş ÇÖĞÜŞ &amp; &#x41;</p:description><p:amount currency = 'TRY'>10200.15</p:amount><v:optional /><p:note>first line
        second line</p:note></p:SubmitOrder>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    public const string Response = """
        <s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
          <s:Header />
          <s:Body><SubmitOrderResponse xmlns="urn:example:orders"><status code='accepted'>OK</status><message xmlns=''>Created &amp; queued</message></SubmitOrderResponse></s:Body>
        </s:Envelope>
        """;

    public static string Create() =>
        SoapLogExtractorTests.Event(Siid, "Servis request i aldı:", $"{Siid} {Request}")
        + SoapLogExtractorTests.Event(
            Siid,
            "Client request i göndermeden önce: Url:https://example.test/downstream",
            SoapLogExtractorTests.Envelope("downstream-request")
        )
        + SoapLogExtractorTests.Event(
            "other-call",
            "Servis request i aldı:",
            SoapLogExtractorTests.Envelope("unrelated-request")
        )
        + SoapLogExtractorTests.Event(
            Siid,
            "Client response aldı:",
            SoapLogExtractorTests.Envelope("downstream-response")
        )
        + SoapLogExtractorTests.Event(Siid, "Servis cevap verdi:", Response);

    public static string CreateTruncated() =>
        SoapLogExtractorTests.Event("other-call", "Processing:", "unrelated event")
        + SoapLogExtractorTests.Event(
            Siid,
            "Servis request i aldı:",
            "<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>\n<s:Body><unfinished>"
        )
        + SoapLogExtractorTests.Event("other-call", "Servis cevap verdi:", Response);
}
