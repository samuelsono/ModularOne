import 'package:fluent_ui/fluent_ui.dart';

import 'models/auth_session.dart';
import 'screens/leave_dashboard_page.dart';
import 'screens/login_page.dart';
import 'screens/splash_page.dart';
import 'services/auth_service.dart';

class ChronosHrmApp extends StatefulWidget {
  const ChronosHrmApp({super.key});

  @override
  State<ChronosHrmApp> createState() => _ChronosHrmAppState();
}

class _ChronosHrmAppState extends State<ChronosHrmApp> {
  final AuthService _authService = AuthService();

  bool _isBootstrapping = true;
  bool _isDarkMode = false;
  AuthSession? _session;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final session = await _authService.restoreSession();
    if (!mounted) {
      return;
    }

    setState(() {
      _session = session;
      _isBootstrapping = false;
    });
  }

  Future<void> _logout() async {
    await _authService.logout();
    if (!mounted) {
      return;
    }

    setState(() {
      _session = null;
    });
  }

  void _toggleThemeMode() {
    setState(() {
      _isDarkMode = !_isDarkMode;
    });
  }

  @override
  Widget build(BuildContext context) {
    return FluentApp(
      title: 'Chronos HRM',
      debugShowCheckedModeBanner: false,
      themeMode: _isDarkMode ? ThemeMode.dark : ThemeMode.light,
      theme: FluentThemeData(
        brightness: Brightness.light,
        accentColor: Colors.blue,
      ),
      darkTheme: FluentThemeData(
        brightness: Brightness.dark,
        accentColor: Colors.blue,
      ),
      home: _isBootstrapping
          ? const SplashPage()
          : _session == null
              ? LoginPage(
                  authService: _authService,
                  onLoginSuccess: (session) {
                    setState(() {
                      _session = session;
                    });
                  },
                )
              : LeaveDashboardPage(
                  session: _session!,
                  onLogout: _logout,
                  isDarkMode: _isDarkMode,
                  onToggleThemeMode: _toggleThemeMode,
                ),
    );
  }
}
