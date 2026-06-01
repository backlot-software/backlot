using System;
using System.Collections.Generic;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Testing.Core;
using NUnit.Framework;

namespace Backlot.Testing;

/*
 * DEFINITIONS;
 * Namingconvention; MethodNameToTest_StateUnderTest_ExpectedBehavior
 * - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
 * Dummy - objects are passed around but never actually used. Usually they are just used to fill parameter lists.
 * Fake - objects actually have working implementations, but usually take some shortcut which makes them not suitable for production (an in memory database is a good example).
 * Stubs - provide canned answers to calls made during the test, usually not responding at all to anything outside what's programmed in for the test. Stubs may also record information about calls, such as an email gateway stub that remembers the messages it 'sent', or maybe only how many messages it 'sent'.
 * Mocks - are what we are talking about here: objects pre-programmed with expectations which form a specification of the calls they are expected to receive
 */

public class PermissionControl
{

    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void ChangeMask_AnonymousObjectWithDefaultPermission_PermissionIsEqualToChangedMaskLevel()
    {
        #region ARRANGE

        var anonymousObject = new
        {
            Uid = "UnitTestAnonymousObject-" + Uid.New(),
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
        };
        
        #endregion
        
        #region ACT
        
        var role = anonymousObject.Presents<IFormula>();
        role.ManagePermission(p => p.SetMask(PermissionLevel.ReadWrite));
        
        #endregion

        #region ASSERT
        
        Assert.That(role.Permission(), Is.EqualTo(Permission.Create(PermissionLevel.ReadWrite)));

        #endregion
    }

    [Test]
    public void SetPermissionViaOriginObject_TypedObjectWithItsOwnPermissionProperty_PermissionPropertyIsgnored()
    {
        #region ARRANGE

        var typed = new FormulaPermission
        {
            Uid = "formulaper-" + Uid.New(),
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            __Permission = "m::0" //try to overwrite backlot permissions
        };
        
        #endregion
        
        #region ACT
        
        var role = typed.Presents<IFormula>();

        #endregion

        #region ASSERT
        
        //because permission is not set via backlot, the default permission is returned. The Actor Intercepter does protect reading the orign permission and turns the actor value into null
        Assert.That(role.Permission(), Is.EqualTo(Permission.Create(PermissionLevel.ReadWriteExecute)), $"{nameof(PermissionLevel.ReadWriteExecute)} is default for IPermissionized roles.");
        // ReSharper disable once SuspiciousTypeConversion.Global
#pragma warning disable CS8602
        Assert.That((role as IJProxy).JActor["__Permission"], Is.Null, $"Actor.{nameof(IPermission.__Permission)} is removed.");
#pragma warning restore CS8602

        #endregion
    }
    
    [Test]
    public void SetPermissionViaOriginObject_AnonymousObjectWithItsOwnPermissionProperty_DefaultPermisisonAreUsed()
    {
        #region ARRANGE

        var anonymousObject = new
        {
            Uid = "formulaper-" + Uid.New(),
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            __Permission = "m::0" //try to overwrite backlot permissions
        };
        
        #endregion
        
        #region ACT & ASSERT

        var f = anonymousObject.Presents<IFormula>();
        
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        // ReSharper disable once SuspiciousTypeConversion.Global
        var per = (f as IJProxy).JActor[nameof(IPermission.__Permission)];

        
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.That(f is IJProxy); // when persented an anonymous object is turned into a IJProxy.
        Assert.That(per == null); // permission value used in the "converted json" orign.
        Assert.That(f.Permission().ToString() == "m::7"); // default persmission is set.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        #endregion
    }
    
