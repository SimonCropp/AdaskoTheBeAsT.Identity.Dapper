﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Exceptions;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper;

public class DapperUserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
    : DapperUserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>,
        IUserRoleStore<TUser>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserRole : IdentityUserRole<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
{
    public DapperUserStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider connectionProvider)
        : base(describer, connectionProvider)
    {
    }

    /// <summary>
    /// Retrieves all users in the specified role.
    /// </summary>
    /// <param name="roleName">The role whose users should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> contains a list of users, if any, that are in the specified role.
    /// </returns>
    public async Task<IList<TUser>> GetUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT
                  u.UserName
                 ,u.NormalizedUserName
                 ,u.Email
                 ,u.NormalizedEmail
                 ,u.EmailConfirmed
                 ,u.PasswordHash
                 ,u.SecurityStamp
                 ,u.ConcurrencyStamp
                 ,u.PhoneNumber
                 ,u.PhoneNumberConfirmed
                 ,u.TwoFactorEnabled
                 ,u.LockoutEnd
                 ,u.LockoutEnabled
                 ,u.AccessFailedCount
             FROM dbo.AspNetUsers u
             INNER JOIN dbo.AspNetUserRoles ur ON u.Id=ur.UserId
             INNER JOIN dbo.AspNetRoles r ON ur.RolesId=r.Id
             WHERE r.NormalizedName=@NormalizedName;";
        return (await connection.QueryAsync<TUser>(
                    query,
                    new { NormalizedName = roleName })
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Adds the given <paramref name="roleName" /> to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the role to.</param>
    /// <param name="roleName">The role to add.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task AddToRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        const string query =
            @"INSERT INTO dbo.AspNetUserRoles(UserId, RoleId)
              VALUES (@UserId, @RoleId);";
        await connection.ExecuteAsync(
                query,
                CreateUserRole(user, role))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the given <paramref name="roleName" /> from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the role from.</param>
    /// <param name="roleName">The role to remove.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task RemoveFromRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        const string query =
            @"DELETE FROM dbo.AspNetUserRoles
               WHERE UserId=@UserId
                 AND RoleId=@RoleId;";
        await connection.ExecuteAsync(
                query,
                CreateUserRole(user, role))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the roles the specified <paramref name="user" /> is a member of.
    /// </summary>
    /// <param name="user">The user whose roles should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the roles the user is a member of.</returns>
    public async Task<IList<string>> GetRolesAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT NormalizedName
                FROM dbo.AspNetUserRoles
               WHERE UserId=@UserId;";
        return (await connection.QueryAsync<string>(
                    query,
                    new { UserId = user.Id })
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Returns a flag indicating if the specified user is a member of the give <paramref name="roleName" />.
    /// </summary>
    /// <param name="user">The user whose role membership should be checked.</param>
    /// <param name="roleName">The role to check membership of.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> containing a flag indicating if the specified user is a member of the given group. If the
    /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
    public async Task<bool> IsInRoleAsync(
        TUser user,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var role = await FindRoleAsync(roleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new RoleNotFoundException($"Role {roleName} not found");
        }

        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT COUNT(*)
                FROM dbo.AspNetUserRoles
               WHERE UserId=@UserId
                 AND RoleId=@RoleId;";
        return (await connection.QueryFirstOrDefaultAsync<int>(
                query,
                CreateUserRole(user, role))
            .ConfigureAwait(false)) > 0;
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserRole`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="role">The associated role.</param>
    /// <returns></returns>
    protected virtual TUserRole CreateUserRole(TUser user, TRole role)
    {
        return new TUserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
        };
    }

    /// <summary>
    /// Return a role with the normalized name if it exists.
    /// </summary>
    /// <param name="roleName">The normalized role name.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The role if it exists.</returns>
    protected async Task<TRole?> FindRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT Id, Name, NormalizedName, ConcurrencyStamp
                FROM dbo.AspNetRoles
               WHERE NormalizedName=@NormalizedName;";
        return await connection.QueryFirstOrDefaultAsync<TRole?>(
                query,
                new { NormalizedName = roleName })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Return a user role for the userId and roleId if it exists.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="roleId">The role's id.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user role if it exists.</returns>
    protected async Task<TUserRole?> FindUserRoleAsync(
        TKey userId,
        TKey roleId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT Id, Name, NormalizedName, ConcurrencyStamp
                FROM dbo.AspNetUserRoles
               WHERE UserId=@UserId
                 AND RoleId=@RoleId;";
        return await connection.QueryFirstOrDefaultAsync<TUserRole?>(
                query,
                new
                {
                    UserId = userId,
                    RoleId = roleId,
                })
            .ConfigureAwait(false);
    }
}
