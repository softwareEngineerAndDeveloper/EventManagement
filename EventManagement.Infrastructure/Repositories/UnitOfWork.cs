using EventManagement.Domain.Entities;
using EventManagement.Domain.Interfaces;
using EventManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventManagement.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IDbContextTransaction? _transaction;
        private bool _disposed;
        
        private IRepository<Tenant>? _tenantRepository;
        private IRepository<User>? _userRepository;
        private IRepository<Event>? _eventRepository;
        private IRepository<Registration>? _registrationRepository;
        private IRepository<Role>? _roleRepository;
        private IRepository<UserRole>? _userRoleRepository;
        
        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public IRepository<Tenant> Tenants => _tenantRepository ??= new Repository<Tenant>(_dbContext);
        public IRepository<User> Users => _userRepository ??= new Repository<User>(_dbContext);
        public IRepository<Event> Events => _eventRepository ??= new Repository<Event>(_dbContext);
        public IRepository<Registration> Registrations => _registrationRepository ??= new Repository<Registration>(_dbContext);
        public IRepository<Role> Roles => _roleRepository ??= new Repository<Role>(_dbContext);
        public IRepository<UserRole> UserRoles => _userRoleRepository ??= new Repository<UserRole>(_dbContext);
        
        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
        
        public async Task BeginTransactionAsync()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }
        
        public async Task CommitTransactionAsync()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
        
        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _dbContext.Dispose();
                _transaction?.Dispose();
            }
            _disposed = true;
        }
    }
} 