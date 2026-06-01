using System;
using System.Collections.Generic;
using System.Linq;
using Backlot.Core.Json;

namespace Backlot.Core.Abstraction.Actors
{
    public sealed class DictionaryInterceptor : BaseInterceptor<IDictionary<string, object>>
    {
        public static TRole Generate<TRole>(IDictionary<string, object> actor) 
            where TRole : IRole
        {
            return (TRole)ProxyGeneration.Generator.CreateInterfaceProxyWithoutTarget(
                typeof(TRole), // main type (interface)
                [typeof(IProxiedRole)], // additional interfaces,
                ProxyGeneration.Options,
                new DictionaryInterceptor(actor, typeof(TRole)));
        }
        
        private DictionaryInterceptor(IDictionary<string, object> origin, Type roleType) : 
            base(new Dictionary<string, object>(origin), // deepclone the actor, because we manipulate it for security reasons. 
                roleType)
        {
            // dictionary intercepting does only respect actors not having fields marked as calculated in the role type they represent. This way we do support actors containing "calculated" fields, but we ignore them.
            var calculatedProps = roleType.GetFieldInfo().Where(f => f.Attributes.Any(att => att is CalculatedAttribute))
                .Select(f => f.Name).ToArray();
            
            // remove all fields from Actor that are calculated.
            foreach (var calculatedProp in calculatedProps)
            {
                if (Actor.ContainsKey(calculatedProp))
                {
                    Actor.Remove(calculatedProp);
                }
            }
        }

        protected override bool IsNull()
        {
            return Actor.Keys.Count == 0 && Backingfields.Keys.Count == 0;
        }

        protected override string[] Skills()
        {
            var skills = RoleType.GetSkills();
            return skills.Union((Actor.TryGetValue(Meta.__Skills, out var sk) ? sk as string[]  : []) ?? []).ToArray();
        }

        protected override bool TryGet(string alias, Type returnType, out object value)
        {
            return Actor.TryGetValue(alias, out value);
        }

        protected override bool TrySet(string alias, object value)
        {
            if (Actor.ContainsKey(alias))
            {
                Actor[alias] = value;
                return true;
            }

            return false;
        }

        protected override bool TryCombine(IDictionary<string, object> additionalActor)
        {
            if(additionalActor == null) return false;
            
            foreach (var (key, value) in additionalActor) // add additional fields not yet in existing actor.
            {
                if (!Actor.ContainsKey(key) && !Backingfields.ContainsKey(key))
                {
                    Actor.Add(key, value);
                }
            }

            return true;
        }

        protected override void AddActorProperty(string alias, object value)
        {
            Actor.Add(alias, value);
        }

        protected override string[] GetActorPropertyNames()
        {
            return Actor.Keys.ToArray();
        }
    }
}