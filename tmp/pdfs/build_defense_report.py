from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import LETTER
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    PageTemplate,
    PageBreak,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
    KeepTogether,
    HRFlowable,
    Flowable,
)


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "output" / "pdf" / "StockSense_Safety_Stock_Defense_Report.pdf"

PAGE_W, PAGE_H = LETTER
MARGIN_X = 0.70 * inch
MARGIN_TOP = 0.72 * inch
MARGIN_BOTTOM = 0.62 * inch
CONTENT_W = PAGE_W - 2 * MARGIN_X

NAVY = colors.HexColor("#102A43")
BLUE = colors.HexColor("#2563EB")
TEAL = colors.HexColor("#0F766E")
AMBER = colors.HexColor("#B45309")
RED = colors.HexColor("#B91C1C")
INK = colors.HexColor("#172033")
MUTED = colors.HexColor("#536273")
LINE = colors.HexColor("#D8E1EA")
PALE_BLUE = colors.HexColor("#EAF2FF")
PALE_TEAL = colors.HexColor("#E8F7F4")
PALE_AMBER = colors.HexColor("#FFF5E6")
PALE_RED = colors.HexColor("#FDECEC")
PALE_GRAY = colors.HexColor("#F4F7FA")
WHITE = colors.white


styles = getSampleStyleSheet()
styles.add(ParagraphStyle(
    name="CoverTitle", parent=styles["Title"], fontName="Helvetica-Bold",
    fontSize=29, leading=33, textColor=WHITE, alignment=TA_LEFT, spaceAfter=16,
))
styles.add(ParagraphStyle(
    name="CoverSub", parent=styles["Normal"], fontName="Helvetica",
    fontSize=13, leading=19, textColor=colors.HexColor("#DDEBFF"), spaceAfter=10,
))
styles.add(ParagraphStyle(
    name="H1x", parent=styles["Heading1"], fontName="Helvetica-Bold",
    fontSize=20, leading=24, textColor=NAVY, spaceBefore=0, spaceAfter=11,
))
styles.add(ParagraphStyle(
    name="H2x", parent=styles["Heading2"], fontName="Helvetica-Bold",
    fontSize=12.5, leading=16, textColor=BLUE, spaceBefore=9, spaceAfter=5,
))
styles.add(ParagraphStyle(
    name="Bodyx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=9.4, leading=13.4, textColor=INK, spaceAfter=6,
))
styles.add(ParagraphStyle(
    name="Smallx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=7.8, leading=10.5, textColor=MUTED, spaceAfter=4,
))
styles.add(ParagraphStyle(
    name="Bulletx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=9.1, leading=12.6, textColor=INK, leftIndent=12, firstLineIndent=-8,
    bulletIndent=2, spaceAfter=3,
))
styles.add(ParagraphStyle(
    name="Callout", parent=styles["BodyText"], fontName="Helvetica-Bold",
    fontSize=10.4, leading=14.5, textColor=NAVY, spaceAfter=0,
))
styles.add(ParagraphStyle(
    name="Formula", parent=styles["BodyText"], fontName="Courier-Bold",
    fontSize=8.6, leading=12, textColor=NAVY, leftIndent=8, spaceAfter=2,
))
styles.add(ParagraphStyle(
    name="TableHead", parent=styles["BodyText"], fontName="Helvetica-Bold",
    fontSize=8.0, leading=10.3, textColor=WHITE,
))
styles.add(ParagraphStyle(
    name="TableCell", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=7.7, leading=10.2, textColor=INK,
))
styles.add(ParagraphStyle(
    name="Quote", parent=styles["BodyText"], fontName="Helvetica-Oblique",
    fontSize=10.2, leading=15, textColor=NAVY, leftIndent=14, rightIndent=14,
    spaceAfter=8,
))


def p(text, style="Bodyx"):
    return Paragraph(text, styles[style])


def bullet(text):
    return Paragraph(f"- {text}", styles["Bulletx"])


def callout(title, body, bg=PALE_BLUE, border=BLUE):
    data = [[p(title, "Callout")], [p(body, "Bodyx")]]
    table = Table(data, colWidths=[CONTENT_W - 18], hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), bg),
        ("BOX", (0, 0), (-1, -1), 0.8, border),
        ("LINEBEFORE", (0, 0), (0, -1), 4, border),
        ("LEFTPADDING", (0, 0), (-1, -1), 10),
        ("RIGHTPADDING", (0, 0), (-1, -1), 10),
        ("TOPPADDING", (0, 0), (-1, 0), 8),
        ("BOTTOMPADDING", (0, -1), (-1, -1), 8),
    ]))
    return KeepTogether([table, Spacer(1, 8)])


def data_table(headers, rows, widths, font_size=7.7):
    header_cells = [p(h, "TableHead") for h in headers]
    cell_style = ParagraphStyle(
        "DynamicCell", parent=styles["TableCell"], fontSize=font_size,
        leading=font_size + 2.3,
    )
    body = [[Paragraph(str(value), cell_style) for value in row] for row in rows]
    table = Table([header_cells] + body, colWidths=widths, repeatRows=1, hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), NAVY),
        ("GRID", (0, 0), (-1, -1), 0.35, LINE),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [WHITE, PALE_GRAY]),
    ]))
    return table