    /// <summary>
    /// Different PermissionLevel checks based on different uses cases all using the same UserCtx as "CurrentUser".
    /// </summary>
    [Test]
    public void CurrentUserPermissionLevel_DifferentSerializationUseCases_PermissionLevelSerializationBasedOnUserCtxObject_()
    {
        // Item1; the created permission
        // Item2; expected current user permission
        // Item3; expected permission serialized permission pattern 
        var permissions = new List<Tuple<Permission, PermissionLevel, string>>();
        
        #region ARRANGE
        
        // -->
        var groupHasHigherLevelThanMask = Permission.Create(PermissionLevel.None)
            .SetGroup("Admin", PermissionLevel.ReadWriteExecute)
            .SetGroup("*", PermissionLevel.ReadExecute);
        permissions.Add(new Tuple<Permission, PermissionLevel, string>(
                groupHasHigherLevelThanMask, 
                PermissionLevel.None,        
                "m::0,g:*:5,g:Admin:7"));     
        
        // -->
        var wildcardHasHigherLevelThanGroup = Permission.Create(PermissionLevel.ReadWriteExecute)
            .SetGroup("Admin", PermissionLevel.None)
            .SetGroup("*", PermissionLevel.ReadExecute);
        permissions.Add(new Tuple<Permission, PermissionLevel, string>(
             wildcardHasHigherLevelThanGroup, 
             PermissionLevel.ReadExecute,  //result of the highest group level (thus the wildcard) is used.
             "m::7,g:*:5,g:Admin:0"));

        // -->
        var twoGroupsWithDifferentLevels = Permission.Create(PermissionLevel.ReadWriteExecute)
             .SetGroup("Admin", PermissionLevel.ReadWriteExecute)
             .SetGroup("Everyone", PermissionLevel.ReadExecute);
        permissions.Add(new Tuple<Permission, PermissionLevel, string>(
             twoGroupsWithDifferentLevels, 
             PermissionLevel.ReadWriteExecute, //result of the highest group level is used admin in this case.
             "m::7,g:Admin:7,g:Everyone:5"));
        
        // -->
        var userIsAlwaysLeading = Permission.Create(PermissionLevel.ReadWriteExecute)
             .SetGroup("Admin", PermissionLevel.ReadWriteExecute)
             .SetGroup("*", PermissionLevel.None)
             .SetUser(UserCtx.UserNameStatic, PermissionLevel.Read);
        permissions.Add(new Tuple<Permission, PermissionLevel, string>(
                userIsAlwaysLeading, 
                PermissionLevel.Read, //result of the user is used before any group checks are done.
                $"m::7,g:*:0,g:Admin:7,u:{UserCtx.UserNameStatic}:4"));

        #endregion
        
        #region ACT && ASSERT

        foreach (var per in permissions)
        {
            // Item1; the created permission
            // Item2; expected current user permission
            // Item3; expected permission serialized permission pattern 
            
            var typed = Acting.New<IFormula>();
            typed.Number1 = 7;
            typed.Number2 = 9;
            typed.Operation = "sum";
            
            // for testing purpose: we reset permissions with the defined test permissions;
            // copy permissions ->

            typed.ManagePermission(p =>
            {
                p.SetMask(per.Item1.MaskLevel);
                p.Clear();
                foreach (var grp in per.Item1.GroupLevels)
                {
                    p.SetGroup(grp.Key, grp.Value);
                }

                foreach (var usr in per.Item1.UserLevels)
                {
                    p.SetUser(usr.Key, usr.Value);
                }
            });
            
            // <-- end copy permissions.

            Assert.That(typed.CurrentUserPermissionLevel() == per.Item2,
                $"Expected {per.Item2} but got {typed.CurrentUserPermissionLevel()}");
            
            Assert.That(typed.Permission().ToString() == per.Item3, 
                $"Expected {per.Item3} but got {typed.Permission()}");
        }

        #endregion
    }

    [Test]
    public void CanRead_MaskLevelPlusGroupsAndUsersAreSetCurrentUserIsNotDefinedInPermissions_CurrentUserCanNotRead()
    {
        var typed = Acting.New<IFormula>();
        typed.Number1 = 7;
        typed.Number2 = 9;
        typed.Operation = "sum";

        typed.ManagePermission(p =>
            p.SetUser("SomeOtherUser", PermissionLevel.ReadExecute)
                .SetGroup("SuperAccess", PermissionLevel.ReadWriteExecute));
        
        Assert.That(typed.CanRead(), Is.False);
    }
    
    [Test]
    public void ManagePermission_Example_Debugging()
    {
        
        var typed = Acting.New<IFormula>();
        typed.Number1 = 7;
        typed.Number2 = 9;
        typed.Operation = "sum";

        typed.ManagePermission(p => p
            .SetMask(PermissionLevel.ReadWriteExecute)
            .Clear() // clear all other permissions and 
            .SetGroup(Access.Open, PermissionLevel.Read)
            .SetUser("TEST", PermissionLevel.ReadWriteExecute)
            .SetGroup(Access.Admin, PermissionLevel.ReadWrite));

        Assert.That(typed.CanWrite());
        Assert.That(!typed.CanExecute());;
    }
}
