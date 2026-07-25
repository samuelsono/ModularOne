import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../models/auth_session.dart';

class AuthService {
  AuthService({http.Client? client}) : _client = client ?? http.Client();

  static const _tokenKey = 'auth.token';
  static const _userNameKey = 'auth.userName';
  static const _emailKey = 'auth.email';

  static const String _baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000/api',
  );

  final http.Client _client;

  Future<AuthSession?> restoreSession() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString(_tokenKey);
    final userName = prefs.getString(_userNameKey);
    if (token == null || userName == null) {
      return null;
    }

    return AuthSession(
      token: token,
      userName: userName,
      email: prefs.getString(_emailKey),
    );
  }

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final uri = Uri.parse('${_baseUrl.replaceAll(RegExp(r'/+$'), '')}/auth/login');
    final response = await _client.post(
      uri,
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({
        'username': email,
        'password': password,
        'rememberMe': false,
      }),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(_buildHttpErrorMessage(response));
    }

    final payload = jsonDecode(response.body);
    if (payload is! Map<String, dynamic>) {
      throw Exception('Unexpected auth response format.');
    }

    final dynamic sessionPayload = payload['session'] is Map<String, dynamic>
        ? payload['session']
        : payload;

    if (payload['requiresTwoFactor'] == true && sessionPayload is! Map<String, dynamic>) {
      throw Exception('This account requires MFA. MFA flow is not implemented in this mobile scaffold yet.');
    }

    if (sessionPayload is! Map<String, dynamic>) {
      throw Exception('Unexpected auth response format.');
    }

    final token = (sessionPayload['accessToken'] ?? sessionPayload['token']) as String?;
    if (token == null || token.isEmpty) {
      throw Exception('Auth token was missing in response.');
    }

    final userPayload = sessionPayload['user'];
    final userMap = userPayload is Map<String, dynamic> ? userPayload : const <String, dynamic>{};

    final userName = (userMap['displayName']
            ?? userMap['username']
            ?? userMap['userName']
            ?? sessionPayload['displayName']
            ?? sessionPayload['userName']
            ?? sessionPayload['username']
            ?? email)
        .toString();

    final session = AuthSession(
      token: token,
      userName: userName,
      email: (userMap['email'] ?? sessionPayload['email'])?.toString() ?? email,
    );

    await _persistSession(session);
    return session;
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_userNameKey);
    await prefs.remove(_emailKey);
  }

  Future<void> _persistSession(AuthSession session) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, session.token);
    await prefs.setString(_userNameKey, session.userName);
    if (session.email != null) {
      await prefs.setString(_emailKey, session.email!);
    } else {
      await prefs.remove(_emailKey);
    }
  }

  String _buildHttpErrorMessage(http.Response response) {
    final statusText = 'Login failed (${response.statusCode}).';

    try {
      final payload = jsonDecode(response.body);
      if (payload is Map<String, dynamic>) {
        final detail = payload['detail']?.toString();
        if (detail != null && detail.isNotEmpty) {
          return '$statusText $detail';
        }

        final errors = payload['errors'];
        if (errors is Map<String, dynamic>) {
          final messages = <String>[];
          for (final entry in errors.entries) {
            final value = entry.value;
            if (value is List) {
              messages.addAll(value.map((item) => item.toString()));
            } else if (value != null) {
              messages.add(value.toString());
            }
          }
          if (messages.isNotEmpty) {
            return '$statusText ${messages.join(' ')}';
          }
        }
      }
    } catch (_) {
      // Fall through to generic message when response is not JSON.
    }

    return '$statusText Check your credentials and API endpoint.';
  }
}
