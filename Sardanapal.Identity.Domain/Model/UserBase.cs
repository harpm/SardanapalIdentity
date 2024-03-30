﻿using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IUserBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string Username { get; set; }
    string HashedPassword { get; set; }
    string Email { get; set; }
    long PhoneNumber { get; set; }
}

public class UserBase<TKey, TRole, TUR> : BaseEntityModel<TKey>
    , IUserBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TRole : class, IRoleBase<TKey>
    where TUR : class, IUserRoleBase<TKey>
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string Username { get; set; }
    public virtual string HashedPassword { get; set; }
    public virtual string Email { get; set; }
    public virtual long PhoneNumber { get; set; }
    public virtual ICollection<TUR> UserRoles { get; set; }
        = new HashSet<TUR>();
}