class ArchitectureFlow(Flowable):
    def __init__(self, width, height=220):
        super().__init__()
        self.width = width
        self.height = height

    def draw_box(self, canvas, x, y, w, h, title, lines, fill, stroke):
        canvas.setFillColor(fill)
        canvas.setStrokeColor(stroke)
        canvas.setLineWidth(0.8)
        canvas.roundRect(x, y, w, h, 7, fill=1, stroke=1)
        canvas.setFillColor(stroke)
        canvas.setFont("Helvetica-Bold", 8.5)
        canvas.drawString(x + 8, y + h - 15, title)
        canvas.setFillColor(INK)
        canvas.setFont("Helvetica", 7.2)
        for idx, line in enumerate(lines):
            canvas.drawString(x + 8, y + h - 30 - idx * 10, line)

    def arrow(self, canvas, x1, y1, x2, y2):
        canvas.setStrokeColor(MUTED)
        canvas.setFillColor(MUTED)
        canvas.setLineWidth(1.2)
        canvas.line(x1, y1, x2, y2)
        canvas.line(x2, y2, x2 - 6, y2 + 3)
        canvas.line(x2, y2, x2 - 6, y2 - 3)

    def draw(self):
        c = self.canv
        left_w = 125
        center_w = 155
        right_w = 150
        gap = 28
        x1 = 0
        x2 = x1 + left_w + gap
        x3 = x2 + center_w + gap
        self.draw_box(c, x1, 145, left_w, 60, "DEMAND INPUT", ["Sales quantity", "Lost sales", "Zero-demand days"], PALE_BLUE, BLUE)
        self.draw_box(c, x1, 68, left_w, 60, "SUPPLY INPUT", ["Completed orders", "Ordered/completed dates", "Supplier lead time"], PALE_TEAL, TEAL)
        self.draw_box(c, x2, 95, center_w, 90, "POLICY ENGINE", ["Validate settings", "Select stage", "Compute safety / ROP / target", "Apply limits and rounding"], PALE_AMBER, AMBER)
        self.draw_box(c, x3, 145, right_w, 60, "DASHBOARD", ["Metrics and confidence", "Filters and explanations", "Admin settings"], PALE_BLUE, BLUE)
        self.draw_box(c, x3, 68, right_w, 60, "ORDER WORKFLOW", ["Inventory position", "Suggested quantity", "Supplier-grouped drafts"], PALE_TEAL, TEAL)
        self.arrow(c, x1 + left_w, 175, x2, 150)
        self.arrow(c, x1 + left_w, 98, x2, 125)
        self.arrow(c, x2 + center_w, 145, x3, 175)
        self.arrow(c, x2 + center_w, 120, x3, 98)
        c.setFont("Helvetica", 7)
        c.setFillColor(MUTED)
        c.drawCentredString(self.width / 2, 28, "Persisted with EF Core and SQL Server under transactional and row-version controls")


def header_footer(canvas, doc):
    page = canvas.getPageNumber()
    if page == 1:
        return
    canvas.saveState()
    canvas.setStrokeColor(LINE)
    canvas.setLineWidth(0.5)
    canvas.line(MARGIN_X, PAGE_H - 0.48 * inch, PAGE_W - MARGIN_X, PAGE_H - 0.48 * inch)
    canvas.setFont("Helvetica-Bold", 7.5)
    canvas.setFillColor(NAVY)
    canvas.drawString(MARGIN_X, PAGE_H - 0.36 * inch, "STOCKSENSE SAFETY STOCK - DEFENSE REPORT")
    canvas.setFont("Helvetica", 7.5)
    canvas.setFillColor(MUTED)
    canvas.drawRightString(PAGE_W - MARGIN_X, 0.34 * inch, f"Page {page}")
    canvas.drawString(MARGIN_X, 0.34 * inch, "Implementation-based report | 26 July 2026")
    canvas.restoreState()


class DefenseDocTemplate(BaseDocTemplate):
    def __init__(self, filename):
        super().__init__(
            filename,
            pagesize=LETTER,
            leftMargin=MARGIN_X,
            rightMargin=MARGIN_X,
            topMargin=MARGIN_TOP,
            bottomMargin=MARGIN_BOTTOM,
            title="StockSense Safety Stock Defense Report",
            author="StockSense Project Team",
            subject="Safety stock methodology, cold-start policy, implementation, and panel defense guide",
        )
        frame = Frame(
            self.leftMargin,
            self.bottomMargin,
            self.width,
            self.height,
            id="main",
            leftPadding=0,
            rightPadding=0,
            topPadding=0,
            bottomPadding=0,
        )
        self.addPageTemplates(PageTemplate(id="normal", frames=[frame], onPage=header_footer))


story = []

# Cover
cover = Table([[""]], colWidths=[PAGE_W], rowHeights=[PAGE_H], hAlign="LEFT")
cover.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), NAVY)]))
# The cover is built with a full-page colored table and overlaid content in the frame.
story.append(Spacer(1, 0.85 * inch))
cover_panel = Table([
    [p("STOCKSENSE", "CoverSub")],
    [p("Safety Stock and Cold-Start Inventory Policy", "CoverTitle")],
    [p("Technical explanation, paper-ready methodology, and panel defense guide", "CoverSub")],
], colWidths=[CONTENT_W - 34])
cover_panel.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, -1), NAVY),
    ("BOX", (0, 0), (-1, -1), 0, NAVY),
    ("LEFTPADDING", (0, 0), (-1, -1), 18),
    ("RIGHTPADDING", (0, 0), (-1, -1), 18),
    ("TOPPADDING", (0, 0), (-1, 0), 12),
    ("BOTTOMPADDING", (0, -1), (-1, -1), 18),
]))
story.append(cover_panel)
story.append(Spacer(1, 0.35 * inch))
story.append(callout(
    "CORE DEFENSE POSITION",
    "StockSense does not apply one formula to every product. It uses a transparent staged policy that matches the amount and quality of available data, then converts the result into controlled, auditable order recommendations.",
    bg=colors.HexColor("#DCEAFF"), border=BLUE,
))
story.append(Spacer(1, 1.25 * inch))
story.append(p("Prepared for manuscript development, system demonstration, and oral defense", "CoverSub"))
story.append(p("Implementation reviewed: 26 July 2026 | Calculation version 1.0 | Location: MAIN", "CoverSub"))
story.append(PageBreak())

