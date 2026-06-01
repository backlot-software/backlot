namespace Backlot.Services.LiteDB;

// TODO: public class LiteUnitOfWork : IUnitOfWork, IDisposable
// TODO: {
// TODO:     private readonly ILiteDatabase _db;
// TODO: 
// TODO:     public LiteUnitOfWork()
// TODO:     {
// TODO:         _db = Db.Store;
// TODO:         _db.BeginTrans();
// TODO:     }
// TODO: 
// TODO:     public Task Commit()
// TODO:     {
// TODO:         _db.Commit();
// TODO:         _db.BeginTrans(); // Start a new transaction for subsequent operations if any
// TODO:         return Task.CompletedTask;
// TODO:     }
// TODO: 
// TODO:     public void Dispose()
// TODO:     {
// TODO:         // If not committed, LiteDB will rollback when the transaction is not closed properly or when the database is closed.
// TODO:         // However, it's better to explicitly rollback if it wasn't committed.
// TODO:         // Actually, LiteDatabase.Dispose() might handle things, but here we are using a shared instance from Db.Database.
// TODO:         // Wait, if Db.Database is a singleton Lazy, then we shouldn't dispose it here.
// TODO:         // But we should probably ensure the transaction is closed.
// TODO:         
// TODO:         try 
// TODO:         {
// TODO:              _db.Rollback();
// TODO:         }
// TODO:         catch
// TODO:         {
// TODO:             // ignored
// TODO:         }
// TODO:     }
// TODO: }
