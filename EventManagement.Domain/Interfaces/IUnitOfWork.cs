using EventManagement.Domain.Entities;

namespace EventManagement.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable //Disposeble pattern : Bu, bellek y�netimi a��s�ndan �nemlidir. 
    {
        IRepository<Tenant> Tenants { get; } //Repository pattern : Bu, veritabanı işlemlerini soyutlar.
        IRepository<User> Users { get; }     
        IRepository<Event> Events { get; }
        IRepository<Registration> Registrations { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
} 