# 1 Executive summary
story.append(p("1. Executive Summary", "H1x"))
story.append(p(
    "The StockSense Safety Stock module is a deterministic inventory-policy engine for deciding when a product should be reordered and how much should be ordered. It combines point-of-sale demand, recorded lost sales, supplier lead-time history, administrator settings, current stock, and incoming stock. The output is not only a safety-stock number: it includes average daily demand, demand variability, lead-time statistics, a reorder point, a target stock level, a calculation stage, a confidence label, and a human-readable reason.",
))
story.append(callout(
    "ONE-SENTENCE DEFENSE ANSWER",
    "The system reduces stockout and overstock risk by adapting the inventory formula to data maturity: expert estimates protect new products, blended estimates stabilize learning products, and statistical variability drives mature products.",
))
story.append(p("The module answers three different operational questions:", "H2x"))
story.extend([
    bullet("Safety stock: How much protective inventory should be kept for uncertainty?"),
    bullet("Reorder point: At what inventory position should replenishment be triggered?"),
    bullet("Target stock: After ordering, what inventory level should the system aim to restore?"),
])
story.append(p("What makes the implementation defensible", "H2x"))
story.extend([
    bullet("Cold-start is explicitly labeled Low confidence instead of presenting an estimate as a precise forecast."),
    bullet("Calendar days without a sale are inserted as zero demand, preventing sales-only averages from overstating demand."),
    bullet("Lost sales are added to fulfilled sales so stockouts do not hide unmet demand."),
    bullet("Observed supplier variability is used only after at least five completed orders; otherwise the configured lead time remains the fallback."),
    bullet("Every recommendation respects minimum order quantity, package size, maximum stock, supplier assignment, and open-order checks."),
    bullet("The calculation is reproducible, validated, unit-tested, transaction-protected, and explainable."),
])
story.append(p("Scope statement", "H2x"))
story.append(p(
    "The current implementation is an inventory policy and decision-support module, not a machine-learning demand forecast. Its strength is transparent, auditable logic suitable for operational use and academic evaluation. Advanced seasonal and intermittent-demand forecasting are appropriate future extensions, not claims of the present version.",
))
story.append(PageBreak())

# 2 Problem and objectives
story.append(p("2. Problem, Objectives, and Research Framing", "H1x"))
story.append(p("Operational problem", "H2x"))
story.append(p(
    "A fixed reorder threshold treats all products as if they have identical demand, supplier reliability, and history. This can create two opposing costs: stockouts when the threshold is too low, and excess holding cost when it is too high. The problem is harder for new products because there may be too little history to estimate variability reliably.",
))
story.append(p("System objective", "H2x"))
story.append(p(
    "To compute an explainable product-level inventory policy that remains usable during cold-start, gradually incorporates observed demand, uses supplier lead-time variability when sufficient evidence exists, and converts policy outputs into constrained replenishment recommendations.",
))
story.append(p("Suggested research questions", "H2x"))
story.extend([
    bullet("How can a retail inventory system calculate safety stock when product demand history is incomplete?"),
    bullet("How can demand and supplier variability be incorporated without making early estimates unstable?"),
    bullet("How can calculated thresholds be translated into practical order quantities while preventing duplicate or excessive orders?"),
    bullet("How can the result remain interpretable to administrators and auditable during evaluation?"),
])
story.append(p("Suggested specific objectives", "H2x"))
story.extend([
    bullet("Record daily fulfilled and lost demand per product."),
    bullet("Classify products into ColdStart, Learning, DataDriven, or Manual calculation stages."),
    bullet("Calculate safety stock, reorder point, and target stock with configurable service and policy limits."),
    bullet("Generate supplier-grouped draft order slips only when validated replenishment conditions are met."),
    bullet("Present calculation evidence, confidence, warnings, and administrative controls through the dashboard."),
])
story.append(callout(
    "IMPORTANT CLAIM BOUNDARY",
    "The 30-day and 90-day thresholds and the 50/50 and 70/30 blend weights are system design parameters. Present them as an implemented staged policy chosen for stability and interpretability, not as universal laws.",
    bg=PALE_AMBER, border=AMBER,
))
story.append(PageBreak())

# 3 Architecture
story.append(p("3. How the Module Works End to End", "H1x"))
story.append(ArchitectureFlow(CONTENT_W))
story.append(Spacer(1, 4))
story.append(p("Processing sequence", "H2x"))
sequence_rows = [
    ("1", "Load policy", "Read the product settings for location MAIN; create defaults if settings do not yet exist."),
    ("2", "Build demand", "Aggregate Sale transaction items by product and date using quantity + lost-sales quantity."),
    ("3", "Complete calendar", "Insert zero demand for every missing calendar date from tracking start through calculation date."),
    ("4", "Build lead times", "Measure positive day differences between OrderedAt and CompletedAt for completed supplier orders."),
    ("5", "Select stage", "Choose ColdStart, Learning, DataDriven, or Manual from usable calendar days and mode."),
    ("6", "Calculate policy", "Compute safety stock, reorder point, target stock, confidence, and explanation; apply limits."),
    ("7", "Persist results", "Save metrics, set Product.ReorderTarget to the calculated reorder point, and record calculation version 1.0."),
    ("8", "Recommend orders", "Compare current + incoming stock with the reorder point and target; apply purchasing constraints."),
]
story.append(data_table(["Step", "Action", "Implementation behavior"], sequence_rows, [32, 90, CONTENT_W - 122]))
story.append(Spacer(1, 8))
story.append(callout(
    "WHY THIS FLOW MATTERS",
    "The calculation and ordering stages are separated. A low inventory position triggers replenishment, while the target stock determines the amount. This avoids confusing safety stock with the order quantity.",
    bg=PALE_TEAL, border=TEAL,
))
story.append(PageBreak())

