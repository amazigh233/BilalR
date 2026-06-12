using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Booking.TConnectConnector;

public sealed class TConnectXmlParser(TimeProvider? timeProvider = null)
{
    private static readonly string[] OrderIdNames = ["OrderId", "OrderID", "OrderNumber", "OrderNo", "Number"];
    private static readonly string[] CustomerContainerNames = ["Customer", "Consumer", "Client"];
    private static readonly string[] CustomerNameNames = ["CustomerName", "FullName", "Name"];
    private static readonly string[] PhoneNames = ["CustomerPhone", "PhoneNumber", "Phone", "Telephone", "Tel"];
    private static readonly string[] AddressContainerNames = ["DeliveryAddress", "Address", "CustomerAddress"];
    private static readonly string[] NoteNames = ["Note", "Notes", "Comment", "Remarks", "Remark"];
    private static readonly string[] StatusNames = ["Status", "OrderStatus"];
    private static readonly string[] DateNames = ["PlacedAtUtc", "PlacedAt", "CreatedAt", "OrderDateTime", "OrderDate", "DateTime"];
    private static readonly string[] TotalNames = ["TotalAmount", "GrandTotal", "OrderTotal", "Total"];
    private static readonly string[] LineNames = ["Item", "OrderLine", "OrderItem", "Product", "Article", "Dish"];
    private static readonly string[] LineNameNames = ["ProductName", "ItemName", "ArticleName", "Description", "Name"];
    private static readonly string[] QuantityNames = ["Quantity", "Count", "Amount", "Qty"];
    private static readonly string[] PriceNames = ["UnitPrice", "Price", "ItemPrice"];
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public ConnectorOrder Parse(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        using var textReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        var document = XDocument.Load(xmlReader);
        var root = document.Root ?? throw new FormatException("Het XML-bestand bevat geen root-element.");

        var orderId = FindValue(root, OrderIdNames)
            ?? throw new FormatException("Geen ordernummer gevonden in het T-Connect XML-bestand.");

        var customerContainer = FindElement(root, CustomerContainerNames);
        var customerName = FindValue(customerContainer, CustomerNameNames)
            ?? FindValue(root, ["CustomerName"])
            ?? "Onbekende klant";
        var phone = FindValue(customerContainer, PhoneNames) ?? FindValue(root, PhoneNames);
        var address = ReadAddress(root);
        var note = FindValue(root, NoteNames);
        var status = FindValue(root, StatusNames) ?? "New";
        var placedAtUtc = ReadDate(root);
        var totalElement = FindElement(root, TotalNames);
        var totalAmount = ReadDecimal(totalElement?.Value) ?? 0m;
        var currency = FindAttribute(totalElement, "currency", "currencyCode")
            ?? FindValue(root, ["Currency", "CurrencyCode"])
            ?? "EUR";
        var lines = ReadLines(root);

        return new ConnectorOrder(
            orderId,
            customerName,
            phone,
            address,
            note,
            status,
            placedAtUtc,
            totalAmount,
            currency,
            lines);
    }

    private DateTime ReadDate(XElement root)
    {
        var value = FindValue(root, DateNames);
        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var invariantDate))
        {
            return invariantDate.UtcDateTime;
        }

        if (DateTimeOffset.TryParse(value, out var currentDate))
        {
            return currentDate.UtcDateTime;
        }

        return _timeProvider.GetUtcNow().UtcDateTime;
    }

    private static IReadOnlyCollection<ConnectorOrderLine> ReadLines(XElement root)
    {
        var candidates = root
            .Descendants()
            .Where(element => HasName(element, LineNames))
            .Where(element => !element.Ancestors().Any(ancestor => HasName(ancestor, LineNames)))
            .Where(element => FindValue(element, LineNameNames) is not null)
            .ToList();

        return candidates
            .Select(element =>
            {
                var name = FindValue(element, LineNameNames)!;
                var quantity = ReadInteger(FindValue(element, QuantityNames)) ?? 1;
                var price = ReadDecimal(FindValue(element, PriceNames)) ?? 0m;
                return new ConnectorOrderLine(name, Math.Max(1, quantity), Math.Max(0m, price));
            })
            .ToList();
    }

    private static string? ReadAddress(XElement root)
    {
        var address = FindElement(root, AddressContainerNames);
        if (address is null)
        {
            return FindValue(root, ["DeliveryAddress"]);
        }

        if (!address.HasElements)
        {
            return Clean(address.Value);
        }

        var street = FindValue(address, ["Street", "StreetName", "AddressLine1"]);
        var houseNumber = FindValue(address, ["HouseNumber", "StreetNumber", "Number"]);
        var postalCode = FindValue(address, ["PostalCode", "Postcode", "ZipCode", "Zip"]);
        var city = FindValue(address, ["City", "Town"]);

        var streetLine = string.Join(
            " ",
            new[] { street, houseNumber }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var cityLine = string.Join(
            " ",
            new[] { postalCode, city }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.Join(
            ", ",
            new[] { streetLine, cityLine }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static XElement? FindElement(XElement? container, IEnumerable<string> names)
    {
        if (container is null)
        {
            return null;
        }

        return container
            .DescendantsAndSelf()
            .FirstOrDefault(element => HasName(element, names));
    }

    private static string? FindValue(XElement? container, IEnumerable<string> names)
    {
        return Clean(FindElement(container, names)?.Value);
    }

    private static string? FindAttribute(XElement? element, params string[] names)
    {
        return element?.Attributes()
            .FirstOrDefault(attribute => names.Contains(
                attribute.Name.LocalName,
                StringComparer.OrdinalIgnoreCase))
            ?.Value;
    }

    private static bool HasName(XElement element, IEnumerable<string> names)
    {
        return names.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal? ReadDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("nl-NL"), out var dutch)
                ? dutch
                : null;
    }

    private static int? ReadInteger(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
