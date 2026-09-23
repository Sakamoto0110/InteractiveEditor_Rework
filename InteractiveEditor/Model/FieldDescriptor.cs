using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace InteractiveEditor.Model
{
    public class FieldDescriptor
    {
        public Type Type { get; init; }
        public string Name { get;init; }
        public string Path { get; init;}
        public string FullPath { get; init;}
        public  FieldAccessors Accessors { get; set; }
        public MemberTypes MemberType { get; init; }


        public class FieldAccessors
        {
            
            public Func<object? , object?>? Getter;
            public Action<object?,object?>? Setter;
        }

    }

}

