# PR-049 — The product is now called "Catrashop"

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/rename-catrashop` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Problem
The owner found "CommerceHub" odd ("Hub") and wants a name that tells a recruiter, at a glance, that this is an
online store and that has a local flavour. After two rounds of options (some tied to the space theme of the icon,
which they did not want) they chose **Catrashop** ("catracho", the Honduran demonym, + "shop").

## Changes — what people see
- **Store:** the header and footer wordmark is **Catra[shop]** (lime pill on "shop", as "Hub" had), the tab title and the
  Spanish/English welcome line of the sign-in page.
- **Admin:** the sign-in wordmark, the sidebar brand ("Catrashop ADMIN"), the tab title and the title of the manual
  notification a customer receives ("A message from Catrashop").
- **Mobile app:** the Android label and the iOS display name ("Catrashop Admin"), the biometric prompt on both platforms
  and the app title.
- **E-mails:** the welcome notification created by the API ("Welcome to Catrashop", the promotions text) and the text a
  customer reads in the three n8n e-mails (welcome, receipt, review request).
- **Docs:** the README titles of the project, the mobile app and the frontend, with a note that **CommerceHub is the
  internal code name**.

## Deliberately not changed
- Code identifiers: namespaces and projects (`CommerceHub.*`), Angular project names (`commercehub-web`,
  `commercehub-admin`), Flutter package and class names, database/schema names, the repository, config keys, token issuer
  and audience, RabbitMQ exchange and queue names. Renaming those is a large, risky change with no visible benefit.
- n8n workflow names and credential names (`CommerceHub RabbitMQ`, `CommerceHub SMTP`): they are identifiers that people
  map when importing; only the text recipients read was changed.
- Older ADRs, the changelog history and architecture docs keep saying CommerceHub (they describe the code as it was).
- The Android application id / iOS bundle id are unchanged, so an installed app updates in place instead of becoming a
  different app.

## Things to know
- The production domain, the Vercel project names, the repository name and any social handles are not touched by this PR.
- Availability of the name (domain, trademark, social networks) was **not checked**; do it before using it publicly.
- Browsers cache the title/favicon and a phone shows the new app label only after the app is reinstalled or updated.

## Verification
- `dotnet test` of the Notifications module 14/14; web 46/46 and admin 6/6; `ng build` of both apps complete; `flutter
  analyze` clean and `flutter test` 40/40; the three edited n8n JSON files still parse.
- Looked at the built store and admin in the browser: the header, the footer, the admin sign-in and the tab titles show the new
  name and the lime pill. (Served statically without an API, so the product list shows an error there; unrelated to the rename.)
- **Not verified:** the app label on a real phone, the emails actually sent (no SMTP/n8n here), and mobile screens with the
  new name beyond the title and prompt strings.
