using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AuthoringTagHelpers.TagHelpers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using MiniRazor;

namespace ConsoleApp1
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var tagHelperAssemblies = new[]
                                       {
                                           typeof(InputTagHelper).GetTypeInfo().Assembly,
                                           typeof(UrlResolutionTagHelper).GetTypeInfo().Assembly,
                                           typeof(EmailTagHelper).GetTypeInfo().Assembly
                                       };
            // MiniRazor already scans all referenced assemblies. So the above line would suffice already.
            MiniRazorTemplateEngine.GetAdditionalAssemblyReferences = () => tagHelperAssemblies;
            
            var engine = new MiniRazorTemplateEngine();

            // Compile template (you may want to cache this instance)
            var template = engine.Compile(@"
@using AuthoringTagHelpers
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, AuthoringTagHelpers
<h1>@Model.Title</h1>
<!-- expected: <a href=""mailto:Support@contoso.com"">Support@contoso.com</a> -->
<email mail-to=""Support""></email>
");

            // Render template
            var result = await template.RenderAsync(new { Title = "TagHelper Test" });

            Console.WriteLine(result);
        }
    }
}
