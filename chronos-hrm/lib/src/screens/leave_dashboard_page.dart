import 'package:fluent_ui/fluent_ui.dart';
import 'package:flutter/material.dart' as material;

import '../models/auth_session.dart';
import '../models/leave_history_item.dart';
import '../models/leave_summary.dart';
import '../models/upcoming_leave_request.dart';
import '../services/leave_dashboard_service.dart';

class LeaveDashboardPage extends StatefulWidget {
  const LeaveDashboardPage({
    super.key,
    required this.session,
    required this.onLogout,
    required this.isDarkMode,
    required this.onToggleThemeMode,
  });

  final AuthSession session;
  final VoidCallback onLogout;
  final bool isDarkMode;
  final VoidCallback onToggleThemeMode;

  @override
  State<LeaveDashboardPage> createState() => _LeaveDashboardPageState();
}

enum _DashboardSection {
  home,
  balances,
  history,
  settings,
}

class _LeaveDashboardPageState extends State<LeaveDashboardPage> {
  _DashboardSection _activeSection = _DashboardSection.home;
  bool _isDrawerOpen = false;
  final LeaveDashboardService _dashboardService = LeaveDashboardService();

  bool _isDashboardLoading = true;
  String? _dashboardError;
  List<LeaveSummary> _leaveBalances = const [];
  List<UpcomingLeaveRequest> _upcomingRequests = const [];
  List<LeaveHistoryItem> _leaveHistory = const [];

  @override
  void initState() {
    super.initState();
    _loadDashboardData();
  }

  Future<void> _loadDashboardData() async {
    setState(() {
      _isDashboardLoading = true;
      _dashboardError = null;
    });

    try {
      final data = await _dashboardService.loadDashboardData(token: widget.session.token);
      if (!mounted) {
        return;
      }

      setState(() {
        _leaveBalances = data.balances;
        _upcomingRequests = data.upcomingRequests;
        _leaveHistory = data.history;
        _isDashboardLoading = false;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _dashboardError = error.toString().replaceFirst('Exception: ', '');
        _isDashboardLoading = false;
      });
    }
  }

