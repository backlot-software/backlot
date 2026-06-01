using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Backlot.Core.Json.Serialization.Newtonsoft.ContractResolvers;

/// <summary>
/// Default attribute provider for all jsonproperties created by BacklotContractResolver
/// </summary>
internal class NoAttributeProvider : IAttributeProvider
{
    static NoAttributeProvider() { Instance = new NoAttributeProvider(); }

    public static NoAttributeProvider Instance { get; }

    public IList<Attribute> GetAttributes(Type attributeType, bool inherit) { return Array.Empty<Attribute>(); }

    public IList<Attribute> GetAttributes(bool inherit) { return Array.Empty<Attribute>(); }
}