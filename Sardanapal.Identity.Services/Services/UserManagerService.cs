﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Domain.Options;
using Sardanapal.Identity.Services.Statics;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services
{
    public interface IUserManagerService<TKey, TUser, TRole>
        where TKey : IComparable<TKey>, IEquatable<TKey>
        where TUser : IUserBase<TKey>, new()
        where TRole : IRoleBase<byte>, new()
    {
        Task<TUser?> GetUser(string username = null, string email = null, long? phoneNumber = null);
        Task<string> Login(string username, string password);
        Task<TKey> RegisterUser(string username, string password);
        Task<TKey> RegisterUser(long phonenumber, string firstname, string lastName);
        Task<TKey> RegisterUser(string email, string firstname, string lastName);
        void EditUserData(TKey id, string username = null
            , string password = null
            , long? phonenumber = null
            , string email = null
            , string firstname = null
            , string lastname = null);
    }

    public class UserManagerService<TKey, TUser, TRole> : IUserManagerService<TKey, TUser, TRole>
        where TKey : IComparable<TKey>, IEquatable<TKey>
        where TUser : class, IUserBase<TKey>, new()
        where TRole : class, IRoleBase<byte>, new()
    {
        protected virtual byte _currentRole { get; }
        protected SdIdentityUnitOfWorkBase<TKey> _context;
        protected IdentityInfo _info;

        public DbSet<TUser> Users
        {
            get
            {
                return _context.Set<TUser>();
            }
        }

        public DbSet<TRole> Roles
        {
            get
            {
                return _context.Set<TRole>();
            }
        }

        public UserManagerService(SdIdentityUnitOfWorkBase<TKey> context, IOptions<IdentityInfo> info, byte curRole)
        {
            _context = context;
            _info = info.Value;
            _currentRole = curRole;
        }

        public async Task<TUser?> GetUser(string username = null, string email = null, long? phoneNumber = null)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                return await Users.AsNoTracking()
                    .Where(x => x.Username == username)
                    .FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                return await Users.AsNoTracking()
                    .Where(x => x.Email == email)
                    .FirstOrDefaultAsync();
            }
            else if (phoneNumber.HasValue)
            {
                return await Users.AsNoTracking()
                    .Where(x => x.PhoneNumber == phoneNumber)
                    .FirstOrDefaultAsync();
            }
            else
            {
                return null;
            }
        }

        public async void EditUserData(TKey id, string username = null, string password = null, long? phonenumber = null, string email = null, string firstname = null, string lastname = null)
        {
            var user = await _context.Users.Where(x => x.Id.Equals(id)).FirstAsync();

            if (string.IsNullOrWhiteSpace(username))
                user.Username = username;

            if (string.IsNullOrWhiteSpace(password))
                user.HashedPassword = password;

            if (phonenumber.HasValue)
                user.PhoneNumber = phonenumber.Value;

            if (string.IsNullOrWhiteSpace(email))
                user.Email = email;

            if (string.IsNullOrWhiteSpace(firstname))
                user.FirstName = firstname;

            if (string.IsNullOrWhiteSpace(lastname))
                user.LastName = lastname;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<string> Login(string username, string password)
        {
            var md5Pass = await Utilities.EncryptToMd5(password);
            
            var user = await _context.Users.Where(x => x.Username == username
                && x.HashedPassword == md5Pass)
                .FirstAsync();

            string token = await Utilities.GenerateToken(_info.SecretKey
                , _info.Issuer
                , _info.Audience
                , username
                , _currentRole.ToString()
                , _info.ExpirationTime);

            return token;
        }

        public async Task<TKey> RegisterUser(string username, string password)
        {
            var  hashedPass = await Utilities.EncryptToMd5(password);
            var newUser = new TUser()
            {
                Username = username,
                HashedPassword = hashedPass
            };

            await _context.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return newUser.Id;
        }

        public async Task<TKey> RegisterUser(long phonenumber, string firstname, string lastName)
        {
            var newUser = new TUser()
            {
                PhoneNumber = phonenumber,
                FirstName = firstname,
                LastName = lastName
            };

            await _context.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return newUser.Id;
        }

        public async Task<TKey> RegisterUser(string email, string firstname, string lastName)
        {
            var newUser = new TUser()
            {
                Email = email,
                FirstName = firstname,
                LastName = lastName
            };

            await _context.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return newUser.Id;

        }
    }
}
