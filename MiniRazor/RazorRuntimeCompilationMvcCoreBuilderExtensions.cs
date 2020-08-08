using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace MiniRazor
{
    internal static class RazorRuntimeCompilationMvcCoreBuilderExtensions
    {
        // TODO copy&pasted from https://github.com/dotnet/aspnetcore/blob/v3.1.6/src/Mvc/Mvc.Razor.RuntimeCompilation/src/DependencyInjection/RazorRuntimeCompilationMvcCoreBuilderExtensions.cs
        public static void AddServices(this RazorProjectEngineBuilder b, Lazy<IReadOnlyList<MetadataReference>> metadataReferencesLazy)
        {
            // Roslyn + TagHelpers infrastructure
            //var referenceManager = s.GetRequiredService<RazorReferenceManager>();
            //builder.Features.Add(new LazyMetadataReferenceFeature(referenceManager));
            b.Features.Add(new MiniRazorMetadataReferences(metadataReferencesLazy));
            b.Features.Add(new CompilationTagHelperFeature());

            // TagHelperDescriptorProviders (actually do tag helper discovery)
            b.Features.Add(new DefaultTagHelperDescriptorProvider());
            //b.Features.Add(new ViewComponentTagHelperDescriptorProvider());
            //builder.SetCSharpLanguageVersion(csharpCompiler.ParseOptions.LanguageVersion);
            //b.SetCSharpLanguageVersion(LanguageVersion.CSharp8);
        }
    }

    internal class MiniRazorMetadataReferences : IMetadataReferenceFeature
    {
        private readonly Lazy<IReadOnlyList<MetadataReference>> _metadataReferencesLazy;

        public MiniRazorMetadataReferences(Lazy<IReadOnlyList<MetadataReference>> metadataReferencesLazy)
        {
            _metadataReferencesLazy = metadataReferencesLazy;
        }

        public RazorEngine Engine { get; set; }

        public IReadOnlyList<MetadataReference> References => _metadataReferencesLazy.Value;
    }
}
