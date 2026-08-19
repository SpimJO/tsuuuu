using Net.Codecrete.QrCodeGenerator;

namespace TsuOrg.Frontend.Services;

public static class DocumentQr
{
    public static string Payload(string documentNumber, string? title = null, string? organization = null)
    {
        var lines = new List<string> { "TSU-ORGDOCX", documentNumber };
        if (!string.IsNullOrWhiteSpace(title))
            lines.Add(title.Trim());
        if (!string.IsNullOrWhiteSpace(organization))
            lines.Add(organization.Trim());
        return string.Join('\n', lines);
    }

    public static string Svg(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return "";

        var qr = QrCode.EncodeText(payload, QrCode.Ecc.Medium);
        return qr.ToSvgString(2);
    }
}
