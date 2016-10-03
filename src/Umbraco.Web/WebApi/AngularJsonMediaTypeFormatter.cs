using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Umbraco.Web.WebApi
{
    /// <summary>
    /// This will format the JSON output for use with AngularJs's approach to JSON Vulnerability attacks
    /// </summary>
    /// <remarks>
    /// See: http://docs.angularjs.org/api/ng.$http (Security considerations)
    /// </remarks>
    public class AngularJsonMediaTypeFormatter : JsonOutputFormatter
    {
        public AngularJsonMediaTypeFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool) : base(serializerSettings, charPool)
        {
        }

        /// <inheritdoc />
        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (selectedEncoding == null) throw new ArgumentNullException("selectedEncoding");
            using (var writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
            {
                //write the special encoding for angular json to the start
                // (see: http://docs.angularjs.org/api/ng.$http)
                writer.Write(")]}',\n");
                writer.Flush();

                WriteObject(writer, context.Object);
                await writer.FlushAsync();
            }                
        }
    }
}
