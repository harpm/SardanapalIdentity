﻿using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IRole<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}
