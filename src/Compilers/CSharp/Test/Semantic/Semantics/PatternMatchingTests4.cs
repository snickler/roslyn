﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests4 : PatternMatchingTestBase
    {
        [Fact]
        public void TestPresenceOfITuple()
        {
            var source =
@"public class C : System.Runtime.CompilerServices.ITuple
{
    public int Length => 1;
    public object this[int i] => null;
    public static void Main()
    {
        System.Runtime.CompilerServices.ITuple t = new C();
        if (t.Length != 1) throw null;
        if (t[0] != null) throw null;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void ITupleFromObject()
        {
            // - should match when input type is object
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
        Console.WriteLine(new object() is (3, 4, 5)); // false
        Console.WriteLine((null as object) is (3, 4, 5)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleMissing()
        {
            // - should not match when ITuple is missing
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (7,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(7, 32),
                // (7,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(7, 32)
                );
        }

        [Fact]
        public void ITupleIsClass()
        {
            // - should not match when ITuple is a class
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public class ITuple
    {
        public int Length => 3;
        public object this[int index] => index + 3;
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (15,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(15, 32)
                );
        }

        [Fact]
        public void ITupleFromDynamic()
        {
            // - should match when input type is dynamic
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        dynamic t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleFromITuple()
        {
            // - should match when input type is ITuple
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        ITuple t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_01()
        {
            // - should match when input type extends ITuple and has no Deconstruct (struct)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public struct C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_02()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (struct)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public struct C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_03()
        {
            // - should match when input type extends ITuple and has no Deconstruct (class)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_04()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (class)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_05()
        {
            // - should match when input type extends ITuple and has no Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_10()
        {
            // - should match when input type extends ITuple and has no Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: ITuple
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_11()
        {
            // - should not match when input type is an unconstrained type parameter
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t)
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4)").WithArguments("T", "Deconstruct").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(13, 32),
                // (14,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("T", "Deconstruct").WithLocation(14, 32),
                // (14,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(14, 32),
                // (15,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 0, 5)").WithArguments("T", "Deconstruct").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(15, 32),
                // (16,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5, 6)").WithArguments("T", "Deconstruct").WithLocation(16, 32),
                // (16,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(16, 32)
                );
        }

        [Fact]
        public void ITuple_06()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_12()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false via ITuple
        Console.WriteLine(t is (3, 4, 5)); // true via ITuple
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public int Deconstruct() => 0;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_12b()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is ());
    }
    public int Deconstruct() => 0; // this is applicable, so prevents ITuple, but it has the wrong return type
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,32): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'T', with 0 out parameters and a void return type.
                //         Console.WriteLine(t is ());
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("T", "0").WithLocation(13, 32)
                );
        }

        [Fact]
        public void ITuple_07()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (inherited)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class B
{
    public void Deconstruct() {}
}
public class C : B, ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_08()
        {
            // - should match when input type extends ITuple and has an inapplicable Deconstruct (static)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class B
{
    public static void Deconstruct() {}
}
public class C : B, ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_09()
        {
            // - should match when input type extends ITuple and has an extension Deconstruct
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (7, 8)); // true (Extensions.Deconstruct)
        Console.WriteLine(t is (3, 4, 5)); // true via ITuple
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
static class Extensions
{
    public static void Deconstruct(this C c, out int X, out int Y) => (X, Y) = (7, 8);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_09b()
        {
            // - An extension Deconstruct hides ITuple
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 4;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (7, 8)); // true (Extensions.Deconstruct)
        Console.WriteLine(t is (3, 4, 5)); // false (ITuple hidden by extension method)
        Console.WriteLine(t is (1, 2, 3)); // true via extension Deconstruct
        Console.WriteLine(t is (3, 4, 5, 6)); // true (via ITuple)
    }
}
static class Extensions
{
    public static void Deconstruct(this C c, out int X, out int Y) => (X, Y) = (7, 8);
    public static void Deconstruct(this ITuple c, out int X, out int Y, out int Z) => (X, Y, Z) = (1, 2, 3);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
False
True
True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleLacksLength()
        {
            // - should give an error when ITuple is missing required member (Length)
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        // int Length { get; }
        object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    // int System.Runtime.CompilerServices.ITuple.Length => 3;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (17,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(17, 32)
                );
        }

        [Fact]
        public void ITupleLacksIndexer()
        {
            // - should give an error when ITuple is missing required member (indexer)
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        // object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 3;
    // object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (17,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(17, 32)
                );
        }

        [Fact]
        public void ObsoleteITuple()
        {
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    [Obsolete(""WarningOnly"")]
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 3;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // (11,18): warning CS0618: 'ITuple' is obsolete: 'WarningOnly'
                // public class C : System.Runtime.CompilerServices.ITuple
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "System.Runtime.CompilerServices.ITuple").WithArguments("System.Runtime.CompilerServices.ITuple", "WarningOnly").WithLocation(11, 18)
                );
            var expectedOutput = @"True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ArgumentNamesInITuplePositional()
        {
            var source =
@"public class Program
{
    public static void Main()
    {
        object t = null;
        var r = t is (X: 3, Y: 4, Z: 5);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,23): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "X:").WithLocation(6, 23),
                // (6,29): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "Y:").WithLocation(6, 29),
                // (6,35): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "Z:").WithLocation(6, 35)
                );
        }

        [Fact]
        public void SymbolInfoForPositionalSubpattern()
        {
            var source =
@"using C2 = System.ValueTuple<int, int>;
public class Program
{
    public static void Main()
    {
        C1 c1 = null;
        if (c1 is (1, 2)) {}       // [0]
        if (c1 is (1, 2) Z1) {}    // [1]
        if (c1 is (1, 2) {}) {}    // [2]
        if (c1 is C1(1, 2) {}) {}  // [3]

        (int X, int Y) c2 = (1, 2);
        if (c2 is (1, 2)) {}       // [4]
        if (c2 is (1, 2) Z2) {}    // [5]
        if (c2 is (1, 2) {}) {}    // [6]
        if (c2 is C2(1, 2) {}) {}  // [7]
    }
}
class C1
{
    public void Deconstruct(out int X, out int Y) => X = Y = 0;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var dpcss = tree.GetRoot().DescendantNodes().OfType<DeconstructionPatternClauseSyntax>().ToArray();
            for (int i = 0; i < dpcss.Length; i++)
            {
                var dpcs = dpcss[i];
                var symbolInfo = model.GetSymbolInfo(dpcs);
                if (i <= 3)
                {
                    Assert.Equal("void C1.Deconstruct(out System.Int32 X, out System.Int32 Y)", symbolInfo.Symbol.ToTestDisplayString());
                }
                else
                {
                    Assert.Null(symbolInfo.Symbol);
                }

                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Empty(symbolInfo.CandidateSymbols);
            }
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_01()
        {
            var source = @"using System;
class C
{
    static (int, int)? Get(int i)
    {
        switch (i)
        {
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_01b()
        {
            var source = @"using System;
class C
{
    static ((int, int)?, int) Get(int i)
    {
        switch (i)
        {
            case 1:
                return ((1, 2), 1);
            case 2:
                return ((3, 4), 1);
            default:
                return (null, 1);
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var ((x, y), z))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_02()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return ('a', 'b');
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}

// Provide a ValueTuple that implements ITuple
namespace System
{
    using ITuple = System.Runtime.CompilerServices.ITuple;
    public struct ValueTuple<T1, T2>: ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
        int ITuple.Length => 2;
        object ITuple.this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    default: throw new System.ArgumentException(""index"");
                }
            }
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"0 a b; 1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_02b()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return (('a', 'b'), 1);
            case 1:
                return ((1, 2), 1);
            case 2:
                return ((3, 4), 1);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var ((x, y), z))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}

// Provide a ValueTuple that implements ITuple
namespace System
{
    using ITuple = System.Runtime.CompilerServices.ITuple;
    public struct ValueTuple<T1, T2>: ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
        int ITuple.Length => 2;
        object ITuple.this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    default: throw new System.ArgumentException(""index"");
                }
            }
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"0 a b; 1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_03()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return ('a', 'b');
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.WriteLine($""{i} {x} {y}"");
        }
    }
}

// Provide a ValueTuple that DOES NOT implements ITuple or have a Deconstruct method
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_04()
        {
            var source = @"using System;
struct C
{
    static C? Get(int i)
    {
        switch (i)
        {
            case 1:
                return new C(1, 2);
            case 2:
                return new C(3, 4);
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }

    public int Item1;
    public int Item2;
    public C(int item1, int item2) => (Item1, Item2) = (item1, item2);
    public void Deconstruct(out int Item1, out int Item2) => (Item1, Item2) = (this.Item1, this.Item2);
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DiscardVsConstantInCase_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case _:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,22): warning CS8512: The name '_' refers to the constant, not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
                //                 case _:
                Diagnostic(ErrorCode.WRN_CaseConstantNamedUnderscore, "_").WithLocation(11, 22)
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case _ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,22): warning CS8512: The name '_' refers to the constant, not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
                //                 case _ when true:
                Diagnostic(ErrorCode.WRN_CaseConstantNamedUnderscore, "_").WithLocation(11, 22)
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case var _:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "012345");
        }

        [Fact]
        public void DiscardVsConstantInCase_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case var _ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "012345");
        }

        [Fact]
        public void DiscardVsConstantInCase_05()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case @_:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_06()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case @_ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsTypeInCase_01()
        {
            var source = @"
class Program
{
    static void Main()
    {
        object o = new _();
        switch (o)
        {
            case _ x: break;
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,18): error CS0119: '_' is a type, which is not valid in the given context
                //             case _ x: break;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "_").WithArguments("_", "type").WithLocation(9, 18),
                // (9,20): error CS1003: Syntax error, ':' expected
                //             case _ x: break;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(":", "").WithLocation(9, 20),
                // (9,20): warning CS0164: This label has not been referenced
                //             case _ x: break;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "x").WithLocation(9, 20)
                );
        }

        [Fact]
        public void DiscardVsTypeInCase_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            switch (e)
            {
                case @_ x: Console.WriteLine(""3""); break;
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsTypeInIs_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is _);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,32): warning CS8513: The name '_' refers to the type '_', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
                //             Console.Write(e is _);
                Diagnostic(ErrorCode.WRN_IsTypeNamedUnderscore, "_").WithArguments("_").WithLocation(9, 32)
                );
            CompileAndVerify(compilation, expectedOutput: "FalseTrueFalse");
        }

        [Fact]
        public void DiscardVsTypeInIs_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is _ x);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,32): warning CS8513: The name '_' refers to the type '_', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.WRN_IsTypeNamedUnderscore, "_").WithArguments("_").WithLocation(9, 32),
                // (9,34): error CS1003: Syntax error, ',' expected
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(9, 34),
                // (9,34): error CS0103: The name 'x' does not exist in the current context
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(9, 34)
                );
        }

        [Fact]
        public void DiscardVsTypeInIs_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is var _);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "TrueTrueTrue");
        }

        [Fact]
        public void DiscardVsTypeInIs_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            if (e is @_)
            {
                Console.Write(""3"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (4, 4);
        foreach (var e in new[] { ((object, object)?)null, o, null })
        {
            if (e is (_, _))
            {
                Console.Write(""5"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "5");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (4, 4);
        foreach (var e in new[] { ((object, object)?)null, o, null })
        {
            if (e is (_ x, _))
            {
                Console.Write(""5"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19),
                // (10,22): error CS8502: Matching the tuple type '(object, object)' requires '2' subpatterns, but '3' subpatterns are present.
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_WrongNumberOfSubpatterns, "(_ x, _)").WithArguments("(object, object)", "2", "3").WithLocation(10, 22),
                // (10,25): error CS1003: Syntax error, ',' expected
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(10, 25),
                // (10,25): error CS0103: The name 'x' does not exist in the current context
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 25)
                );
        }

        [Fact]
        public void DiscardVsDeclarationInNested_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (new _(), 4);
        foreach (var e in new[] { ((object, object)?)null, o, (_, 8) })
        {
            if (e is (@_ x, var y))
            {
                Console.Write(y);
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "4");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (new _(), 4);
        foreach (var e in new[] { ((object, object)?)null, o, (_, 8) })
        {
            if (e is (@_, var y))
            {
                Console.Write(y);
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "8");
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_01()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        return (b1, b2) switch {
            (false, false) => 1,
            (false, true) => 2,
            // (true, false) => 3,
            (true, true) => 4
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,25): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //         return (b1, b2) switch {
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(6, 25)
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_02()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        return (b1, b2) switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_03()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        (bool? b1, bool? b2)? cond = (b1, b2);
        return cond switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4,
            (null, true) => 5
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_04()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        (bool? b1, bool? b2)? cond = (b1, b2);
        return cond switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4,
            _ => 5,
            (null, true) => 6
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,13): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //             (null, true) => 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "(null, true)").WithLocation(13, 13)
                );
        }

        [Fact]
        public void DeconstructVsITuple_01()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found one applicable Deconstruct method (even though the type implements ITuple): use it
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y))  // tuple pattern containing var patterns
            Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    int ITuple.Length => throw null;
    object ITuple.this[int i] => throw null;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3 4");
        }

        [Fact]
        public void DeconstructVsITuple_01b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found one applicable Deconstruct method (even though the type implements ITuple): use it
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y))  // var pattern containing tuple designator
            Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    int ITuple.Length => throw null;
    object ITuple.this[int i] => throw null;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3 4");
        }

        [Fact]
        public void DeconstructVsITuple_02()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found more than one applicable Deconstruct method (even though the type implements ITuple): error

            // var pattern with tuple designator
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y)) Console.Write($""{x} {y}"");
    }
}
interface I1
{
    void Deconstruct(out int X, out int Y);
}
interface I2
{
    void Deconstruct(out int X, out int Y);
}
interface IA: I1, I2 {}
class A: IA, I1, I2, ITuple
{
    void I1.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    void I2.Deconstruct(out int X, out int Y) => (X, Y) = (7, 8);
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,22): error CS0121: The call is ambiguous between the following methods or properties: 'I1.Deconstruct(out int, out int)' and 'I2.Deconstruct(out int, out int)'
                //         if (a is var (x, y)) Console.Write($"{x} {y}");
                Diagnostic(ErrorCode.ERR_AmbigCall, "(x, y)").WithArguments("I1.Deconstruct(out int, out int)", "I2.Deconstruct(out int, out int)").WithLocation(8, 22)
                );
        }

        [Fact]
        public void DeconstructVsITuple_02b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found more than one applicable Deconstruct method (even though the type implements ITuple): error

            // tuple pattern with var subpatterns
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y)) Console.Write($""{x} {y}"");
    }
}
interface I1
{
    void Deconstruct(out int X, out int Y);
}
interface I2
{
    void Deconstruct(out int X, out int Y);
}
interface IA: I1, I2 {}
class A: IA, I1, I2, ITuple
{
    void I1.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    void I2.Deconstruct(out int X, out int Y) => (X, Y) = (7, 8);
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,18): error CS0121: The call is ambiguous between the following methods or properties: 'I1.Deconstruct(out int, out int)' and 'I2.Deconstruct(out int, out int)'
                //         if (a is (var x, var y)) Console.Write($"{x} {y}");
                Diagnostic(ErrorCode.ERR_AmbigCall, "(var x, var y)").WithArguments("I1.Deconstruct(out int, out int)", "I2.Deconstruct(out int, out int)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void UnmatchedInput_01()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "InvalidOperationException");
        }

        [Fact]
        public void UnmatchedInput_02()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (MatchFailureException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System
{
    public class MatchFailureException : InvalidOperationException
    {
        public MatchFailureException() {}
        // public MatchFailureException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "MatchFailureException()");
        }

        [Fact]
        public void UnmatchedInput_03()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (MatchFailureException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System
{
    public class MatchFailureException : InvalidOperationException
    {
        public MatchFailureException() {}
        public MatchFailureException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "MatchFailureException((1, 2))");
        }

        [Fact]
        public void UnmatchedInput_04()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        try
        {
            _ = (1, 2) switch { (3, 4) => 1 };
        }
        catch (MatchFailureException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System
{
    public class MatchFailureException : InvalidOperationException
    {
        public MatchFailureException() {}
        public MatchFailureException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,24): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //             _ = (1, 2) switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(8, 24)
                );
            CompileAndVerify(compilation, expectedOutput: "MatchFailureException()");
        }

        [Fact]
        public void UnmatchedInput_05()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        try
        {
            R r = new R();
            _ = r switch { (3, 4) => 1 };
        }
        catch (MatchFailureException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
ref struct R
{
    public void Deconstruct(out int X, out int Y) => (X, Y) = (1, 2);
}
namespace System
{
    public class MatchFailureException : InvalidOperationException
    {
        public MatchFailureException() {}
        public MatchFailureException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //             _ = r switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "MatchFailureException()");
        }

        [Fact]
        public void DeconstructVsITuple_03()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found inapplicable Deconstruct method; use ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y)) Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y, out int Z);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y, out int Z) => throw null;
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "5 6");
        }

        [Fact]
        public void DeconstructVsITuple_03b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found inapplicable Deconstruct method; use ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y)) Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y, out int Z);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y, out int Z) => throw null;
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "5 6");
        }

        [Fact]
        public void ShortTuplePattern_01()
        {
            // test 0-element tuple pattern via ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new object[] { null, new ValueTuple(), new C(), new object() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C : ITuple
{
    int ITuple.Length => 0;
    object ITuple.this[int i] => throw new NotImplementedException();
}
namespace System
{
    struct ValueTuple : ITuple
    {
        int ITuple.Length => 0;
        object ITuple.this[int i] => throw new NotImplementedException();
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "12");
        }

        [Fact]
        public void ShortTuplePattern_02()
        {
            // test 1-element tuple pattern via ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new object[] { null, new ValueTuple<char>('a'), new C(), new object() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C : ITuple
{
    int ITuple.Length => 1;
    object ITuple.this[int i] => 'b';
}
namespace System
{
    struct ValueTuple<TItem1> : ITuple
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
        int ITuple.Length => 1;
        object ITuple.this[int i] => this.Item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a 2 b");
        }

        [Fact]
        public void ShortTuplePattern_03()
        {
            // test 0-element tuple pattern via Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C
{
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ShortTuplePattern_03b()
        {
            // test 0-element tuple pattern via extension Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C
{
}
public static class Extension
{
    public static void Deconstruct(this C self) {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ShortTuplePattern_04()
        {
            // test 1-element tuple pattern via Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C
{
    public void Deconstruct(out char a) => a = 'a';
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a");
        }

        [Fact]
        public void ShortTuplePattern_04b()
        {
            // test 1-element tuple pattern via extension Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C
{
}
public static class Extension
{
    public static void Deconstruct(this C self, out char a) => a = 'a';
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a");
        }

        [Fact]
        public void ShortTuplePattern_05()
        {
            // test 0-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple[] { new ValueTuple() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

namespace System
{
    struct ValueTuple
    {
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0");
        }

        [Fact]
        public void ShortTuplePattern_06()
        {
            // test 1-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple<char>[] { new ValueTuple<char>('a') };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

namespace System
{
    struct ValueTuple<TItem1>
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0 a");
        }

        [Fact]
        public void ShortTuplePattern_06b()
        {
            // test 1-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple<char>[] { new ValueTuple<char>('a') };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is var (x)) Console.Write($""{i} {x} "");
        }
    }
}

namespace System
{
    struct ValueTuple<TItem1>
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0 a");
        }

        [Fact, WorkItem(31167, "https://github.com/dotnet/roslyn/issues/31167")]
        public void NonExhaustiveBoolSwitchExpression()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        new Program().Start();
    }
    void Start()
    {
        Console.Write(M(true));
        try
        {
            Console.Write(M(false));
        }
        catch (Exception)
        {
            Console.Write("" throw"");
        }
    }
    public int M(bool b) 
    {
        return b switch
        {
           true => 1
        }; 
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (22,18): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //         return b switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(22, 18)
                );
            CompileAndVerify(compilation, expectedOutput: "1 throw");
        }

        [Fact]
        public void PointerAsInput_01()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p is var x)
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"12";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void PointerAsInput_02()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p switch { _ => true })
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"12";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void PointerAsInput_03()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p is null)
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"1";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void PointerAsInput_04()
        {
            var source =
@"public class C
{
    static unsafe void M(int* p)
    {
        if (p is {}) { }
        if (p is 1) { }
        if (p is var (x, y)) { }
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                // (5,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //         if (p is {}) { }
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "{}").WithLocation(5, 18),
                // (6,18): error CS0266: Cannot implicitly convert type 'int' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "int*").WithLocation(6, 18),
                // (6,18): error CS0150: A constant value is expected
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1").WithLocation(6, 18),
                // (7,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //         if (p is var (x, y)) { }
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var (x, y)").WithLocation(7, 18)
                );
        }
    }
}
