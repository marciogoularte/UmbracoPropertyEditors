using Newtonsoft.Json.Linq;
using Umbraco.Web;
using Umbraco.Web.WebApi;

namespace Codevos.Umbraco.PropertyEditors.PartialSorter
{
    /// <summary>
    /// Provides methods for loading PartialSorter JSON configuration files in the Umbraco backoffice.
    /// </summary>
    public class PartialSorterConfigController : UmbracoAuthorizedApiController
    {
        /// <summary>
        /// <para>Gets the JSON configuration file from the property with the given alias, by reading it's data type's pre values.</para>
        /// <para>This includes translations of document type / partial names.</para>
        /// </summary>
        /// <param name="pageId">The of the page containing the PartialSorter.</param>
        /// <param name="propertyAlias">The PartialSorter property alias.</param>
        /// <returns>The loaded JSON configuration file with translations.</returns>
        public JObject Get(int pageId, string propertyAlias)
        {
            return Config.GetTranslatedConfiguration(pageId, propertyAlias, UmbracoContext.Current.Security.CurrentUser.Language);
        }
    }
}