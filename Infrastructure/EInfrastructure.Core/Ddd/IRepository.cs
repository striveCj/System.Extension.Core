﻿namespace EInfrastructure.Core.Ddd
{
    public interface IRepository<TEntity,T> where TEntity : IAggregateRoot<T> where T:struct 
    {
        TEntity FindById(T id);

        void Add(TEntity entity);

        void Remove(TEntity entity);

        void Update(TEntity entity);

        TEntity LoadIntegrate(T id);
    }
}