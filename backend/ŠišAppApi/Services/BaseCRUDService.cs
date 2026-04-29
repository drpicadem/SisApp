using MapsterMapper;
using ŠišAppApi.Data;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Models;
using System.Reflection;

namespace ŠišAppApi.Services
{
    public interface ICRUDService<T, TSearch, TInsert, TUpdate>
        where T : class
        where TSearch : class
        where TInsert : class
        where TUpdate : class
    {
        Task<IEnumerable<T>> Get(TSearch search = null);
        Task<T> GetById(int id);
        Task<T> Insert(TInsert request);
        Task<T> Update(int id, TUpdate request);
        Task<T> Delete(int id);
    }

    public class BaseCRUDService<T, TSearch, TDb, TInsert, TUpdate> : ICRUDService<T, TSearch, TInsert, TUpdate>
        where T : class
        where TSearch : class
        where TDb : class
        where TInsert : class
        where TUpdate : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IMapper _mapper;

        public BaseCRUDService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public virtual async Task<IEnumerable<T>> Get(TSearch search = null)
        {
            var entity = _context.Set<TDb>().AsQueryable();

            var list = await entity.ToListAsync();
            return _mapper.Map<List<T>>(list);
        }

        public virtual async Task<T> GetById(int id)
        {
            var entity = await _context.Set<TDb>().FindAsync(id);
            return _mapper.Map<T>(entity);
        }

        public virtual async Task<T> Insert(TInsert request)
        {
            var entity = _mapper.Map<TDb>(request);
            _context.Set<TDb>().Add(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<T>(entity);
        }

        public virtual async Task<T> Update(int id, TUpdate request)
        {
            var entity = await _context.Set<TDb>().FindAsync(id);
            _mapper.Map(request, entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<T>(entity);
        }

        public virtual async Task<T> Delete(int id)
        {
            var entity = await _context.Set<TDb>().FindAsync(id);
            if (entity != null)
            {
                if (!TryApplySoftDelete(entity))
                {
                    _context.Set<TDb>().Remove(entity);
                }
                await _context.SaveChangesAsync();
            }
            return _mapper.Map<T>(entity);
        }

        private static bool TryApplySoftDelete(TDb entity)
        {
            var entityType = entity.GetType();
            var isDeletedProp = entityType.GetProperty("IsDeleted", BindingFlags.Public | BindingFlags.Instance);
            if (isDeletedProp == null || isDeletedProp.PropertyType != typeof(bool) || !isDeletedProp.CanWrite)
            {
                return false;
            }

            isDeletedProp.SetValue(entity, true);

            var deletedAtProp = entityType.GetProperty("DeletedAt", BindingFlags.Public | BindingFlags.Instance);
            if (deletedAtProp != null && deletedAtProp.CanWrite && deletedAtProp.PropertyType == typeof(DateTime?))
            {
                deletedAtProp.SetValue(entity, DateTime.UtcNow);
            }

            var isActiveProp = entityType.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            if (isActiveProp != null && isActiveProp.CanWrite && isActiveProp.PropertyType == typeof(bool))
            {
                isActiveProp.SetValue(entity, false);
            }

            return true;
        }
    }
}
