using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace MiniRazor
{
    // TODO implement functionality needed for TagHelper support
    // copy&pasted from https://github.com/dotnet/aspnetcore/blob/v3.1.6/src/Mvc/Mvc.Razor/src/RazorPageBase.cs
    public abstract partial class MiniRazorTemplateBase
    {

        void WriteTagHelperOutput(TagHelperOutput t)
        {
            using var writer = new BufferWriter(_buffer);
            t.WriteTo(writer, HtmlEncoder);
        }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        //[RazorInject]
        public HtmlEncoder HtmlEncoder = HtmlEncoder.Default;

        private Stack<TagHelperScopeInfo> TagHelperScopes { get; } = new Stack<TagHelperScopeInfo>();

        private ITagHelperFactory _tagHelperFactory;

        private ITagHelperFactory TagHelperFactory
        {
            get
            {
                if (_tagHelperFactory == null)
                {
                    //var services = ViewContext.HttpContext.RequestServices;
                    //_tagHelperFactory = services.GetRequiredService<ITagHelperFactory>();
                    _tagHelperFactory = new DefaultTagHelperFactory(new DefaultTagHelperActivator(new TypeActivatorCache())); // from IServiceCollection Singleton
                }

                return _tagHelperFactory;
            }
        }

        /// <summary>
        /// Gets the application base relative path to the page.
        /// </summary>
        //public string Path;
        public string Path = "MiniRazorTemplate";

        private IViewBufferScope _bufferScope;

        private IViewBufferScope BufferScope
        {
            get
            {
                if (_bufferScope == null)
                {
                    //var services = ViewContext.HttpContext.RequestServices;
                    //_bufferScope = services.GetRequiredService<IViewBufferScope>();
                    var viewBufferPool = ArrayPool<ViewBufferValue>.Shared; // from IServiceCollection Singleton
                    var charPool = ArrayPool<char>.Shared; // from IServiceCollection Singleton
                    _bufferScope = new MemoryPoolViewBufferScope(viewBufferPool, charPool); // from IServiceCollection Singleton
                }

                return _bufferScope;
            }
        }

        /// <summary>
        /// Creates and activates a <see cref="ITagHelper"/>.
        /// </summary>
        /// <typeparam name="TTagHelper">A <see cref="ITagHelper"/> type.</typeparam>
        /// <returns>The activated <see cref="ITagHelper"/>.</returns>
        /// <remarks>
        /// <typeparamref name="TTagHelper"/> must have a parameterless constructor.
        /// </remarks>
        public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper
        {
            var sp = new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());
            return new TypeActivatorCache().CreateInstance<TTagHelper>(sp, typeof(TTagHelper));
            //return TagHelperFactory.CreateTagHelper<TTagHelper>(ViewContext);
        }

        /// <summary>
        /// Starts a new writing scope and optionally overrides <see cref="HtmlEncoder"/> within that scope.
        /// </summary>
        /// <param name="encoder">
        /// The <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/> handles
        /// non-<see cref="IHtmlContent"/> C# expressions. If <c>null</c>, does not change <see cref="HtmlEncoder"/>.
        /// </param>
        /// <remarks>
        /// All writes to the <see cref="Output"/> or <see cref="ViewContext.Writer"/> after calling this method will
        /// be buffered until <see cref="EndTagHelperWritingScope"/> is called.
        /// </remarks>
        public void StartTagHelperWritingScope(HtmlEncoder encoder)
        {
            //var viewContext = ViewContext;
            var buffer = new ViewBuffer(BufferScope, Path, ViewBuffer.TagHelperPageSize);
            //TagHelperScopes.Push(new TagHelperScopeInfo(buffer, HtmlEncoder, viewContext.Writer));
            //TagHelperScopes.Push(new TagHelperScopeInfo(buffer, HtmlEncoder, WriterProxy));
            TagHelperScopes.Push(new TagHelperScopeInfo(buffer, null, null));

            // If passed an HtmlEncoder, override the property.
            if (encoder != null)
            {
                HtmlEncoder = encoder;
            }

            // We need to replace the ViewContext's Writer to ensure that all content (including content written
            // from HTML helpers) is redirected.
            //viewContext.Writer = new ViewBufferTextWriter(buffer, viewContext.Writer.Encoding);
        }

        /// <summary>
        /// Ends the current writing scope that was started by calling <see cref="StartTagHelperWritingScope"/>.
        /// </summary>
        /// <returns>The buffered <see cref="TagHelperContent"/>.</returns>
        public TagHelperContent EndTagHelperWritingScope()
        {
            if (TagHelperScopes.Count == 0)
            {
                throw new InvalidOperationException("Resources.RazorPage_ThereIsNoActiveWritingScopeToEnd");
            }

            var scopeInfo = TagHelperScopes.Pop();

            // Get the content written during the current scope.
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.AppendHtml(scopeInfo.Buffer);

            // Restore previous scope.
            //HtmlEncoder = scopeInfo.HtmlEncoder;
            //ViewContext.Writer = scopeInfo.Writer;

            return tagHelperContent;
        }

        //public void BeginAddHtmlAttributeValues()
        //public void AddHtmlAttributeValue()
        //public void EndAddHtmlAttributeValues()
        //public void BeginWriteTagHelperAttribute()
        //public string EndWriteTagHelperAttribute()

        private readonly struct TagHelperScopeInfo
        {
            public TagHelperScopeInfo(ViewBuffer buffer, HtmlEncoder encoder, TextWriter writer)
            {
                Buffer = buffer;
                HtmlEncoder = encoder;
                Writer = writer;
            }

            public ViewBuffer Buffer { get; }

            public HtmlEncoder HtmlEncoder { get; }

            public TextWriter Writer { get; }
        }
    }

    internal class BufferWriter : TextWriter
    {
        private readonly StringBuilder _buffer;

        public BufferWriter(StringBuilder buffer)
        {
            _buffer = buffer;
        }

        // Microsoft.AspNetCore.Mvc.Formatters.MediaType GetEncoding() GetEncodingFromCharset()
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _buffer.Append(value);
        }
    }
}