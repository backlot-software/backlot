// using Backlot.Core;
// using Backlot.Core.Abstraction.Scenarios;
// using Backlot.Core.Json;
// using Backlot.Core.Security;
// using GraphQL.Net;
// using Newtonsoft.Json.Linq;
//
// namespace Backlot.Functions.Scenarios.GraphQl;
//
// [Scenario(typeof(Ql), access: new []{ Access.Admin })]
// public class Ql : Scenario<IGraph, object>
// {
//     public Ql(IGraph role) : base(role)
//     {
//     }
//
//     protected override object Exec()
//     {
//         //return false;
//         var query = Role.Query;
//         
//         var schema = GraphQL.Net.GraphQL<IRole>.CreateDefaultSchema();//.Create //.Types.Schema.For(Role.Play<Schema, string>());
//
//         var json = schema.ExecuteAsync(_ =>
//         {
//             _.Query = query;
//             _.Root = new { 
//                 //this works;
//                 Persist = new Example()
//                 //this not;
//                 //Persist = new[]
//                 //{
//                 //    new Example(),
//                 //    new Example(),
//                 //}
//             };
//         }).Result;
//
//         return Json.DeSerialize<JContainer>(json);
//         // todo: get the IGraphQlRequest request from the request.
//         //throw new System.NotImplementedException();
//         
//     }
//     
//     /*
//     [Obsolete("Example only")]
//     private class Example : IRole
//     {
//         public string Uid => "EF27B30B-735B-4C9C-BB82-BAA0B0B320A4";
//         public string Name => "Example";
//     }
//     */
// }
//
