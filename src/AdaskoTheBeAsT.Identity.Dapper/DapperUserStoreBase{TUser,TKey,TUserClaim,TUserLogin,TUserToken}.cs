using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace AdaskoTheBeAsT.Identity.Dapper;

public class DapperUserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
    : IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserAuthenticationTokenStore<TUser>,
        IUserAuthenticatorKeyStore<TUser>,
        IUserTwoFactorRecoveryCodeStore<TUser>
    where TUser : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TUserToken : IdentityUserToken<TKey>, new()
{
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";
    private bool _disposed;

    protected DapperUserStoreBase(
        IdentityErrorDescriber describer,
        IIdentityDbConnectionProvider connectionProvider)
    {
        ErrorDescriber = describer ?? throw new ArgumentNullException(nameof(describer));
        ConnectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
    }

    /// <summary>
    /// Gets or sets the <see cref="T:Microsoft.AspNetCore.Identity.IdentityErrorDescriber" /> for any error that occurred with the current operation.
    /// </summary>
    public IdentityErrorDescriber ErrorDescriber { get; set; }

    protected IIdentityDbConnectionProvider ConnectionProvider { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the user identifier for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose identifier should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the identifier for the specified <paramref name="user" />.</returns>
    public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(ConvertIdToString(user.Id) ?? string.Empty);
    }

    /// <summary>
    /// Gets the user name for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the name for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.UserName);
    }

    /// <summary>
    /// Sets the given <paramref name="userName" /> for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be set.</param>
    /// <param name="userName">The user name to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.UserName = userName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the normalized user name for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose normalized name should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the normalized user name for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.NormalizedUserName);
    }

    /// <summary>
    /// Sets the given normalized name for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose name should be set.</param>
    /// <param name="normalizedName">The normalized name to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates the specified <paramref name="user" /> in the user store.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the creation operation.</returns>
    public async Task<IdentityResult> CreateAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            const string query =
                @"INSERT INTO dbo.AspNetUsers(
                      UserName
                     ,NormalizedUserName
                     ,Email
                     ,NormalizedEmail
                     ,EmailConfirmed
                     ,PasswordHash
                     ,SecurityStamp
                     ,ConcurrencyStamp
                     ,PhoneNumber
                     ,PhoneNumberConfirmed
                     ,TwoFactorEnabled
                     ,LockoutEnd
                     ,LockoutEnabled
                     ,AccessFailedCount)
                 VALUES(
                     @UserName
                    ,@NormalizedUserName
                    ,@Email
                    ,@NormalizedEmail
                    ,@EmailConfirmed
                    ,@PasswordHash
                    ,@SecurityStamp
                    ,@ConcurrencyStamp
                    ,@PhoneNumber
                    ,@PhoneNumberConfirmed
                    ,@TwoFactorEnabled
                    ,@LockoutEnd
                    ,@LockoutEnabled
                    ,@AccessFailedCount);
                SELECT SCOPE_IDENTITY();";
            user.Id = await connection.QueryFirstAsync<TKey>(query, user).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "1",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Updates the specified <paramref name="user" /> in the user store.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the update operation.</returns>
    public async Task<IdentityResult> UpdateAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            const string query =
                @"UPDATE dbo.AspNetUsers
                 SET UserName=@UserName
                    ,NormalizedUserName=@NormalizedUserName
                    ,Email=@Email
                    ,NormalizedEmail=@NormalizedEmail
                    ,EmailConfirmed=@EmailConfirmed
                    ,PasswordHash=@PasswordHash
                    ,SecurityStamp=@SecurityStamp
                    ,ConcurrencyStamp=@ConcurrencyStamp
                    ,PhoneNumber=@PhoneNumber
                    ,PhoneNumberConfirmed=@PhoneNumberConfirmed
                    ,TwoFactorEnabled=@TwoFactorEnabled
                    ,LockoutEnd=@LockoutEnd
                    ,LockoutEnabled=@LockoutEnabled
                    ,AccessFailedCount=@AccessFailedCount
                 WHERE Id=@Id;";
            await connection.ExecuteAsync(query, user).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "2",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Deletes the specified <paramref name="user" /> from the user store.
    /// </summary>
    /// <param name="user">The user to delete.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" /> of the update operation.</returns>
    public async Task<IdentityResult> DeleteAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            using var connection = ConnectionProvider.Provide();
            const string query =
                @"DELETE FROM dbo.AspNetUsers
                 WHERE Id=@Id;";
            await connection.ExecuteAsync(query, user).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "3",
                    Description = ex.Message,
                });
        }
    }

    /// <summary>
    /// Finds and returns a user, if any, who has the specified <paramref name="userId" />.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user matching the specified <paramref name="userId" /> if it exists.
    /// </returns>
    public async Task<TUser?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT
                  UserName
                 ,NormalizedUserName
                 ,Email
                 ,NormalizedEmail
                 ,EmailConfirmed
                 ,PasswordHash
                 ,SecurityStamp
                 ,ConcurrencyStamp
                 ,PhoneNumber
                 ,PhoneNumberConfirmed
                 ,TwoFactorEnabled
                 ,LockoutEnd
                 ,LockoutEnabled
                 ,AccessFailedCount
             FROM dbo.AspNetUsers
             WHERE Id=@Id;";
        return await connection.QueryFirstOrDefaultAsync<TUser?>(
                query,
                new { Id = ConvertIdFromString(userId) })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the provided <paramref name="id" /> to a strongly typed key object.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>An instance of <typeparamref name="TKey" /> representing the provided <paramref name="id" />.</returns>
    public virtual TKey? ConvertIdFromString(string? id)
    {
        if (id == null)
        {
            return default;
        }

        return (TKey?)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
    }

    /// <summary>
    /// Converts the provided <paramref name="id" /> to its string representation.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>An <see cref="T:System.String" /> representation of the provided <paramref name="id" />.</returns>
    public virtual string? ConvertIdToString(TKey id) => Equals(id, default(TKey)) ? null : id.ToString();

    /// <summary>
    /// Finds and returns a user, if any, who has the specified normalized user name.
    /// </summary>
    /// <param name="normalizedUserName">The normalized user name to search for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName" /> if it exists.
    /// </returns>
    public async Task<TUser?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT
                  UserName
                 ,NormalizedUserName
                 ,Email
                 ,NormalizedEmail
                 ,EmailConfirmed
                 ,PasswordHash
                 ,SecurityStamp
                 ,ConcurrencyStamp
                 ,PhoneNumber
                 ,PhoneNumberConfirmed
                 ,TwoFactorEnabled
                 ,LockoutEnd
                 ,LockoutEnabled
                 ,AccessFailedCount
             FROM dbo.AspNetUsers
             WHERE NormalizedUserName=@NormalizedUserName;";
        return await connection.QueryFirstOrDefaultAsync<TUser?>(
                query,
                new { NormalizedUserName = normalizedUserName })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the password hash for a user.
    /// </summary>
    /// <param name="user">The user to set the password hash for.</param>
    /// <param name="passwordHash">The password hash to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the password hash for a user.
    /// </summary>
    /// <param name="user">The user to retrieve the password hash for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the password hash for the user.</returns>
    public virtual Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PasswordHash);
    }

    /// <summary>
    /// Returns a flag indicating if the specified user has a password.
    /// </summary>
    /// <param name="user">The user to retrieve the password hash for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> containing a flag indicating if the specified user has a password. If the
    /// user has a password the returned value with be true, otherwise it will be false.</returns>
    public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.PasswordHash != null);
    }

    /// <summary>
    /// Get the claims associated with the specified <paramref name="user" /> as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose claims should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that contains the claims granted to a user.</returns>
    public async Task<IList<Claim>> GetClaimsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT ClaimType AS Type
                    ,ClaimValue AS Value
             FROM dbo.AspNetUserClaims
             WHERE UserId=@Id;";
        return (await connection.QueryAsync<Claim>(
                query,
                user)
            .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Adds the <paramref name="claims" /> given to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the claim to.</param>
    /// <param name="claims">The claim to add to the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task AddClaimsAsync(
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"INSERT INTO dbo.AspNetUserClaims(UserId, ClaimType, ClaimValue)  
              VALUES (@UserId, @ClaimType, @ClaimValue);";
        foreach (var claim in claims)
        {
            await connection.ExecuteAsync(
                    query,
                    CreateUserClaim(user, claim))
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Replaces the <paramref name="claim" /> on the specified <paramref name="user" />, with the <paramref name="newClaim" />.
    /// </summary>
    /// <param name="user">The user to replace the claim on.</param>
    /// <param name="claim">The claim replace.</param>
    /// <param name="newClaim">The new claim replacing the <paramref name="claim" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task ReplaceClaimAsync(
        TUser user,
        Claim claim,
        Claim newClaim,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"IF EXISTS(SELECT Id
                        FROM dbo.AspNetUserClaims
                        WHERE UserId=@UserId
                          AND ClaimType=@ClaimTypeOld
                          AND ClaimValue=@ClaimValueOld)
            BEGIN
                DELETE FROM dbo.AspNetUserClaims
                WHERE UserId=@UserId
                  AND ClaimType=@ClaimTypeOld
                  AND ClaimValue=@ClaimValueOld
            END;
            IF NOT EXISTS(SELECT Id
                         FROM dbo.AspNetUserClaims
                         WHERE UserId=@UserId
                           AND ClaimType=@ClaimTypeNew
                           AND ClaimValue=@ClaimValueNew)
            BEGIN
                INSERT INTO dbo.AspNetUserClaims(UserId, ClaimType, ClaimValue)
                VALUES (@UserId,@ClaimTypeNew,@ClaimValueNew);
            END;";
        await connection.ExecuteAsync(
                query,
                new
                {
                    UserId = user.Id,
                    ClaimTypeOld = claim.Type,
                    ClaimValueOld = claim.Value,
                    ClaimTypeNew = newClaim.Type,
                    ClaimValueNew = newClaim.Value,
                })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the <paramref name="claims" /> given from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the claims from.</param>
    /// <param name="claims">The claim to remove.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task RemoveClaimsAsync(
        TUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"DELETE FROM dbo.AspNetUserClaims
                WHERE UserId=@UserId
                  AND ClaimType=@ClaimType
                  AND ClaimValue=@ClaimValue;";
        foreach (var claim in claims)
        {
            await connection.ExecuteAsync(
                    query,
                    CreateUserClaim(user, claim))
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Adds the <paramref name="login" /> given to the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to add the login to.</param>
    /// <param name="login">The login to add to the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task AddLoginAsync(
        TUser user,
        UserLoginInfo login,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"INSERT INTO dbo.AspNetUserLogins(
                      LoginProvider
                     ,ProviderKey
                     ,ProviderDisplayName
                     ,UserId)
                 VALUES(
                     @LoginProvider
                    ,@ProviderKey
                    ,@ProviderDisplayName
                    ,@UserId);
                SELECT SCOPE_IDENTITY();";
        await connection.ExecuteAsync(
                query,
                CreateUserLogin(user, login))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the <paramref name="loginProvider" /> given from the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user to remove the login from.</param>
    /// <param name="loginProvider">The login to remove from the user.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public async Task RemoveLoginAsync(
        TUser user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"DELETE FROM dbo.AspNetUserLogins
              WHERE LoginProvider=@LoginProvider
                AND ProviderKey=@ProviderKey
                AND UserId=@UserId;";
        await connection.ExecuteAsync(
                query,
                new
                {
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                    UserId = user.Id,
                })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the associated logins for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose associated logins to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> for the asynchronous operation, containing a list of <see cref="T:Microsoft.AspNetCore.Identity.UserLoginInfo" /> for the specified <paramref name="user" />, if any.
    /// </returns>
    public async Task<IList<UserLoginInfo>> GetLoginsAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT LoginProvider
                    ,ProviderKey
                    ,ProviderDisplayName 
              FROM dbo.AspNetUserLogins
              WHERE UserId=@Id;";
        return (await connection.QueryAsync<UserLoginInfo>(
                    query,
                    user)
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Retrieves the user associated with the specified login provider and login provider key..
    /// </summary>
    /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey" />.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
    /// </returns>
    public virtual async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var val = await FindUserLoginAsync(loginProvider, providerKey, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val != null)
        {
            return await FindUserAsync(val.UserId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        return null;
    }

    /// <summary>
    /// Gets a flag indicating whether the email address for the specified <paramref name="user" /> has been verified, true if the email address is verified otherwise
    /// false.
    /// </summary>
    /// <param name="user">The user whose email confirmation status should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous operation, a flag indicating whether the email address for the specified <paramref name="user" />
    /// has been confirmed or not.
    /// </returns>
    public virtual Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.EmailConfirmed);
    }

    /// <summary>
    /// Sets the flag indicating whether the specified <paramref name="user" />'s email address has been confirmed or not.
    /// </summary>
    /// <param name="user">The user whose email confirmation status should be set.</param>
    /// <param name="confirmed">A flag indicating if the email address has been confirmed, true if the address is confirmed otherwise false.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the <paramref name="email" /> address for a <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email should be set.</param>
    /// <param name="email">The email to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.Email = email;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the email address for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object containing the results of the asynchronous operation, the email address for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.Email);
    }

    /// <summary>
    /// Returns the normalized email for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email address to retrieve.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous lookup operation, the normalized email address if any associated with the specified user.
    /// </returns>
    public virtual Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.NormalizedEmail);
    }

    /// <summary>
    /// Sets the normalized email for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose email address to set.</param>
    /// <param name="normalizedEmail">The normalized email to set for the specified <paramref name="user" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the user, if any, associated with the specified, normalized email address.
    /// </summary>
    /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
    /// </returns>
    public async Task<TUser?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT
                  UserName
                 ,NormalizedUserName
                 ,Email
                 ,NormalizedEmail
                 ,EmailConfirmed
                 ,PasswordHash
                 ,SecurityStamp
                 ,ConcurrencyStamp
                 ,PhoneNumber
                 ,PhoneNumberConfirmed
                 ,TwoFactorEnabled
                 ,LockoutEnd
                 ,LockoutEnabled
                 ,AccessFailedCount
             FROM dbo.AspNetUsers
             WHERE NormalizedEmail=@NormalizedEmail;";
        return await connection.QueryFirstOrDefaultAsync<TUser?>(
                query,
                new { NormalizedEmail = normalizedEmail })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the last <see cref="T:System.DateTimeOffset" /> a user's last lockout expired, if any.
    /// Any time in the past should be indicates a user is not locked out.
    /// </summary>
    /// <param name="user">The user whose lockout date should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the result of the asynchronous query, a <see cref="T:System.DateTimeOffset" /> containing the last time
    /// a user's lockout expired, if any.
    /// </returns>
    public virtual Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.LockoutEnd);
    }

    /// <summary>
    /// Locks out a user until the specified end date has passed. Setting a end date in the past immediately unlocks a user.
    /// </summary>
    /// <param name="user">The user whose lockout date should be set.</param>
    /// <param name="lockoutEnd">The <see cref="T:System.DateTimeOffset" /> after which the <paramref name="user" />'s lockout should end.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records that a failed access has occurred, incrementing the failed access count.
    /// </summary>
    /// <param name="user">The user whose cancellation count should be incremented.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the incremented failed access count.</returns>
    public virtual Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    /// <summary>
    /// Resets a user's failed access count.
    /// </summary>
    /// <param name="user">The user whose failed access count should be reset.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    /// <remarks>This is typically called after the account is successfully accessed.</remarks>
    public virtual Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves the current failed access count for the specified <paramref name="user" />..
    /// </summary>
    /// <param name="user">The user whose failed access count should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the failed access count.</returns>
    public virtual Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.AccessFailedCount);
    }

    /// <summary>
    /// Retrieves a flag indicating whether user lockout can enabled for the specified user.
    /// </summary>
    /// <param name="user">The user whose ability to be locked out should be returned.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, true if a user can be locked out, otherwise false.
    /// </returns>
    public virtual Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.LockoutEnabled);
    }

    /// <summary>
    /// Set the flag indicating if the specified <paramref name="user" /> can be locked out..
    /// </summary>
    /// <param name="user">The user whose ability to be locked out should be set.</param>
    /// <param name="enabled">A flag indicating if lock out can be enabled for the specified <paramref name="user" />.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the telephone number for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose telephone number should be set.</param>
    /// <param name="phoneNumber">The telephone number to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the telephone number, if any, for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose telephone number should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the user's telephone number, if any.</returns>
    public virtual Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PhoneNumber);
    }

    /// <summary>
    /// Gets a flag indicating whether the specified <paramref name="user" />'s telephone number has been confirmed.
    /// </summary>
    /// <param name="user">The user to return a flag for, indicating whether their telephone number is confirmed.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, returning true if the specified <paramref name="user" /> has a confirmed
    /// telephone number otherwise false.
    /// </returns>
    public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    /// <summary>
    /// Sets a flag indicating if the specified <paramref name="user" />'s phone number has been confirmed..
    /// </summary>
    /// <param name="user">The user whose telephone number confirmation status should be set.</param>
    /// <param name="confirmed">A flag indicating whether the user's telephone number has been confirmed.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the provided security <paramref name="stamp" /> for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="stamp">The security stamp to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (stamp == null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the security stamp for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.SecurityStamp);
    }

    /// <summary>
    /// Sets a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or not,
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
    /// <param name="enabled">A flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a flag indicating whether the specified <paramref name="user" /> has two factor authentication enabled or not,
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing a flag indicating whether the specified
    /// <paramref name="user" /> has two factor authentication enabled or not.
    /// </returns>
    public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return Task.FromResult(user.TwoFactorEnabled);
    }

    /// <summary>
    /// Retrieves all users with the specified claim.
    /// </summary>
    /// <param name="claim">The claim whose users should be retrieved.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>
    /// The <see cref="T:System.Threading.Tasks.Task" /> contains a list of users, if any, that contain the specified claim.
    /// </returns>
    public async Task<IList<TUser>> GetUsersForClaimAsync(
        Claim claim,
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
             FROM dbo.AspNetUsers u INNER JOIN
                  dbo.AspNetUserClaims c ON u.Id=c.UserId
             WHERE c.ClaimType=@ClaimType
               AND c.ClaimValue=@ClaimValue;";
        return (await connection.QueryAsync<TUser>(
                    query,
                    new
                    {
                        ClaimType = claim.Type,
                        ClaimValue = claim.Value,
                    })
                .ConfigureAwait(false))
            .AsList();
    }

    /// <summary>
    /// Sets the token value for a particular user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="value">The value of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetTokenAsync(TUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return SetTokenImplAsync(user, loginProvider, name, value, cancellationToken);
    }

    /// <summary>
    /// Deletes a token for a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return RemoveTokenImplAsync(user, loginProvider, name, cancellationToken);
    }

    /// <summary>
    /// Returns the token value.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="loginProvider">The authentication provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return GetTokenImplAsync(user, loginProvider, name, cancellationToken);
    }

    /// <summary>
    /// Sets the authenticator key for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose authenticator key should be set.</param>
    /// <param name="key">The authenticator key to set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    public virtual Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
    {
        return SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);
    }

    /// <summary>
    /// Get the authenticator key for the specified <paramref name="user" />.
    /// </summary>
    /// <param name="user">The user whose security stamp should be set.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user" />.</returns>
    public virtual Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
    {
        return GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);
    }

    /// <summary>
    /// Returns how many recovery code are still valid for a user.
    /// </summary>
    /// <param name="user">The user who owns the recovery code.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The number of valid recovery codes for the user..</returns>
    public virtual Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return CountCodesImplAsync(user, cancellationToken);
    }

    /// <summary>
    /// Updates the recovery codes for the user while invalidating any previous recovery codes.
    /// </summary>
    /// <param name="user">The user to store new recovery codes for.</param>
    /// <param name="recoveryCodes">The new recovery codes for the user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The new recovery codes for the user.</returns>
    public virtual Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var value = string.Join(";", recoveryCodes);
        return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, value, cancellationToken);
    }

    /// <summary>
    /// Returns whether a recovery code is valid for a user. Note: recovery codes are only valid
    /// once, and will be invalid after use.
    /// </summary>
    /// <param name="user">The user who owns the recovery code.</param>
    /// <param name="code">The recovery code to use.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>True if the recovery code was found for the user.</returns>
    public virtual Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        var code2 = code;
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (code2 == null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        return RedeemCodeImplAsync(user, code2, cancellationToken);
    }

    protected virtual async Task SetTokenImplAsync(
        TUser user,
        string loginProvider,
        string name,
        string? value,
        CancellationToken cancellationToken)
    {
        var val = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val == null)
        {
            await AddUserTokenAsync(CreateUserToken(user, loginProvider, name, value)).ConfigureAwait(continueOnCapturedContext: false);
        }
        else
        {
            val.Value = value;
        }
    }

    protected virtual async Task RemoveTokenImplAsync(
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken)
    {
        var val = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (val != null)
        {
            await RemoveUserTokenAsync(val).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected virtual async Task<string?> GetTokenImplAsync(
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken)
    {
        return (await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))?.Value;
    }

    protected virtual async Task<int> CountCodesImplAsync(
        TUser user,
        CancellationToken cancellationToken)
    {
        var text = (await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) ?? string.Empty;
        return text.Length > 0 ? text.Split(';').Length : 0;
    }

    protected virtual async Task<bool> RedeemCodeImplAsync(
        TUser user,
        string code2,
        CancellationToken cancellationToken)
    {
        var source = ((await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) ?? string.Empty).Split(';');
        if (source.Contains(code2, StringComparer.OrdinalIgnoreCase))
        {
            var recoveryCodes = new List<string>(source.Where(s => !string.Equals(
                s,
                code2,
                StringComparison.Ordinal)));
            await ReplaceCodesAsync(user, recoveryCodes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return true;
        }

        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserClaim`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="claim">The associated claim.</param>
    /// <returns></returns>
    protected virtual TUserClaim CreateUserClaim(TUser user, Claim claim)
    {
        var val = new TUserClaim
        {
            UserId = user.Id,
        };
        val.InitializeFromClaim(claim);
        return val;
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserLogin`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="login">The associated login.</param>
    /// <returns></returns>
    protected virtual TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
    {
        return new TUserLogin
        {
            UserId = user.Id,
            ProviderKey = login.ProviderKey,
            LoginProvider = login.LoginProvider,
            ProviderDisplayName = login.ProviderDisplayName,
        };
    }

    /// <summary>
    /// Called to create a new instance of a <see cref="T:Microsoft.AspNetCore.Identity.IdentityUserToken`1" />.
    /// </summary>
    /// <param name="user">The associated user.</param>
    /// <param name="loginProvider">The associated login provider.</param>
    /// <param name="name">The name of the user token.</param>
    /// <param name="value">The value of the user token.</param>
    /// <returns></returns>
    protected virtual TUserToken CreateUserToken(TUser user, string loginProvider, string name, string? value)
    {
        return new TUserToken
        {
            UserId = user.Id,
            LoginProvider = loginProvider,
            Name = name,
            Value = value,
        };
    }

    /// <summary>
    /// Return a user with the matching userId if it exists.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user if it exists.</returns>
    protected async Task<TUser?> FindUserAsync(
        TKey userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT
                  UserName
                 ,NormalizedUserName
                 ,Email
                 ,NormalizedEmail
                 ,EmailConfirmed
                 ,PasswordHash
                 ,SecurityStamp
                 ,ConcurrencyStamp
                 ,PhoneNumber
                 ,PhoneNumberConfirmed
                 ,TwoFactorEnabled
                 ,LockoutEnd
                 ,LockoutEnabled
                 ,AccessFailedCount
             FROM dbo.AspNetUsers
             WHERE Id=@Id;";
        return await connection.QueryFirstOrDefaultAsync<TUser?>(
                query,
                new { Id = userId })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Return a user login with the matching userId, provider, providerKey if it exists.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user login if it exists.</returns>
    protected async Task<TUserLogin?> FindUserLoginAsync(
        TKey userId,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT TOP 1 LoginProvider
                    ,ProviderKey
                    ,ProviderDisplayName
                    ,UserId
              FROM dbo.AspNetUserLogins
              WHERE UserId=@UserId
                AND LoginProvider=@LoginProvider
                AND ProviderKey=@ProviderKey;";
        return await connection.QueryFirstOrDefaultAsync<TUserLogin>(
                query,
                new
                {
                    UserId = userId,
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Return a user login with  provider, providerKey if it exists.
    /// </summary>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="providerKey">The key provided by the <paramref name="loginProvider" /> to identify a user.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user login if it exists.</returns>
    protected async Task<TUserLogin?> FindUserLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT TOP 1 LoginProvider
                    ,ProviderKey
                    ,ProviderDisplayName
                    ,UserId
              FROM dbo.AspNetUserLogins
              WHERE LoginProvider=@LoginProvider
                AND ProviderKey=@ProviderKey;";
        return await connection.QueryFirstOrDefaultAsync<TUserLogin>(
                query,
                new
                {
                    LoginProvider = loginProvider,
                    ProviderKey = providerKey,
                })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Find a user token if it exists.
    /// </summary>
    /// <param name="user">The token owner.</param>
    /// <param name="loginProvider">The login provider for the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The user token if it exists.</returns>
    protected async Task<TUserToken?> FindTokenAsync(
        TUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"SELECT TOP 1 UserId
                    ,LoginProvider
                    ,Name
                    ,Value
              FROM dbo.AspNetUserTokens
              WHERE UserId=@UserId
                AND LoginProvider=@LoginProvider
                AND Name=@Name;";
        return await connection.QueryFirstOrDefaultAsync<TUserToken?>(
                query,
                new
                {
                    UserId = user.Id,
                    LoginProvider = loginProvider,
                    Name = name,
                })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Add a new user token.
    /// </summary>
    /// <param name="token">The token to be added.</param>
    /// <returns></returns>
    protected async Task AddUserTokenAsync(TUserToken token)
    {
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"INSERT INTO dbo.AspNetUserTokens(
                     UserId
                    ,LoginProvider
                    ,Name
                    ,Value
              VALUES (
                     @UserId
                    ,@LoginProvider
                    ,@Name
                    ,@Value);";
        await connection.ExecuteAsync(
                query,
                token)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Remove a new user token.
    /// </summary>
    /// <param name="token">The token to be removed.</param>
    /// <returns></returns>
    protected async Task RemoveUserTokenAsync(TUserToken token)
    {
        ThrowIfDisposed();
        using var connection = ConnectionProvider.Provide();
        const string query =
            @"DELETE FROM dbo.AspNetUserTokens
               WHERE UserId=@UserId
                 AND LoginProvider=@LoginProvider
                 AND Name=@Name
                 AND Value=@Value;";
        await connection.ExecuteAsync(
                query,
                token)
            .ConfigureAwait(false);
    }
}
