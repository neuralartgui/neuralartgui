using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// DeepDream Parameter to represent XML Nodes.
    /// </summary>
    public class DeepDreamParameter
    {
        /// <summary>
        /// Paramter Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Human-Readable Parameter Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter Description. Usually visible as Tooltip.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sample Image for Parameter. To use for ModelParameter.
        /// </summary>
        public ImageSource SampleImge { get; set; }
    }

    /// <summary>
    /// Parametercollection. Extends Observable Collection by curstom Index Operator.
    /// </summary>
    public class ParameterCollection
    {
        /// <summary>
        /// Observable Colletion of DeepDreamParameter. Can be bound to UI.
        /// </summary>
        public ObservableCollection<DeepDreamParameter> Collection { get; set; } = new ObservableCollection<DeepDreamParameter>();

        /// <summary>
        /// Returns Parameter for Name. This is Used to bind DeepDreamParameter directyl from XAML. 
        /// Returns "Not Available" Parameter is Key was not found.
        /// </summary>
        /// <param name="key">Parameterkey</param>
        /// <returns>DeepDreamParameter for Key</returns>
        public DeepDreamParameter this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key)) return null;
                return Collection?.FirstOrDefault(p => String.Equals(p.Key, key, StringComparison.CurrentCultureIgnoreCase)) ?? new DeepDreamParameter {Name = "Nicht Eingetragen", Key = key};
            }
        }
    }

    /// <summary>
    /// Set of Model- and Inputparameter Collections
    /// </summary>
    public class DeepDreamParameterSet
    {
        /// <summary>
        /// Collection of UI Input Element Parameter. Names of UI Input Controls can be bound to this.
        /// </summary>
        public ParameterCollection InputParameter;

        /// <summary>
        /// Collection of DeepDream ModelParameter. Content of DeepDream Model selection Combobox is bound to this.
        /// </summary>
        public ParameterCollection ModelParameter;
    }

    /// <summary>
    /// Class to load Parameternames from xml File.
    /// </summary>
    public class ParameterModel
    {
        private static DeepDreamParameterSet cache = null;
        /// <summary>
        /// Load Paramter from XML File. Returns cached result if already performed.
        /// </summary>
        /// <returns>Set of loaded parameter; Cached</returns>
        public static Task<DeepDreamParameterSet> LoadParameter()
        {

            return Task<DeepDreamParameterSet>.Factory.StartNew(() =>
            {
                if (cache != null) return cache;

                var location = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                if (location != null)
                {
                    var xmlFile = Path.Combine(location, "parameternames.xml");

                    var inputParam = new ParameterCollection();
                    var filterParam = new ParameterCollection();

                    XDocument xml = null;
                    try
                    {
                        xml = XDocument.Load(xmlFile);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("ParameterModel.LoadParameter; "+ex);
                        throw;
                    }

                    foreach (var param in xml.Descendants("input_parameter").Elements())
                    {
                        try
                        {
                            inputParam.Collection.Add(new DeepDreamParameter
                            {
                                Key = param?.Attribute("key")?.Value,
                                Description = param?.Attribute("description")?.Value,
                                Name = param?.Attribute("name")?.Value
                            });
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"ParameteModel.LoadParameter; {ex}");
                            throw;
                        }
                    }

                    var exeloc = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) ?? String.Empty;
                    foreach (var param in xml.Descendants("model_parameter").Elements())
                    {
                        try
                        {
                            var sampleName = param?.Attribute("sample")?.Value ?? "null";
                            filterParam.Collection.Add(new DeepDreamParameter
                            {
                                Key = param?.Attribute("key")?.Value,
                                Description = param?.Attribute("description")?.Value,
                                Name = param?.Attribute("name")?.Value,
                                SampleImge = ImageModel.LoadImage(Path.Combine(exeloc, "samples", sampleName))
                            });
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"ParameteModel.LoadParameter; {ex}");
                            //Console.WriteLine(ex);
                            throw;
                        }   
                    }
                    cache = new DeepDreamParameterSet { ModelParameter = filterParam, InputParameter = inputParam };
                    return cache;
                }
                return null;
            });
        }
    }
}
