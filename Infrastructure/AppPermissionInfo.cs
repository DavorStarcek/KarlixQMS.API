namespace KarlixQMS.API.Infrastructure;

public static class AppPermissionInfo
{
    public const string PermissionClaimType = "perm";

    public const string QmsAdmin = "qms.admin";
    public const string QmsRead = "qms.read";

    public const string QmsActionsRead = "qms.actions.read";
    public const string QmsActionsWriteBasic = "qms.actions.write.basic";
    public const string QmsActionsVerify = "qms.actions.verify";
    public const string QmsActionsWriteAll = "qms.actions.write.all";
}
