import 'package:material_ui/material_ui.dart';

import '../../core/api_client.dart';
import '../../ui.dart';
import 'client_auth.dart';
import 'ddts_mark.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({
    super.key,
    required this.onSignedIn,
    this.authenticate = signInClient,
    this.forgotPassword = requestPasswordReset,
    this.expiredNotice = false,
  });

  final ValueChanged<ClientSession> onSignedIn;
  final Future<ClientSession> Function(String email, String password)
  authenticate;
  final Future<void> Function(String email) forgotPassword;

  /// Oturum süresi dolduğu için girişe dönüldüyse true.
  final bool expiredNotice;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _showPassword = false;
  bool _submitting = false;
  String? _error;
  String? _info;

  @override
  void initState() {
    super.initState();
    if (widget.expiredNotice) {
      _info = 'Oturumunuz sona erdi. Yeniden giriş yapın.';
    }
  }

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _signIn() async {
    if (_submitting || !_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _submitting = true;
      _error = null;
      _info = null;
    });
    try {
      final session = await widget.authenticate(
        _email.text.trim(),
        _password.text,
      );
      if (!mounted) return;
      _password.clear();
      widget.onSignedIn(session);
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// "Parolamı unuttum" (UC-03): sıfırlama bağlantısı e-postayla gelir.
  Future<void> _forgotPassword() async {
    if (_submitting) return;
    final email = _email.text.trim();
    if (!RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(email)) {
      setState(() {
        _info = null;
        _error = 'Sıfırlama bağlantısı için önce e-posta adresinizi yazın.';
      });
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
      _info = null;
    });
    try {
      await widget.forgotPassword(email);
      if (mounted) {
        setState(
          () => _info =
              'Bu adres kayıtlıysa sıfırlama bağlantısı gönderildi. '
              'E-postadaki bağlantıdan yeni şifrenizi belirleyip buradan giriş yapın.',
        );
      }
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  void _clearError(String _) {
    if (_error != null) setState(() => _error = null);
  }

  InputDecoration _field(String label, IconData icon, {Widget? suffix}) {
    return InputDecoration(
      labelText: label,
      prefixIcon: Icon(icon),
      suffixIcon: suffix,
      filled: true,
      fillColor: canvas,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(14)),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: line),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: ConstrainedBox(
              constraints: BoxConstraints(minHeight: constraints.maxHeight),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 420),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 32),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const _BrandHeader(),
                        const SizedBox(height: 24),
                        Panel(
                          radius: 24,
                          padding: const EdgeInsets.all(22),
                          child: AutofillGroup(
                            onDisposeAction: AutofillContextAction.cancel,
                            child: Form(
                              key: _formKey,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  Text(
                                    'Danışan girişi',
                                    style: theme.textTheme.headlineSmall
                                        ?.copyWith(fontWeight: FontWeight.w800),
                                  ),
                                  const SizedBox(height: 6),
                                  const Text(
                                    'Diyetisyeninizin davet ettiği e-posta adresini kullanın.',
                                    style: TextStyle(color: muted, height: 1.4),
                                  ),
                                  const SizedBox(height: 20),
                                  TextFormField(
                                    controller: _email,
                                    enabled: !_submitting,
                                    keyboardType: TextInputType.emailAddress,
                                    textInputAction: TextInputAction.next,
                                    autofillHints: const [
                                      AutofillHints.username,
                                    ],
                                    autocorrect: false,
                                    onChanged: _clearError,
                                    decoration: _field(
                                      'E-posta',
                                      Icons.mail_outline,
                                    ),
                                    validator: (value) {
                                      final email = value?.trim() ?? '';
                                      return RegExp(
                                            r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
                                          ).hasMatch(email)
                                          ? null
                                          : 'Geçerli bir e-posta adresi girin.';
                                    },
                                  ),
                                  const SizedBox(height: 12),
                                  TextFormField(
                                    controller: _password,
                                    enabled: !_submitting,
                                    obscureText: !_showPassword,
                                    autocorrect: false,
                                    enableSuggestions: false,
                                    textInputAction: TextInputAction.done,
                                    autofillHints: const [
                                      AutofillHints.password,
                                    ],
                                    onChanged: _clearError,
                                    onFieldSubmitted: (_) => _signIn(),
                                    decoration: _field(
                                      'Şifre',
                                      Icons.lock_outline,
                                      suffix: IconButton(
                                        tooltip: _showPassword
                                            ? 'Şifreyi gizle'
                                            : 'Şifreyi göster',
                                        icon: Icon(
                                          _showPassword
                                              ? Icons.visibility_off_outlined
                                              : Icons.visibility_outlined,
                                        ),
                                        onPressed: () => setState(
                                          () => _showPassword = !_showPassword,
                                        ),
                                      ),
                                    ),
                                    validator: (value) =>
                                        value == null || value.isEmpty
                                        ? 'Şifrenizi girin.'
                                        : null,
                                  ),
                                  Align(
                                    alignment: Alignment.centerRight,
                                    child: TextButton(
                                      onPressed: _submitting
                                          ? null
                                          : _forgotPassword,
                                      child: const Text('Şifremi unuttum'),
                                    ),
                                  ),
                                  if (_info case final info?) ...[
                                    const SizedBox(height: 4),
                                    Semantics(
                                      liveRegion: true,
                                      child: Text(
                                        info,
                                        style: const TextStyle(color: teal),
                                      ),
                                    ),
                                  ],
                                  if (_error case final error?) ...[
                                    const SizedBox(height: 12),
                                    Semantics(
                                      liveRegion: true,
                                      child: Text(
                                        error,
                                        style: TextStyle(
                                          color: theme.colorScheme.error,
                                        ),
                                      ),
                                    ),
                                  ],
                                  const SizedBox(height: 20),
                                  FilledButton(
                                    style: FilledButton.styleFrom(
                                      backgroundColor: brandForest,
                                      minimumSize: const Size(0, 52),
                                    ),
                                    onPressed: _submitting ? null : _signIn,
                                    child: _submitting
                                        ? const SizedBox.square(
                                            dimension: 22,
                                            child: CircularProgressIndicator(
                                              strokeWidth: 2,
                                              semanticsLabel: 'Giriş yapılıyor',
                                            ),
                                          )
                                        : const Text(
                                            'Giriş yap',
                                            style: TextStyle(
                                              fontWeight: FontWeight.w700,
                                            ),
                                          ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _BrandHeader extends StatelessWidget {
  const _BrandHeader();

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const DdtsMark(size: 74),
      const SizedBox(height: 18),
      Text(
        'Diyetisyen Danışan\nTakip Sistemi',
        style: Theme.of(context).textTheme.headlineMedium?.copyWith(
          fontWeight: FontWeight.w800,
          letterSpacing: -0.8,
          height: 1.12,
        ),
      ),
      const SizedBox(height: 8),
      const Text(
        'Beslenme planınız, öğünleriniz ve gelişiminiz.',
        style: TextStyle(color: muted, height: 1.4),
      ),
    ],
  );
}
