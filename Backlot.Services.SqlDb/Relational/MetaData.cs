using Backlot.Core;
using Backlot.Services.SqlDb;
using SqlKata.Execution;

namespace Backlot.Experimental.Services.SqlDb.Relational.Experimental;

public static class MetaData
{
    #region defaults
    
    /// <summary>
    /// When a primary key is a combinatior between 2 or more fields, this is the default seperator.
    /// </summary>
    private const string DefaultPrimaryKeySeperator = "~";
    
    /// <summary>
    /// The name of the MPRT = Master Persisted Role Table.
    /// This table contains the metadata of all roles and their primary keys.
    /// </summary>
    private const string MprTableName = "MetaPersistedRoleTable";
    
    #endregion
    
    public static (string Tablename, IDictionary<string, object> Pkvalues) Get(RoleReference reference)
    {
        return Get(reference.Uid);
    }

    /// <summary>
    /// Return the metadata which can be used to build a where clause for the given key.
    /// </summary>
    /// <param name="reference"></param>
    /// <param name="uid"></param>
    /// <returns>Tablename and a dictionary of the primarykey values and theire columnnames</returns>
    public static (string Tablename, IDictionary<string, object> Pkvalues) Get(string uid)
    {
        
        //todo: caching is needed here.
        using (var db = Db.Store())
        {
            var query = db.Query("ViewPrimaryKeyValue as vpkv")
                .Join("ViewPrimaryKeyMetadata as vpkm", "vpkv.TABLE_NAME", "vpkm.TABLE_NAME")
                .Select("vpkv.TABLE_NAME", "vpkv.PK_VALUE")
                .SelectRaw(
                    $"STRING_AGG(vpkm.COLUMN_NAME, '{DefaultPrimaryKeySeperator}') WITHIN GROUP (ORDER BY vpkm.ORDINAL_POSITION) AS COLUMN_NAMES")
                .Where("PK_VALUE", uid)
                .GroupBy("vpkv.TABLE_NAME", "vpkv.PK_VALUE");

            // debug: var sql = Compiler.Compile(query).Sql;

            var itm = query.First();

            if (itm == null) return default;

            var dic = new Dictionary<string, object>();
            var columnnames =
                (itm.COLUMN_NAMES as string)?.Split(
                    DefaultPrimaryKeySeperator); // column_names and values are always int the same order and do have an equeal total of items.
            var pkvalues = (itm.PK_VALUE as string)?.Split(DefaultPrimaryKeySeperator);
            for (var i = 0; i < columnnames?.Count(); i++)
            {
                //todo: currently prepared for guids and strings, we need support for others (like integers/numbers)

                dic.Add(columnnames[i], pkvalues?[i]);
            }

            return (itm.TABLE_NAME as string, dic);
        }
    }
    
}