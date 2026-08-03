using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using StockSense.Domain.Entities;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace StockSense.Infrastructure.Services;

public class BarcodeService
{
    // "20"-"29" is the GS1-reserved prefix range for internal/in-store use only barcodes
    // (never assigned to real retail products), which is exactly what StockSense needs here.
    private const string InternalPrefix = "20";

    /// <summary>
    /// Deterministically builds a valid, unique EAN-13 barcode value from a product's own Id.
    /// Because it's derived directly from the (already-unique) primary key, there is no
    /// collision risk and no need to query the database to check uniqueness.
    /// </summary>
    public static string GenerateBarcodeValue(int productId)
    {
        // Prefix (2 digits) + zero-padded product id (10 digits) = 12 data digits.
        var data = InternalPrefix + productId.ToString().PadLeft(10, '0');
        var checkDigit = ComputeEan13CheckDigit(data);
        return data + checkDigit;
    }

    private static int ComputeEan13CheckDigit(string twelveDigits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = twelveDigits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        return (10 - (sum % 10)) % 10;
    }

    /// <summary>Renders the barcode as a PNG image (bytes) for embedding in a PDF or the UI.</summary>
    public byte[] GenerateBarcodeImage(string barcodeValue)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.EAN_13,
            Options = new EncodingOptions
            {
                Width = 320,
                Height = 140,
                Margin = 10,
                PureBarcode = false
            }
        };

        using SKBitmap bitmap = writer.Write(barcodeValue);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Renders a QR code as a PNG image (bytes) — encodes the product's URL or barcode as fallback content.</summary>
    public byte[] GenerateQrCodeImage(Product product)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = 200,
                Height = 200,
                Margin = 10
            }
        };

        using SKBitmap bitmap = writer.Write(product.Barcode ?? $"Sap Shop-{product.Id}");
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Builds a printable one-page PDF label for a product with barcode and/or QR code.</summary>
    public byte[] GenerateBarcodeLabelPdf(Product product, byte[] barcodeImagePng, byte[] qrCodeImagePng, string format = "both")
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6.Landscape());
                page.Margin(0.4f, Unit.Inch);

                page.Header().Column(col =>
                {
                    col.Item().Text("Sap Shop").FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().Text(product.Name).FontSize(16).SemiBold();
                    col.Item().Text($"{product.Category} · {product.Brand}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content()
                    .PaddingTop(10)
                    .Row(row =>
                    {
                        if (format == "barcode" || format == "both")
                        {
                            row.RelativeItem(3).AlignCenter().AlignMiddle().Image(barcodeImagePng);
                        }
                        if (format == "qr" || format == "both")
                        {
                            row.RelativeItem(format == "both" ? 2 : 1).AlignCenter().AlignMiddle().Image(qrCodeImagePng);
                        }
                    });
            });
        }).GeneratePdf();
    }
}
