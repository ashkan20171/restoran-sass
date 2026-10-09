# Ashkan Restaurant — Premium Bilingual Restaurant Website

A multi-page restaurant experience built with **HTML, Sass/CSS and vanilla JavaScript**. The new English-first presentation has a dark culinary palette, responsive navigation, bilingual routes, a menu with filtering and a demo basket, a reservation request modal, and a local rule-based dining assistant.

## Start

Run `python -m http.server 8000` from the project root and open `http://localhost:8000`.

## Pages

- English (default): `/index.html`, `/menu.html`, `/about.html`, `/gallery.html`, `/services.html`, `/contact.html`, `/reservation.html`.
- Persian (RTL): `/fa/index.html` and the matching seven pages. Use **FA / فارسی** or **EN** to switch languages.

## Architecture

- `scss/` contains the original Sass sources.
- `css/main.css` contains the original compiled styling.
- `css/premium.css` contains the new English-first premium theme (editable CSS).
- `js/app.js` is the original Persian page interaction layer.
- `js/premium.js` is the English interaction layer, including demo basket and a **rule-based** FAQ assistant.
- `images/` contains the original project photography.

## Important demo limitations

This is a **static frontend demo**. Reservation and contact forms do not transmit information or create real bookings. The basket is stored only in the browser, and there is no payment checkout. The assistant is a transparent local FAQ/rule-based chatbot, **not a connected AI model**. To add genuine AI, implement a server-side API with secure credentials, moderation, and data-handling controls. Never put API keys in client-side JavaScript.

Prices, addresses, opening hours, reviews, and contact details inherited from the original project are illustrative and require verification before production. Update sample metadata and replace `example.com` URLs before launch. Do not publish unverified testimonials or promotions.

## Next steps for production

Connect reservations to a booking backend, add real inventory and checkout, verify restaurant information, create localized content from a single translation catalog, implement automated accessibility tests and integrate an authenticated AI service.

## Stage 2
Every route loads `js/experience.js` and `css/experience.css` for accessible navigation and clear demo disclosure. The English reservation form validates local dates and basic phone format. These checks are client-side only; a real booking service must validate again on the server.

## Stage 3 — localized menu prices
English pages show sample USD prices; Persian pages show toman prices. Both use the same underlying sample toman amounts. `js/pricing.js` defines an **illustrative** rate of 100,000 toman per USD, **not a live exchange rate**. Set independently approved USD menu prices or integrate a trusted pricing backend before production. The English cart total uses USD; the Persian menu displays toman.
