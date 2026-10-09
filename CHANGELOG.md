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
