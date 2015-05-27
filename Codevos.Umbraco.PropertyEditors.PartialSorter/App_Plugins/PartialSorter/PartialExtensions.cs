using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codevos.Umbraco.PropertyEditors.PartialSorter;
using Constants = Codevos.Umbraco.PropertyEditors.PartialSorter.Constants;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Newtonsoft.Json.Linq;

namespace System.Web.Mvc.Html
{
    /// <summary>
    /// Represents the functionality to render partial views as HTML-encoded strings, based on the PartialSorter JSON configuration files.
    /// </summary>
    public static class PartialExtensions
    {
        private static void GetSortedPartialsForDocumentType(IPublishedContent settingsPage, string documentTypeAlias, string propertyAlias, HashSet<string> partials)
        {
            JObject sortValues = settingsPage.GetPropertyValue(propertyAlias) as JObject;

            if (sortValues != null)
            {
                JArray partialNames = sortValues[documentTypeAlias] as JArray;

                if (partialNames != null)
                {
                    foreach (JToken name in partialNames)
                    {
                        partials.Add((string)name);
                    }
                }
            }
        }

        private static void MergeSortedPartialsWithDefaultConfig(int settingsPageId, string documentTypeAlias, string propertyAlias, HashSet<string> partials)
        {
            JObject config = Config.GetConfiguration(settingsPageId, propertyAlias);

            if (config != null)
            {
                JArray docTypes = config[Constants.Json.DocTypes] as JArray;

                if (docTypes != null)
                {
                    JToken docType = docTypes.FirstOrDefault(d => (string)d[Constants.Json.Alias] == documentTypeAlias);

                    if (docType != null)
                    {
                        JArray defaultPartials = docType[Constants.Json.Partials] as JArray;

                        if (defaultPartials != null)
                        {
                            foreach (JToken partial in defaultPartials)
                            {
                                string partialName = (string)partial[Constants.Json.Alias];

                                if (!partials.Contains(partialName))
                                {
                                    partials.Add(partialName);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <para>Checks the template settings node for a property with the given alias.</para>
        /// <para>When the property exists, it's JSON value is used to render a list of partial views, based on the page's document type alias.</para>
        /// <para>If the document type alias doesn't exist, the partial names from the JSON config file are used.</para>
        /// </summary>
        /// <param name="helper">The helper.</param>
        /// <param name="page">The content page to render the partial views for.</param>
        /// <param name="propertyAlias">The PartialSorter property alias.</param>
        /// <param name="model">The model to pass to the partials views.</param>
        /// <returns>All parsed partial views.</returns>
        public static MvcHtmlString SortedPartials(this HtmlHelper helper, IPublishedContent page, string propertyAlias, object model)
        {
            HashSet<string> partials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            IPublishedContent homePage = page.AncestorOrSelf(1);

            if (homePage != null)
            {
                IPublishedContent settingsPage = homePage.Children.FirstOrDefault(c => c.DocumentTypeAlias.EndsWith("settings", StringComparison.OrdinalIgnoreCase));

                if (settingsPage != null)
                {
                    if (String.IsNullOrEmpty(propertyAlias))
                    {
                        PublishedPropertyType propertyType = settingsPage.ContentType.PropertyTypes.FirstOrDefault(p => p.PropertyEditorAlias == Constants.PropertyEditor.Alias);

                        if (propertyType != null)
                        {
                            propertyAlias = propertyType.PropertyTypeAlias;
                        }                      
                    }

                    if (!String.IsNullOrEmpty(propertyAlias))
                    {
                        GetSortedPartialsForDocumentType(settingsPage, page.DocumentTypeAlias, propertyAlias, partials);
                    }
                    
                    MergeSortedPartialsWithDefaultConfig(settingsPage.Id, page.DocumentTypeAlias, propertyAlias, partials);
                }
            }
           
            StringBuilder outputBuilder = new StringBuilder();

            foreach (string partial in partials)
            {
                if (!String.IsNullOrWhiteSpace(partial))
                {
                    outputBuilder.Append(helper.Partial(partial, model));
                }
            }

            return outputBuilder.Length > 0 ? new MvcHtmlString(outputBuilder.ToString()) : null;
        }

        /// <summary>
        /// <para>Gets the first property with the PartialSorter type from the template settings node.</para>
        /// <para>When the property exists, it's JSON value is used to render a list of partial views, based on the page's document type alias.</para>
        /// <para>If the document type alias doesn't exist, the partial names from the JSON config file are used.</para>
        /// </summary>
        /// <param name="helper">The helper.</param>
        /// <param name="page">The content page to render the partial views for.</param>
        /// <param name="model">The model to pass to the partials views.</param>
        /// <returns>All parsed partial views.</returns>
        public static MvcHtmlString SortedPartials(this HtmlHelper helper, IPublishedContent page, object model)
        {
            return SortedPartials(helper, page, null, model);
        }
    }
}