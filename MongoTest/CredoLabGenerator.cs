using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace MongoTest
{
    public class CredoLabGenerator
    {
        [Fact]
        public void CreateAssembly()
        {
            AssemblyName assName = new AssemblyName("CredoLab.Mobile");
            var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.Save);
            var moduleBuilder = assBuilder.DefineDynamicModule("CredoLabModule", "credolab.mobile.dll");

            var typeBuilder = moduleBuilder.DefineType("CredoAppConstants", TypeAttributes.Public | TypeAttributes.Class);
            
            
            var nestedType = typeBuilder.DefineNestedType("CredoAppConstants", TypeAttributes.Public | TypeAttributes.Class);


        }

    }
}