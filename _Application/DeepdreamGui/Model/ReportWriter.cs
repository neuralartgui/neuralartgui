using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows.Forms;
using DeepdreamGui.ViewModel;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// Class to generate HTML Reports.
    /// </summary>
    public class ReportWriter
    {
        private List<ResultImageViewModel> images;
        private string outputDir;
        private string creatorName;

        private string[] TableHeader= {Properties.Resources.param_picture, Properties.Resources.info_serial, Properties.Resources.param_name, Properties.Resources.param_achieved_change, Properties.Resources.param_comment};
        private string reportName;
        private string instanceOutputDir;


        private string OutputDir
        {
            get
            {
                //Check if folder exists, if not create.
                if (!String.IsNullOrEmpty(instanceOutputDir)) return instanceOutputDir;
                instanceOutputDir = Path.Combine(outputDir, DateTime.Now.ToString($"yyyyMMdd-HHmm") + $"_{reportName}");
                if (Directory.Exists(instanceOutputDir)) return instanceOutputDir;
                try
                {
                    Trace.WriteLine("ReportWriter.OutputDir; Try to create dir="+instanceOutputDir);
                    Directory.CreateDirectory(instanceOutputDir);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"ReportWriter.OutputDir; {ex}");
                    //Console.WriteLine(ex);
                    throw ex;
                }

                return instanceOutputDir;
            }
        }

        private string ImageDir
        {
            get
            {
                var outDir = Path.Combine(OutputDir, "img");
                if (Directory.Exists(outDir)) return outDir;
                try
                {
                    Trace.WriteLine("ReportWriter.ImageDir; Dir to create dir="+outDir);
                    Directory.CreateDirectory(outDir);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("ReportWriter.ImageDir; "+ex);
                    //Console.WriteLine(ex);
                    throw ex;
                }

                return outDir;
            }
        }

        /// <summary>
        /// Constructor for Report Writer.
        /// </summary>
        /// <param name="images">Collection of Image to output</param>
        /// <param name="outputDir">Directory to save Report</param>
        /// <param name="creatorName">Creator name</param>
        /// <param name="reportName">Report name</param>
        public ReportWriter(ICollection<ResultImageViewModel> images, string outputDir, string creatorName, string reportName)
        {
            this.images = images.ToList();
            this.outputDir = outputDir;
            this.creatorName = creatorName;
            this.reportName = reportName;
        }

        /// <summary>
        /// Get relative image path to Report's img/ directory
        /// C:/User/.../landscape.jpg to img/landscape.jpg
        /// </summary>
        /// <param name="path">Image Path</param>
        /// <returns>Relative path to img folder</returns>
        private string GetRelativeImagePath(string path)
        {
            if (!File.Exists(path)) return String.Empty;

            var filename = Path.GetFileName(path);
            if(String.IsNullOrEmpty(filename)) return String.Empty;

            return "img/" + filename;
        }


        /// <summary>
        /// Write CSS to Report
        /// </summary>
        /// <param name="writer">Current Writer</param>
        private void GenerateStyle(HtmlTextWriter writer)
        {
            string style = @"
body{
font-family: 'Helvetica', 'Arial', sans-serif;
color:white;
background-color: #252525;
}
table{
margin:auto;
width:100%;
}
tr{
background:rgba(255, 255, 255, 0.05);
margin-top:5px;
}
td{
text-align: center;
}
img{
max-width:300;
max-height:300;
}
    ";


            writer.RenderBeginTag(HtmlTextWriterTag.Style);
            writer.Write(style);
            writer.RenderEndTag();
        }

        private void GenerateFileHeader(HtmlTextWriter writer)
        {
            Trace.WriteLine($"ReportWriter.GenerateFileHeader; Write header Report: {reportName} by: {creatorName}.");
            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            writer.WriteEncodedText($"{Properties.Resources.report_report}: {reportName} {Properties.Resources.report_by}: {creatorName}, {Properties.Resources.report_date}: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
            writer.RenderEndTag();
        }

        private void GenerateTableRow(HtmlTextWriter writer, ResultImageViewModel vm)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            var img = ImageModel.SaveImageWithRandomName(vm.Image, ImageDir);
            img.Wait();
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "resultImage");
            writer.AddAttribute(HtmlTextWriterAttribute.Src, GetRelativeImagePath(img.Result));
            writer.RenderBeginTag(HtmlTextWriterTag.Img);
            writer.RenderEndTag(); //img
            writer.RenderEndTag(); //td

            //IterationNumber
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.WriteEncodedText(vm.Iteration.ToString());
            writer.RenderEndTag();

            //Name
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.WriteEncodedText(vm.Name ?? String.Empty);
            writer.RenderEndTag();

            //SubjectiveVariation
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.WriteEncodedText(vm.SubjectiveVariation.ToString());
            writer.RenderEndTag();

            //Comment
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.WriteEncodedText(vm.Description ?? String.Empty);
            writer.RenderEndTag();


            writer.RenderEndTag(); //Tr
        }

        private void GenerateImageHeader(HtmlTextWriter writer, ResultImageViewModel firstInGroup)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "imageHeader");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.RenderBeginTag(HtmlTextWriterTag.H2);
            writer.WriteEncodedText(firstInGroup.ModeName + " " + Properties.Resources.report_series);
            writer.RenderEndTag();

            writer.RenderBeginTag(HtmlTextWriterTag.H3);
            writer.WriteEncodedText(Properties.Resources.report_parameter);
            writer.RenderEndTag();


            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            

            if (firstInGroup.Mode == Modes.Dream)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.param_source_picture);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.param_model);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.param_octave);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.param_intensity);
                writer.RenderEndTag();

                writer.RenderEndTag(); //tr
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                //Img
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var img = ImageModel.SaveImageWithRandomName(firstInGroup.OriginalImage, ImageDir);
                img.Wait();
                writer.AddAttribute(HtmlTextWriterAttribute.Src, GetRelativeImagePath(img.Result));
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag(); //img
                writer.RenderEndTag();  //td

                //Model
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.WriteEncodedText(firstInGroup.SelectedModelParameter.Name + " / " + firstInGroup.SelectedModelParameter.Key);
                writer.RenderEndTag(); //img

                //Octave
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.WriteEncodedText(firstInGroup.Octave.ToString());
                writer.RenderEndTag(); //img

                //Intensity
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.WriteEncodedText(firstInGroup.Intensity.ToString(CultureInfo.CurrentCulture));
                writer.RenderEndTag(); //img

                writer.RenderEndTag(); //Tr
               
            }
            else
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.param_source_picture);
                writer.RenderEndTag();

                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(Properties.Resources.info_style_image);
                writer.RenderEndTag();

                writer.RenderEndTag(); //Tr
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                
                //Img
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                var img = ImageModel.SaveImageWithRandomName(firstInGroup.OriginalImage, ImageDir);
                img.Wait();
                writer.AddAttribute(HtmlTextWriterAttribute.Src, GetRelativeImagePath(img.Result));
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag(); //img

                //Style Img
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                img = ImageModel.SaveImageWithRandomName(firstInGroup.StyleImage, ImageDir);
                img.Wait();
                writer.AddAttribute(HtmlTextWriterAttribute.Src, GetRelativeImagePath(img.Result));
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag(); //img
                writer.RenderEndTag(); //td


                writer.RenderEndTag(); //Tr
            }
            writer.RenderEndTag(); //Table

          
            writer.RenderBeginTag(HtmlTextWriterTag.H3);
            writer.WriteEncodedText(Properties.Resources.report_results);
            writer.RenderEndTag();

            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            foreach (var s in TableHeader)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Th);
                writer.WriteEncodedText(s ?? "--");
                writer.RenderEndTag();
            }

            writer.RenderEndTag(); //tr
          
        }

        /// <summary>
        /// Generate Report.
        /// </summary>
        /// <returns>Return runing generator task</returns>
        public Task Generate()
        {
            return  Task.Factory.StartNew(() =>
            {

               // outputDir = @"D:\tmp";
                //var writer = new StringWriter();

                using (StreamWriter writer = new StreamWriter(Path.Combine(OutputDir, "report.html")))
                {
                    Trace.WriteLine($"ReportWriter.Generate; Generate html-report to={outputDir}");
                    var htmlWriter = new HtmlTextWriter(writer);

                    GenerateStyle(htmlWriter);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);
                    GenerateFileHeader(htmlWriter);


                    //Get base Images
                    var grouped = images.GroupBy(img => img.SeriesId).ToList();

                    foreach (var group in grouped)
                    {
                        GenerateImageHeader(htmlWriter, group.First());
                        foreach (var resultImageViewModel in @group)
                        {
                            GenerateTableRow(htmlWriter,resultImageViewModel);
                        }
                        htmlWriter.RenderEndTag(); //table
                    }


                    htmlWriter.RenderEndTag(); //body
                    htmlWriter.RenderEndTag(); //html
                    Trace.WriteLine($"ReportWriter.Generate; Finish writing;");
                    writer.Flush();
                }
            });
        }
    }
}
