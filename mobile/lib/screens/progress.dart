import 'dart:math' as math;

import 'package:material_ui/material_ui.dart';

import '../core/api_client.dart';
import '../core/format.dart';
import '../features/measurements/client_measurements.dart';
import '../ui.dart';

/// Danışanın kendi ölçüm geçmişi ve eğilim grafiği (UC-28, FR-C4).
class ProgressScreen extends StatefulWidget {
  const ProgressScreen({super.key, required this.api});

  final SessionApi api;

  @override
  State<ProgressScreen> createState() => _ProgressScreenState();
}

enum _Metric {
  weight('Kilo', 'kg'),
  waist('Bel çevresi', 'cm');

  const _Metric(this.label, this.unit);
  final String label;
  final String unit;

  double value(Measurement item) =>
      this == weight ? item.weightKg : item.waistCm;
  double? change(Measurement item) =>
      this == weight ? item.weightChangeKg : item.waistChangeCm;
}

class _ProgressScreenState extends State<ProgressScreen> {
  MeasurementHistory? _history;
  String? _error;
  bool _loading = false;
  _Metric _metric = _Metric.weight;

  @override
  void initState() {
    super.initState();
    _fetch();
  }

  Future<void> _fetch() async {
    if (_loading) return;
    setState(() {
      _loading = true;
      _error = null;
      _history = null;
    });
    try {
      final history = await loadClientMeasurements(widget.api);
      if (mounted) _history = history;
    } on ApiException catch (error) {
      if (!mounted) return;
      _error = error.message;
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final history = _history;
    final error = _error;
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Row(
          children: [
            const Expanded(
              child: ScreenHeading(
                'ÖLÇÜMLER',
                'Gelişim',
                subtitle: 'Diyetisyeninin kaydettiği ölçümler',
              ),
            ),
            IconButton(
                tooltip: 'Ölçümleri yenile',
                onPressed: _loading ? null : _fetch,
                icon: const Icon(Icons.refresh),
              ),
          ],
        ),
        const SizedBox(height: 12),
        if (_loading)
          const Center(
            child: CircularProgressIndicator(
              semanticsLabel: 'Ölçümler yükleniyor',
            ),
          )
        else if (error != null)
          Panel(
            child: Column(
              children: [
                Text(error),
                TextButton(onPressed: _fetch, child: const Text('Tekrar dene')),
              ],
            ),
          )
        else if (history != null && history.items.isEmpty)
          const Panel(child: Text('Henüz ölçüm kaydı yok.'))
        else if (history != null)
          ..._historyPanels(context, history),
      ],
    );
  }

  List<Widget> _historyPanels(
    BuildContext context,
    MeasurementHistory history,
  ) {
    final theme = Theme.of(context);
    final items = history.items;
    final values = items.map(_metric.value).toList();
    final latest = '${_format(values.last)} ${_metric.unit}';
    final delta = _metric.change(items.last);
    final target = _metric == _Metric.weight ? history.targetWeightKg : null;
    return [
      Panel(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SegmentedButton<_Metric>(
              segments: [
                for (final option in _Metric.values)
                  ButtonSegment(value: option, label: Text(option.label)),
              ],
              selected: {_metric},
              onSelectionChanged: (selection) =>
                  setState(() => _metric = selection.single),
            ),
            const SizedBox(height: 12),
            const Text(
              'Son ölçüm',
              style: TextStyle(color: muted, fontSize: 11),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  latest,
                  style: theme.textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (delta != null)
                  Flexible(
                    child: Text(
                      'Önceki ölçüme göre ${delta > 0 ? '+' : ''}${_format(delta)} ${_metric.unit}',
                      textAlign: TextAlign.end,
                      style: const TextStyle(color: teal, fontSize: 12),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 10),
            Semantics(
              label: '${_metric.label} eğilim grafiği. Son ölçüm $latest.',
              child: SizedBox(
                height: 85,
                child: CustomPaint(
                  painter: _Trend(values, target),
                  child: const SizedBox.expand(),
                ),
              ),
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  dayLabel(items.first.measuredOn),
                  style: const TextStyle(color: muted, fontSize: 11),
                ),
                if (items.length > 1)
                  Text(
                    dayLabel(items.last.measuredOn),
                    style: const TextStyle(color: muted, fontSize: 11),
                  ),
              ],
            ),
            if (target != null) ...[
              const SizedBox(height: 10),
              Text('Hedef kilo ${_format(target)} kg'),
              if (history.remainingKg case final remaining?)
                Text('Hedefe fark ${_format(remaining)} kg'),
            ],
          ],
        ),
      ),
      const SizedBox(height: 12),
      Panel(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Ölçüm geçmişi', style: theme.textTheme.titleMedium),
            for (final item in items.reversed)
              ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                title: Text(
                  '${dayLabel(item.measuredOn)} ${item.measuredOn.year}',
                ),
                subtitle: Text(
                        [
                          'Boy ${_format(item.heightCm)} cm',
                          if (item.hipCm case final hip?)
                            'Kalça ${_format(hip)} cm',
                          if (item.bodyFatPercent case final fat?)
                            'Yağ %${_format(fat)}',
                          if (item.note case final note? when note.isNotEmpty)
                            note,
                        ].join(' · '),
                      ),
                trailing: Text(
                  '${_format(item.weightKg)} kg · ${_format(item.waistCm)} cm',
                ),
              ),
          ],
        ),
      ),
    ];
  }

  String _format(double value) => value
      .toStringAsFixed(2)
      .replaceFirst(RegExp(r'\.?0+$'), '')
      .replaceAll('.', ',');
}

class _Trend extends CustomPainter {
  const _Trend(this.values, this.target);

  final List<double> values;
  final double? target;

  @override
  void paint(Canvas canvas, Size size) {
    if (values.isEmpty) return;
    const padding = 8.0;
    final plotValues = [?target, ...values];
    final minValue = plotValues.reduce(math.min);
    final span = math.max(1.0, plotValues.reduce(math.max) - minValue);
    double y(double value) =>
        size.height -
        padding -
        (value - minValue) * (size.height - 2 * padding) / span;
    final line = Paint()
      ..color = teal
      ..strokeWidth = 2;
    if (target case final goal?) {
      final marker = Paint()..color = muted;
      for (var x = padding; x < size.width - padding; x += 8) {
        canvas.drawLine(
          Offset(x, y(goal)),
          Offset(math.min(x + 4, size.width - padding), y(goal)),
          marker,
        );
      }
    }
    final points = [
      for (final (index, value) in values.indexed)
        Offset(
          values.length == 1
              ? size.width / 2
              : padding +
                    index * (size.width - 2 * padding) / (values.length - 1),
          y(value),
        ),
    ];
    for (var i = 1; i < points.length; i++) {
      canvas.drawLine(points[i - 1], points[i], line);
    }
    for (final point in points) {
      canvas.drawCircle(point, 4, line);
    }
  }

  @override
  bool shouldRepaint(_Trend oldDelegate) =>
      target != oldDelegate.target || values != oldDelegate.values;
}
