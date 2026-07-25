class AuthSession {
  const AuthSession({
    required this.token,
    required this.userName,
    this.email,
  });

  final String token;
  final String userName;
  final String? email;

  Map<String, String?> toJson() => {
    'token': token,
    'userName': userName,
    'email': email,
  };

  static AuthSession? fromJson(Map<String, Object?> json) {
    final token = json['token'] as String?;
    final userName = json['userName'] as String?;
    if (token == null || token.isEmpty || userName == null || userName.isEmpty) {
      return null;
    }

    return AuthSession(
      token: token,
      userName: userName,
      email: json['email'] as String?,
    );
  }
}
