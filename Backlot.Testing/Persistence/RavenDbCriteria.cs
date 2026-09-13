using Backlot.Services.RavenDb;
using Backlot.Testing.Core;
using NUnit.Framework;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using static Backlot.Testing.Persistence.CriteriaSets;

namespace Backlot.Testing.Persistence;

/*
 * Namingconvention; MethodNameToTest_StateUnderTest_ExpectedBehavior
 *
 * RavenDb renders its RQL entirely client side, so RavenPersistedRoleRepository.BuildQuery is
 * asserted without a server: Initialize() with DisableTopologyUpdates issues no request, and only
 * executing the query ever would. Db.Store is deliberately not used, because initializing that one
 * calls IndexCreation.CreateIndexes, which does need a server.
 */
public class RavenDbCriteria
{
    private IDocumentStore _store;
    private IDocumentSession _session;

    [OneTimeSetUp]
    public void OpenStore()
    {
        _store = new DocumentStore
        {
            Urls = ["http://localhost:18080"], // never contacted, see the remark above.
            Database = "BacklotTest",
            Conventions = new DocumentConventions { DisableTopologyUpdates = true }
        }.Initialize();
    }

    [OneTimeTearDown]
    public void CloseStore()
    {
        _session?.Dispose();
        _store?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        Initialize.Setup();

        _session?.Dispose();
        _session = _store.OpenSession();
    }

    private string Build(params Backlot.Core.Criteria[] criteria) =>
        Normalize(RavenPersistedRoleRepository.BuildQuery(_session, typeof(IFormula), criteria).ToString());

    [Test]
    public void BuildQuery_NoCriteria_OnlySkillAndReadPermissionFilters()
    {
        #region ACT

        var rql = Build();

        #endregion

        #region ASSERT

        Assert.That(rql, Does.StartWith($"from index '{Roles_BySkillAndReadPermission._IndexName}'"));
        Assert.That(rql, Does.Contain("where CanRead = $p0 and Skills in ($p1)"));
        Assert.That(rql, Does.Contain(
            "and (UsersCanRead in ($p2) or GroupsCanRead in ($p3) or (true and not exists(UsersCanRead) and not exists(GroupsCanRead)))"));
        Assert.That(rql, Does.Not.Contain("_Operation"), "no criteria means no criteria clause");

        #endregion
    }

