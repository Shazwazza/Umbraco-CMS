using System;
using System.Collections.Generic;
using Umbraco.Core.Macros;

namespace Umbraco.Core.Configuration.UmbracoSettings
{
    internal class ContentElement : IContentSection
    {
        public ContentElement()
        {
            //set defaults
            ResolveUrlsFromTextString = true;
            UploadAllowDirectories = true;
            EnsureUniqueNaming = true;
            XmlCacheEnabled = true;
            ContinouslyUpdateXmlDiskCache = true;
            XmlContentCheckForDiskChanges = false;
            EnableSplashWhileLoading = false;
            PropertyContextHelpOption = "text";
            ForceSafeAliases = true;
            PreviewBadge = @"<a id=""umbracoPreviewBadge"" style=""position: absolute; top: 0; right: 0; border: 0; width: 149px; height: 149px; background: url('{1}/preview/previewModeBadge.png') no-repeat;"" href=""{0}/endPreview.aspx?redir={2}""><span style=""display:none;"">In Preview Mode - click to end</span></a>";
            UmbracoLibraryCacheDuration = 1800;
            MacroErrorBehaviour = MacroErrorBehaviour.Inline;
            DisallowedUploadFiles = new List<string> { "ashx", "aspx", "ascx", "config", "cshtml", "vbhtml", "asmx", "air", "axd" };
            CloneXmlContent = true;
            GlobalPreviewStorageEnabled = false;
            DefaultDocumentTypeProperty = "Textstring";
            EnableInheritedDocumentTypes = true;
            Error404Collection = new List<IContentErrorPage>();
            ImageAutoFillProperties = new List<IImagingAutoFillUploadField>();
            ImageFileTypes = new List<string>();
            ImageTagAllowedAttributes = new List<string>();
            ScriptFileTypes = new List<string>();
        }

        public bool CloneXmlContent { get; set; }

        public bool ContinouslyUpdateXmlDiskCache { get; set; }

        public string DefaultDocumentTypeProperty { get; set; }

        public bool DisableHtmlEmail { get; set; }

        //bindable
        public List<string> DisallowedUploadFiles { get; set; }
        //explicit
        IEnumerable<string> IContentSection.DisallowedUploadFiles
        {
            get { return DisallowedUploadFiles; }
        }

        public bool EnableInheritedDocumentTypes { get; set; }

        public bool EnableSplashWhileLoading { get; set; }

        public bool EnsureUniqueNaming { get; set; }

        //bindable
        public List<IContentErrorPage> Error404Collection { get; set; }
        //explicit
        IEnumerable<IContentErrorPage> IContentSection.Error404Collection
        {
            get { return Error404Collection; }
        }

        public bool ForceSafeAliases { get; set; }

        public bool GlobalPreviewStorageEnabled { get; set; }

        //bindable
        public List<IImagingAutoFillUploadField> ImageAutoFillProperties { get; set; }
        //explicit
        IEnumerable<IImagingAutoFillUploadField> IContentSection.ImageAutoFillProperties
        {
            get { return ImageAutoFillProperties; }
        }

        //bindable
        public List<string> ImageFileTypes { get; set; }
        //explicit
        IEnumerable<string> IContentSection.ImageFileTypes
        {
            get { return ImageFileTypes; }
        }

        //bindable
        public List<string> ImageTagAllowedAttributes { get; set; }
        //explicit
        IEnumerable<string> IContentSection.ImageTagAllowedAttributes
        {
            get { return ImageFileTypes; }
        }

        public MacroErrorBehaviour MacroErrorBehaviour { get; set; }

        public string NotificationEmailAddress { get; set; }

        public string PreviewBadge { get; set; }

        public string PropertyContextHelpOption { get; set; }

        public bool ResolveUrlsFromTextString { get; set; }

        public bool ScriptEditorDisable { get; set; }

        //bindable
        public List<string> ScriptFileTypes { get; set; }
        //explicit
        IEnumerable<string> IContentSection.ScriptFileTypes
        {
            get { return ScriptFileTypes; }
        }

        public string ScriptFolderPath { get; set; }

        public int UmbracoLibraryCacheDuration{ get; set; }        

        public bool UploadAllowDirectories { get; set; }

        public bool XmlCacheEnabled { get; set; }

        public bool XmlContentCheckForDiskChanges { get; set; }
        
    }
}