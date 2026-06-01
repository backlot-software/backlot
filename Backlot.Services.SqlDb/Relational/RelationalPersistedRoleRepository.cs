using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Security;
using Backlot.Core.Services;
using Backlot.Services.SqlDb;
using SqlKata.Execution;

// ReSharper disable ConvertToUsingDeclaration

namespace Backlot.Experimental.Services.SqlDb.Relational.Experimental;

// todo: EXPERIMENTAL we can get primarykey field names from the [ViewPrimaryKeyMetadata]
//       to optimize this we can create a configuration of a default primary key field name, f.e. invessed has the same field name for every "role"/table.

// todo: add caching for metadata quering

// todo: We have one problem not yet fixed and that's that roles can not start with an I because we have reserved that for Interface naming conventions!


public class RelationalPersistedRoleRepository : BasePersistedRoleRepository
{

    
    public override void Terminate(string key)
    {
        throw new NotImplementedException("DELETE NOT YET IMPLEMENTED");
    }

    public override void FlushDb()
    {
        throw new ApplicationException("For safety reasons. We do not support flushing complete SQL Databases.");
    }

    public override IEnumerable<IRole> GetAll(Type objType, int page, int pageSize, out int total, IEnumerable<Criteria> criteria = null,
        DateTimeOffset? from=null, DateTimeOffset? till=null, string orderby=null)
    {
        var tablename = objType.GetRoleName() + "s"; //todo: get tablename from tableinfo attribute or configuration files or from metadata stored in the database MPRT (field C#Type??)

        using (var db = Db.Store())
        {
            var query = db.Query(tablename)
                .Select();

            foreach (var c in criteria ?? [])
            {
                if (c.ConditionEnum == ConditionEnum.ct)
                {
                    /* todo: Only with these legacy systems we not need to map the fieldname used in the query (and defined in the role) to the actual field of the table.
                     * ADR in the making: therefor we need either a required mapping attribute on top of any role field representing something from a table, or we require names have to be the equal.
                     * We need to take in mind https://github.com/Chuhukon/Backlot/blob/main/_adr/docs/decisions/0001-Save-with-role-property.md#positive-consequences
                     */
                    query = query.WhereContains(c.Field, c.Value);
                }
                else
                {
                    var op = c.ConditionEnum == ConditionEnum.gt ? ">" : // greater then
                        c.ConditionEnum == ConditionEnum.lt ? "<" : // less then
                        "="; // equal

                    query = query.Where(c.Field, op, c.Value);
                }
            }

            total = query.Clone().Count<int>();

            if (!string.IsNullOrWhiteSpace(orderby))
                query = query.OrderBy(orderby);

            //todo: query.ForPage(page, pageSize);

            var result = db.GetDictionary(query)
                .Select(r => r.PresentsType(objType,
                    (p, o) => MetaDataInitializer.Initialize(p as IPersist, o as IDictionary<string, object>)));

            return result;
        }
    }

    public override IEnumerable<T> GetAll<T>(int page, int pageSize, out int total, IEnumerable<Criteria> criteria = null, DateTimeOffset? from = null,
        DateTimeOffset? till = null, string orderby = null)
    {
        return GetAll(typeof(T), page, pageSize, out total, criteria, from, till, orderby).OfType<T>();
    }

    public override Task<IEnumerable<IPersist>> GetBulk(IEnumerable<RoleReference> refereces, bool includeNoAccess = false)
    {
        throw new NotImplementedException();
    }

