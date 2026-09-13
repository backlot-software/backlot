using System.Linq;
using System.Text.RegularExpressions;
using Backlot.Core;

namespace Backlot.Testing.Persistence;

/*
 * The criteria group semantics of AGENTS.MD -> Persistence -> RoleRepository implementations:
 *
 *   - criteria are grouped by field name; one field name is always exactly one group;
 *   - inside a group, lt and gt are joined with AND (a range narrows);
 *   - inside a group, all other conditions are joined with OR (widens);
 *   - inside a group, the OR-ed part and the AND-ed range part are joined with AND;
 *   - between groups it is always AND.
 *
 *   (Name = 'John' OR Name ct 'Doe') AND (LastModified > 1901 AND LastModified < 1998)
 *
 * Every implementation has to produce that same boolean shape, so every provider is fed the exact
 * same criteria from here and only the expected dialect differs per test class.
 */
internal static class CriteriaSets
{
    internal static Criteria Eq(string field, object value) => new() { Field = field, Condition = "eq", Value = value };
    internal static Criteria Ct(string field, object value) => new() { Field = field, Condition = "ct", Value = value };
    internal static Criteria Lt(string field, object value) => new() { Field = field, Condition = "lt", Value = value };
    internal static Criteria Gt(string field, object value) => new() { Field = field, Condition = "gt", Value = value };

    /// <summary>Two eq on one field: widens, so Or-ed.</summary>
    internal static Criteria[] TwoEqOnOneField => [Eq("Operation", "sum"), Eq("Operation", "avg")];

    /// <summary>eq and ct on one field: both widen, so Or-ed.</summary>
    internal static Criteria[] EqAndCtOnOneField => [Eq("Operation", "sum"), Ct("Operation", "average")];

    /// <summary>lt and gt on one field: narrows, so And-ed.</summary>
    internal static Criteria[] RangeOnOneField => [Gt("Number1", 1901), Lt("Number1", 1998)];

    /// <summary>The bracketing case: one field holding both an Or-ed part and an And-ed range.</summary>
    internal static Criteria[] MatchAndRangeOnOneField =>
        [Eq("Number1", 1950), Gt("Number1", 1901), Lt("Number1", 1998)];

    /// <summary>As MatchAndRangeOnOneField, with a second widening condition so the Or-ed part has two arms.</summary>
    internal static Criteria[] MatchesAndRangeOnOneField =>
        [Eq("Number1", 1950), Ct("Number1", "195"), Gt("Number1", 1901), Lt("Number1", 1998)];

    /// <summary>Two field names, so two groups, And-ed.</summary>
    internal static Criteria[] TwoGroups => [Eq("Operation", "sum"), Eq("Number1", 1950)];

    /// <summary>
    /// A ct group followed by a second group. The ct is what makes a query builder fall back to its
    /// own default operator (an OrElse in ravendb), so this is the regression guard for the rule that
    /// every operator is emitted explicitly.
    /// </summary>
    internal static Criteria[] CtGroupThenSecondGroup => [Ct("Operation", "average"), Eq("Number1", 1950)];

    /// <summary>One field name written in two casings is still exactly one group.</summary>
    internal static Criteria[] SameFieldDifferentCasing => [Eq("Operation", "sum"), Eq("operation", "avg")];

    /// <summary>ct sorts after eq, so the eq is emitted first whatever the input order is.</summary>
    internal static Criteria[] CtBeforeEqInInput => [Ct("Operation", "average"), Eq("Operation", "sum")];

    /// <summary>Collapses runs of whitespace so an assert targets structure and not incidental spacing.</summary>
    internal static string Normalize(string query) =>
        Regex.Replace(query ?? string.Empty, @"\s+", " ").Trim();

    /// <summary>
    /// Replaces the numbered parameter placeholders with a bare one, so an assert on the boolean shape
    /// does not have to count how many parameters the filters in front of the criteria happened to use.
    /// </summary>
    internal static string MaskParameters(string sql) => Regex.Replace(sql ?? string.Empty, @"@p\d+", "@p");

    /// <summary>Every column name declared in the generated "OPENJSON(...) WITH (...)" of a sql provider.</summary>
    internal static string[] OpenJsonColumns(string sql)
    {
        var with = Regex.Match(sql, @"OPENJSON\([^)]*\)\s*WITH\s*\((?<cols>.*?)\)\s*AS\s+jsn", RegexOptions.Singleline);
        if (!with.Success) return [];

        return with.Groups["cols"].Value
            .Split(',')
            .Select(c => c.Trim().Split(' ')[0])
            .Where(c => c.Length > 0)
            .ToArray();
    }

    /// <summary>Every field addressed through the OPENJSON alias in the generated sql.</summary>
    internal static string[] JsnReferences(string sql) =>
        Regex.Matches(sql, @"\[jsn\]\.\[(?<field>[^\]]+)\]")
            .Select(m => m.Groups["field"].Value)
            .Distinct()
            .ToArray();
}
