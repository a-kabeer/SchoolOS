# 32 — Registration and Onboarding Specification

**Version:** v1.6 · **Status:** Proposed specification; Not Started

## 1. Supported paths (both selected)

1. **Self-registration:** visitor creates an account, verifies email/phone, enters organization information and submits for review.
2. **Assisted/manual registration:** authorized SchoolOS staff create an organization record or invite an owner/contact and complete onboarding with the customer.

Self-registration is not equivalent to production approval. Email verification, organization review, payment verification, subscription activation and Local Node activation are independent gates.

## 2. Suggested lifecycle

`Draft → Email Pending → Submitted → Under Review → (Trial | Approved | Rejected) → Active` with `Suspended` as a separate controlled state. Rejected applicants may correct and resubmit according to policy. Trial expiry and cancellation are separate transitions.

### State meanings

- Draft: registration started, not submitted.
- Email Pending: email challenge not completed.
- Submitted: required registration fields submitted.
- Under Review: staff verification underway.
- Trial: limited trial access; production licence is not implied.
- Approved: organization identity/business review passed; billing and provisioning may continue.
- Active: approved organization with required subscription/payment and authorized production setup.
- Rejected: review declined with reason code and customer-facing explanation.
- Suspended: access restricted by authorized action with reason, notice and audit.

## 3. Registration data

Organization: legal/display name, entity type (where applicable), address, city, province, country, website, school category, estimated active students, campuses, curriculum/boards, current software, desired deployment mode, primary contact and billing contact. Personal fields should be minimized and retained only as required.

Contact: name, work email, phone, role, verification state, consent/preferences. Avoid collecting national identity numbers unless legal review proves a specific requirement; this is not a default field.

## 4. Onboarding checklist

- Confirm organization/contact details.
- Verify ownership/contact channel.
- Review duplicate organization and existing customer match.
- Confirm deployment profile and campus topology.
- Confirm hardware/OS/LAN readiness for local installation.
- Agree plan, capacity, add-ons, installation scope and support scope.
- Verify payment through bank/payment evidence, not screenshot alone.
- Create subscription and entitlements.
- Create campus and register node only when installation identity is known.
- Issue signed licence after required approval and payment gates.
- Confirm backup, restore, support and customer administrator training.

## 5. Separation of approvals

Use independent records/status transitions for `OrganizationReview`, `PaymentVerification`, `SubscriptionActivation`, `NodeRegistration` and `LicenseIssuance`. No single generic “Approve” button should silently perform all these actions. Each operation should have an actor, timestamp, reason/evidence reference, prior/new state and audit event.

## 6. Entities

`RegistrationApplications`, `RegistrationContacts`, `EmailVerificationChallenges`, `PhoneVerificationChallenges`, `OrganizationReviews`, `ReviewDecisions`, `OnboardingCases`, `OnboardingChecklistItems`, `OnboardingActivities`, `OrganizationInvitations`, `DuplicateOrganizationMatches`, `TrialEntitlements`.

## 7. Routes and APIs

Routes: `/register`, `/register/verify-email`, `/register/organization`, `/register/status`, `/register/submitted`, `/portal/onboarding`.

APIs:
- `POST /api/v1/registration/start`
- `POST /api/v1/registration/verify-email`
- `POST /api/v1/registration/verify-phone`
- `POST /api/v1/registration/organization`
- `POST /api/v1/registration/submit`
- `GET /api/v1/registration/status`
- `POST /api/v1/admin/registrations/{id}/request-information`
- `POST /api/v1/admin/registrations/{id}/approve`
- `POST /api/v1/admin/registrations/{id}/reject`

## 8. Acceptance criteria

- Unverified registrant cannot submit as verified or access customer data.
- Submission creates a review case but does not issue a production licence.
- Approval, payment verification, subscription activation and node/licence provisioning have distinct permissions and audit records.
- Duplicate organizations are flagged for review rather than silently merged.
- Customer can see a safe status and action-required message without seeing internal staff notes.