    // public override IEnumerable<IPersist> GetRelated(Type objType, RoleReference parent, int page, int pageSize, out int total,
    //     IEnumerable<Criteria> criteria = null, string orderby = null)
    // {
    //     /* EXAMPLE:
    //      
    //      -- INS you are the PARENT TABLE
    //     exec sp_GetForeignKeyReferences 'portfolios'
    //     -- OUTS you are the CHILD TABLE
    //     exec sp_GetForeignKeyReferences 'holdings'
    // 
    //     -- IN: // query the child table - holdings is the child here
    //     SELECT holdings.* FROM holdings
    //     where holdings.portfolioId = '99EC7E59-3669-EF11-9C33-002248435B16'
    // 
    //     -- OUT: // query the parent table - portfolio is the parent here
    //     SELECT portfolios.* FROM portfolios
    //     INNER JOIN holdings ON holdings.portfolioId = portfolios.guid
    //     where holdings.guid = '5143FD92-0C71-EF11-9C35-6045BD107A47'
    //     
    //     */
    //     
    //     var relations = new List<IPersist>();
    //     var anchorMetaData = MetaData.Get(parent); // the anchor everything is related to (either parent or child)
    //     
    //     var referenceMetaData = Db.Store.Select("exec sp_GetForeignKeyReferences @tbl",
    //         new { tbl = anchorMetaData.Tablename });
    // 
    //     foreach (var reference in referenceMetaData)
    //     {
    //         if (reference.DIRECTION == "IN") // IN:
    //         {
    //             var sql =
    //                 $"SELECT {reference.CHILD_TABLE_NAME}.* FROM {reference.CHILD_TABLE_NAME} WHERE {reference.CHILD_TABLE_NAME}.{reference.CHILD_COLUMN_NAME} = @uid";
    //             var results = Db.Store.Select(sql,
    //                     new { uid = parent.Uid }).Cast<IDictionary<string, object>>();
    //             
    //             foreach (var res in results)
    //             {
    //                 relations.Add(res.PresentsType(objType, (p, o) => MetaDataInitializer.Initialize(p as IPersist, o as IDictionary<string, object>)) as IPersist);
    //             }
    //         }
    //         else // OUT:
    //         {
    //             var sql =
    //                 $"SELECT {reference.PARENT_TABLE_NAME}.* FROM {reference.PARENT_TABLE_NAME} INNER JOIN {reference.CHILD_TABLE_NAME} ON {reference.CHILD_TABLE_NAME}.{reference.CHILD_COLUMN_NAME} = {reference.PARENT_TABLE_NAME}.{reference.PARENT_COLUMN_NAME} WHERE";
    //             
    //             var param = new Dictionary<string, object>();
    //             foreach(var pk in anchorMetaData.Pkvalues) // when the primary key is a combination of fields split by ~
    //             {
    //                 sql += $" {anchorMetaData.Tablename}.{pk.Key} = @{pk.Key} AND";
    //                 param.Add(pk.Key, pk.Value);
    //             } if(sql.EndsWith("AND")) sql = sql.Substring(0, sql.Length - 4); // sql remove last AND
    //             
    //             var results = Db.Store.Select(sql, param).Cast<IDictionary<string, object>>();
    //             foreach (var res in results)
    //             {
    //                 relations.Add(res.PresentsType(objType, (p, o) => MetaDataInitializer.Initialize(p as IPersist, o as IDictionary<string, object>)) as IPersist);
    //             }
    //         }
    //     }
    // 
    //     total = relations.Count;
    //     return relations;
    // }

    /// <summary>
    /// Sql Servers do not support revisions. Therefor Backlot only supports this for NoSql implementations and bigstorage(s)
    /// </summary>
    /// <param name="uid"></param>
    /// <typeparam name="TR"></typeparam>
    /// <returns></returns>
    public override IEnumerable<Revision> GetRevisions<TR>(string uid)
    {
        // revisions are not supported with mssql.
        // by default it has to return the same object as TryGet does or an empty collection when nothing is available.
        
        // todo: log debug information GetRevisions is called but not supported within this installation.
        
        if (TryGet<TR>(uid, out var result))
        {
            return
            [
                new Revision
                {
                    Reference = result.GetReference(),
                    Checksum = result.GetChecksum(),
                    Content = result
                }
            ];
        }
        
        return [];
    }

    public override bool TryGetPermission(string uid, out Permission permission)
    {
        using (var db = Db.Store())
        {
            var itm = db.Query("MetaPersistedRoleTable")
                .Where("Uid", uid).First();

            if (itm != null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute : Permission is set to not null during database initialization.
                permission = Permission.Deserialize(itm.Permission as string);
            }

            permission = Permission.Create(PermissionLevel.None);
            return false;
        }
    }

    protected override Task<TRole> Store<TRole>(TRole role)
    {
        // get the role skills
        // then check if there is a skill with a tableinfo attribute
        // if so use that to update the table
        // if there are more than one, update all
        // if there are none check insert and update it in the bigstorage -- a feature we have to implement in the future.
        
        // also be aware we have selfs and proxied roles.
        
        
        // create or update statement using SqlKata
        // // // var tableName = role.GetType().GetRoleName();
        // // // 
        // // // var data = new Dictionary<string, object>();
        // // // // todo: dictionary of data fields to update (only changed fields).
        // // // 
        // // // var db = ServiceLocator.Get<QueryFactory>();
        // // // 
        // // // // try update first
        // // // var updated = Db.Store.Query(tableName)
        // // //     .Where("Uid", role.Uid)
        // // //     .Update(data);
        // // // 
        // // // // if update affected no rows, perform an insert
        // // // if (updated == 0)
        // // // {
        // // //     Db.Store.Query(tableName)
        // // //         .Insert(role as Dictionary<>);
        // // // }
        
        return new Task<TRole>(() => role);
    }

    /// <summary>
    /// Try get the specified role based on the given uid and type.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="objType"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected override bool TryGetType(string uid, Type objType, out IRole obj)
    {
        try
        {
            var metadata = MetaData.Get(uid);

            using (var db = Db.Store())
            {
                var query = db.Query(metadata.Tablename)
                    .Select();

                foreach (var pk in metadata.Pkvalues)
                {
                    query = query.Where(pk.Key, pk.Value);
                }

                var result = db.GetDictionary(query).FirstOrDefault();

                obj = default;
                if (result == null) return false;

                //todo: this Loader.GetRoleByName(metadata.Tablename), must be the same as objType

                obj = result.PresentsType(objType,
                    (p, o) => MetaDataInitializer.Initialize(p as IPersist, o as IDictionary<string, object>));
                return true;
            }
        }
        catch
        {
            obj = default;
            return false;
        }
    }

    
}