# 4 Data preparation
story.append(p("4. Data Preparation and Quality Rules", "H1x"))
story.append(p("Demand construction", "H2x"))
story.append(p(
    "Only transactions classified as Sale and belonging to the selected location are included. For each product-day, the system sums fulfilled quantity and lost-sales quantity. Lost sales represent demand that could not be fulfilled because inventory was unavailable, so including them reduces downward bias after a stockout.",
))
story.append(p("Daily demand = sold quantity + lost-sales quantity", "Formula"))
story.append(p("Complete calendar", "H2x"))
story.append(p(
    "The engine creates one observation for every calendar date between InventoryTrackingStartDate and the calculation date, inclusive. A missing transaction date is treated as zero demand. The stage thresholds therefore refer to usable calendar days, not merely days that contain sales.",
))
story.append(callout(
    "PANEL DETAIL",
    "If the system averaged only sales days, a product sold twice on one day and zero on six days could be misread as two units per day. Completing the week produces the correct average of 2/7 units per day.",
))
story.append(p("Supplier lead-time construction", "H2x"))
story.append(p(
    "The engine uses completed order slips with both OrderedAt and CompletedAt dates. Lead time is the positive number of calendar days between those dates. Observations are grouped by supplier because supplier performance affects every product sourced from that supplier. At least five valid observations are required before measured lead-time variability is used.",
))
story.append(p("Lead time (days) = CompletedAt.Date - OrderedAt.Date", "Formula"))
story.append(p("Validation and defensive controls", "H2x"))
validation_rows = [
    ("Demand", "No negative daily demand; at least one calendar day."),
    ("Lead time", "Every observation must be positive."),
    ("Service level", "Only 0.9000, 0.9500, 0.9750, 0.9800, or 0.9900 is accepted."),
    ("Policy limits", "Minimums cannot be negative; maximum safety cannot be below minimum safety."),
    ("Maximum stock", "Must be positive and cannot be below the applied reorder point."),
    ("Concurrency", "Serializable recalculation transaction plus row-version conflict checks for settings/workflows."),
]
story.append(data_table(["Area", "Rule"], validation_rows, [110, CONTENT_W - 110]))
story.append(PageBreak())

# 5 Stages
story.append(p("5. The Staged Calculation Policy", "H1x"))
stage_rows = [
    ("ColdStart", "1-29 days", "Low", "Estimated weekly demand / 7", "Buffer-days policy"),
    ("Learning A", "30-59 days", "Medium", "50% observed + 50% estimate", "Demand variability with fixed lead time"),
    ("Learning B", "60-89 days", "Medium", "70% observed + 30% estimate", "Demand variability with fixed lead time"),
    ("DataDriven", "90+ days, <5 lead times", "Medium", "Observed mean", "Demand variability with configured lead time"),
    ("DataDriven", "90+ days, >=5 lead times", "High", "Observed mean", "Combined demand and lead-time variability"),
    ("Manual", "Any history", "Low before 90 days", "Observed mean for reference", "Admin safety and reorder values; target recomputed"),
]
story.append(data_table(
    ["Stage", "Usable history", "Confidence", "Demand used", "Safety-stock basis"],
    stage_rows, [65, 75, 58, 138, CONTENT_W - 336], font_size=7.1,
))
story.append(Spacer(1, 10))
story.append(p("Why the stages are necessary", "H2x"))
story.extend([
    bullet("ColdStart avoids calculating a misleading standard deviation from very little history."),
    bullet("Learning reduces sudden jumps by combining observed demand with the original business estimate."),
    bullet("DataDriven removes the initial estimate after sufficient history and uses measured variability."),
    bullet("Manual preserves administrator control for exceptional products, but explicitly labels the override."),
])
story.append(p("Confidence labels", "H2x"))
story.append(p(
    "Confidence is not a statistical confidence interval. It is an operational evidence label. Low means insufficient history or early manual policy; Medium means sufficient demand history but limited supplier lead-time history; High means at least 90 demand days and at least five completed supplier lead-time observations.",
))
story.append(callout(
    "SAY THIS TO THE PANEL",
    "We separate the calculation stage from the confidence label. A product can have a valid operational policy while still being labeled Low confidence, which tells the administrator to review it more carefully.",
    bg=PALE_TEAL, border=TEAL,
))
story.append(PageBreak())

# 6 Equations
story.append(p("6. Equations Used by the Implementation", "H1x"))
story.append(p("Notation", "H2x"))
notation_rows = [
    ("d_bar", "Applied average daily demand"),
    ("sigma_d", "Population standard deviation of daily demand"),
    ("L_bar", "Average lead time in days"),
    ("sigma_L", "Population standard deviation of lead time"),
    ("z", "Z-score mapped from configured service level"),
    ("B", "Configured buffer days"),
    ("R", "Configured review period in days"),
]
story.append(data_table(["Symbol", "Meaning"], notation_rows, [70, CONTENT_W - 70]))
story.append(Spacer(1, 8))
story.append(p("Cold-start", "H2x"))
story.append(p("d_bar = InitialEstimatedWeeklyDemand / 7", "Formula"))
story.append(p("SafetyStock = ceil(d_bar * B)", "Formula"))
story.append(p("Learning or data-driven with fixed lead time", "H2x"))
story.append(p("SafetyStock = ceil(z * sigma_d * sqrt(L_bar))", "Formula"))
story.append(p("Data-driven with demand and lead-time variability", "H2x"))
story.append(p("SafetyStock = ceil(z * sqrt(L_bar * sigma_d^2 + d_bar^2 * sigma_L^2))", "Formula"))
story.append(p("Reorder and target levels", "H2x"))
story.append(p("ReorderPoint = ceil(d_bar * L_bar + SafetyStock)", "Formula"))
story.append(p("TargetStock = ceil(d_bar * (L_bar + R) + SafetyStock)", "Formula"))
story.append(p(
    "All calculated quantities are rounded upward to whole units. Safety stock is then constrained by configured minimum and maximum safety values. Target stock cannot be below the reorder point and is capped by MaximumStockLevel when configured.",
))
z_rows = [
    ("90.00%", "1.2816"), ("95.00%", "1.6449"), ("97.50%", "1.9600"),
    ("98.00%", "2.0537"), ("99.00%", "2.3263"),
]
story.append(p("Supported service levels", "H2x"))
story.append(data_table(["Service level", "Z-score"], z_rows, [120, 120]))
story.append(PageBreak())

