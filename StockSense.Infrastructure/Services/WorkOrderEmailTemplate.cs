using System.Net;
using System.Text;

namespace StockSense.Infrastructure.Services;

public static class WorkOrderEmailTemplate
{
    public static string BuildConfirmed(string customerName, string workOrderType, int workOrderId, string summary, string baseUrl)
    {
        var safeName = WebUtility.HtmlEncode(customerName);
        var safeSummary = WebUtility.HtmlEncode(summary);
        var historyUrl = workOrderType == "Appointment"
            ? $"{baseUrl.TrimEnd('/')}/my-bookings"
            : $"{baseUrl.TrimEnd('/')}/my-builds";
        var label = workOrderType == "Appointment" ? "appointment" : "build";

        return Wrap($"Your {label} has been confirmed",
            $"Hi {safeName},<br><br>Your {label} has been confirmed and is ready to proceed.",
            summaryBlock(label, workOrderId, safeSummary),
            historyUrl,
            $"View {label}");
    }

    public static string BuildCompleted(string customerName, string workOrderType, int workOrderId, string summary, string baseUrl)
    {
        var safeName = WebUtility.HtmlEncode(customerName);
        var safeSummary = WebUtility.HtmlEncode(summary);
        var historyUrl = workOrderType == "Appointment"
            ? $"{baseUrl.TrimEnd('/')}/my-bookings"
            : $"{baseUrl.TrimEnd('/')}/my-builds";
        var label = workOrderType == "Appointment" ? "appointment" : "build";

        return Wrap($"Your {label} is complete",
            $"Hi {safeName},<br><br>Your {label} has been completed. Below is your transaction summary.",
            summaryBlock(label, workOrderId, safeSummary),
            historyUrl,
            $"View {label}");
    }

    public static string BuildRescheduled(string customerName, int appointmentId, string oldDate, string newDate, string mechanic, string baseUrl)
    {
        var safeName = WebUtility.HtmlEncode(customerName);
        var safeOld = WebUtility.HtmlEncode(oldDate);
        var safeNew = WebUtility.HtmlEncode(newDate);
        var safeMechanic = WebUtility.HtmlEncode(mechanic);
        var historyUrl = $"{baseUrl.TrimEnd('/')}/my-bookings";

        var sb = new StringBuilder();
        sb.Append($@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:100%;background-color:#fef2f2;border:1px solid #fecaca;border-radius:8px;"">");
        sb.Append($@"<tr><td style=""padding:18px 20px;"">");
        sb.Append($@"<p style=""margin:0 0 5px;color:#991b1b;font-size:11px;font-weight:700;letter-spacing:1px;text-transform:uppercase;"">Appointment #{appointmentId}</p>");
        sb.Append($@"<p style=""margin:0;color:#18181b;font-size:14px;line-height:1.6;""><strong>Previous:</strong> {safeOld}</p>");
        sb.Append($@"<p style=""margin:4px 0 0;color:#18181b;font-size:14px;line-height:1.6;""><strong>New:</strong> {safeNew}</p>");
        if (!string.Equals(mechanic, "Any Available", StringComparison.OrdinalIgnoreCase))
            sb.Append($@"<p style=""margin:4px 0 0;color:#18181b;font-size:14px;line-height:1.6;""><strong>Mechanic:</strong> {safeMechanic}</p>");
        sb.Append(@"</td></tr></table>");

        return Wrap("Your appointment has been rescheduled",
            $"Hi {safeName},<br><br>Your appointment schedule has been updated. Please review the changes below.",
            sb.ToString(),
            historyUrl,
            "View appointment");
    }

    private static string summaryBlock(string label, int id, string safeSummary)
    {
        var sb = new StringBuilder();
        sb.Append($@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:100%;background-color:#fef2f2;border:1px solid #fecaca;border-radius:8px;"">");
        sb.Append($@"<tr><td style=""padding:18px 20px;"">");
        sb.Append($@"<p style=""margin:0 0 5px;color:#991b1b;font-size:11px;font-weight:700;letter-spacing:1px;text-transform:uppercase;"">{WebUtility.HtmlEncode(label)} #{id}</p>");
        sb.Append($@"<p style=""margin:0;color:#18181b;font-size:14px;line-height:1.6;"">{safeSummary}</p>");
        sb.Append(@"</td></tr></table>");
        return sb.ToString();
    }

    private static string Wrap(string heading, string bodyHtml, string detailBlock, string buttonUrl, string buttonText)
    {
        var safeHeading = WebUtility.HtmlEncode(heading);
        var safeButtonText = WebUtility.HtmlEncode(buttonText);

        return $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#18181b;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background-color:#f4f4f5;">
                    <tr>
                        <td align="center" style="padding:40px 16px;">
                            <table role="presentation" width="520" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:520px;background-color:#ffffff;border:1px solid #e4e4e7;border-radius:8px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);">
                                <tr>
                                    <td style="padding:32px;">
                                        <h1 style="margin:0;text-align:center;color:#09090b;font-size:24px;font-weight:600;">{{safeHeading}}</h1>
                                        <h2 style="margin:8px 0 0;text-align:center;color:#71717a;font-size:16px;font-weight:500;">Sap Shop</h2>

                                        <div style="border-top:1px solid #e4e4e7;margin:24px 0;"></div>

                                        <p style="text-align:center;font-size:14px;color:#71717a;margin-bottom:24px;line-height:1.6;">
                                            {{bodyHtml}}
                                        </p>

                                        {{detailBlock}}

                                        <div style="text-align:center;margin:24px 0;">
                                            <a href="{{buttonUrl}}"
                                               style="display:inline-block;padding:12px 32px;border-radius:6px;background-color:#dc2626;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;">
                                                {{safeButtonText}}
                                            </a>
                                        </div>

                                        <p style="font-size:14px;color:#52525b;margin-top:24px;line-height:1.6;">
                                            Regards,<br>
                                            <strong style="color:#18181b;">Sap Shop (Motor Parts &amp; Accessories)</strong>
                                        </p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:17px 32px;background-color:#fafafa;border-top:1px solid #e4e4e7;text-align:center;color:#a1a1aa;font-size:11px;line-height:1.5;">
                                        This notification was generated by SAP SHOP inventory management.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
