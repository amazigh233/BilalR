using Booking.TConnectConnector;

namespace Booking.TConnectConnector.Tests;

public sealed class TConnectXmlParserTests
{
    [Fact]
    public void Parse_ReadsCanonicalTConnectOrder()
    {
        const string xml = """
            <Order>
              <OrderId>TB-2001</OrderId>
              <CreatedAt>2026-06-08T18:30:00Z</CreatedAt>
              <Status>Confirmed</Status>
              <Customer>
                <Name>Jan Jansen</Name>
                <Phone>0612345678</Phone>
              </Customer>
              <DeliveryAddress>
                <Street>Damrak</Street>
                <HouseNumber>1</HouseNumber>
                <PostalCode>1012LG</PostalCode>
                <City>Amsterdam</City>
              </DeliveryAddress>
              <Total currency="EUR">24.50</Total>
              <Items>
                <Item>
                  <Name>Pizza Margherita</Name>
                  <Quantity>2</Quantity>
                  <UnitPrice>12.25</UnitPrice>
                </Item>
              </Items>
            </Order>
            """;

        var order = new TConnectXmlParser().Parse(xml);

        Assert.Equal("TB-2001", order.ExternalOrderId);
        Assert.Equal("Jan Jansen", order.CustomerName);
        Assert.Equal("Damrak 1, 1012LG Amsterdam", order.DeliveryAddress);
        Assert.Equal(24.50m, order.TotalAmount);
        Assert.Equal("EUR", order.Currency);
        var item = Assert.Single(order.Items);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void Parse_RejectsXmlWithoutOrderNumber()
    {
        var parser = new TConnectXmlParser();

        Assert.Throws<FormatException>(() => parser.Parse("<Order><CustomerName>Jan</CustomerName></Order>"));
    }
}
