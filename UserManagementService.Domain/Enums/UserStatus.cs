namespace UserManagementService.Domain.Enums;
/// <summary>
/// Represents the current state of a user account.
/// This allows us to track user lifecycle without deleting records.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is active and can be used normally.
    /// </summary>
    Active,

    /// <summary>
    /// User account has been temporarily disabled by an admin.
    /// The user cannot log in but data is preserved.
    /// </summary>
    Disabled,

    /// <summary>
    /// User account has been deleted (soft delete).
    /// This is a logical deletion - the record remains for audit purposes.
    /// </summary>
    Deleted
}