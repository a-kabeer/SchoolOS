namespace SchoolOS.Modules.Identity.Domain;

/// <summary>
/// Categorizes a person's operational role within a school (ported from Education-Portal ENG-ENUM-01..03).
/// </summary>
public enum UserType
{
    Principal = 1,
    Teacher = 2,
    Staff = 3,
    Accountant = 4,
    Librarian = 5,
    Student = 6,
    Parent = 7,
    SchoolAdministrator = 8,
}

/// <summary>
/// Lifecycle state of a user identity aggregate (DOC-63 §3.1).
/// </summary>
public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    LockedOut = 3,
    PendingActivation = 4,
}

/// <summary>
/// Outcome of an authentication attempt (ported from Education-Portal PasswordSignInStatus + DOC-63).
/// </summary>
public enum SignInOutcome
{
    Succeeded = 1,
    InvalidCredentials = 2,
    LockedOut = 3,
    Suspended = 4,
    TenantMismatch = 5,
    CampusAssignmentRequired = 6,
}
