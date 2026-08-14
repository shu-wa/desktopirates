from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = PROJECT_ROOT / "ArtSource" / "Generated" / "UI" / "v03-arrowless-markers"
OUTPUT_ROOT = (
    PROJECT_ROOT
    / "Assets"
    / "Desktopirates"
    / "Resources"
    / "Textures"
    / "UI"
    / "ConceptV02"
    / "Icons"
)
KINDS = ("enemy", "port", "wreck", "treasure")
OUTPUT_SIZE = 128
SUBJECT_SIZE = 118


def prepare(kind: str) -> None:
    source = SOURCE_ROOT / f"marker_{kind}_alpha_v03.png"
    output = OUTPUT_ROOT / f"marker_{kind}_v02.png"
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value > 8 else 0).getbbox()
    if bounds is None:
        raise RuntimeError(f"No opaque marker pixels found in {source}")

    subject = image.crop(bounds)
    scale = min(SUBJECT_SIZE / subject.width, SUBJECT_SIZE / subject.height)
    resized = subject.resize(
        (max(1, round(subject.width * scale)), max(1, round(subject.height * scale))),
        Image.Resampling.NEAREST,
    )
    canvas = Image.new("RGBA", (OUTPUT_SIZE, OUTPUT_SIZE), (0, 0, 0, 0))
    canvas.alpha_composite(
        resized,
        ((OUTPUT_SIZE - resized.width) // 2, (OUTPUT_SIZE - resized.height) // 2),
    )
    canvas.save(output, optimize=True)
    print(f"Prepared {output.relative_to(PROJECT_ROOT)}")


def main() -> None:
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    for kind in KINDS:
        prepare(kind)


if __name__ == "__main__":
    main()
