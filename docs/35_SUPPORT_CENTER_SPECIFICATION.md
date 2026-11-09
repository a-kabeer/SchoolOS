# 35 — Support Center Specification

**Version:** v1.6 · **Status:** Proposed specification; Not Started

## 1. Customer support channels (both selected)

- Built-in support portal with knowledge base, ticketing and live chat.
- WhatsApp integration for customer convenience, linked to the same support case/history where identity can be reliably matched.

Email may be used for ticket notifications and replies. Channel policy and provider selection remain open.

## 2. Ticket lifecycle

Default lifecycle: `Created → Assigned → Investigating → Resolution Proposed → Customer Confirmation → Closed`. Allow `Reopened`, `Waiting on Customer`, `Escalated` and `Cancelled` states where needed. Every status transition needs actor, timestamp and optional internal/customer-visible note.

Categories: Account, Billing, Licence, Installation, Synchronization, Data, Finance, Academic, AI, Feature Request, Other.

Priorities: Critical, High, Normal, Low. SLA clocks, business hours, holidays and escalation timers must be configured only after staffing capacity is known. Do not market unapproved response-time targets.

## 3. Live chat

- Authenticated portal chat should be linked to customer organization and user.
- Anonymous pre-sales chat is a separate funnel and must not expose customer-specific support.
- Support agents see only assigned/authorized conversations.
- Store message status, timestamp, author/channel and attachment reference.
- Define retention, transcript export, customer consent and abusive-content controls.

SignalR is a candidate for near-real-time delivery within the ASP.NET Core application; use a durable message store so chat does not depend on a live socket.

## 4. WhatsApp integration

- Use an approved WhatsApp Business integration/provider; provider selection and commercial terms remain open.
- Verify webhook signatures, timestamps and replay protections.
- Store provider message IDs and delivery/read/failure statuses.
- Resolve sender to a verified contact. Unknown senders should enter a controlled identification flow; do not disclose account details to an unverified number.
- Deduplicate inbound webhook deliveries using provider message ID/idempotency key.
- Keep opt-in, opt-out, template/notification policy and region/provider rules under review.
- WhatsApp is not a secure channel for passwords, private keys, student lists, full bank statements or sensitive academic/financial records.

## 5. Support access to Local Nodes

Remote troubleshooting is opt-in and explicitly granted by the customer. Grants must define organization, campus/node, permitted scope, start/expiry, requesting agent and customer approver. Default to read-only diagnostics. Any temporary elevated action requires explicit approval and audit. Do not expose public PostgreSQL ports or create persistent generic remote shell access. Prefer customer-initiated outbound support tunnel/VPN or a narrowly scoped signed session design after security review.

## 6. Knowledge Base

Categories: Getting Started, Installation, Campus Setup, Users & Roles, Admissions, Fees & Finance, Attendance, Academics, Exams, Reports, Offline Operation, Sync, Backup & Restore, Licensing, Billing, AI, Security and Troubleshooting. Articles need version applicability, review date, author, reviewer, attachments, related ticket tags and published state.

## 7. Entities

`SupportTickets`, `TicketMessages`, `TicketAssignments`, `TicketAttachments`, `TicketStatusHistory`, `TicketInternalNotes`, `SupportSlaPolicies`, `SupportEscalations`, `ChatSessions`, `ChatParticipants`, `ChatMessages`, `SupportChannels`, `WhatsAppMessages`, `WhatsAppWebhookEvents`, `SupportAccessRequests`, `SupportAccessGrants`, `RemoteSupportSessions`, `KnowledgeBaseArticles`, `KnowledgeBaseRevisions`, `KnowledgeBaseCategories`, `TicketFeedback`.

## 8. APIs and webhooks

- `POST /api/v1/support/tickets`
- `GET /api/v1/support/tickets`
- `GET /api/v1/support/tickets/{ticketId}`
- `POST /api/v1/support/tickets/{ticketId}/messages`
- `POST /api/v1/support/tickets/{ticketId}/attachments`
- `POST /api/v1/support/chat/sessions`
- `POST /api/v1/support/access-requests`
- `POST /api/v1/support/access-grants/{id}/approve`
- `POST /api/v1/webhooks/whatsapp/messages`
- `POST /api/v1/webhooks/whatsapp/status`

Webhook handlers validate signature, timestamp, payload schema and idempotency before accepting events. Return provider-compatible responses without disclosing internal errors.

## 9. Acceptance criteria

- Customer cannot access another organization's ticket by guessing an ID.
- Unverified WhatsApp senders receive no sensitive account disclosure.
- Duplicate webhook deliveries do not duplicate messages or tickets.
- Attachments are scanned and authorized before download.
- Support access automatically expires and cannot exceed the approved scope.
- Ticket and chat records retain an auditable message/status history.
