# PR-042 — i18n phase 1: Transloco base and the storefront in Spanish

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/i18n-web-es` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
Every text of the web apps was written in English inside the templates, dates used fixed English formats and there
was no way to add a language. The store sells in Honduras (`LPS.`), so Spanish has to be the main language, with
English kept as the fallback and room for more languages.

## Changes
- **Transloco** (`@jsverse/transloco` 8.4) with the translation files **bundled** in the app
  (`frontend/shared/i18n/es.json`, `en.json`): one build, no extra request, no flash of untranslated keys, and it fits
  the Vercel deployment. Keys are in English (`orders.status.*` style: `status.paid`, `cart.items.one`), Spanish is
  the default language and English the fallback.
- `provideI18n()` (shared) registers Transloco, `LOCALE_ID` (`es-HN`) and an app initializer that loads the stored or
  default language **before the first render** and sets `<html lang>`.
- `I18n` service: active language as a signal, `use(lang)` (persisted in `localStorage` key `ch_lang`), `t()`,
  `tOr()` and `problem()` (the server's own message when it sent one, otherwise a translated fallback).
- `LocalDatePipe` (`localDate`) and `PluralPipe` (`plural`) replace Angular's `DatePipe` and the hand-written
  `n === 1 ? 'item' : 'items'`; both follow the active language. `LanguageSwitcher` (ES | EN) sits in the store header.
- Every text of the storefront moved to the files: header, footer, catalog, product page, cart, checkout, orders,
  order detail, tracking, notifications, account, login and register, including messages set from code.
- Shared `StatusBadge` and `Timeline` are translated (`status.*`, an unknown status still shows readable English).
- **Admin:** installs the same base (`provideI18n({ defaultLang: 'en' })`) so the shared badge and timeline work
  there; its own pages stay in English until PR-049.
- `index.html`: `lang="es"` and a plain title.

## Things to know
- A new language = one JSON file, one entry in `LANGS`, one loader in `i18n.ts`.
- Texts the **server** sends are not translated yet: validation messages, and the history notes of an order
  (`Payment captured`, `Order created`). That is PR-049 (stable error codes / `Accept-Language`).
- The cancellation reason sent to the API (`Cancelled by customer`) is data stored by the server, not UI text.
- Prices keep `LPS. 1,234.50` in both languages (decided in PR-040); only dates and words change.
- `status.*` is a single namespace shared by orders, payments and shipments because one badge renders all of them.

## Verification
- `ng test` web 45/45 (new: key parity es/en, same `{{params}}`, no empty texts, every known status translated,
  runtime switch, persistence, plural, dates, server message vs fallback) and admin 6/6.
- `ng build` web and admin complete with no errors (the only warning is the existing `product-detail` style budget).
- A script that scans the templates confirmed every key used in code exists in both files.
- In the built-in browser against the real local API (HNL data), signed in as the seed customer: catalog, product,
  cart, checkout, orders, order detail, account, notifications and tracking read in Spanish; the ES/EN toggle changes
  the order page text and dates live and `<html lang>` follows; header checked at 1280, 768 and 375 px.
- **Not verified:** Playwright e2e (updated to the Spanish texts but not run); the admin pages in a browser; a
  first visit with no stored language in a browser with an English system language (default is Spanish by design);
  screen-reader output of the translated `aria-label`s; the checkout confirmation after placing a real order.
