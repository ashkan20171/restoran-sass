# Ashkan Restaurant — Stage 4

This is a **static browser-only demonstration**, not a production booking or ordering system.

- Default English LTR and Persian RTL under `/fa/`.
- Independent USD and toman price overrides via `admin-demo.html`, stored in localStorage.
- Initial sample USD prices are derived from the former illustrative rate (100,000 toman/USD) solely as seed values; override them independently for real menus.
- English reservation requests saved in localStorage only and shown in the local demo operations page; **no confirmation, backend, email, or database**.
- English basket totals now use USD line-item prices; previous basket entries are migrated by display fallback.
- Rule-based chatbot remains a demo and is not connected to an AI API.

## Production requirements

Add an authenticated server API, database, role-based access, server-side validation, consent/privacy policy, real menu data and verified prices, secure reservation confirmation workflow, and a server-side AI proxy with protected API credentials. Never put AI API keys in browser JavaScript.

Run `python -m http.server 8000` from the project folder, then open `http://localhost:8000`.
