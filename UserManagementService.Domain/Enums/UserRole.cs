namespace UserManagementService.Domain.Enums;
public enum UserRole
{
    /// <summary>
    /// Can only access channels they are explicitly added to.
    /// </summary>
    User = 3,

    /// <summary>
    /// Can manage users and channels they have been given authority over by an Admin.
    /// </summary>
    Operator = 2,

    /// <summary>
    /// Can create users, assign roles, manage all channels, and grant permissions to operators.
    /// </summary>
    Admin = 1
}