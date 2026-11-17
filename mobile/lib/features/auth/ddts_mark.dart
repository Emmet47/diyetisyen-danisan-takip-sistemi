import 'package:material_ui/material_ui.dart';

const brandForest = Color(0xff103f3b);

class DdtsMark extends StatelessWidget {
  const DdtsMark({super.key, required this.size});

  final double size;

  @override
  Widget build(BuildContext context) => SizedBox.square(
    dimension: size,
    child: const CustomPaint(painter: _DdtsMarkPainter()),
  );
}

class _DdtsMarkPainter extends CustomPainter {
  const _DdtsMarkPainter();

  @override
  void paint(Canvas canvas, Size size) {
    canvas.save();
    canvas.scale(size.width / 96, size.height / 96);
    canvas.drawRRect(
      RRect.fromLTRBR(0, 0, 96, 96, const Radius.circular(26)),
      Paint()..color = brandForest,
    );

    final outline = Paint()
      ..color = const Color(0xffe7faef)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
    final letter = Path()
      ..moveTo(63, 34)
      ..cubicTo(59, 28, 52, 24, 43, 24)
      ..lineTo(28, 24)
      ..lineTo(28, 72)
      ..lineTo(43, 72)
      ..cubicTo(61, 72, 71, 62, 71, 51);
    canvas.drawPath(letter, outline);

    final leaf = Path()
      ..moveTo(43, 58)
      ..cubicTo(42, 44, 52, 39, 67, 40)
      ..cubicTo(67, 54, 57, 63, 43, 58)
      ..close();
    canvas.drawPath(leaf, Paint()..color = const Color(0xffa9dd9d));
    canvas.drawPath(
      Path()
        ..moveTo(43, 58)
        ..quadraticBezierTo(51, 52, 56, 48),
      Paint()
        ..color = brandForest
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..strokeCap = StrokeCap.round,
    );
    canvas.restore();
  }

  @override
  bool shouldRepaint(covariant _DdtsMarkPainter oldDelegate) => false;
}
