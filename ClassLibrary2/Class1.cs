using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ClassLibrary2
{
    [Generator]
    public class HelloWorldGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var configuration = RazorConfiguration.Default;

            var compilation = context.Compilation;

            // TODO: Figure these out.
            var projectDirectory = @"C:\Users\prkrishn\source\repos\ClassLibrary2\ConsoleApp2\";
            var rootNamespace = "ConsoleApp2";

            var tagHelperFeature = new CompilationTagHelperFeature(() => compilation);

            var discoveryProjectEngine = RazorProjectEngine.Create(configuration, RazorProjectFileSystem.Create(projectDirectory), b =>
            {
                b.Features.Add((IRazorFeature)Activator.CreateInstance(typeof(CompilerFeatures).Assembly.GetType("Microsoft.CodeAnalysis.Razor.DefaultTypeNameFeature")));
                b.Features.Add(new SetSuppressPrimaryMethodBodyOptionFeature());
                b.Features.Add(new SuppressChecksumOptionsFeature());

                b.SetRootNamespace(rootNamespace);

                var metadataReferences = new List<MetadataReference>(context.Compilation.References);
                b.Features.Add(new DefaultMetadataReferenceFeature { References = metadataReferences });

                b.Features.Add(tagHelperFeature);
                b.Features.Add(new DefaultTagHelperDescriptorProvider());

                CompilerFeatures.Register(b);

                b.SetCSharpLanguageVersion(((CSharpParseOptions)context.ParseOptions).LanguageVersion);
            });

            foreach (var file in context.AdditionalFiles.Where(f => f.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)))
            {
                var codeGen = discoveryProjectEngine.Process(discoveryProjectEngine.FileSystem.GetItem(file.Path, FileKinds.Component));

                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(codeGen.GetCSharpDocument().GeneratedCode));
            }

            var tagHelpers = tagHelperFeature.GetDescriptors();

            var projectEngine = RazorProjectEngine.Create(configuration, RazorProjectFileSystem.Create(projectDirectory), b =>
            {
                b.Features.Add((IRazorFeature)Activator.CreateInstance(typeof(CompilerFeatures).Assembly.GetType("Microsoft.CodeAnalysis.Razor.DefaultTypeNameFeature")));
                b.SetRootNamespace(rootNamespace);

                b.Features.Add(new StaticTagHelperFeature { TagHelpers = tagHelpers, });
                b.Features.Add(new DefaultTagHelperDescriptorProvider());

                CompilerFeatures.Register(b);

                b.SetCSharpLanguageVersion(((CSharpParseOptions)context.ParseOptions).LanguageVersion);
            });

            foreach (var file in context.AdditionalFiles.Where(f => f.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)))
            {
                var codeGen = projectEngine.Process(projectEngine.FileSystem.GetItem(file.Path, FileKinds.Component));

                var path = file.Path.Replace(':', '_').Replace('\\', '_').Replace('/', '_');

                context.AddSource(path, SourceText.From(codeGen.GetCSharpDocument().GeneratedCode, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private class SetSuppressPrimaryMethodBodyOptionFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                options.SuppressPrimaryMethodBody = true;
            }
        }

        internal class SuppressChecksumOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                options.SuppressChecksum = true;
            }
        }

        private sealed class CompilationTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
        {
            private ITagHelperDescriptorProvider[] _providers;
            private IMetadataReferenceFeature _referenceFeature;
            private readonly Func<Compilation> _compilationFactory;

            public CompilationTagHelperFeature(Func<Compilation> compilationFactory)
            {
                _compilationFactory = compilationFactory;
            }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
            {
                var compilation = _compilationFactory();
                var results = new List<TagHelperDescriptor>();

                var context = TagHelperDescriptorProviderContext.Create(results);
                context.SetCompilation(_compilationFactory());

                for (var i = 0; i < _providers.Length; i++)
                {
                    _providers[i].Execute(context);
                }

                return results;
            }

            protected override void OnInitialized()
            {
                _referenceFeature = Engine.Features.OfType<IMetadataReferenceFeature>().FirstOrDefault();
                _providers = Engine.Features.OfType<ITagHelperDescriptorProvider>().OrderBy(f => f.Order).ToArray();
            }

            internal static bool IsValidCompilation(Compilation compilation)
            {
                var @string = compilation.GetSpecialType(SpecialType.System_String);

                // Do some minimal tests to verify the compilation is valid. If symbols for System.String
                // is missing or errored, the compilation may be missing references.
                return @string != null && @string.TypeKind != TypeKind.Error;
            }
        }

        private class StaticTagHelperFeature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => TagHelpers;
        }
    }
}
