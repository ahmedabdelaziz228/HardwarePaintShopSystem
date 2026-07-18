/// Authenticated user data returned by login and session-restore endpoints.
class UserSession {
  const UserSession({
    required this.userName,
    required this.permissions,
  });

  final String userName;
  final Set<String> permissions;

  factory UserSession.fromJson(Map<String, dynamic> json) => UserSession(
        userName: '${json['userName'] ?? ''}',
        permissions: ((json['permissions'] as List?) ?? const [])
            .map((value) => '$value')
            .toSet(),
      );
}
