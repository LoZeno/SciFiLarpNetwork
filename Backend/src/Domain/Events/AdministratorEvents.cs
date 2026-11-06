namespace SciFiLarpNetwork.Backend.Domain.Events;

public static class AdministratorEvents
{
    public const string AccountCreated = "scifi.administrator.account-created";
    public const string AccountPasswordChanged = "scifi.administrator.account-password-changed";
    public const string AccountRoleChanged = "scifi.administrator.account-role-changed";
    public const string BootstrapCredentialIssued = "scifi.administrator.bootstrap-credential-issued";
    public const string RecoveryCredentialIssued = "scifi.administrator.recovery-credential-issued";
    public const string GrantConsumed = "scifi.administrator.grant-consumed";
    public const string SessionCreated = "scifi.administrator.session-created";
    public const string SessionRevoked = "scifi.administrator.session-revoked";
}
