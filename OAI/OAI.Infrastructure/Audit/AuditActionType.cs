namespace OAI.Infrastructure.Audit;

public enum AuditActionType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Uploaded = 3,
    Processed = 4,
    Validated = 5,
    Exported = 6,
    Login = 7,
    Logout = 8
}