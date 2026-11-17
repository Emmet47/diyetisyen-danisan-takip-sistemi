import 'package:material_ui/material_ui.dart';

import '../../ui.dart';

/// KVKK aydınlatma metni (gereksinimler 2.5-1). Danışan giriş yaptıktan sonra
/// uygulamaya geçmeden önce gösterilir.
class KvkkNoticeScreen extends StatelessWidget {
  const KvkkNoticeScreen({
    super.key,
    required this.onAccepted,
    required this.onCancel,
  });

  final VoidCallback onAccepted;
  final VoidCallback onCancel;

  static const paragraphs = [
    'Hangi verileri tutuyoruz: ad-soyad, e-posta, telefon, doğum tarihi, '
        'cinsiyet, hedef, beslenme tercihleri, alerji/kısıt bilgisi, '
        'ölçümleriniz, beslenme planınız, öğün kayıtlarınız ve öğün '
        'fotoğraflarınız. TC kimlik no, adres gibi bilgileri almıyoruz.',
    'Amaç ve dayanak: Bu bilgiler yalnızca diyetisyeninizin beslenme '
        'takibinizi yapabilmesi için işlenir. Sağlık verisi olduğu için '
        'hukuki dayanak, diyetisyeninize verdiğiniz açık rızadır.',
    'Kimler görür: Verilerinizi yalnızca siz ve bağlı olduğunuz diyetisyen '
        'görür. Platform yöneticisi sağlık verilerinizi göremez. Bildirim '
        'metinlerinde sağlık verisi yer almaz.',
    'Saklama ve haklarınız: Diyetisyeninizle çalışmanız bitince hesabınız '
        'pasife alınır. Silinmesini isterseniz ölçüm, plan, öğün kaydı, '
        'randevu ve fotoğraflarınız kalıcı olarak silinir (en geç 30 gün '
        'içinde). Verilerinize erişme, düzeltilmesini ve silinmesini isteme '
        'hakkınız vardır; talebinizi diyetisyeninize iletebilirsiniz.',
  ];

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Aydınlatma metni')),
    body: SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const ScreenHeading(
            'KVKK',
            'Kişisel verileriniz',
            subtitle: 'Uygulamayı kullanmadan önce lütfen okuyun.',
          ),
          const SizedBox(height: 14),
          Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                for (final text in paragraphs)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Text(text, style: const TextStyle(height: 1.4)),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: onAccepted,
            child: const Text('Okudum, devam et'),
          ),
          TextButton(onPressed: onCancel, child: const Text('Çıkış yap')),
        ],
      ),
    ),
  );
}