# 7 Cold start
story.append(p("7. Cold-Start: The Main Defense Topic", "H1x"))
story.append(p("Definition in StockSense", "H2x"))
story.append(p(
    "Cold-start occurs when a product has fewer than 30 usable calendar days. In this period, the demand history is too short to rely on variability as the main protection. StockSense converts the administrator's estimated weekly demand into a daily rate and multiplies it by buffer days. The result is marked Low confidence.",
))
story.append(callout(
    "WHY THIS IS NOT ZERO STOCK",
    "No history does not mean no demand. A new product may have future demand even when the database contains no sales. The initial weekly estimate supplies a temporary operational prior until actual observations accumulate.",
    bg=PALE_AMBER, border=AMBER,
))
story.append(p("Worked cold-start example", "H2x"))
cold_rows = [
    ("Initial estimated weekly demand", "70 units/week"),
    ("Applied daily demand", "70 / 7 = 10 units/day"),
    ("Buffer", "7 days"),
    ("Safety stock", "ceil(10 * 7) = 70 units"),
    ("Configured lead time", "7 days"),
    ("Reorder point", "ceil(10 * 7 + 70) = 140 units"),
    ("Review period", "7 days"),
    ("Target stock", "ceil(10 * (7 + 7) + 70) = 210 units"),
]
story.append(data_table(["Input / output", "Value"], cold_rows, [180, CONTENT_W - 180]))
story.append(Spacer(1, 8))
story.append(p("How to defend the estimate", "H2x"))
story.extend([
    bullet("The estimate must be based on owner knowledge, comparable products, supplier guidance, pre-orders, or a pilot period."),
    bullet("InitialEstimatedWeeklyDemand defaults to zero, so new-product onboarding must set a defensible estimate or minimum safety stock; otherwise the automatic cold-start outputs can all be zero."),
    bullet("The dashboard labels the result Low confidence and exposes the inputs for review."),
    bullet("At 30 days, observed demand begins influencing the policy; at 90 days, the initial estimate is removed from automatic demand calculation."),
    bullet("The estimate can be bounded by minimum/maximum safety stock and maximum stock level to control business risk."),
])
story.append(p("Critical limitation", "H2x"))
story.append(p(
    "Cold-start quality depends on the initial estimate. A poor estimate can overstock or understock the product. The system manages this risk through transparency, confidence labeling, staged replacement by actual data, and administrator limits; it does not claim to eliminate estimation error.",
))
story.append(PageBreak())

# 8 Learning and data-driven
story.append(p("8. Learning and Data-Driven Behavior", "H1x"))
story.append(p("Learning stage", "H2x"))
story.append(p(
    "From 30 to 59 days, the applied daily demand is a 50/50 blend of observed average and cold-start estimate. From 60 to 89 days, observed demand receives 70% weight. Safety stock is based on observed demand standard deviation and configured lead time. This creates a controlled transition instead of an abrupt switch on day 30.",
))
story.append(p("Example: observed demand = 4/day; initial estimate = 14/week = 2/day", "Formula"))
story.append(p("30-59 days: d_bar = 0.50(4) + 0.50(2) = 3/day", "Formula"))
story.append(p("60-89 days: d_bar = 0.70(4) + 0.30(2) = 3.4/day", "Formula"))
story.append(p("Data-driven stage", "H2x"))
story.append(p(
    "At 90 or more usable demand days, the applied average is the observed average. If fewer than five completed supplier orders exist, StockSense uses the configured lead time and demand variability only. With five or more valid lead times, it uses both demand variability and lead-time variability and labels the result High confidence.",
))
data_rows = [
    ("Demand history", "90 alternating observations of 0 and 2 units"),
    ("Mean / deviation", "1.0 / 1.0 units per day"),
    ("Observed lead times", "2, 4, 6, 8, 10 days"),
    ("Lead mean / deviation", "6.0 / approximately 2.8284 days"),
    ("Service level", "95%; z = 1.6449"),
    ("Calculated safety stock", "7 units"),
    ("Reorder point", "13 units"),
    ("Target stock", "16 units"),
]
story.append(data_table(["Tested example", "Result"], data_rows, [170, CONTENT_W - 170]))
story.append(Spacer(1, 8))
story.append(callout(
    "WHAT CHANGES OVER TIME",
    "The product does not remain cold-start forever. Each recalculation rebuilds the complete demand series, reevaluates the stage, and stores the new calculation reason, metrics, confidence, timestamp, and version.",
))
story.append(PageBreak())

