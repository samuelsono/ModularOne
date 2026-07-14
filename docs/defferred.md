1. Multi-step approval

Add a workflow beyond single manager approval — e.g. HR second step or finance sign-off. Needs a workflow/approval-steps model.

2. Accrual engine

Automatically accrue leave over time (monthly or on work anniversary) via a background job (IHostedService), instead of manual/annual allocation only.

3. Half-day / hourly leave

Let employees book part of a day (morning/afternoon or hours). Needs time-of-day on requests; AllowHalfDay already exists on leave types.

4. Payroll export
Export approved leave in a payroll-ready format (fixed-width file or direct API) for salary/payroll systems.

5. Outlook / Google Calendar sync
Push approved leave to external calendars so it shows in Outlook/Google, not just the in-app team calendar.

6. Mobile app
Native or PWA mobile experience for applying leave, approvals, and balances on the go.

7. Multi-company tenancy

Support multiple companies/branches as separate tenants — would be a platform-wide architecture change, not leave-only.