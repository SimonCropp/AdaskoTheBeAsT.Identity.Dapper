using System;
using Microsoft.AspNetCore.Identity;

namespace Sample;

public class ApplicationUserToken
    : IdentityUserToken<Guid>
{
}
