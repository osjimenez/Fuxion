﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Fuxion.Identity.Test.Mocks;
using static Fuxion.Identity.Functions;
using Xunit;
using Xunit.Abstractions;
using Fuxion.Test;
using Fuxion.Math.Graph;

namespace Fuxion.Identity.Test
{
    public class FunctionTest : BaseTest
    {
        public FunctionTest(ITestOutputHelper output) : base(output) { }
        [Fact(DisplayName = "Function - IsValid")]
        public void Validate()
        {
            Assert.False(new GuidFunction(Guid.NewGuid(), null).IsValid());
            Assert.False(new GuidFunction(Guid.NewGuid(), "").IsValid());
            Assert.False(new GuidFunction(Guid.NewGuid(), " ").IsValid());
            Assert.False(new GuidFunction(default(Guid), "valid").IsValid());
            Assert.True(new GuidFunction(Guid.NewGuid(), "valid").IsValid());

            Assert.False(new StringFunction(null).IsValid());
            Assert.False(new StringFunction("").IsValid());
            Assert.False(new StringFunction(" ").IsValid());
            Assert.True(new StringFunction("valid").IsValid());
        }
        [Fact(DisplayName = "Function - Inclusions & exclusion")]
        public void IncludesAndExcludes()
        {
            Reset();
            Assert.Equal(Read.GetAllInclusions().Count(), 0);
            Assert.Equal(Read.GetAllExclusions().Count(), 5);
            Assert.Equal(Edit.GetAllInclusions().Count(), 1);
            Assert.Equal(Edit.GetAllExclusions().Count(), 4);
            var Custom = AddCustom(CreateCustom("CUSTOM", new[] { Read }, new[] { Manage }));
            Assert.Equal(Custom.GetAllInclusions().Count(), 1);
            Assert.Equal(Custom.GetAllExclusions().Count(), 2);
            var oo = Read.GetAllExclusions();
            Assert.Equal(Read.GetAllExclusions().Count(), 6);
            Reset();
        }
        [Fact(DisplayName = "Function - Cycles detection")]
        public void CyclesDetection()
        {
            //var graph = new Graph<IFunction>();
            //graph.AddEdge(Admin, Manage);
            //graph.AddEdge(Manage, Edit);
            //graph.AddEdge(Manage, Delete);
            //graph.AddEdge(Edit, Read);
            //graph.AddEdge(Create, Read);
            //var Custom = AddCustom("CUSTOM", new[] { Read }, new[] { Manage });
            //graph.AddEdge(Manage, Custom);
            //graph.AddEdge(Custom, Read);
            //Assert.Equal(1, graph.GetDescendants(Custom).Count());
            //Assert.Equal(2, graph.GetAscendants(Custom).Count());
            //Assert.False(graph.HasCycles());
            //var CustomCycle = AddCustom("CUSTOM_CYCLE", new IFunction[] { }, new IFunction[] { });
            //graph.AddEdge(CustomCycle, Manage);
            //graph.AllowCycles = true;
            //graph.AddEdge(Read, CustomCycle);
            //Assert.True(graph.HasCycles());
            //Assert.True(false, "Graph is not used yet in Functions class");
            Assert.Throws<GraphCyclicException>(() => AddCustom(CreateCustom("CUSTOM", new[] { Edit }, new[] { Read })));
            foreach (var fun in GetAll())
            {
                if (fun.Inclusions != null)
                    Assert.False(fun.Inclusions.Any(f => f.Id.ToString() == "CUSTOM"),
                        $"La funcion {fun.Name} tiene incluido a CUSTOM");
                if (fun.Exclusions != null)
                    Assert.False(fun.Exclusions.Any(f => f.Id.ToString() == "CUSTOM"),
                        $"La funcion {fun.Name} tiene excluido a CUSTOM");
            }
            //Reset();
        }
        [Fact(DisplayName = "Function - Heterogeneous id types")]
        public void HeterogeneousIdTypes()
        {
            IEnumerable<IFunction> funcs;
            try
            {
                funcs = Functions.GetAll();
                //Reset();
                var intFunction = AddCustom(CreateCustom(1, new[] { Read }, new[] { Manage }));
                Reset();
            }catch
            {
                var oo = Functions.GetAll();
                throw;
            }
        }
        [Fact(DisplayName = "Function - Create custom function")]
        public void CustomFunction()
        {
            //Reset();
            var cus = CreateCustom("Custom", new[] { Read }, new[] { Edit });
            AddCustom(cus);
            var cus2 = GetById("Custom");
            Assert.True(cus.Id == cus2.Id);
            Reset();
        }
    }
    //public static class ext
    //{
    //    public static IFunction MyFunc(this ICustomFunction me) => GetById("");
    //}
}
