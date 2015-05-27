using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Codevos.Umbraco.PropertyEditors.PartialSorter
{
    /// <summary>
    /// Provides methods for loading PartialSorter JSON configuration files.
    /// </summary>
    public static class Config
    {
        #region Private variables

        private static ConcurrentDictionary<string, JObject> JsonConfigs = new ConcurrentDictionary<string, JObject>();
        private static ConcurrentDictionary<string, JObject> JsonConfigsTranslated = new ConcurrentDictionary<string, JObject>();

        #endregion


        #region Private methods

        private static string GetDictionaryKey(int pageId, string propertyAlias)
        {
            return String.Format("{0}_{1}", pageId, propertyAlias);
        }

        private static JObject GetCachedConfig(ConcurrentDictionary<string, JObject> dictionary, int pageId, string propertyAlias)
        {
            JObject config;
            dictionary.TryGetValue(GetDictionaryKey(pageId, propertyAlias), out config);
            return config;
        }

        private static void SetCachedConfig(ConcurrentDictionary<string, JObject> dictionary, int pageId, string propertyAlias, JObject config)
        {
            dictionary[GetDictionaryKey(pageId, propertyAlias)] = config;
        }

        private static void AddTranslations(JObject parent, IDictionaryItem dictionaryItem)
        {
            if (dictionaryItem != null)
            {
                JObject translations = new JObject();

                foreach (IDictionaryTranslation translation in dictionaryItem.Translations)
                {
                    translations.Add(translation.Language.IsoCode.Replace('-', '_').ToLower(), translation.Value);
                }

                parent.Add(Constants.Json.Translations, translations);
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Gets the JSON configuration file from the property with the given alias, by reading it's data type's pre values.
        /// </summary>
        /// <param name="pageId">The of the page containing the PartialSorter.</param>
        /// <param name="propertyAlias">The PartialSorter property alias.</param>
        /// <returns>The loaded JSON configuration file.</returns>
        public static JObject GetConfiguration(int pageId, string propertyAlias)
        {
            JObject config = GetCachedConfig(JsonConfigs, pageId, propertyAlias);

            if (config == null)
            {
                ServiceContext services = ApplicationContext.Current.Services;
                IContentService contentService = services.ContentService;
                IContent settingsPage = contentService.GetPublishedVersion(pageId);

                PropertyType property = settingsPage.ContentType.PropertyTypes.FirstOrDefault(p => p.Alias == propertyAlias);

                if (property != null)
                {
                    PreValueCollection preValues = services.DataTypeService.GetPreValuesCollectionByDataTypeId(property.DataTypeDefinitionId);
                    PreValue jsonConfigUrl;

                    if (preValues.PreValuesAsDictionary.TryGetValue("jsonConfigUrl", out jsonConfigUrl) && !String.IsNullOrWhiteSpace(jsonConfigUrl.Value))
                    {
                        string url = jsonConfigUrl.Value;

                        if (!url.StartsWith("~"))
                        {
                            url = String.Format("{0}{1}", url.StartsWith("/") ? "~" : "~/", url);
                        }

                        string json;

                        using (StreamReader sr = new StreamReader(HttpContext.Current.Server.MapPath(url)))
                        {
                            json = sr.ReadToEnd();
                        }

                        config = JObject.Parse(json);
                        SetCachedConfig(JsonConfigs, pageId, propertyAlias, config);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// <para>Gets the JSON configuration file from the property with the given alias, by reading it's data type's pre values.</para>
        /// <para>This includes translations of document type / partial names.</para>
        /// </summary>
        /// <param name="pageId">The of the page containing the PartialSorter.</param>
        /// <param name="propertyAlias">The PartialSorter property alias.</param>
        /// <param name="language">The language use in the JSON configuration file.</param>
        /// <returns>The loaded JSON configuration file with translations.</returns>
        public static JObject GetTranslatedConfiguration(int pageId, string propertyAlias, string language)
        {
            JObject config = GetCachedConfig(JsonConfigsTranslated, pageId, propertyAlias);

            if (config == null)
            {
                config = (JObject)GetConfiguration(pageId, propertyAlias).DeepClone();

                ServiceContext services = ApplicationContext.Current.Services;
                ILocalizationService localizationService = services.LocalizationService;
                IContentTypeService contentTypeService = services.ContentTypeService;

                foreach (JObject docType in config[Constants.Json.DocTypes])
                {
                    IContentType contentType = contentTypeService.GetContentType((string)docType[Constants.Json.Alias]);

                    if (contentType != null)
                    {
                        string name = contentType.Name;
                        if (name.StartsWith("#"))
                        {
                            name = name.Substring(1);
                        }

                        AddTranslations(docType, localizationService.GetDictionaryItemByKey(name));
                    }

                    foreach (JObject partial in docType[Constants.Json.Partials])
                    {
                        AddTranslations(partial, localizationService.GetDictionaryItemByKey((string)partial[Constants.Json.Alias]));
                    }
                }

                SetCachedConfig(JsonConfigsTranslated, pageId, propertyAlias, config);
            }

            config = (JObject)config.DeepClone();
            config.Add("language", new JValue(language));

            return config;
        }

        #endregion
    }
}