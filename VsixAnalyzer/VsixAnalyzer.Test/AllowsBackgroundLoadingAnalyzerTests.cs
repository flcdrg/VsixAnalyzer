using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using ApprovalTests.Reporters;
using NUnit.Framework;
using TestHelper;

namespace VsixAnalyzer.Test
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
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
        public void AsyncPackage()
        {
            const string test = @"
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
        }

        [Test]
        public void FixApplied_NoExitingAttribute_DoesNothing()
        {
            const string test = @"using System;

namespace TestApp
{
    public sealed partial class TestPackage : AsyncPackage
    {   
    }
}";
            VerifyCSharpFix(test);
        }


        [Test]
        public void FixApplied_ExitingAttributePropertyFalse_SetsTrue()
        {
            const string test = @"using System;

namespace TestApp
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = false)]
    public sealed partial class TestPackage : AsyncPackage
    {   
    }
}";
            VerifyCSharpFix(test);
        }

        [Test]
        public void FixApplied_NoExitingAttributeProperty_Adds()
        {
            const string test = @"using System;

namespace TestApp
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    public sealed partial class TestPackage : AsyncPackage
    {   
    }
}";
            VerifyCSharpFix(test, allowNewCompilerDiagnostics: true);
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