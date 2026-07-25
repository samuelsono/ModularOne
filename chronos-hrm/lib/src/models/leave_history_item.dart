class LeaveHistoryItem {
  const LeaveHistoryItem({
    required this.id,
    required this.type,
    required this.status,
    required this.startDate,
    required this.endDate,
    required this.workingDays,
    this.notes,
    this.createdAt,
  });

  final String id;
  final String type;
  final String status;
  final String startDate;
  final String endDate;
  final double workingDays;
  final String? notes;
  final String? createdAt;
}
