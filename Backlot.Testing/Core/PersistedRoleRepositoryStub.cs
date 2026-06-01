using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Security;
using Backlot.Core.Services;

namespace Backlot.Testing.Core
{
    internal class PersistedRoleRepositoryStub : BasePersistedRoleRepository
    {
        public bool SetPersisted { get; set; }
        public int StoreCallCount { get; private set; }
        public IRole TryGetOutRole { get; set; } = null!;
        public bool SetTryGet { get; set; }

        public override void Terminate(string key)
        {
            throw new NotImplementedException();
        }

        public override void FlushDb()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null,
            DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> GetAll<T>(int page, int pageSize, out int total,
            IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null, DateTimeOffset? till = null,
            string orderby = null)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> refereces, bool includeNoAccess = false)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Revision> GetRevisions<TR>(string uid)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetPermission(string uid, out Permission permission)
        {
            permission = Permission.Create(PermissionLevel.ReadWriteExecute);
            return true;
        }

        protected override Task<TRole> Store<TRole>(TRole role)
        {
            StoreCallCount++;
            return  Task.FromResult(role);
            
        }


        protected override bool TryGetType(string uid, Type objType, out IRole obj)
        {
            obj = TryGetOutRole;
            return SetTryGet;
        }
    }
}