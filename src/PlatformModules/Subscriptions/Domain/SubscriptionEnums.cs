namespace SchoolOS.PlatformModules.Subscriptions.Domain;

public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    PastDue = 3,
    Suspended = 4,
    Cancelled = 5
}

public enum BillingCycle
{
    Monthly = 1,
    Annual = 2
}

public enum InvoiceStatus
{
    Open = 1,
    Paid = 2,
    Void = 3
}

public enum PaymentMethod
{
    BankTransfer = 1,
    Cheque = 2,
    Cash = 3,
    Card = 4
}

public enum AddOnRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum TenantModuleStatus
{
    Active = 1,
    Suspended = 2,
    Expired = 3
}
