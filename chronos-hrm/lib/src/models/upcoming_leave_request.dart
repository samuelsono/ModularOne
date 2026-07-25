class UpcomingLeaveRequest {
  const UpcomingLeaveRequest({
    required this.date,
    required this.type,
    required this.status,
  });

  final String date;
  final String type;
  final String status;
}