  void _showBalanceDetails(LeaveSummary item) {
    showDialog<void>(
      context: context,
      builder: (dialogContext) => ContentDialog(
        title: Text(item.category),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Balance: ${item.total.toStringAsFixed(1)} days'),
            const SizedBox(height: 6),
            Text('Used: ${item.used.toStringAsFixed(1)} days'),
            const SizedBox(height: 6),
            Text('Pending: ${item.pending.toStringAsFixed(1)} days'),
            const SizedBox(height: 6),
            Text('Remaining: ${item.remaining.toStringAsFixed(1)} days'),
          ],
        ),
        actions: [
          Button(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  void _showHistoryDetails(LeaveHistoryItem item) {
    showDialog<void>(
      context: context,
      builder: (dialogContext) => ContentDialog(
        title: const Text('Leave Request Details'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Type: ${item.type}'),
            const SizedBox(height: 6),
            Text('Status: ${item.status}'),
            const SizedBox(height: 6),
            Text('Start: ${item.startDate}'),
            const SizedBox(height: 6),
            Text('End: ${item.endDate}'),
            const SizedBox(height: 6),
            Text('Working days: ${item.workingDays.toStringAsFixed(1)}'),
            const SizedBox(height: 6),
            Text('Notes: ${item.notes?.trim().isNotEmpty == true ? item.notes : 'No notes'}'),
            if (item.createdAt != null && item.createdAt!.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text('Created: ${item.createdAt}'),
            ],
          ],
        ),
        actions: [
          Button(
            onPressed: () => Navigator.of(dialogContext).pop(),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  void _toggleDrawer() {
    setState(() {
      _isDrawerOpen = !_isDrawerOpen;
    });
  }

  void _closeDrawer() {
    if (!_isDrawerOpen) {
      return;
    }

    setState(() {
      _isDrawerOpen = false;
    });
  }

  void _selectSection(_DashboardSection section) {
    setState(() {
      _activeSection = section;
      _isDrawerOpen = false;
    });
  }

  Color _personaColorFromName() {
    const palette = [
      Color(0xFFF7630C),
      Color(0xFFE3008C),
      Color(0xFF8764B8),
      Color(0xFF00B7C3),
      Color(0xFF10893E),
      Color(0xFF0063B1),
      Color(0xFFD83B01),
      Color(0xFF5C2E91),
    ];

    final source = widget.session.userName.trim();
    final hash = source.isEmpty
        ? 0
        : source.runes.fold<int>(0, (value, code) => (value * 31 + code) & 0x7fffffff);
    return palette[hash % palette.length];
  }

  String _initialsFromName(String name) {
    final parts = name.trim().split(RegExp(r'\s+')).where((part) => part.isNotEmpty).toList();
    if (parts.isEmpty) {
      return 'U';
    }
    if (parts.length == 1) {
      return parts.first.substring(0, 1).toUpperCase();
    }
    return '${parts.first.substring(0, 1)}${parts.last.substring(0, 1)}'.toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return NavigationView(
      content: SafeArea(
        child: ScaffoldPage(
          content: Stack(
            children: [
              Column(
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 0, vertical: 12),
                    decoration: BoxDecoration(
                      border: Border(
                        bottom: BorderSide(color: theme.resources.cardStrokeColorDefaultSolid),
                      ),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Padding(
                            padding: const EdgeInsets.only(left: 12),
                            child: Row(
                              children: [
                                GestureDetector(
                                  onTap: _toggleDrawer,
                                  child: Container(
                                    width: 36,
                                    height: 36,
                                    alignment: Alignment.center,
                                    decoration: BoxDecoration(
                                      color: _personaColorFromName(),
                                      borderRadius: BorderRadius.circular(999),
                                    ),
                                    child: Text(
                                      _initialsFromName(widget.session.userName),
                                      style: const TextStyle(fontWeight: FontWeight.w700, color: Colors.white),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Text(widget.session.userName, style: theme.typography.bodyStrong),
                                      const SizedBox(height: 2),
                                      Text(
                                        widget.session.email?.trim().isNotEmpty == true
                                            ? widget.session.email!
                                            : 'No email available',
                                        overflow: TextOverflow.ellipsis,
                                        style: theme.typography.caption,
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        IconButton(
                          icon: Icon(widget.isDarkMode ? FluentIcons.clear_night : FluentIcons.sunny),
                          onPressed: widget.onToggleThemeMode,
                        ),
                        const SizedBox(width: 8),
                      ],
                    ),
                  ),
                  Expanded(
                    child: switch (_activeSection) {
                      _DashboardSection.home => _DashboardHome(
                          isLoading: _isDashboardLoading,
                          error: _dashboardError,
                          leaveBalances: _leaveBalances,
                          upcomingRequests: _upcomingRequests,
                          onRefresh: _loadDashboardData,
                        ),
                      _DashboardSection.balances => _BalancesSection(
                          isLoading: _isDashboardLoading,
                          error: _dashboardError,
                          leaveBalances: _leaveBalances,
                          onRefresh: _loadDashboardData,
                          onOpenDetails: _showBalanceDetails,
                        ),
                      _DashboardSection.history => _LeaveHistorySection(
                          isLoading: _isDashboardLoading,
                          error: _dashboardError,
                          leaveHistory: _leaveHistory,
                          onRefresh: _loadDashboardData,
                          onOpenDetails: _showHistoryDetails,
                        ),
                      _DashboardSection.settings => ListView(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          children: [
                            Text('Settings', style: theme.typography.subtitle),
                            const SizedBox(height: 12),
                            const _ShadowCard(
                              child: Padding(
                                padding: EdgeInsets.all(14),
                                child: Text('Settings screen placeholder.'),
                              ),
                            ),
                            const SizedBox(height: 12),
                            Button(
                              onPressed: widget.onLogout,
                              child: const Text('Logout'),
                            ),
                          ],
                        ),
                    },
                  ),
                  Container(
                    padding: const EdgeInsets.fromLTRB(8, 8, 8, 10),
                    decoration: BoxDecoration(
                      color: theme.resources.cardBackgroundFillColorDefault,
                      border: Border(
                        top: BorderSide(color: theme.resources.cardStrokeColorDefaultSolid),
                      ),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: _BottomNavButton(
                            label: 'Home',
                            icon: FluentIcons.home,
                            selected: _activeSection == _DashboardSection.home,
                            onPressed: () => _selectSection(_DashboardSection.home),
                          ),
                        ),
                        Expanded(
                          child: _BottomNavButton(
                            label: 'Settings',
                            icon: FluentIcons.settings,
                            selected: _activeSection == _DashboardSection.settings,
                            onPressed: () => _selectSection(_DashboardSection.settings),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              IgnorePointer(
                ignoring: !_isDrawerOpen,
                child: AnimatedOpacity(
                  duration: const Duration(milliseconds: 220),
                  curve: Curves.easeOut,
                  opacity: _isDrawerOpen ? 1 : 0,
                  child: GestureDetector(
                    onTap: _closeDrawer,
                    child: Container(color: Colors.black.withValues(alpha: 0.35)),
                  ),
                ),
              ),
              AnimatedPositioned(
                duration: const Duration(milliseconds: 240),
                curve: Curves.easeOutCubic,
                left: _isDrawerOpen ? 0 : -280,
                top: 0,
                bottom: 0,
                width: 280,
                child: Container(
                  color: theme.resources.cardBackgroundFillColorDefault.withValues(alpha: 1),
                  padding: const EdgeInsets.fromLTRB(0, 20, 0, 14),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Padding(
                        padding: const EdgeInsets.only(left: 12, bottom: 10),
                        child: Text('Menu', style: theme.typography.subtitle),
                      ),
                      _SidebarMenuItem(
                        label: 'Home',
                        icon: FluentIcons.home,
                        selected: _activeSection == _DashboardSection.home,
                        onTap: () => _selectSection(_DashboardSection.home),
                      ),
                      _SidebarMenuItem(
                        label: 'Balances',
                        icon: FluentIcons.bulleted_list,
                        selected: _activeSection == _DashboardSection.balances,
                        onTap: () => _selectSection(_DashboardSection.balances),
                      ),
                      _SidebarMenuItem(
                        label: 'Leave History',
                        icon: FluentIcons.history,
                        selected: _activeSection == _DashboardSection.history,
                        onTap: () => _selectSection(_DashboardSection.history),
                      ),
                      _SidebarMenuItem(
                        label: 'Settings',
                        icon: FluentIcons.settings,
                        selected: _activeSection == _DashboardSection.settings,
                        onTap: () => _selectSection(_DashboardSection.settings),
                      ),
                      _SidebarMenuItem(
                        label: 'Logout',
                        icon: FluentIcons.sign_out,
                        selected: false,
                        onTap: () {
                          _closeDrawer();
                          widget.onLogout();
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _DashboardHome extends StatelessWidget {
  const _DashboardHome({
    required this.isLoading,
    required this.error,
    required this.leaveBalances,
    required this.upcomingRequests,
    required this.onRefresh,
  });

  final bool isLoading;
  final String? error;
  final List<LeaveSummary> leaveBalances;
  final List<UpcomingLeaveRequest> upcomingRequests;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    if (isLoading) {
      return const Center(
        child: ProgressRing(),
      );
    }

    if (error != null) {
      return ListView(
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          const SizedBox(height: 2),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Leave Dashboard', style: theme.typography.subtitle?.copyWith(
              color: theme.typography.body?.color?.withValues(alpha: 0.82),
              fontSize: 18,
            )),
          ),
          const SizedBox(height: 2),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text(
              'Balances and upcoming requests',
              style: theme.typography.caption?.copyWith(
                color: theme.typography.body?.color?.withValues(alpha: 0.82),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Unable to load dashboard', style: theme.typography.subtitle),
          ),
          const SizedBox(height: 8),
          Text(error!, style: theme.typography.body),
          const SizedBox(height: 12),
          Button(
            onPressed: () => onRefresh(),
            child: const Text('Retry'),
          ),
        ],
      );
    }

    return material.RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          const SizedBox(height: 2),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Leave Dashboard', style: theme.typography.bodyStrong),
          ),
          const SizedBox(height: 2),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text(
              'Balances and upcoming requests',
              style: theme.typography.caption?.copyWith(
                color: theme.typography.caption?.color?.withValues(alpha: 0.82),
              ),
            ),
          ),
          const SizedBox(height: 14),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text(
              'Your Leave Balances',
              style: theme.typography.subtitle,
            ),
          ),
          const SizedBox(height: 12),
          if (leaveBalances.isEmpty)
            const Text('No leave balances found.')
          else
            ...leaveBalances.take(3).map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: _LeaveBalanceCard(item: item),
              ),
            ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text(
              'Upcoming Requests',
              style: theme.typography.subtitle,
            ),
          ),
          const SizedBox(height: 12),
          _ShadowCard(
            child: Column(
              children: upcomingRequests.isEmpty
                  ? const [
                      Padding(
                        padding: EdgeInsets.all(14),
                        child: Text('No upcoming leave requests.'),
                      ),
                    ]
                  : upcomingRequests
                      .map(
                        (request) => ListTile.selectable(
                          title: Text('${request.type} · ${request.date}'),
                          subtitle: Text('Status: ${request.status}'),
                        ),
                      )
                      .toList(growable: false),
            ),
          ),
        ],
      ),
    );
  }
}

class _BalancesSection extends StatelessWidget {
  const _BalancesSection({
    required this.isLoading,
    required this.error,
    required this.leaveBalances,
    required this.onRefresh,
    required this.onOpenDetails,
  });

  final bool isLoading;
  final String? error;
  final List<LeaveSummary> leaveBalances;
  final Future<void> Function() onRefresh;
  final ValueChanged<LeaveSummary> onOpenDetails;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    if (isLoading) {
      return const Center(child: ProgressRing());
    }

    if (error != null) {
      return ListView(
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Balances', style: theme.typography.subtitle),
          ),
          const SizedBox(height: 8),
          Text(error!, style: theme.typography.body),
          const SizedBox(height: 12),
          Button(onPressed: () => onRefresh(), child: const Text('Retry')),
        ],
      );
    }

    return material.RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Balances', style: theme.typography.subtitle),
          ),
          const SizedBox(height: 12),
          if (leaveBalances.isEmpty)
            const Text('No leave balances found.')
          else
            _ShadowCard(
              child: Column(
                children: leaveBalances
                    .map(
                      (item) => ListTile.selectable(
                        title: Text(item.category),
                        subtitle: Text('Remaining: ${item.remaining.toStringAsFixed(1)} days'),
                        trailing: const Icon(FluentIcons.chevron_right),
                        onPressed: () => onOpenDetails(item),
                      ),
                    )
                    .toList(growable: false),
              ),
            ),
        ],
      ),
    );
  }
}

class _LeaveHistorySection extends StatelessWidget {
  const _LeaveHistorySection({
    required this.isLoading,
    required this.error,
    required this.leaveHistory,
    required this.onRefresh,
    required this.onOpenDetails,
  });

  final bool isLoading;
  final String? error;
  final List<LeaveHistoryItem> leaveHistory;
  final Future<void> Function() onRefresh;
  final ValueChanged<LeaveHistoryItem> onOpenDetails;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    if (isLoading) {
      return const Center(child: ProgressRing());
    }

    if (error != null) {
      return ListView(
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Leave History', style: theme.typography.subtitle),
          ),
          const SizedBox(height: 8),
          Text(error!, style: theme.typography.body),
          const SizedBox(height: 12),
          Button(onPressed: () => onRefresh(), child: const Text('Retry')),
        ],
      );
    }

    return material.RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(vertical: 16),
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Leave History', style: theme.typography.subtitle),
          ),
          const SizedBox(height: 12),
          if (leaveHistory.isEmpty)
            const Text('No leave history found.')
          else
            _ShadowCard(
              child: Column(
                children: leaveHistory
                    .map(
                      (item) => ListTile.selectable(
                        title: Text('${item.type} · ${item.startDate}'),
                        subtitle: Text('Status: ${item.status}'),
                        trailing: const Icon(FluentIcons.chevron_right),
                        onPressed: () => onOpenDetails(item),
                      ),
                    )
                    .toList(growable: false),
              ),
            ),
        ],
      ),
    );
  }
}

