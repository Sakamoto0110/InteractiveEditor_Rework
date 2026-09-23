using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace InteractiveEditor.Model;

public static class  ReflectionDiscovery
{

    public static IReadOnlyList<FieldDescriptor> ResolveFor<T>()
    {
        var type = typeof(T);
        var fields = new List<FieldDescriptor>();
 
        var flags =  BindingFlags.Public      | BindingFlags.Instance;
        foreach (var mi in type.GetMembers(flags).Where(mi => mi is FieldInfo || mi is PropertyInfo))
        {            
            var descriptor = new FieldDescriptor()
            {
                Type = type,
                Name = mi.Name,
                Path = $"{mi.DeclaringType?.FullName ?? string.Empty}.{mi.Name}",
                FullPath = fields.Count == 0 ? $"{mi.DeclaringType?.FullName ?? string.Empty}_{mi.Name}" : $"{fields[fields.Count - 1].FullPath}.{mi.Name}",
                MemberType = mi.MemberType,
                
                Accessors = mi switch
                {
                    FieldInfo fi => new FieldDescriptor.FieldAccessors
                    {
                        Getter = obj => fi.GetValue(obj),
                        Setter = (obj, value) => fi.SetValue(obj, value)
                    },

                    PropertyInfo pi => new FieldDescriptor.FieldAccessors
                    {
                        Getter = pi.CanRead ? obj => pi.GetValue(obj) : null,
                        Setter = pi.CanWrite ? (obj, value) => pi.SetValue(obj, value) : null
                    },
                    
                }
            };
            
            fields.Add(descriptor);
        }
        return fields;
    }



}
