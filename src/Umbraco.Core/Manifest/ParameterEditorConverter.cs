using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core.IO;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Serialization;

namespace Umbraco.Core.Manifest
{
    /// <summary>
    /// Used to convert a parameter editor manifest to a property editor object
    /// </summary>
    internal class ParameterEditorConverter : JsonCreationConverter<ParameterEditor>
    {
        private readonly IOHelper _ioHelper;

        public ParameterEditorConverter(IOHelper ioHelper)
        {
            _ioHelper = ioHelper;
        }

        protected override ParameterEditor Create(Type objectType, JObject jObject)
        {
            return new ParameterEditor(_ioHelper);
        }

        protected override void Deserialize(JObject jObject, ParameterEditor target, JsonSerializer serializer)
        {
            //since it's a manifest editor, we need to create it's instance.
            //we need to specify the view value for the editor here otherwise we'll get an exception.
            target.ManifestDefinedParameterValueEditor = new ParameterValueEditor
            {
                View = jObject["view"].ToString()
            };

            base.Deserialize(jObject, target, serializer);
        }
    }
}