class _LeaveBalanceCard extends StatelessWidget {
  const _LeaveBalanceCard({required this.item});

  final LeaveSummary item;

  @override
  Widget build(BuildContext context) {
    return _ShadowCard(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${item.category} Leave', style: FluentTheme.of(context).typography.bodyStrong),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: _LeaveMetricCard(
                          value: item.total.toStringAsFixed(1),
                          label: 'Balance',
                          icon: FluentIcons.home,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _LeaveMetricCard(
                          value: item.used.toStringAsFixed(1),
                          label: 'Used',
                          icon: FluentIcons.history,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _LeaveMetricCard(
                          value: item.pending.toStringAsFixed(1),
                          label: 'Pending',
                          icon: FluentIcons.clock,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: 18),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.blue.normal,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(item.remaining.toStringAsFixed(1), style: const TextStyle(fontWeight: FontWeight.w700, color: Colors.white)),
            ),
          ],
        ),
      ),
    );
  }
}

class _LeaveMetricCard extends StatelessWidget {
  const _LeaveMetricCard({
    required this.value,
    required this.label,
    required this.icon,
  });

  final String value;
  final String label;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
      decoration: BoxDecoration(
        color: theme.resources.subtleFillColorSecondary,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  value,
                  style: theme.typography.subtitle?.copyWith(fontWeight: FontWeight.w300),
                ),
              ),
              Icon(icon, size: 18, color: theme.typography.body?.color?.withValues(alpha: 0.72)),
            ],
          ),
          const SizedBox(height: 4),
          Text(label, style: theme.typography.caption),
        ],
      ),
    );
  }
}

