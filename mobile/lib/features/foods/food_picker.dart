import 'package:flutter/material.dart';

import '../../demo_data.dart' show energyLabel;
import '../../ui.dart';
import 'food_catalog.dart';

class FoodPicker extends StatefulWidget {
  const FoodPicker({super.key, required this.mealName, required this.selected});

  final String mealName;
  final List<LoggedFood> selected;

  @override
  State<FoodPicker> createState() => _FoodPickerState();
}

class _FoodPickerState extends State<FoodPicker> {
  final _search = TextEditingController();

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _pick(FoodItem food, double? existing) async {
    FocusScope.of(context).unfocus();
    final portions = await showModalBottomSheet<double>(
      context: context,
      useSafeArea: true,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _PortionSheet(food: food, existing: existing),
    );
    if (!mounted || portions == null) return;
    Navigator.pop(context, (food: food, portions: portions));
  }

  @override
  Widget build(BuildContext context) {
    final foods = searchFoods(_search.text);
    final selected = {
      for (final item in widget.selected) item.food.id: item.portions,
    };
    return Scaffold(
      appBar: AppBar(title: const Text('Besin ekle')),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              ScreenHeading(
                widget.mealName,
                'Besin seç',
                subtitle: 'Örnek besin kataloğu',
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _search,
                textInputAction: TextInputAction.search,
                onChanged: (_) => setState(() {}),
                decoration: InputDecoration(
                  labelText: 'Besin ara',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: _search.text.isEmpty
                      ? null
                      : IconButton(
                          tooltip: 'Aramayı temizle',
                          icon: const Icon(Icons.close),
                          onPressed: () => setState(_search.clear),
                        ),
                  filled: true,
                  fillColor: Colors.white,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
              const SizedBox(height: 14),
              Expanded(
                child: foods.isEmpty
                    ? const Center(
                        child: Text(
                          'Besin bulunamadı. Başka bir adla arayın.',
                          textAlign: TextAlign.center,
                        ),
                      )
                    : ListView.separated(
                        keyboardDismissBehavior:
                            ScrollViewKeyboardDismissBehavior.onDrag,
                        padding: const EdgeInsets.only(bottom: 16),
                        itemCount: foods.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 8),
                        itemBuilder: (context, index) {
                          final food = foods[index];
                          final existing = selected[food.id];
                          return Panel(
                            padding: EdgeInsets.zero,
                            child: ListTile(
                              onTap: () => _pick(food, existing),
                              leading: const CircleAvatar(
                                backgroundColor: mint,
                                foregroundColor: teal,
                                child: Icon(Icons.restaurant_outlined),
                              ),
                              title: Text(food.name),
                              subtitle: Text(
                                '${food.portionName} · ${food.portionGrams} g\n'
                                '${energyLabel(food.forPortions(1).kcal)} kcal',
                              ),
                              isThreeLine: true,
                              trailing: Icon(
                                existing == null
                                    ? Icons.add_circle_outline
                                    : Icons.edit_outlined,
                                color: teal,
                                semanticLabel: existing == null
                                    ? 'Ekle'
                                    : 'Miktarı düzenle',
                              ),
                            ),
                          );
                        },
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PortionSheet extends StatefulWidget {
  const _PortionSheet({required this.food, required this.existing});

  final FoodItem food;
  final double? existing;

  @override
  State<_PortionSheet> createState() => _PortionSheetState();
}

class _PortionSheetState extends State<_PortionSheet> {
  final _formKey = GlobalKey<FormState>();
  late final _amount = TextEditingController(
    text: quantityLabel(widget.existing ?? 1),
  );

  double? get _validAmount {
    final text = _amount.text.trim();
    if (!RegExp(r'^\d+(?:[.,]\d{1,2})?$').hasMatch(text)) return null;
    final value = double.tryParse(text.replaceAll(',', '.'));
    return value != null && value >= 0.01 && value <= 20 ? value : null;
  }

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  void _submit() {
    if (_formKey.currentState!.validate()) Navigator.pop(context, _validAmount);
  }

  @override
  Widget build(BuildContext context) {
    final food = widget.food;
    final amount = _validAmount;
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        24,
        0,
        24,
        24 + MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: SafeArea(
        top: false,
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(food.name, style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(height: 4),
              Text(
                '${food.portionName} · ${food.portionGrams} g',
                style: const TextStyle(color: muted),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _amount,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                textInputAction: TextInputAction.done,
                onChanged: (_) => setState(() {}),
                onFieldSubmitted: (_) => _submit(),
                validator: (_) => _validAmount == null
                    ? '0,01 ile 20 arasında porsiyon girin.'
                    : null,
                decoration: const InputDecoration(
                  labelText: 'Porsiyon sayısı',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 10),
              if (amount != null)
                Text(
                  '${quantityLabel(food.portionGrams * amount)} g · ${energyLabel(food.forPortions(amount).kcal)} kcal',
                  style: const TextStyle(
                    color: teal,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              const SizedBox(height: 14),
              FilledButton(
                onPressed: _submit,
                child: Text(
                  widget.existing == null ? 'Öğüne ekle' : 'Miktarı güncelle',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
