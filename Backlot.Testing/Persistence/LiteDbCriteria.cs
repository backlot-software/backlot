using System;
using Backlot.Services.LiteDB;
using Backlot.Testing.Core;
using LiteDB;
using NUnit.Framework;
using static Backlot.Testing.Persistence.CriteriaSets;

namespace Backlot.Testing.Persistence;

/*
 * Namingconvention; MethodNameToTest_StateUnderTest_ExpectedBehavior
 *
 * LitePersistedRoleRepository.BuildQueryExpression is pure, so no database is opened here: the
 * generated BsonExpression is asserted on its rendered Source, and - stronger, because it can not
 * drift with LiteDb's formatting - by evaluating it against an in memory document.
 */
public class LiteDbCriteria
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    private static string Build(params Backlot.Core.Criteria[] criteria) =>
        LitePersistedRoleRepository.BuildQueryExpression(typeof(IFormula), criteria, null, null).Source;

    /// <summary>A StoreEntity shaped document that the current test user is allowed to read.</summary>
    private static BsonDocument Entity(string name, int number1) =>
        new()
        {
            ["_id"] = "UnitTestObject-1",
            ["CanRead"] = true,
            ["Skills"] = new BsonArray { "Formula" },
            ["UsersCanRead"] = new BsonArray { UserCtx.UserNameStatic },
            ["GroupsCanRead"] = new BsonArray(),
            ["LastModified"] = DateTime.UtcNow,
            ["Permission"] = "m::7",
            ["Data"] = new BsonDocument { ["Name"] = name, ["Number1"] = number1 }
        };

    private static bool Matches(Backlot.Core.Criteria[] criteria, string name, int number1) =>
        LitePersistedRoleRepository
            .BuildQueryExpression(typeof(IFormula), criteria, null, null)
            .ExecuteScalar(Entity(name, number1))
            .AsBoolean;

    [Test]
    public void BuildQueryExpression_NoCriteria_OnlySkillAndReadPermissionFilters()
    {
        #region ACT

        var source = Build();

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.CanRead=true"));
        Assert.That(source, Does.Contain("$.Skills[*] ANY=\"Formula\""));
        Assert.That(source, Does.Contain($"$.UsersCanRead[*] ANY=\"{UserCtx.UserNameStatic}\""));
        Assert.That(source, Does.Contain("$.GroupsCanRead[*] ANY=\"Admin\""));
        Assert.That(source, Does.Contain("$.GroupsCanRead[*] ANY=\"*\""), "wildcard groups have to be supported");
        Assert.That(source, Does.Contain("COUNT($.UsersCanRead)=0 AND COUNT($.GroupsCanRead)=0"),
            "a role without any user or group level falls back to its mask");
        Assert.That(source, Does.Not.Contain("$.Data."), "no criteria means no criteria clause");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_SingleEqCriterion_NoRedundantBrackets()
    {
        #region ACT

        var source = Build(Eq("Name", "John"));

        #endregion

        #region ASSERT

        Assert.That(source, Does.EndWith("AND $.Data.Name=\"John\")"),
            "a group of one condition does not need brackets of its own");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_TwoEqOnSameField_OrJoined()
    {
        #region ACT

        var source = Build(TwoEqOnOneField);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("($.Data.Operation=\"sum\" OR $.Data.Operation=\"avg\")"));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_EqAndCtOnSameField_OrJoined()
    {
        #region ACT

        var source = Build(EqAndCtOnOneField);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("($.Data.Operation=\"sum\" OR $.Data.Operation LIKE \"%average%\")"));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_LtAndGtOnSameField_AndJoined()
    {
        #region ACT

        var source = Build(RangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("($.Data.Number1<1998 AND $.Data.Number1>1901)"),
            "a range narrows, so it is And-ed; lt sorts before gt");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_MatchAndRangeOnSameField_RangeBracketedAndAndJoined()
    {
        #region ACT

        var source = Build(MatchAndRangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("($.Data.Number1=1950 AND ($.Data.Number1<1998 AND $.Data.Number1>1901))"));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_MatchesAndRangeOnSameField_BothPartsBracketedAndAndJoined()
    {
        #region ACT

        var source = Build(MatchesAndRangeOnOneField);

        #endregion

        #region ASSERT

        // AND binds stronger than OR, so without brackets of its own the Or-ed part would read as
        // '$.Data.Number1=1950 OR (LIKE "%195%" AND range)'.
        Assert.That(source, Does.Contain(
            "(($.Data.Number1=1950 OR $.Data.Number1 LIKE \"%195%\") AND ($.Data.Number1<1998 AND $.Data.Number1>1901))"));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_CriteriaOnDifferentFields_GroupsAndJoined()
    {
        #region ACT

        var source = Build(TwoGroups);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.Data.Operation=\"sum\""));
        Assert.That(source, Does.Contain("$.Data.Number1=1950"));
        Assert.That(source, Does.Not.Contain("OR $.Data.Number1"), "between groups it is always And");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_CtGroupFollowedByAnotherGroup_GroupsAndJoined()
    {
        #region ACT

        var source = Build(CtGroupThenSecondGroup);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.Data.Operation LIKE \"%average%\""));
        Assert.That(source, Does.Contain("$.Data.Number1=1950"));
        Assert.That(source, Does.Not.Contain("OR $.Data.Number1"),
            "a preceding ct may never turn the operator between two groups into an Or");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_CtBeforeEqInInput_EqEmittedFirst()
    {
        #region ACT

        var source = Build(CtBeforeEqInInput);

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("($.Data.Operation=\"sum\" OR $.Data.Operation LIKE \"%average%\")"),
            "conditions within a group are ordered by their ConditionEnum, so eq comes before ct");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_UnknownCondition_TreatedAsEq()
    {
        #region ACT

        var source = Build(new Backlot.Core.Criteria { Field = "Operation", Condition = "xx", Value = "sum" });

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.Data.Operation=\"sum\""));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_FieldNameWithIllegalCharacters_Sanitized()
    {
        #region ACT

        var source = Build(Eq("Oper ation!", "sum"));

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.Data.Operation=\"sum\""));
        Assert.That(source, Does.Not.Contain(" ation"));

        #endregion
    }

    [Test]
    public void BuildQueryExpression_DateRange_FiltersOnLastModified()
    {
        #region ARRANGE

        var from = new DateTimeOffset(1901, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var till = new DateTimeOffset(1998, 1, 1, 0, 0, 0, TimeSpan.Zero);

        #endregion

        #region ACT

        var source = LitePersistedRoleRepository
            .BuildQueryExpression(typeof(IFormula), null, from, till).Source;

        #endregion

        #region ASSERT

        Assert.That(source, Does.Contain("$.LastModified>={$date:\"1901-01-01T00:00:00.0000000Z\"}"));
        Assert.That(source, Does.Contain("$.LastModified<={$date:\"1998-01-01T00:00:00.0000000Z\"}"));

        #endregion
    }

    [Test]
    public void ResolveSortField_TopLevelStoreEntityField_NotPrefixed()
    {
        #region ACT & ASSERT

        Assert.That(LitePersistedRoleRepository.ResolveSortField("LastModified"), Is.EqualTo("LastModified"));
        Assert.That(LitePersistedRoleRepository.ResolveSortField("Id"), Is.EqualTo("Id"));

        #endregion
    }

    [Test]
    public void ResolveSortField_RoleField_PrefixedWithDataAndSanitized()
    {
        #region ACT & ASSERT

        Assert.That(LitePersistedRoleRepository.ResolveSortField("Oper ation!"), Is.EqualTo("Data.Operation"));

        #endregion
    }

    // - - - the same contract, asserted on what the expression actually decides instead of how it
    //       is rendered. This is what breaks when the brackets are wrong.

    [Test]
    public void BuildQueryExpression_MatchesAndRangeOnSameField_MatchesOnlyWhenBothPartsHold()
    {
        #region ARRANGE

        // (Name = 'John' OR Name ct 'Doe') AND (Number1 > 1901 AND Number1 < 1998)
        Backlot.Core.Criteria[] criteria =
            [Eq("Name", "John"), Ct("Name", "Doe"), Gt("Number1", 1901), Lt("Number1", 1998)];

        #endregion

        #region ACT & ASSERT

        Assert.That(Matches(criteria, "John", 1950), Is.True, "eq arm, inside the range");
        Assert.That(Matches(criteria, "Doedel", 1950), Is.True, "ct arm, inside the range");

        // both of these hold only when the Or-ed part has brackets of its own: mis-bound as
        // 'Name = John OR (Name ct Doe AND range)' the first would wrongly match and the second
        // would still match on its eq arm alone.
        Assert.That(Matches(criteria, "John", 1899), Is.False, "eq arm, but outside the range");
        Assert.That(Matches(criteria, "Doedel", 2001), Is.False, "ct arm, but outside the range");

        Assert.That(Matches(criteria, "Peter", 1950), Is.False, "inside the range, but neither arm");

        #endregion
    }

    [Test]
    public void BuildQueryExpression_CriteriaOnDifferentFields_MatchesOnlyWhenEveryGroupHolds()
    {
        #region ARRANGE

        Backlot.Core.Criteria[] criteria = [Eq("Name", "John"), Eq("Number1", 1950)];

        #endregion

        #region ACT & ASSERT

        Assert.That(Matches(criteria, "John", 1950), Is.True);
        Assert.That(Matches(criteria, "John", 1899), Is.False, "between groups it is always And");
        Assert.That(Matches(criteria, "Peter", 1950), Is.False, "between groups it is always And");

        #endregion
    }
}
