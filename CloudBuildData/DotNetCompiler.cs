using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CloudBuildData
{
    public class DotNetCompiler
    {

        public static byte[] CompileConsoleApp(string sourceCode)
        {
            // create the Roslyn compilation for the main program with
            // ConsoleApplication compilation options
            // adding references to A.netmodule and B.netmodule
            var mainCompilation =
                DotNetCompiler.CreateCompilationWithMscorlib
                (
                    "program",
                    sourceCode,
                    compilerOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                );

            // emit the compilation result to a byte array 
            // corresponding to A.netmodule byte code
            byte[] result = DotNetCompiler.EmitToArray(mainCompilation);

            return result;
        }

        public static byte[] TestRoslyn()
        {
            var mainProgramString =
                @"public class Program
                        {
                            public static void Main()
                            {
                                int i=3;
                                System.Console.Write(""hello world!!!!""); 
                            }
                        }";

            byte[] result = DotNetCompiler.CompileConsoleApp(mainProgramString);

            return result;

            //File.WriteAllBytes(@"c:\temp\try.exe", mainCompiledResult);
        }


        // a utility method that creates Roslyn compilation
        // for the passed code. 
        // The compilation references the collection of 
        // passed "references" arguments plus
        // the mscore library (which is required for the basic
        // functionality).
        public static CSharpCompilation CreateCompilationWithMscorlib
        (
            string assemblyOrModuleName,
            string code,
            CSharpCompilationOptions compilerOptions = null,
            IEnumerable<MetadataReference> references = null)
        {
            // create the syntax tree
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(code, null, "");

            // get the reference to mscore library
            MetadataReference mscoreLibReference =
                AssemblyMetadata
                    .CreateFromFile(typeof(string).Assembly.Location)
                    .GetReference();

            // create the allReferences collection consisting of 
            // mscore reference and all the references passed to the method
            var allReferences =
                new List<MetadataReference>() { mscoreLibReference };
            if (references != null)
            {
                allReferences.AddRange(references);
            }

            // create and return the compilation
            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyOrModuleName,
                new[] { syntaxTree },
                options: compilerOptions,
                references: allReferences
            );

            return compilation;
        }

        // emit the compilation result into a byte array.
        // throw an exception with corresponding message
        // if there are errors
        public static byte[] EmitToArray(Compilation compilation)
        {
            using (var stream = new MemoryStream())
            {
                // emit result into a stream
                var emitResult = compilation.Emit(stream);

                if (!emitResult.Success)
                {
                    // if not successful, throw an exception
                    Diagnostic firstError =
                        emitResult
                            .Diagnostics
                            .FirstOrDefault
                            (
                                diagnostic =>
                                    diagnostic.Severity == DiagnosticSeverity.Error
                            );

                    throw new Exception(firstError?.GetMessage());
                }

                // get the byte array from a stream
                return stream.ToArray();
            }
        }
    }
}
