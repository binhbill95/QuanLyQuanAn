using System.Globalization;
using System.Text;
using QRCoder;
using RestaurantManagement.Application.Models.Payments;

namespace RestaurantManagement.Web.Services;

public static class VietQrCodeGenerator
{
    private const string NapasApplicationId = "A000000727";

    public static byte[] CreatePng(
        BankTransferSettingsDto bank,
        decimal amount,
        string transferReference)
    {
        var merchantAccountInfo = Tlv("00", NapasApplicationId) +
                                 Tlv("01", bank.BankCode) +
                                 Tlv("02", bank.AccountNumber) +
                                 Tlv("03", "QRIBFTTA");
        var additionalData = Tlv("08", NormalizeReference(transferReference));
        var payload = Tlv("00", "01") +
                      Tlv("01", "12") +
                      Tlv("38", merchantAccountInfo) +
                      Tlv("52", "0000") +
                      Tlv("53", "704") +
                      Tlv("54", amount.ToString("0.##", CultureInfo.InvariantCulture)) +
                      Tlv("58", "VN") +
                      Tlv("59", NormalizeAccountName(bank.AccountName)) +
                      Tlv("62", additionalData);
        payload += Tlv("63", ComputeCrc16(payload + "6304"));

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8);
    }

    private static string Tlv(string tag, string value)
    {
        var byteLength = Encoding.UTF8.GetByteCount(value);
        if (byteLength > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "VietQR field exceeds the EMV length limit.");
        }

        return string.Concat(tag, byteLength.ToString("D2", CultureInfo.InvariantCulture), value);
    }

    private static string NormalizeAccountName(string value)
    {
        var decomposed = value
            .Replace('Đ', 'D')
            .Replace('đ', 'D')
            .Normalize(NormalizationForm.FormD);
        var normalized = new string(decomposed
            .Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant();

        return new string(normalized
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == ' ')
            .ToArray())
            .Trim();
    }

    private static string NormalizeReference(string value)
    {
        var normalized = new string(value
            .ToUpperInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .Take(25)
            .ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? "THANHTOAN" : normalized;
    }

    private static string ComputeCrc16(string value)
    {
        ushort crc = 0xFFFF;
        foreach (var valueByte in Encoding.UTF8.GetBytes(value))
        {
            crc ^= (ushort)(valueByte << 8);
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
