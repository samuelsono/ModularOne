import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/leave_history_item.dart';
import '../models/leave_summary.dart';
import '../models/upcoming_leave_request.dart';

class LeaveDashboardData {
  const LeaveDashboardData({
    required this.balances,
    required this.upcomingRequests,
    required this.history,
  });

  final List<LeaveSummary> balances;
  final List<UpcomingLeaveRequest> upcomingRequests;
  final List<LeaveHistoryItem> history;
}

class LeaveDashboardService {
  LeaveDashboardService({http.Client? client}) : _client = client ?? http.Client();

  static const String _baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000/api',
  );

  final http.Client _client;

  Future<LeaveDashboardData> loadDashboardData({required String token}) async {
    final balances = await _getLeaveBalances(token: token);
    final history = await _getLeaveHistory(token: token);
    final requests = _toUpcomingRequests(history);

    return LeaveDashboardData(
      balances: balances,
      upcomingRequests: requests,
      history: history,
    );
  }

  Future<List<LeaveSummary>> _getLeaveBalances({required String token}) async {
    final uri = Uri.parse('${_baseUrl.replaceAll(RegExp(r'/+$'), '')}/leave/balances');

    final response = await _client.get(
      uri,
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('Failed to load leave balances (${response.statusCode}).');
    }

    final payload = jsonDecode(response.body);
    if (payload is! List) {
      throw Exception('Unexpected leave balances response format.');
    }

    return payload
        .whereType<Map<String, dynamic>>()
        .map((item) {
          final category = (item['leaveTypeName'] ?? 'Leave').toString();
          return LeaveSummary(
            category: category,
            total: _toDouble(item['allocated']),
            used: _toDouble(item['used']),
            pending: _toDouble(item['pending']),
          );
        })
        .toList(growable: false);
  }

  Future<List<LeaveHistoryItem>> _getLeaveHistory({required String token}) async {
    final uri = Uri.parse('${_baseUrl.replaceAll(RegExp(r'/+$'), '')}/leave/requests');

    final response = await _client.get(
      uri,
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('Failed to load leave requests (${response.statusCode}).');
    }

    final payload = jsonDecode(response.body);
    if (payload is! List) {
      throw Exception('Unexpected leave requests response format.');
    }

    final rows = payload.whereType<Map<String, dynamic>>().map((item) {
      return LeaveHistoryItem(
        id: (item['id'] ?? '').toString(),
        type: (item['leaveType'] ?? 'Leave').toString(),
        status: (item['status'] ?? 'Pending').toString(),
        startDate: (item['startDate'] ?? '').toString(),
        endDate: (item['endDate'] ?? '').toString(),
        workingDays: _toDouble(item['workingDays']),
        notes: item['notes']?.toString(),
        createdAt: item['createdAt']?.toString(),
      );
    }).where((row) => row.id.isNotEmpty).toList(growable: false);

    rows.sort((left, right) {
      final leftDate = DateTime.tryParse(left.startDate);
      final rightDate = DateTime.tryParse(right.startDate);
      if (leftDate == null && rightDate == null) {
        return 0;
      }
      if (leftDate == null) {
        return 1;
      }
      if (rightDate == null) {
        return -1;
      }
      return rightDate.compareTo(leftDate);
    });

    return rows;
  }

  List<UpcomingLeaveRequest> _toUpcomingRequests(List<LeaveHistoryItem> history) {
    final today = DateTime.now();
    final todayOnly = DateTime(today.year, today.month, today.day);

    final filtered = history.where((row) {
      final parsed = DateTime.tryParse(row.startDate);
      if (parsed == null) {
        return false;
      }
      final day = DateTime(parsed.year, parsed.month, parsed.day);
      return !day.isBefore(todayOnly);
    }).toList(growable: false);

    filtered.sort((left, right) => left.startDate.compareTo(right.startDate));
    return filtered
        .take(5)
        .map((row) => UpcomingLeaveRequest(
              date: row.startDate,
              type: row.type,
              status: row.status,
            ))
        .toList(growable: false);
  }

  static double _toDouble(Object? value) {
    if (value is num) {
      return value.toDouble();
    }
    if (value is String) {
      return double.tryParse(value) ?? 0;
    }
    return 0;
  }
}
