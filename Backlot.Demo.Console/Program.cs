// See https://aka.ms/new-console-template for more information

// YOU NEED :
// 1) an implementation of IUserContext, to manage the permissions and context of the user.
// 2) an implementation IFileSystem to get access to configuration files etc.
// 3) you

using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Defaults.Roles;
using Backlot.Defaults.Scenarios.Persistance;
using Backlot.Defaults.Scenarios.Query;
using Backlot.Demo.Console;
using Backlot.Demo.Console.Models;
using Backlot.Demo.Console.Roles;
using Backlot.Demo.Console.Scenarios;
using Newtonsoft.Json;

Setup.ForConsoles();

var formula = new
{
    Uid = "110D4DF5-DBDC-4150-9CFA-7445D9B7BB84",
    Number1 = 7,
    Number2 = 9,
    Op = "sum" // "op" alias does represent the operation property of IFormula -- Test with Typed object!
}.Presents<IFormula>();

// scenario based;
var result = await Calculate
    .With(formula)
    .Play();

// role based / fluent; var result = await formula.PlayAsync<Calculate>();

Console.WriteLine($"THE RESULT: {result.Uid} {result.Outcome}");

Console.WriteLine(JsonConvert.SerializeObject(await Detail.Play(new { For = result.GetReference() })));

var results = await Find.Play(new SimpleQuery<IResult>
{
    Criteria =
    [
        new Criteria {
            Field = "Info",
            Condition = "ct",
            Value = "24", // the calculate function adds the date to the info.
        }
    ]
});

results.Results.OfSkill<IResult>().ToList()
    .ForEach(r => Console.WriteLine($"Found with a criteria, the name is: {r.Info}"));
