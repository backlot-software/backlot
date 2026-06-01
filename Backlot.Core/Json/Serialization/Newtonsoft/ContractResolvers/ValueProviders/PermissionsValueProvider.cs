using System;
using System.Collections.Generic;
using Backlot.Core.Security;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers.ValueProviders;

internal class PermissionsValueProvider : IValueProvider
{
    public object GetValue(object target)
    {
        if(target is IRole role)
        {
            if (target is IUid permission && target is IPermission) // check if this is a permissionized role and if it has a uid, otherwise we can't encrypt it.
            {
                return new Dictionary<string, object>
                {
                    { "CanExecute", role.CanExecute() },
                    { "CanWrite", role.CanWrite() },
                    { "CanRead", role.CanRead() },
                    { "Encrypted", permission.EncryptedPermissionString() }
                };
            }

            return new Dictionary<string, object>
            {
                { "CanExecute", role.CanExecute() },
                { "CanWrite", role.CanWrite() },
                { "CanRead", role.CanRead() },
            };
        }

        return null;
    }

    public void SetValue(object target, object value)
    {
        throw new NotImplementedException("SetValue not implemented for fixed properties; set JsonProperty.Writable = false.");
    }

    
}