# 9 Ordering
story.append(p("9. From Inventory Policy to Order Recommendation", "H1x"))
story.append(p("Inventory position", "H2x"))
story.append(p("InventoryPosition = CurrentStock + IncomingStock", "Formula"))
story.append(p(
    "Incoming stock includes the unreceived balance of Approved, Ordered, and PartiallyReceived slips. Drafts do not count as incoming, but any open slip - including a Draft - blocks a duplicate recommendation for that product. Reserved and backorder values are currently zero in this version.",
))
story.append(p("Trigger and quantity", "H2x"))
story.append(p("If InventoryPosition > ReorderPoint, SuggestedQuantity = 0", "Formula"))
story.append(p("Shortage = max(0, TargetStock - InventoryPosition)", "Formula"))
story.append(p("BaseQuantity = max(Shortage, MinimumOrderQuantity)", "Formula"))
story.append(p("SuggestedQuantity = round BaseQuantity upward to PackageSize", "Formula"))
story.append(p(
    "If MaximumStockLevel exists, the quantity is capped at the largest whole package that fits. If the capped result cannot meet the minimum order quantity, the recommendation becomes zero and the dashboard/workflow returns a warning.",
))
story.append(p("Worked purchasing example", "H2x"))
order_rows = [
    ("Current stock", "4"),
    ("Approved incoming stock", "8"),
    ("Inventory position", "12"),
    ("Reorder point", "10"),
    ("Decision", "No new order, because 12 > 10"),
    ("Control achieved", "Incoming stock prevents duplicate replenishment"),
]
story.append(data_table(["Item", "Value"], order_rows, [170, CONTENT_W - 170]))
story.append(p("Workflow controls", "H2x"))
story.extend([
    bullet("Only products with valid settings, metrics, automatic ordering enabled, and an assigned supplier are recommended."),
    bullet("Drafts are grouped by supplier and preserve calculation snapshots for later audit."),
    bullet("Automatic ordering means automatic recommendation generation only; a human still reviews the draft and an Admin approves it before ordering."),
    bullet("Valid status flow is Draft -> Approved -> Ordered -> PartiallyReceived/Completed."),
    bullet("Receiving updates stock and triggers recalculation; invalid dates and quantities are rejected."),
])
story.append(PageBreak())

# 10 Dashboard/admin
story.append(p("10. Dashboard and Administrative Controls", "H1x"))
story.append(p(
    "The Safety Stock Dashboard is the operational explanation layer. It lists current stock, incoming stock, inventory position, daily demand, variability, safety stock, reorder point, target stock, stage, confidence, last calculation time, and the recorded calculation explanation.",
))
story.append(p("Available actions", "H2x"))
action_rows = [
    ("Recalculate one", "Refresh one product and display stage and output values."),
    ("Recalculate selected", "Process checked products with per-product progress and error reporting."),
    ("Recalculate all", "Rebuild policy metrics for all products at location MAIN."),
    ("Settings", "Admin-only dialog for mode, demand estimate, lead/review/buffer days, service level, safety limits, purchasing constraints, and tracking date."),
    ("Save and recalculate", "Persist row-version-protected settings and immediately refresh the product policy."),
]
story.append(data_table(["Control", "Purpose"], action_rows, [120, CONTENT_W - 120]))
story.append(p("Role and reliability controls", "H2x"))
story.extend([
    bullet("Admin and Employee roles can view the dashboard and run calculations."),
    bullet("Only Admin can update inventory settings."),
    bullet("Settings use row-version optimistic concurrency so a stale edit cannot silently overwrite a newer edit."),
    bullet("Recalculation uses a serializable database transaction and the EF Core execution strategy."),
    bullet("Metrics include a calculation version and a human-readable reason for reproducibility."),
    bullet("The legacy Product.ReorderTarget field stores the calculated reorder point; the separate metric TargetStock field stores the order-up-to target."),
    bullet("Recalculation can occur on explicit dashboard actions, settings save, committed POS sales, cancellations, and receipts; a completed receipt propagates new supplier lead-time evidence."),
])
story.append(callout(
    "DEMO TIP",
    "Hover the Calculated value to show the full explanation. Then open Settings, change one safe input, save, and point out that the product recalculates immediately. This demonstrates traceability from input to output.",
    bg=PALE_TEAL, border=TEAL,
))
story.append(PageBreak())

# 11 Paper-ready
story.append(p("11. Paper-Ready Methodology", "H1x"))
story.append(p("Suggested subsection title: Adaptive Safety Stock and Replenishment Policy", "H2x"))
story.append(p(
    "The proposed system implemented a staged safety-stock policy to address differences in product data maturity. Daily demand was derived from point-of-sale transactions by summing fulfilled quantity and recorded lost-sales quantity for each product and calendar date. Missing dates within the tracking interval were represented as zero-demand observations to avoid inflating the daily mean through sales-day-only sampling. Supplier lead time was measured from completed order records as the positive calendar-day difference between order placement and completion.",
))
story.append(p(
    "Products with fewer than 30 usable calendar days were assigned to a cold-start stage. Their initial estimated weekly demand was converted to average daily demand, and protective stock was computed from the configured buffer days. Products with 30 to 59 days used an equal blend of observed and initial demand, while products with 60 to 89 days used a 70% observed and 30% initial blend. At 90 days or more, the system used observed demand exclusively. Demand variability was represented using population standard deviation. Supplier lead-time variability was included only when at least five valid completed-order observations were available; otherwise, the configured default lead time was retained.",
))
story.append(p(
    "The system calculated safety stock according to the selected stage, then derived the reorder point from expected demand during lead time plus safety stock. Target stock covered lead time and the configured review period plus safety stock. Values were rounded upward to whole units and constrained by administrator-defined minimum and maximum levels. Replenishment was triggered when inventory position, defined as current stock plus incoming unreceived stock, was at or below the reorder point. Suggested quantity restored inventory toward the target while enforcing minimum order quantity, package-size multiples, maximum stock level, supplier assignment, and duplicate open-order controls.",
))
story.append(p("Suggested results language", "H2x"))
story.append(p(
    "Functional tests confirmed correct stage transitions, service-level mappings, rounding, minimum and maximum safety-stock enforcement, demand and lead-time validation, reorder triggering, incoming-stock duplicate prevention, package-size rounding, maximum-stock capping, and order-slip workflow transitions. In the focused automated test run, all 50 safety-stock and order-slip mathematics tests passed.",
))
story.append(callout(
    "DO NOT OVERCLAIM",
    "Unless you have completed a real operational trial, say that the tests demonstrate functional correctness of the implemented rules. Do not claim proven reductions in stockouts, costs, or waste without before-and-after field data.",
    bg=PALE_RED, border=RED,
))
story.append(PageBreak())

