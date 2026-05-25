using LDMS_Final.Data;
using LDMS_Final.Models;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;

namespace LDMS_Final.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public OrderService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.Orders
                .CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date);
            return $"ORD-{today}-{(count + 1):D4}";
        }

        public string GenerateQrCode(string orderNumber)
        {
            var folder = Path.Combine(_env.WebRootPath, "qrcodes");
            Directory.CreateDirectory(folder);

            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(orderNumber, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(10);

            var fileName = $"{orderNumber}.png";
            var path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, qrBytes);

            return $"/qrcodes/{fileName}";
        }

        public string GenerateShippingLabelPdf(Order order, decimal totalWeightKg = 0)
        {
            var folder = Path.Combine(_env.WebRootPath, "shipping-labels");
            Directory.CreateDirectory(folder);

            var fileName = $"label-{order.OrderNumber}.pdf";
            var filePath = Path.Combine(folder, fileName);

            // Delete old cached label to force regeneration
            if (File.Exists(filePath))
                File.Delete(filePath);

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);

            // A6-ish label size (148 x 180 mm)
            var pageSize = new iText.Kernel.Geom.PageSize(425, 520);
            using var doc = new Document(pdf, pageSize);
            doc.SetMargins(0, 0, 0, 0);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var outerBorder = new SolidBorder(ColorConstants.BLACK, 2);
            var innerBorder = new SolidBorder(ColorConstants.BLACK, 1);

            // ── Header: Logo ──────────────────────────────────────
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                .UseAllAvailableWidth()
                .SetBorder(outerBorder);

            var logoPath = Path.Combine(_env.WebRootPath, "images", "Logo IT15 with name.png");
            var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(10);

            if (File.Exists(logoPath))
            {
                var logoImg = new iText.Layout.Element.Image(ImageDataFactory.Create(logoPath))
                    .SetHeight(50)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                logoCell.Add(logoImg);
            }
            else
            {
                logoCell.Add(new Paragraph("Ld Gow").SetFont(bold).SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            headerTable.AddCell(logoCell);

            doc.Add(headerTable);

            // ── Barcode ───────────────────────────────────────────
            // Generate barcode as QR reuse (iText barcode1D)
            var barcodeTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 }))
                .UseAllAvailableWidth()
                .SetBorderLeft(outerBorder).SetBorderRight(outerBorder).SetBorderBottom(innerBorder).SetBorderTop(innerBorder);

            var barcode = new iText.Kernel.Pdf.Canvas.PdfCanvas(pdf.AddNewPage());
            // Use iText Barcode128
            var bc128 = new iText.Barcodes.Barcode128(pdf);
            bc128.SetCode(order.OrderNumber);
            bc128.SetBarHeight(60f);
            bc128.SetX(1.8f);

            var barcodeImage = new iText.Layout.Element.Image(bc128.CreateFormXObject(ColorConstants.BLACK, ColorConstants.BLACK, pdf))
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetWidth(340)
                .SetHeight(70);

            barcodeTable.AddCell(new Cell()
                .Add(barcodeImage)
                .SetPadding(12)
                .SetBorder(Border.NO_BORDER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER));

            doc.Add(barcodeTable);

            // ── Receiver + QR side by side ────────────────────────
            var midTable = new Table(UnitValue.CreatePercentArray(new float[] { 1.6f, 1f }))
                .UseAllAvailableWidth()
                .SetBorderLeft(outerBorder).SetBorderRight(outerBorder).SetBorderBottom(innerBorder).SetBorderTop(Border.NO_BORDER);

            // Left column: Receiver + Sender stacked
            var leftCell = new Cell()
                .SetBorderRight(innerBorder)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderBottom(Border.NO_BORDER)
                .SetBorderLeft(Border.NO_BORDER)
                .SetPadding(0);

            var receiverInner = new Table(UnitValue.CreatePercentArray(new float[] { 1 })).UseAllAvailableWidth();
            receiverInner.AddCell(new Cell()
                .Add(new Paragraph("Receiver:").SetFont(bold).SetFontSize(9).SetMarginBottom(2))
                .Add(new Paragraph(order.DeliveryFullName).SetFont(regular).SetFontSize(10))
                .Add(new Paragraph(order.DeliveryContactNumber).SetFont(regular).SetFontSize(10))
                .Add(new Paragraph(order.DeliveryAddress).SetFont(regular).SetFontSize(9))
                .SetPadding(10)
                .SetBorderBottom(innerBorder)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderLeft(Border.NO_BORDER)
                .SetBorderRight(Border.NO_BORDER));

            receiverInner.AddCell(new Cell()
                .Add(new Paragraph("Sender:").SetFont(bold).SetFontSize(9).SetMarginBottom(2))
                .Add(new Paragraph("Ld Gow").SetFont(regular).SetFontSize(10))
                .Add(new Paragraph("Davao City").SetFont(regular).SetFontSize(10))
                .SetPadding(10)
                .SetBorder(Border.NO_BORDER));

            leftCell.Add(receiverInner);
            midTable.AddCell(leftCell);

            // Right column: QR Code
            var qrCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetPadding(10);

            if (!string.IsNullOrEmpty(order.QrCodePath))
            {
                var qrPath = Path.Combine(_env.WebRootPath,
                order.QrCodePath.TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
                if (File.Exists(qrPath))
                {
                    var qrImg = new iText.Layout.Element.Image(ImageDataFactory.Create(qrPath))
                        .SetWidth(95).SetHeight(95)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    qrCell.Add(qrImg);
                }
            }

            midTable.AddCell(qrCell);
            doc.Add(midTable);

            // ── Bottom: details + payment method ─────────────────
            var bottomTable = new Table(UnitValue.CreatePercentArray(new float[] { 1.6f, 1f }))
                .UseAllAvailableWidth()
                .SetBorderLeft(outerBorder).SetBorderRight(outerBorder)
                .SetBorderBottom(outerBorder).SetBorderTop(Border.NO_BORDER);

            bottomTable.AddCell(new Cell()
                .Add(new Paragraph($"Weight: {totalWeightKg:N2} KG").SetFont(regular).SetFontSize(9))
                .Add(new Paragraph($"Order No.: {order.OrderNumber}").SetFont(regular).SetFontSize(9))
                .Add(new Paragraph($"Items: {order.Items.Sum(i => i.Quantity)}").SetFont(regular).SetFontSize(9))
                .Add(new Paragraph($"COD Amount: {order.TotalAmount:N2}").SetFont(regular).SetFontSize(9))
                .SetPadding(10)
                .SetBorderRight(innerBorder)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderLeft(Border.NO_BORDER)
                .SetBorderBottom(Border.NO_BORDER));

            bottomTable.AddCell(new Cell()
                .Add(new Paragraph(order.PaymentMethod)
                    .SetFont(bold).SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE))
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(10));

            doc.Add(bottomTable);

            return $"/shipping-labels/{fileName}";
        }
    }
}