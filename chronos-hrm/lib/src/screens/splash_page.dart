import 'package:fluent_ui/fluent_ui.dart';

class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return NavigationView(
      content: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: const [
            ProgressRing(),
            SizedBox(height: 16),
            Text('Loading Chronos...'),
          ],
        ),
      ),
    );
  }
}