# 12 Evaluation and limitations
story.append(p("12. Evaluation, Limitations, and Future Work", "H1x"))
story.append(p("Evidence already available", "H2x"))
evidence_rows = [
    ("Unit tests", "50 focused tests passed for safety-stock and order-slip mathematics."),
    ("Boundary tests", "29/30/60/90-day behavior, ceiling, min/max limits, supported service levels."),
    ("Data validation", "Negative demand, non-positive lead times, empty series, and invalid settings are rejected."),
    ("Ordering tests", "Trigger point, incoming stock, MOQ, package size, maximum stock, duplicate/order status rules."),
    ("Persistence controls", "Transactions, row versions, calculation reason, snapshots, timestamps, and versioning."),
]
story.append(data_table(["Evidence", "What it supports"], evidence_rows, [110, CONTENT_W - 110]))
story.append(p("Limitations to acknowledge", "H2x"))
story.extend([
    bullet("Cold-start depends on the quality of the administrator's initial weekly-demand estimate."),
    bullet("Because the weekly-demand estimate defaults to zero, incomplete onboarding can produce zero cold-start safety, reorder, and target values."),
    bullet("The stage thresholds and blend weights are design parameters and have not yet been optimized through field experimentation."),
    bullet("The current demand model does not explicitly model trend, seasonality, promotions, or intermittent-demand structure."),
    bullet("Missing sales dates are assumed to be true zero demand; operational closures or missing transaction capture could violate that assumption."),
    bullet("The calculation date is included as a full usable day, so intraday recalculation can slightly depress the current mean before the day is complete."),
    bullet("Lead-time observations are pooled by supplier, which may not reflect product-specific fulfillment differences."),
    bullet("Reserved and backorder components are not yet included in inventory position; the current values are zero."),
    bullet("The current operational scope is location MAIN and uses a discrete set of supported service levels."),
])
story.append(p("Recommended future evaluation", "H2x"))
story.extend([
    bullet("Run a pre/post pilot and measure stockout rate, fill rate, average inventory, emergency purchases, and inventory turnover."),
    bullet("Compare the staged policy against the store's former fixed-threshold method using the same product periods."),
    bullet("Add rolling backtests and sensitivity analysis for thresholds, blend weights, service levels, and buffer days."),
    bullet("Evaluate seasonal forecasts and intermittent-demand methods such as Croston-type approaches where product behavior warrants them."),
    bullet("Extend inventory position to reservations, backorders, multi-location transfers, and product-specific supplier lead times."),
])
story.append(PageBreak())

# 13 Panel script
story.append(p("13. Suggested 6-Minute Panel Explanation", "H1x"))
script_rows = [
    ("0:00-0:40", "Problem", "Our previous inventory threshold could not adapt to different demand patterns or new products. The module calculates when to reorder and how much to order while showing the evidence behind the decision."),
    ("0:40-1:25", "Inputs", "We use daily sales plus recorded lost sales, zero-demand calendar days, supplier completion history, product settings, current stock, and incoming stock."),
    ("1:25-2:35", "Cold-start", "For fewer than 30 days, history is insufficient. We convert the owner's weekly estimate to daily demand, multiply by buffer days, label it Low confidence, and progressively replace it with observed data."),
    ("2:35-3:25", "Learning/data", "At 30-59 days we blend 50/50; at 60-89, 70/30; at 90+, observed demand drives the result. Five completed supplier orders allow lead-time variability and High confidence."),
    ("3:25-4:20", "Outputs", "Safety stock protects against uncertainty, reorder point is the trigger, and target stock is the replenishment objective. These are distinct values."),
    ("4:20-5:15", "Ordering", "The system triggers only at or below reorder point, subtracts incoming stock through inventory position, rounds to package size, applies MOQ and maximum stock, prevents duplicates, and groups drafts by supplier."),
    ("5:15-6:00", "Evidence", "The calculations are explainable and versioned. A focused test run passed all 50 math/workflow tests. We acknowledge that field KPIs are needed before claiming business impact."),
]
story.append(data_table(["Time", "Topic", "What to say"], script_rows, [58, 75, CONTENT_W - 133], font_size=7.5))
story.append(Spacer(1, 10))
story.append(p("Strong closing statement", "H2x"))
story.append(p(
    '"The contribution of StockSense is not merely a formula. It is a controlled path from uncertain early estimates to evidence-based replenishment, with clear confidence, audit history, and purchasing safeguards."',
    "Quote",
))
story.append(p("Live demonstration order", "H2x"))
story.extend([
    bullet("Open Safety Stock Dashboard and identify Current, Incoming, Position, Safety, Reorder, Target, Stage, and Confidence."),
    bullet("Show a new or short-history product and explain its Low-confidence ColdStart label."),
    bullet("Open Settings and identify weekly estimate, lead time, review period, buffer, service level, MOQ, package size, and caps."),
    bullet("Run recalculation and show the recorded explanation and updated values."),
    bullet("Open the order preview and explain duplicate prevention, missing supplier warnings, supplier grouping, and suggested quantity."),
])
story.append(PageBreak())

