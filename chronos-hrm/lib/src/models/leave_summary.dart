class LeaveSummary {
  const LeaveSummary({
    required this.category,
    required this.total,
    required this.used,
    required this.pending,
  });

  final String category;
  final double total;
  final double used;
  final double pending;

  double get remaining => total - used - pending;
}
