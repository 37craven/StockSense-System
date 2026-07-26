from pathlib import Path
from PIL import Image, ImageDraw

folder = Path(__file__).resolve().parent
pages = sorted(folder.glob("defense-page-*.png"))
thumb_w, thumb_h = 306, 396
gap = 18
cols = 4
rows = (len(pages) + cols - 1) // cols
sheet = Image.new("RGB", (cols * thumb_w + (cols + 1) * gap, rows * thumb_h + (rows + 1) * gap), "#dce3ea")
draw = ImageDraw.Draw(sheet)
for index, page_path in enumerate(pages):
    image = Image.open(page_path).convert("RGB")
    image.thumbnail((thumb_w, thumb_h))
    x = gap + (index % cols) * (thumb_w + gap)
    y = gap + (index // cols) * (thumb_h + gap)
    sheet.paste(image, (x, y))
    draw.rectangle((x, y, x + image.width - 1, y + image.height - 1), outline="#73808c", width=1)
sheet.save(folder / "defense-contact-sheet.png")
