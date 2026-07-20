# System Wishlist — HCM Platform

Recommended direction: grow Chronos as a modular HCM platform of independent modules that share auth, org directory, notifications, and auditing.

## Active planning (July 2026)

CarTrack-scoped implementation plans (preferred over enterprise wishlists for delivery):

| Module | Plan |
|--------|------|
| Performance, Recruitment, Payroll (overview) | [hcm-modules-overview.md](./hcm-modules-overview.md) |
| Performance Management | [performance-module-implementation-plan.md](./performance-module-implementation-plan.md) |
| Recruitment (ATS) | [recruitment-module-implementation-plan.md](./recruitment-module-implementation-plan.md) |
| Payroll | [payroll-module-implementation-plan.md](./payroll-module-implementation-plan.md) |
| Tender Search | [tender-search-module-implementation-plan.md](./tender-search-module-implementation-plan.md) |

Related capability wishlists / older plans: [payroll-management.md](./payroll-management.md), [leave-management.md](./leave-management.md), [leave-module-implementation-plan.md](./leave-module-implementation-plan.md).

## Desired module set

- Core HR (Companies, Employees, Departments, Positions)
- Leave Management
- Attendance & Time Tracking
- Payroll Management
- Recruitment & Applicant Tracking
- Performance Management
- Tender Search (URL sources + keyword scrape + optional schedule)
- Training & Learning Management
- Asset Management
- Employee Self-Service Portal
- Manager Self-Service Portal
- HR Administration Portal
- Reporting & Analytics
- Workflow & Approval Engine
- Document Management
- Notification Service
- API & Integration Layer

This modular architecture allows each subsystem to evolve independently while sharing common services such as authentication, employee records, workflows, notifications, and auditing.