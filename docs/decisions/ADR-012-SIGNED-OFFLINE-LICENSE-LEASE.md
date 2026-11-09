# ADR-012 — Signed Offline License Lease

- **Status:** Accepted as design direction; cryptographic format pending implementation review
- **Date:** 9 November 2026
- **Decision owners:** Product owner and technical lead

## Context

Customers pay monthly but Local Nodes must continue approved operations during internet outages. The cloud cannot be contacted for every request, so the local node needs a trusted, time-bounded entitlement snapshot.

## Decision

Cloud issues a node-bound, time-bounded license document signed with an asymmetric private key. **Renewal must support both automatic renewal over an authenticated connection when the Local Node is online and manual signed `.lic` file download/import as an offline fallback.** Both routes use the same local signature/claims validator and atomic activation pipeline.

**After the configured grace period, the node enters Restricted Mode.** Historical data remains viewable and exportable; backup, renewal and support functions remain available; new operational writes are blocked. A valid renewed licence reactivates the existing installation without requiring a software update. Local Node verifies it using a public key. The license includes tenant/node scope, plan, limits, enabled features, paid-through date, grace boundary, lease expiry, sequence number and signing key ID.

Confirmed policy: both automatic online renewal and manual offline file renewal; restricted mode after the grace boundary. The initial seven-calendar-day grace period remains a proposal pending commercial approval.

## Security rules

- Private signing key remains in the Cloud Control Plane's protected key store/HSM where feasible.
- No shared signing secret is embedded in every customer installation.
- License content is canonicalized and digitally signed.
- Local node checks signature, node binding, sequence, dates and entitlements on the server side.
- Automatic online renewal and manual `.lic` file import/USB fallback are both supported.
- Renewal updates entitlements on the existing installation; it is not a software update.
- Renewal file import is supported for disconnected but paid customers.
- Restricted mode preserves historical access, export, backup, billing/licence renewal and support while blocking new operational writes.
- Data export and backups remain available after expiry.

## Limitations

An offline node cannot receive immediate remote revocation. A customer with unrestricted OS root access can potentially alter system time or patch local binaries. Managed appliance, TPM/secure boot where available, tamper evidence and contractual licensing reduce but do not eliminate this risk.

## Alternatives considered

- Online-only license check on every request — rejected because local operation must survive internet outages.
- Static activation key with no expiry — rejected because monthly subscription needs periodic enforcement.
- Shared HMAC secret embedded in the local application — rejected because extracting it from one installation could compromise all installations.
