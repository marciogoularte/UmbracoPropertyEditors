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
        private static void GetSortedPartialsForDocumentType(IPublishedContent sorterPage, string sorterPropertyAlias, string documentTypeAlias, HashSet<string> partials)
        {
            JObject sortValues = sorterPage.GetPropertyValue(sorterPropertyAlias) as JObject;

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

        private static void MergeSortedPartialsWithDefaultConfig(int sorterPageId, string sorterPropertyAlias, string documentTypeAlias, HashSet<string> partials)
        {
            JObject config = Config.GetConfiguration(sorterPageId, sorterPropertyAlias);

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
        /// <para>Renders a sorted list of partials.</para>
        /// <para>The sortorder is based on the PartialSorter JSON configuration, merged with the PartialSorter property value.</para>
        /// </summary>
        /// <param name="helper">The helper.</param>
        /// <param name="page">The content page to render the partial views for.</param>
        /// <param name="model">The model to pass to the partials views.</param>
        /// <param name="sorterPageId">The id of the page containing the PartialSorter property. If empty, the first found child with a document alias ending with 'settings' is used.</param>
        /// <param name="sorterPropertyAlias">The PartialSorter property alias. If empty, the first found PartialSorter is used.</param>
        /// <returns>All parsed partial views.</returns>
        public static MvcHtmlString SortedPartials(this HtmlHelper helper, IPublishedContent page, object model, int sorterPageId = 0, string sorterPropertyAlias = null)
        {
            HashSet<string> partials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IPublishedContent sorterPage = null;

            if (sorterPageId > 0)
            {
                sorterPage = new UmbracoHelper(UmbracoContext.Current).TypedContent(sorterPageId);
            }
            else
            {
                IPublishedContent homePage = page.AncestorOrSelf(1);
                if (homePage != null)
                {
                    sorterPage = homePage.Children.FirstOrDefault(c => c.DocumentTypeAlias.EndsWith("settings", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (sorterPage != null)
            {
                if (String.IsNullOrWhiteSpace(sorterPropertyAlias))
                {
                    PublishedPropertyType propertyType = sorterPage.ContentType.PropertyTypes.FirstOrDefault(p => p.PropertyEditorAlias == Constants.PropertyEditor.Alias);
                    
                    if (propertyType != null)
                    {
                        sorterPropertyAlias = propertyType.PropertyTypeAlias;
                    }
                }

                if (!String.IsNullOrEmpty(sorterPropertyAlias))
                {
                    GetSortedPartialsForDocumentType(sorterPage, sorterPropertyAlias, page.DocumentTypeAlias, partials);
                    MergeSortedPartialsWithDefaultConfig(sorterPage.Id, sorterPropertyAlias, page.DocumentTypeAlias, partials);
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
    }
}