# Brand assets

The user supplied the approved silver overlapping cards and white stars on a black rounded-square tile. The supplied image was processed with the built-in image_gen tool (not the API/CLI) to prepare two PNG assets. These are image-derived assets, not a newly designed vector mark.

`logo-mark.png`: transparent mark used in the app header.

Prompt: Use case: background-extraction. Edit target: attached approved Skill Wallet logo. Extract ONLY the three overlapping silver/charcoal cards, the large white four-point star on the front card and the small white four-point sparkle at upper right. Remove the black rounded-square tile and outer white canvas entirely, replacing both with genuine transparent alpha. Preserve exact original card count, geometry, angles, proportions, relative position of both stars and metallic grayscale gradients. No redesign, no new elements, no text, no colored tones. Square transparent PNG, tightly frame the extracted mark with a small 5% transparent margin. This is an app header logo asset, not a mockup.

`app-icon.png`: black rounded-square app tile used as the window icon and source for `wallet.ico`.

Prompt: Use case: background-extraction. Prepare the attached approved app icon for Windows packaging. Preserve the complete existing BLACK rounded-square icon with all three original silver cards and both white stars EXACTLY as shown. Remove ONLY the outside white canvas. Tightly frame the black rounded-square tile with 2% margin, actual transparent alpha outside the rounded tile. Square PNG. Do not redraw or redesign the card/star artwork. No mockup, no extra shadow, no text. The black rounded-square background must remain solid black. Clean smooth antialiased perimeter.

The ICO contains PNG frames at 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels. All assets are embedded in the executable; no absolute local asset paths are required at runtime.
