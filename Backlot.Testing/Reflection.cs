using System;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Services;
using Backlot.Testing.Core;
using Backlot.Testing.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing;

public class Reflection
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void GetRoleByName_InterfaceNameStartingWithI()
    {
        #region ARRANGE

        var correctRoleName = "InterfaceStartingWithI";
        var correctRoleName_Lowercase = "interfacestartingwithi";
        var correctRoleName_Uppercase = "INTERFACESTARTINGWITHI";

        var incorrectRoleName_ExtraI = "IInterfaceStartingWithI";
        var incorrectRoleName_MissingI = "nterfaceStartingWithI";
        var incorrectRoleName_WrongInterface = "Formula";

        var interfaceTypeToCheck = typeof(IInterfaceStartingWithI);

        #endregion

        Assert.That(GetByNameResultIsTypeof(correctRoleName, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(correctRoleName_Lowercase, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(correctRoleName_Uppercase, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(incorrectRoleName_ExtraI, interfaceTypeToCheck));
        
        Assert.That(GetByNameResultIsTypeof(incorrectRoleName_MissingI, interfaceTypeToCheck), Is.False);
        Assert.That(GetByNameResultIsTypeof(incorrectRoleName_WrongInterface, interfaceTypeToCheck), Is.False);
    }

    [Test]
    public void GetRoleByName_IFormula()
    {
        #region ARRANGE

        var correctRoleName = "Formula";
        var correctRoleName_Lowercase = "formula";
        var correctRoleName_Uppercase = "FORMULA";

        var interfacename = "IFormula";
        var incorrectRoleName_ImplementingClass = "FormulaSelf";
        var incorrectRoleName_WrongInterface = "Director";

        var interfaceTypeToCheck = typeof(IFormula);

        #endregion

        Assert.That(GetByNameResultIsTypeof(correctRoleName, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(correctRoleName_Lowercase, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(correctRoleName_Uppercase, interfaceTypeToCheck));
        Assert.That(GetByNameResultIsTypeof(interfacename, interfaceTypeToCheck));
        
        Assert.That(GetByNameResultIsTypeof(incorrectRoleName_ImplementingClass, interfaceTypeToCheck), Is.False);
        Assert.That(GetByNameResultIsTypeof(incorrectRoleName_WrongInterface, interfaceTypeToCheck), Is.False);
    }

    private static bool GetByNameResultIsTypeof(string name, Type type)
    {
        if (Loader.TryGetRoleByName(name, out var t))
        {
            return t == type;
        }

        return false;
    }

    [Test]
    public void GetRoleByName_ReturnsNullIfNotExist()
    {
        var uid = Uid.New();
        var nonExistingRoleName = "ThisInterfaceWillNeverExistInBacklot" + uid;

        Assert.That(Loader.TryGetRoleByName(nonExistingRoleName, out _), Is.False);
    }

    [Test]
    public void GetName_InterfaceNameStartingWithI()
    {
        var result = Loader.GetRoleName(typeof(IInterfaceStartingWithI));

        Assert.That(result == "InterfaceStartingWithI");
    }

    [Test]
    public void GetName_WithTypedActor()
    {
        #region ARRANGE

        var origin = new Formula()
        {
            Uid = "023d02600ded",
            Number1 = 30,
            Number2 = 21,
            Operation = "sum"
        };

        var role = origin.Presents<IFormula>();

        #endregion

        var result = Loader.GetRoleName(role.GetType());

        Assert.That(result == "Formula");
    }

    [Test]
    public void GetName_WithJsonActor()
    {
        #region ARRANGE

        var origin = JObject.FromObject(new
        {
            Uid = "023d02600ded",
            Number1 = 7,
            Number2 = 9,
            Operation = "sum"
        });

        var role = origin.Presents<IFormula>();
        // ReSharper disable once SuspiciousTypeConversion.Global; within this unit test always an IJProxy.
        var jProxy = (IJProxy)role;

        #endregion

        var resultRole = Loader.GetRoleName(role.GetType());
        var resultProxy = Loader.GetRoleName(jProxy.GetType());

        Assert.Throws<ArgumentException>(() => Loader.GetRoleName(origin.GetType()));
        Assert.That(resultRole == "Formula");
        Assert.That(resultProxy == "Formula");
    }

    [Test]
    public void GetName_WithSelfRole()
    {
        #region ARRANGE

        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 52,
            Number2 = 100,
            Operation = "sum"
        };

        #endregion

        var presented = self.Presents<IFormula>();

        var resultPresented = Loader.GetRoleName(presented.GetType());
        var resultSelf = Loader.GetRoleName(self.GetType());

        Assert.That(resultPresented == "FormulaSelf");
        Assert.That(resultSelf == "FormulaSelf");
    }

    [Test]
    public async Task GetName_WithDbRole()
    {
        #region ARRANGE

        var self = new FormulaSelf()
        {
            Uid = "f5d3c5aa",
            Number1 = 82,
            Number2 = 22,
            Operation = "sum"
        };

        var repo = ServiceLocator.Get<IPersistedRoleRepository>();

        #endregion

        #region ACT

        var presented = self.Presents<IFormula>();
        await Dummy.Play(presented);
        repo.TryGet<IFormula>(self.Uid, out var dbentity);

        #endregion

        var resultSelf = self.GetType().GetRoleName();
        var resultPresented = presented.GetType().GetRoleName();
        var resultDb = dbentity.GetType().GetRoleName();

        Console.WriteLine(resultPresented);
        Console.WriteLine(resultSelf);
        Assert.That(resultPresented == "FormulaSelf");
        Assert.That(resultSelf == "FormulaSelf");
        Assert.That(resultDb == "FormulaSelf");
    }

}