using System.Linq;
using Backlot.Core;
using Backlot.Services.SqlDb.Dynamic;
using Backlot.Testing.Core;
using NUnit.Framework;
using SqlKata.Compilers;
using static Backlot.Testing.Persistence.CriteriaSets;

namespace Backlot.Testing.Persistence;

/*
 * Namingconvention; MethodNameToTest_StateUnderTest_ExpectedBehavior
 *
 * DynamicPersistedRoleRepository.SelectValidCriteria and BuildQuery are pure builders, and compiling
 * a SqlKata query opens no connection, so no database is needed here. The repository itself is never
 * constructed either, which is what keeps Db.Store() out of the picture.
 */
public class SqlDbDynamicCriteria
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    private static string Build(params Criteria[] criteria)
    {
        var valid = DynamicPersistedRoleRepository.SelectValidCriteria(typeof(IFormula), criteria);
        var query = DynamicPersistedRoleRepository.BuildQuery(typeof(IFormula), valid);
        return Normalize(new SqlServerCompiler().Compile(query).Sql);
    }

    private static string Shape(params Criteria[] criteria) => MaskParameters(Build(criteria));

    [Test]
    public void BuildQuery_NoCriteria_OnlySkillAndReadPermissionFiltersAndNoOpenJson()
    {
        #region ACT

        var sql = Build();

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("[r].[CanRead] = cast(1 as bit)"));
        Assert.That(sql, Does.Contain("EXISTS (SELECT 1 FROM STRING_SPLIT(r.Skills, ',') as s WHERE [s].[value] = @p0)"));
        Assert.That(sql, Does.Contain("COALESCE(r.GroupsCanRead, '') = '' AND COALESCE(r.UsersCanRead, '') = ''"),
            "a role without any user or group level falls back to its mask");
        Assert.That(sql, Does.Contain("STRING_SPLIT(r.UsersCanRead, ',') as u"));
        Assert.That(sql, Does.Contain("STRING_SPLIT(r.GroupsCanRead, ',') as g"));
        Assert.That(sql, Does.Not.Contain("OPENJSON"), "without json backed criteria there is nothing to unpack");
        Assert.That(sql, Does.Contain("ORDER BY [LastModified]"), "the default ordering");

        #endregion
    }

    [Test]
    public void BuildQuery_SingleEqCriterion_NoRedundantBrackets()
    {
        #region ACT

        var sql = Shape(Eq("Operation", "sum"));

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_TwoEqOnSameField_OrJoined()
    {
        #region ACT

        var sql = Shape(TwoEqOnOneField);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p OR [jsn].[Operation] = @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_EqAndCtOnSameField_OrJoined()
    {
        #region ACT

        var sql = Shape(EqAndCtOnOneField);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p OR LOWER([jsn].[Operation]) like @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_LtAndGtOnSameField_AndJoined()
    {
        #region ACT

        var sql = Shape(RangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Number1] < @p AND [jsn].[Number1] > @p))"),
            "a range narrows, so it is And-ed; lt sorts before gt");

        #endregion
    }

    [Test]
    public void BuildQuery_MatchAndRangeOnSameField_BothPartsBracketedAndAndJoined()
    {
        #region ACT

        var sql = Shape(MatchAndRangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Number1] = @p) AND ([jsn].[Number1] < @p AND [jsn].[Number1] > @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_MatchesAndRangeOnSameField_BothPartsBracketedAndAndJoined()
    {
        #region ACT

        var sql = Shape(MatchesAndRangeOnOneField);

        #endregion

        #region ASSERT

        // AND binds stronger than OR in sql, so without brackets of its own the Or-ed part would read
        // as 'jsn.Number1 = @p OR (jsn.Number1 like @p AND range)'.
        Assert.That(sql, Does.Contain(
            "WHERE (([jsn].[Number1] = @p OR LOWER([jsn].[Number1]) like @p) AND ([jsn].[Number1] < @p AND [jsn].[Number1] > @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_CriteriaOnDifferentFields_GroupsAndJoined()
    {
        #region ACT

        var sql = Shape(TwoGroups);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p)) AND (([jsn].[Number1] = @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_CtGroupFollowedByAnotherGroup_GroupsAndJoined()
    {
        #region ACT

        var sql = Shape(CtGroupThenSecondGroup);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE ((LOWER([jsn].[Operation]) like @p)) AND (([jsn].[Number1] = @p))"));
        Assert.That(sql, Does.Not.Contain("like @p)) OR (("),
            "a preceding ct may never turn the operator between two groups into an Or");

        #endregion
    }

    [Test]
    public void BuildQuery_CtBeforeEqInInput_EqEmittedFirst()
    {
        #region ACT

        var sql = Shape(CtBeforeEqInInput);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p OR LOWER([jsn].[Operation]) like @p))"),
            "conditions within a group are ordered by their ConditionEnum, so eq comes before ct");

        #endregion
    }

    [Test]
    public void BuildQuery_UnknownCondition_TreatedAsEq()
    {
        #region ACT

        var sql = Shape(new Criteria { Field = "Operation", Condition = "xx", Value = "sum" });

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain("WHERE (([jsn].[Operation] = @p))"));

        #endregion
    }

    [Test]
    public void BuildQuery_UidAndNameCriteria_ShrinkTheBaseInsteadOfTheJson()
    {
        #region ACT

        var sql = Build(Eq("Uid", "UnitTestObject-1"), Ct("Name", "Joh"));

        #endregion

        #region ASSERT

        // Uid and Name are indexed columns of their own, so they narrow the base CTE and never need
        // the json to be unpacked for them.
        Assert.That(sql, Does.Contain("[r].[Uid] = @p"));
        Assert.That(sql, Does.Contain("LOWER([r].[Name]) like @p"));

        #endregion
    }

    [Test]
    public void BuildQuery_OrderbyJsonField_PrefixedWithTheJsonAliasAndSanitized()
    {
        #region ACT

        var sql = Normalize(new SqlServerCompiler().Compile(
            DynamicPersistedRoleRepository.BuildQuery(typeof(IFormula),
                DynamicPersistedRoleRepository.SelectValidCriteria(typeof(IFormula), [Eq("Operation", "sum")]),
                orderby: "Oper ation!")).Sql);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.EndWith("ORDER BY [jsn].[Operation]"));

        #endregion
    }

    [Test]
    public void BuildQuery_OrderbyIndexedColumn_NotPrefixed()
    {
        #region ACT

        var sql = Normalize(new SqlServerCompiler().Compile(
            DynamicPersistedRoleRepository.BuildQuery(typeof(IFormula), [], orderby: nameof(IPersist.Name))).Sql);

        #endregion

        #region ASSERT

        Assert.That(sql, Does.EndWith("ORDER BY [Name]"));

        #endregion
    }

    [Test]
    public void BuildQuery_CriteriaValueTypes_MappedOntoAnOpenJsonColumnType()
    {
        #region ACT

        var sql = Build(Eq("Operation", "sum"), Eq("Number1", 1950));

        #endregion

        #region ASSERT

        Assert.That(sql, Does.Contain($"Operation NVARCHAR({DynamicPersistedRoleRepository.DefaultMaxStringLength}) '$.Operation'"));
        Assert.That(sql, Does.Contain("Number1 FLOAT '$.Number1'"));

        #endregion
    }

    [Test]
    public void SelectValidCriteria_FieldThatIsNotOnTheRole_Dropped()
    {
        #region ACT

        var valid = DynamicPersistedRoleRepository.SelectValidCriteria(typeof(IFormula),
            [Eq("Operation", "sum"), Eq("NotAFieldOfIFormula", "x")]);

        #endregion

        #region ASSERT

        // the fieldname ends up in the generated OPENJSON WITH, so an unknown one would be a sql error.
        Assert.That(valid.Select(c => c.Field), Is.EqualTo(new[] { "Operation" }));

        #endregion
    }

    [Test]
    public void SelectValidCriteria_NoCriteria_Empty()
    {
        #region ACT & ASSERT

        Assert.That(DynamicPersistedRoleRepository.SelectValidCriteria(typeof(IFormula), null), Is.Empty);

        #endregion
    }

    // - - - the generated OPENJSON has to stay valid t-sql whatever the criteria are: sql server
    //       rejects a WITH clause that names a column twice, and a jsn.<field> that it never declared.

    [Test]
    public void BuildQuery_SeveralCriteriaOnOneJsonField_DeclaresThatColumnOnlyOnce()
    {
        #region ACT

        var columns = OpenJsonColumns(Build(MatchesAndRangeOnOneField));

        #endregion

        #region ASSERT

        Assert.That(columns, Is.Not.Empty, "the json has to be unpacked for a json backed criterion");
        Assert.That(columns, Is.Unique,
            "sql server rejects 'The column name is specified more than once in the WITH clause of OPENJSON'");

        #endregion
    }

    [Test]
    public void BuildQuery_JsonFieldCombinedWithANameCriterion_DeclaresEveryFieldItAddresses()
    {
        #region ARRANGE

        // (Name = 'John' OR Name ct 'Doe') AND (Number1 > 1901 AND Number1 < 1998)
        Criteria[] criteria = [Eq("Name", "John"), Ct("Name", "Doe"), Gt("Number1", 1901), Lt("Number1", 1998)];

        #endregion

        #region ACT

        var sql = Build(criteria);

        #endregion

        #region ASSERT

        Assert.That(JsnReferences(sql), Is.SubsetOf(OpenJsonColumns(sql)),
            "every field addressed through the json alias has to be declared in the OPENJSON WITH");

        #endregion
    }
}
