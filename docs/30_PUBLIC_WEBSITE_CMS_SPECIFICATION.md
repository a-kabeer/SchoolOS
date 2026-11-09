# 30 — Public Website and CMS Specification

**Version:** v1.6 · **Status:** Proposed specification; Not Started

## 1. Website information architecture

| Page | Main content / purpose | Primary action |
|---|---|---|
| Home | Value proposition, key proof points, offline + cloud explanation, product overview | Try Demo / Request Demo |
| Product | What SchoolOS is and who it serves | Explore Features |
| Features | Functional modules grouped by school workflow | Try Demo |
| Solutions | Single-campus, school chain, disconnected campuses and hybrid deployment | Request Consultation |
| Pricing | Approved plans, capacity rules, add-ons, onboarding and exclusions | Get Started / Contact Sales |
| Offline Architecture | Local Data Plane, Cloud Control Plane, sync boundaries and outage behavior | View Demo |
| Security & Privacy | Tenant isolation, encryption, local data ownership, AI governance and support access | Contact Security |
| AI | Six AI capabilities, privacy routing, limits and human approval | Try AI Demo (synthetic only) |
| Resources | Guides, articles, release notes, FAQs and downloads | Read / Subscribe |
| About | Company profile, mission and contact channels | Contact |
| Contact | Sales/support enquiry routes | Submit Enquiry |
| Legal | Terms, privacy notice, cookies, data processing terms and acceptable use | View Policy |

## 2. Content requirements

- Explain that local campus operation depends on local node/LAN/power; cloud functions and cross-campus reporting may be delayed during internet outages.
- Avoid claims that every workflow is fully offline until each workflow is implemented and tested.
- Explain that each campus which must independently operate while disconnected needs its own reachable Local Node.
- Pricing page reads only published plan/price records. Draft/test pricing must never leak publicly.
- Distinguish standard core modules from separately metered AI, messaging, storage and private inference add-ons.
- Use customer-approved testimonials/logos only; no fabricated customer claims or performance statistics.

## 3. CMS roles and workflow

- Content Author: create/edit drafts.
- Content Reviewer: review, comment and approve.
- Publisher: publish/schedule/unpublish.
- SEO Manager: metadata, redirect and canonical settings.
- CMS Admin: templates, categories, media and permissions.

Workflow: Draft → In Review → Approved → Scheduled/Published → Archived. Emergency unpublish must be restricted and audited. Every published page should have author, reviewer, publish timestamp and revision history.

## 4. CMS entities

`WebsitePages`, `WebsitePageRevisions`, `WebsiteSections`, `NavigationMenus`, `NavigationItems`, `BlogArticles`, `ArticleCategories`, `FAQs`, `MediaAssets`, `SeoMetadata`, `RedirectRules`, `PublishedPriceSnapshots`, `ContentReviews`, `ContentComments`, `WebsiteAnnouncements`, `CookieConsentRecords`.

Media assets need storage key, MIME type, size, hash, alt text, upload actor, scan status, visibility and retention metadata. Avoid storing large binary objects directly in PostgreSQL; use object storage abstraction.

## 5. Public routes

`/`, `/product`, `/features`, `/solutions`, `/pricing`, `/offline-architecture`, `/security`, `/ai`, `/resources`, `/resources/{slug}`, `/about`, `/contact`, `/demo`, `/register`, `/legal/terms`, `/legal/privacy`, `/legal/cookies`.

## 6. APIs (illustrative contracts)

- `GET /api/v1/public/pages/{slug}`
- `GET /api/v1/public/navigation`
- `GET /api/v1/public/plans`
- `GET /api/v1/public/features`
- `GET /api/v1/public/articles`
- `GET /api/v1/public/articles/{slug}`
- `GET /api/v1/public/faqs`
- `POST /api/v1/public/contact`
- `POST /api/v1/public/leads`

Public APIs expose published fields only. They must not return drafts, internal notes, private lead data or unpublished plan prices.

## 7. Analytics and consent

Track page views and funnel events such as pricing viewed, Try Demo clicked, demo started, registration started and lead submitted. Apply cookie/analytics consent policy and retention limits. Keep essential operational logs separate from optional marketing analytics.

## 8. Acceptance criteria

- Responsive, accessible, SEO-capable pages with CMS-managed content.
- Draft, review, preview, schedule, publish and rollback tested.
- Public endpoint tests prove draft/private content is never returned.
- Pricing content is sourced from an approved published version.
- Consent and analytics preferences are honored.
