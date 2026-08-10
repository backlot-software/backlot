# Phase 1: Foundation & Auth - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-21
**Phase:** 1-Foundation & Auth
**Areas discussed:** Navigation shell, Session lifetime, Login page style, Navbar identity

---

## Navigation Shell

### Q1: Layout topology

| Option | Description | Selected |
|--------|-------------|----------|
| Left sidebar | Fixed sidebar with section links; standard admin/dev tool pattern | ✓ |
| Top navbar only | Horizontal Bootstrap navbar; simpler but cramped with multiple sections | |
| You decide | Pick whatever fits Bootstrap admin tool convention | |

**User's choice:** Left sidebar

---

### Q2: Sidebar content in Phase 1

| Option | Description | Selected |
|--------|-------------|----------|
| Placeholder nav items (disabled links) | Show Scenarios and Roles as disabled/grayed — makes shell feel complete | ✓ |
| Only what's live | Phase 1 sidebar mostly empty; each phase adds its own nav entry | |
| You decide | Build whatever is cleanest for the Razor Pages layout pattern | |

**User's choice:** Placeholder nav items (disabled links)

---

### Q3: Collapsible vs always visible

| Option | Description | Selected |
|--------|-------------|----------|
| Always visible | Fixed sidebar, no toggle; simpler | |
| Collapsible with toggle button | Icon-only collapsed mode; more screen real estate | ✓ |
| You decide | Pick whatever Bootstrap admin pattern suggests | |

**User's choice:** Collapsible with toggle button

---

## Session Lifetime

### Q1: Idle timeout duration

| Option | Description | Selected |
|--------|-------------|----------|
| 8 hours | Matches a workday; log in once at the start and stay in all day | ✓ |
| 2 hours | Moderate; forces re-login mid-day if stepping away | |
| 30 minutes | Strict; fine for public apps, disruptive for dev tools | |

**User's choice:** 8 hours

---

### Q2: Sliding vs absolute expiry

| Option | Description | Selected |
|--------|-------------|----------|
| Sliding | Resets timeout on every page visit; active use = stay logged in | ✓ |
| Absolute | Expires exactly 8h after login regardless of activity | |

**User's choice:** Sliding

---

## Login Page Style

### Q1: Login page appearance

| Option | Description | Selected |
|--------|-------------|----------|
| Centered card with branding | Full-page centered Bootstrap card: title at top, fields, button | ✓ |
| Minimal — just the form | No branding, raw fields on plain page | |
| Full-screen split | Left branding panel + right form panel; polish overkill for internal tool | |

**User's choice:** Centered card with branding

---

### Q2: Error display

| Option | Description | Selected |
|--------|-------------|----------|
| Banner alert above the form | Bootstrap alert-danger: "Invalid username or password" | ✓ |
| Inline below the submit button | Error text below the Login button | |
| Field-level inline errors | Highlight fields in red; misleading since Basic Auth doesn't indicate which field | |

**User's choice:** Banner alert above the form

---

## Navbar Identity

### Q1: Identity location and form

| Option | Description | Selected |
|--------|-------------|----------|
| Top of sidebar — username + logout link | Username near top/bottom of sidebar with logout link beneath | ✓ |
| Top navbar — username inline with logout button | Username + Logout at far right of horizontal bar | |
| Dropdown in top navbar | Avatar/username opens dropdown; JS overhead, overkill for single user | |

**User's choice:** Top of sidebar — username + logout link

---

### Q2: Data shown from whoami

| Option | Description | Selected |
|--------|-------------|----------|
| Username only | Just the username string; clean and minimal | ✓ |
| Username + role/type label | e.g. "jeroen (admin)"; useful for multi-role tools | |
| You decide | Display whatever whoami usefully exposes | |

**User's choice:** Username only

---

## Claude's Discretion

- Sidebar collapse animation details (CSS transition, icon choice)
- Whether sidebar toggle state persists across Turbo navigations (`data-turbo-permanent`)
- Bootstrap color theme default (light/dark)
- Exact wording of "Backlot Studio" branding on login card

## Deferred Ideas

None — discussion stayed within Phase 1 scope.
