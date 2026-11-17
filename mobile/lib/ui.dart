import 'package:material_ui/material_ui.dart';

const ink = Color(0xff182a28);
const muted = Color(0xff4d6661);
const teal = Color(0xff087e80);
const mint = Color(0xffe0f3ef);
const canvas = Color(0xfff1f8f6);
const line = Color(0xffdce9e7);
const chrome = Color(0xffe5efe9);
const chromeSelection = Color(0xffcce2d6);

class ScreenHeading extends StatelessWidget {
  const ScreenHeading(this.eyebrow, this.title, {super.key, this.subtitle});

  final String eyebrow;
  final String title;
  final String? subtitle;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        eyebrow,
        style: const TextStyle(
          color: teal,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
      const SizedBox(height: 4),
      Text(
        title,
        style: const TextStyle(
          color: ink,
          fontSize: 27,
          fontWeight: FontWeight.bold,
        ),
      ),
      if (subtitle != null) ...[
        const SizedBox(height: 4),
        Text(subtitle!, style: const TextStyle(color: muted, fontSize: 13)),
      ],
    ],
  );
}

class Panel extends StatelessWidget {
  const Panel({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(14),
    this.radius = 16,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final double radius;

  @override
  Widget build(BuildContext context) => Material(
    color: Colors.white,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(radius),
      side: const BorderSide(color: line),
    ),
    clipBehavior: Clip.antiAlias,
    child: Padding(padding: padding, child: child),
  );
}
