﻿using AdaskoTheBeAsT.Identity.Dapper.Abstractions;

namespace Sample.SqlServer
{
    public class IdentityUserSql
        : IIdentityUserSql
    {
        public string CreateSql { get; } =
            @"INSERT INTO id.AspNetUsers(
[UserName]
,[Email]
,[EmailConfirmed]
,[PasswordHash]
,[SecurityStamp]
,[ConcurrencyStamp]
,[PhoneNumber]
,[PhoneNumberConfirmed]
,[TwoFactorEnabled]
,[LockoutEnd]
,[LockoutEnabled]
,[AccessFailedCount]
,[IsActive])
VALUES(
@UserName
,@Email
,@EmailConfirmed
,@PasswordHash
,@SecurityStamp
,@ConcurrencyStamp
,@PhoneNumber
,@PhoneNumberConfirmed
,@TwoFactorEnabled
,@LockoutEnd
,@LockoutEnabled
,@AccessFailedCount
,@Active)
OUTPUT inserted.Id
VALUES(1);";

        public string UpdateSql { get; } =
            @"UPDATE id.AspNetUsers
SET [UserName]=@UserName
,[Email]=@Email
,[EmailConfirmed]=@EmailConfirmed
,[PasswordHash]=@PasswordHash
,[SecurityStamp]=@SecurityStamp
,[ConcurrencyStamp]=@ConcurrencyStamp
,[PhoneNumber]=@PhoneNumber
,[PhoneNumberConfirmed]=@PhoneNumberConfirmed
,[TwoFactorEnabled]=@TwoFactorEnabled
,[LockoutEnd]=@LockoutEnd
,[LockoutEnabled]=@LockoutEnabled
,[AccessFailedCount]=@AccessFailedCount
,[IsActive]=@Active
WHERE Id=@Id;";

        public string DeleteSql { get; } =
            @"DELETE FROM id.AspNetUsers WHERE Id=@Id;";

        public string FindByIdSql { get; } =
            @"SELECT Id
,[UserName] AS UserName
,[UserName] AS NormalizedUserName
,[Email] AS Email
,[Email] AS NormalizedEmail
,[EmailConfirmed] AS EmailConfirmed
,[PasswordHash] AS PasswordHash
,[SecurityStamp] AS SecurityStamp
,[ConcurrencyStamp] AS ConcurrencyStamp
,[PhoneNumber] AS PhoneNumber
,[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,[TwoFactorEnabled] AS TwoFactorEnabled
,[LockoutEnd] AS LockoutEnd
,[LockoutEnabled] AS LockoutEnabled
,[AccessFailedCount] AS AccessFailedCount
,[IsActive] AS Active
,[UserName] AS NormalizedUserName
,[Email] AS NormalizedEmail
FROM id.AspNetUsers
WHERE Id=@Id;";

        public string FindByNameSql { get; } =
            @"SELECT Id
,[UserName] AS UserName
,[UserName] AS NormalizedUserName
,[Email] AS Email
,[Email] AS NormalizedEmail
,[EmailConfirmed] AS EmailConfirmed
,[PasswordHash] AS PasswordHash
,[SecurityStamp] AS SecurityStamp
,[ConcurrencyStamp] AS ConcurrencyStamp
,[PhoneNumber] AS PhoneNumber
,[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,[TwoFactorEnabled] AS TwoFactorEnabled
,[LockoutEnd] AS LockoutEnd
,[LockoutEnabled] AS LockoutEnabled
,[AccessFailedCount] AS AccessFailedCount
,[IsActive] AS Active
,[UserName] AS NormalizedUserName
,[Email] AS NormalizedEmail
FROM id.AspNetUsers
WHERE UserName=@UserName;";

        public string FindByEmailSql { get; } =
            @"SELECT Id
,[UserName] AS UserName
,[UserName] AS NormalizedUserName
,[Email] AS Email
,[Email] AS NormalizedEmail
,[EmailConfirmed] AS EmailConfirmed
,[PasswordHash] AS PasswordHash
,[SecurityStamp] AS SecurityStamp
,[ConcurrencyStamp] AS ConcurrencyStamp
,[PhoneNumber] AS PhoneNumber
,[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,[TwoFactorEnabled] AS TwoFactorEnabled
,[LockoutEnd] AS LockoutEnd
,[LockoutEnabled] AS LockoutEnabled
,[AccessFailedCount] AS AccessFailedCount
,[IsActive] AS Active
,[UserName] AS NormalizedUserName
,[Email] AS NormalizedEmail
FROM id.AspNetUsers
WHERE Email=@Email;";

        public string GetUsersForClaimSql { get; } =
            @"SELECT u.Id
,u.[UserName] AS UserName
,u.[UserName] AS NormalizedUserName
,u.[Email] AS Email
,u.[Email] AS NormalizedEmail
,u.[EmailConfirmed] AS EmailConfirmed
,u.[PasswordHash] AS PasswordHash
,u.[SecurityStamp] AS SecurityStamp
,u.[ConcurrencyStamp] AS ConcurrencyStamp
,u.[PhoneNumber] AS PhoneNumber
,u.[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,u.[TwoFactorEnabled] AS TwoFactorEnabled
,u.[LockoutEnd] AS LockoutEnd
,u.[LockoutEnabled] AS LockoutEnabled
,u.[AccessFailedCount] AS AccessFailedCount
,u.[IsActive] AS Active
,u.[UserName] AS NormalizedUserName
,u.[Email] AS NormalizedEmail
FROM id.AspNetUsers u
INNER JOIN id.AspNetUserClaims c ON u.Id=c.UserId
WHERE c.ClaimType=@ClaimType
  AND c.ClaimValue=@ClaimValue;";

        public string GetUsersInRoleSql { get; } =
            @"SELECT u.Id
,u.[UserName] AS UserName
,u.[UserName] AS NormalizedUserName
,u.[Email] AS Email
,u.[Email] AS NormalizedEmail
,u.[EmailConfirmed] AS EmailConfirmed
,u.[PasswordHash] AS PasswordHash
,u.[SecurityStamp] AS SecurityStamp
,u.[ConcurrencyStamp] AS ConcurrencyStamp
,u.[PhoneNumber] AS PhoneNumber
,u.[PhoneNumberConfirmed] AS PhoneNumberConfirmed
,u.[TwoFactorEnabled] AS TwoFactorEnabled
,u.[LockoutEnd] AS LockoutEnd
,u.[LockoutEnabled] AS LockoutEnabled
,u.[AccessFailedCount] AS AccessFailedCount
,u.[IsActive] AS Active
,u.[UserName] AS NormalizedUserName
,u.[Email] AS NormalizedEmail
FROM id.AspNetUsers u
INNER JOIN id.AspNetUserRoles ur ON u.Id=ur.UserId
INNER JOIN id.AspNetRoles r ON ur.RolesId=r.Id
WHERE r.Name=@Name;";
    }
}
