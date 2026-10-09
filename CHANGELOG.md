## Stage 15
- Audit log coverage for menu create/update and stock adjustments.
- Admin-only bilingual inventory CSV export with formula injection protection.
- Staff dashboard export shortcut.

## Stage 10
- Added admin-only 30-day operations analytics endpoint.
- Added bilingual analytics panel with order volume bars and separate completed-order currency totals.
- Corrected sales-summary reporting to exclude incomplete orders.

# Changelog

## 2.0.0
- بازطراحی کامل رابط کاربری و تجربه کاربری
- افزودن صفحات مستقل و navigation
- افزودن منوی قابل فیلتر، سبد محلی، رزرو، چت‌بات، فرم تماس
- افزودن SEO فنی و structured data
- حذف وابستگی‌های خارجی Font Awesome/Google Fonts
- بهبود responsive و accessibility

## Stage 2 — quality and accessibility
- Correct local-date validation in reservation modal, including a future-date check.
- Validate phone numbers client-side and preserve explicit demo-only status.
- Harden basket storage for restricted browsers and cap demo cart size.
- Fix greeting keyword matching and cap chat message length.
- Add bilingual back-to-top control, keyboard focus styles, reduced-motion support and demo disclosure to all 14 routes.
- Keep credentials out of the frontend; AI and real bookings still require a backend.

### Stage 3
- Added bilingual USD/toman formatting for menu prices and English cart totals.
- Centralized configurable demonstration pricing logic in `js/pricing.js`.
- Clarified that conversion is illustrative, not real-time FX.

## Stage 6
- Same-origin ASP.NET Core static hosting and API-connected English/Persian reservations and assistant.
- Read-only staff dashboard and staff sign-in.
- Removed unprotected admin mutation endpoints pending CSRF protections.
- Explicit demo/live distinction and setup instructions.

## Stage 12
- Admin-only team provisioning and role assignment endpoints; bilingual staff management interface.

## Stage 13
- Operational Kitchen/Reception authorization policies and administrator-only order audit trail.

## Stage 14
- Role-aware staff workspace and restored missing dashboard scripts.
- Reception authorization and reservation audit entries.