class _ShadowCard extends StatelessWidget {
  const _ShadowCard({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Card(child: child);
  }
}

class _SidebarMenuItem extends StatelessWidget {
  const _SidebarMenuItem({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(horizontal: 0, vertical: 0),
        color: selected ? theme.resources.subtleFillColorSecondary : Colors.transparent,
        child: ListTile.selectable(
          contentPadding: const EdgeInsets.symmetric(horizontal: 0, vertical: 0),
          leading: Icon(icon, size: 18),
          title: Text(label),
          onPressed: onTap,
        ),
      ),
    );
  }
}

class _BottomNavButton extends StatelessWidget {
  const _BottomNavButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);
    final activeColor = theme.typography.bodyStrong?.color ?? const Color(0xFF323130);
    final inactiveColor = theme.typography.body?.color?.withValues(alpha: 0.72) ?? const Color(0xFF8A8886);

    return GestureDetector(
      onTap: onPressed,
      behavior: HitTestBehavior.opaque,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        curve: Curves.easeOut,
        padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 6),
        decoration: BoxDecoration(
          color: selected ? theme.resources.subtleFillColorSecondary : Colors.transparent,
          borderRadius: BorderRadius.circular(10),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 18, color: selected ? activeColor : inactiveColor),
            const SizedBox(height: 4),
            Text(
              label,
              style: TextStyle(
                fontSize: 12,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                color: selected ? activeColor : inactiveColor,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