    [Test]
    public void BuildQuery_SingleEqCriterion_NoRedundantBrackets()
    {
        #region ACT

        var rql = Build(Eq("Name", "John"));

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Name = $p4)"),
            "dynamic index fields are prefixed with _, and a group of one condition needs no inner brackets");

        #endregion
    }

    [Test]
    public void BuildQuery_TwoEqOnSameField_OrJoined()
    {
        #region ACT

        var rql = Build(TwoEqOnOneField);

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Operation = $p4 or _Operation = $p5)"));

        #endregion
    }

    [Test]
    public void BuildQuery_EqAndCtOnSameField_OrJoined()
    {
        #region ACT

        var rql = Build(EqAndCtOnOneField);

        #endregion

        #region ASSERT

        // ct searches the ngram twin of the field, which is what makes it a contains.
        Assert.That(rql, Does.EndWith(
            $"and (_Operation = $p4 or search('_Operation{Roles_BySkillAndReadPermission.NGramSuffix}', $p5, and))"));

        #endregion
    }

    [Test]
    public void BuildQuery_LtAndGtOnSameField_AndJoined()
    {
        #region ACT

        var rql = Build(RangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Number1 < $p4 and _Number1 > $p5)"),
            "a range narrows, so it is And-ed; lt sorts before gt");

        #endregion
    }

    [Test]
    public void BuildQuery_MatchAndRangeOnSameField_BothPartsBracketedAndAndJoined()
    {
        #region ACT

        var rql = Build(MatchAndRangeOnOneField);

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and ((_Number1 = $p4) and (_Number1 < $p5 and _Number1 > $p6))"));

        #endregion
    }

    [Test]
    public void BuildQuery_MatchesAndRangeOnSameField_BothPartsBracketedAndAndJoined()
    {
        #region ACT

        var rql = Build(MatchesAndRangeOnOneField);

        #endregion

        #region ASSERT

        // And binds stronger than Or in rql, so without brackets of its own the Or-ed part would
        // read as '_Number1 = $p4 or (search(...) and range)'.
        Assert.That(rql, Does.EndWith(
            $"and ((_Number1 = $p4 or search('_Number1{Roles_BySkillAndReadPermission.NGramSuffix}', $p5, and)) and (_Number1 < $p6 and _Number1 > $p7))"));

        #endregion
    }

    [Test]
    public void BuildQuery_CriteriaOnDifferentFields_GroupsAndJoined()
    {
        #region ACT

        var rql = Build(TwoGroups);

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Operation = $p4) and (_Number1 = $p5)"));

        #endregion
    }

    [Test]
    public void BuildQuery_CtGroupFollowedByAnotherGroup_GroupsAndJoined()
    {
        #region ACT

        var rql = Build(CtGroupThenSecondGroup);

        #endregion

        #region ASSERT

        // ravendb walks back to the last where token, past any closing bracket, and defaults to an
        // OrElse when that token happens to be a Search. So the 'and' in front of the second group
        // has to be emitted explicitly.
        Assert.That(rql, Does.EndWith(
            $"and (search('_Operation{Roles_BySkillAndReadPermission.NGramSuffix}', $p4, and)) and (_Number1 = $p5)"));
        Assert.That(rql, Does.Not.Contain($"search('_Operation{Roles_BySkillAndReadPermission.NGramSuffix}', $p4, and)) or ("),
            "a preceding Search may never leak an Or between two groups");

        #endregion
    }

    [Test]
    public void BuildQuery_CtBeforeEqInInput_EqEmittedFirst()
    {
        #region ACT

        var rql = Build(CtBeforeEqInInput);

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith(
            $"and (_Operation = $p4 or search('_Operation{Roles_BySkillAndReadPermission.NGramSuffix}', $p5, and))"),
            "conditions within a group are ordered by their ConditionEnum, so eq comes before ct");

        #endregion
    }

    [Test]
    public void BuildQuery_UnknownCondition_TreatedAsEq()
    {
        #region ACT

        var rql = Build(new Backlot.Core.Criteria { Field = "Operation", Condition = "xx", Value = "sum" });

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Operation = $p4)"));

        #endregion
    }

    [Test]
    public void BuildQuery_FieldNameWithIllegalCharacters_Sanitized()
    {
        #region ACT

        var rql = Build(Eq("Oper ation!", "sum"));

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Operation = $p4)"));

        #endregion
    }

    [Test]
    public void BuildQuery_CtOnNonStringValue_DegradesToEquality()
    {
        #region ACT

        var rql = Build(Ct("Number1", 1950));

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("and (_Number1 = $p4)"),
            "the ngram twin only exists for strings");

        #endregion
    }

    [Test]
    public void BuildQuery_CtWithNothingMatchableInIt_DegradesToPrefixMatch()
    {
        #region ACT

        var rql = Build(Ct("Operation", "su"));

        #endregion

        #region ASSERT

        // A wildcard Search is not an option: ravendb can not build an NGramAnalyzer for a wildcard
        // term and within this index every Search resolves to that analyzer.
        Assert.That(rql, Does.EndWith("and (startsWith(_Operation, $p4))"));

        #endregion
    }

    [Test]
    public void BuildQuery_Orderby_SanitizedAndAppended()
    {
        #region ACT

        var rql = Normalize(RavenPersistedRoleRepository
            .BuildQuery(_session, typeof(IFormula), [Eq("Name", "John")], null, null, "Oper ation!").ToString());

        #endregion

        #region ASSERT

        Assert.That(rql, Does.EndWith("order by Operation"));

        #endregion
    }

    // - - - the ngram rewrite of a ct search term. NGramAnalyzer only analyzes the indexed value,
    //       never the search term, so the term has to be cut into pieces the index can match.

    [Test]
    public void ToNGramTerms_WordShorterThanMinGram_Dropped()
    {
        #region ACT & ASSERT

        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("su"), Is.Null, "no indexed ngram is this short");
        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("a b"), Is.Null);

        #endregion
    }

    [Test]
    public void ToNGramTerms_WordWithinGramBounds_KeptWhole()
    {
        #region ACT & ASSERT

        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("Doe"), Is.EqualTo("Doe"));
        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("Doedel"), Is.EqualTo("Doedel"));

        #endregion
    }

    [Test]
    public void ToNGramTerms_WordLongerThanMaxGram_CutIntoOverlappingWindows()
    {
        #region ACT & ASSERT

        // MaxGram sized windows slid over the word, all of which the value has to contain.
        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("lenjonas"), Is.EqualTo("lenjon enjona njonas"));

        #endregion
    }

    [Test]
    public void ToNGramTerms_TermWithWordSeparators_CutPerWord()
    {
        #region ACT & ASSERT

        // ngrams never cross a word boundary, and the indexing tokenizer treats the / as one too.
        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("roles/lenjonas"),
            Is.EqualTo("roles lenjon enjona njonas"));
        Assert.That(RavenPersistedRoleRepository.ToNGramTerms("Doe su"), Is.EqualTo("Doe"),
            "the word that has no matchable ngram is dropped, the other one survives");

        #endregion
    }
}
