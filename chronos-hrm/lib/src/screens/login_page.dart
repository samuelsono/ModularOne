import 'package:fluent_ui/fluent_ui.dart';

import '../models/auth_session.dart';
import '../services/auth_service.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({
    super.key,
    required this.authService,
    required this.onLoginSuccess,
  });

  final AuthService authService;
  final ValueChanged<AuthSession> onLoginSuccess;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _isSubmitting = false;
  bool _hidePassword = true;
  String? _error;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;

    if (email.isEmpty || password.isEmpty) {
      setState(() {
        _error = 'Enter your email and password.';
      });
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
    });

    try {
      final session = await widget.authService.login(email: email, password: password);
      widget.onLoginSuccess(session);
    } catch (error) {
      setState(() {
        _error = error.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return NavigationView(
      content: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 380),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Chronos HRM',
                    style: FluentTheme.of(context).typography.title,
                  ),
                  const SizedBox(height: 8),
                  const Text('Sign in to continue'),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    InfoBar(
                      title: const Text('Login failed'),
                      content: Text(_error!),
                      severity: InfoBarSeverity.error,
                    ),
                  ],
                  const SizedBox(height: 16),
                  const Text('Email'),
                  const SizedBox(height: 6),
                  TextBox(
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    placeholder: 'you@company.com',
                  ),
                  const SizedBox(height: 12),
                  const Text('Password'),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Expanded(
                        child: TextBox(
                          controller: _passwordController,
                          placeholder: '••••••••',
                          obscureText: _hidePassword,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Button(
                        onPressed: () {
                          setState(() {
                            _hidePassword = !_hidePassword;
                          });
                        },
                        child: Text(_hidePassword ? 'Show' : 'Hide'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton(
                      onPressed: _isSubmitting ? null : _submit,
                      child: _isSubmitting
                          ? const Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                SizedBox(width: 16, height: 16, child: ProgressRing(strokeWidth: 2)),
                                SizedBox(width: 8),
                                Text('Signing in...'),
                              ],
                            )
                          : const Text('Sign in'),
                    ),
                  ),
                  const SizedBox(height: 10),
                  const Text(
                    'API endpoint: --dart-define=API_BASE_URL=http://localhost:5000/api',
                    style: TextStyle(fontSize: 11),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