# 14 Q&A
story.append(p("14. Likely Panel Questions and Defensible Answers", "H1x"))
qa_rows = [
    ("Why 30 and 90 days?", "They are explicit design thresholds for staged evidence maturity. Thirty days begins observed blending; ninety days provides a longer demand window. They are configurable research assumptions for future validation, not universal constants."),
    ("Why not use zero for a new product?", "No recorded history is not proof of zero demand. A business estimate provides a temporary prior, is labeled Low confidence, and is replaced gradually by observed data."),
    ("Is this machine learning?", "No. It is a deterministic statistical inventory policy. That improves explainability and reproducibility. Forecasting models are future work."),
    ("Why include lost sales?", "Fulfilled sales alone understate demand during stockouts. Quantity plus lost sales better represents attempted demand."),
    ("Why fill missing dates with zero?", "A complete calendar prevents sales-only sampling from inflating the average. The assumption requires reliable transaction capture and should exclude known closure anomalies in future work."),
    ("Why five lead-time records?", "The system requires a minimum evidence threshold before using measured variability. Below five, it falls back to configured lead time and labels confidence Medium rather than High."),
    ("Safety stock vs reorder point?", "Safety stock is the protective buffer. Reorder point adds expected demand during lead time. They are related but not interchangeable."),
    ("Why target stock?", "The reorder point answers when to order; target stock answers how far to replenish, including the review period."),
    ("How do you avoid duplicate orders?", "Inventory position includes incoming quantities, and any open slip blocks another recommendation for that product."),
    ("Can an admin override it?", "Yes, Manual mode accepts explicit safety stock and reorder point, records that an override was used, and still computes reference metrics and target stock."),
    ("How do you prove correctness?", "Boundary and formula tests cover stages, rounding, limits, validation, ordering constraints, and workflow states. All 50 focused tests passed."),
    ("Did it reduce stockouts?", "The implementation supports that goal, but a causal reduction claim requires a controlled pre/post operational evaluation. We currently claim functional correctness, not unmeasured business impact."),
]
story.append(data_table(["Question", "Recommended answer"], qa_rows, [125, CONTENT_W - 125], font_size=7.25))
story.append(PageBreak())

# 15 Checklist/references
story.append(p("15. Defense Checklist and Implementation References", "H1x"))
story.append(p("Before the presentation", "H2x"))
story.extend([
    bullet("Prepare one product in each stage: ColdStart, Learning, and DataDriven; optionally show Manual."),
    bullet("Confirm at least one product has a supplier and one intentionally demonstrates a missing-supplier warning."),
    bullet("Prepare one example with incoming stock to demonstrate duplicate-order prevention."),
    bullet("Know the weekly estimate, lead time, review period, buffer days, service level, MOQ, package size, and maximum stock for the demo product."),
    bullet("Run the focused tests and keep the result available: 50 passed, 0 failed, 0 skipped on 26 July 2026."),
    bullet("Do not use fabricated defense seed data in production or present synthetic outcomes as operational evidence."),
])
story.append(p("Implementation evidence", "H2x"))
refs = [
    ("Core formulas and stages", "StockSense.Infrastructure/Services/SafetyStockMath.cs"),
    ("Demand, zero days, lead times, persistence", "StockSense.Infrastructure/Services/SafetyStockCalculationService.cs"),
    ("Order quantity and status rules", "StockSense.Infrastructure/Services/OrderSlipMath.cs"),
    ("Order previews, supplier grouping, snapshots", "StockSense.Infrastructure/Services/OrderSlipWorkflowService.cs"),
    ("Dashboard and settings API", "StockSense.Web/Controllers/InventoryController.cs"),
    ("Admin dashboard interaction", "StockSense.Client/Pages/Admin/ManageSafetyStock.razor"),
    ("Calculation boundary tests", "tests/StockSense.Tests/SafetyStockMathTests.cs"),
    ("Ordering and workflow math tests", "tests/StockSense.Tests/OrderSlipMathTests.cs"),
]
story.append(data_table(["Evidence area", "Repository source"], refs, [165, CONTENT_W - 165], font_size=7.3))
story.append(p("Terminology to use consistently", "H2x"))
terms = [
    ("Safety stock", "Protective buffer against demand and supply uncertainty."),
    ("Reorder point", "Inventory-position trigger for replenishment."),
    ("Target stock", "Desired post-replenishment coverage level."),
    ("Cold-start", "Fewer than 30 usable calendar days; estimate-based and Low confidence."),
    ("Inventory position", "Current stock plus incoming unreceived stock in the current implementation."),
    ("Confidence", "Operational evidence label, not a statistical confidence interval."),
]
story.append(data_table(["Term", "Use this meaning"], terms, [120, CONTENT_W - 120]))
story.append(Spacer(1, 10))
story.append(callout(
    "FINAL REMINDER",
    "Be precise about what the system currently proves: the rules are implemented, transparent, constrained, and tested. Business impact should be presented as an evaluation objective until supported by real longitudinal data.",
    bg=PALE_BLUE, border=BLUE,
))


def draw_cover_background(canvas, doc):
    if canvas.getPageNumber() != 1:
        header_footer(canvas, doc)
        return
    canvas.saveState()
    canvas.setFillColor(NAVY)
    canvas.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    canvas.setFillColor(BLUE)
    canvas.circle(PAGE_W - 35, PAGE_H - 55, 130, fill=1, stroke=0)
    canvas.setFillColor(TEAL)
    canvas.circle(PAGE_W - 5, 35, 95, fill=1, stroke=0)
    canvas.restoreState()


doc = DefenseDocTemplate(str(OUTPUT))
doc.pageTemplates[0].onPage = draw_cover_background
doc.build(story)
print(OUTPUT)
