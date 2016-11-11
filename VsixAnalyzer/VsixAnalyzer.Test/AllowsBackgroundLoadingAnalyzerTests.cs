using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using NUnit.Framework;
using TestHelper;

namespace VsixAnalyzer.Test
{
    [TestFixture]
    public class AllowsBackgroundLoadingAnalyzerTests : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [Test]
        public void Empty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [Test]
        public void TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace TestApp
    {
        public sealed partial class TestPackage : AsyncPackage
        {   
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "VsixAnalyzer",
                Message = String.Format(VsixAnalyzer.Resources.AllowsBackgroundLoadingAnalyzerMessageFormat, "TestPackage"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 37)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
/*
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";
            VerifyCSharpFix(test, fixtest);*/
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AllowsBackgroundLoadingCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AllowsBackgroundLoadingAnalyzer();
        }
    }
}