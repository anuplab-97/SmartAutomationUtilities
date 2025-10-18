
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Rendering.Skia;

namespace SmartAutomationUtilities;


/// <summary>
/// This class deals with the pdf based helper methods to identify annotations, color of annotations, text and nearest
/// annotation found under a text.
/// Limitation found so far - Relatively smaller text is not getting recognized
/// </summary>
public class PdfProcessingUtil
{

    public static byte[] ReadPDFBytes(string path)
    {
        return File.ReadAllBytes(path);
    }


    /// <summary>
    /// Based on the annotation type we return the list of annotation objects based on page numbers
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private static Dictionary<int, List<Annotation>> ReadAnnotations(byte[] bytes, AnnotationType type)
    {
        Dictionary<int, List<Annotation>> pageBasedAnnotation = [];

        using PdfDocument document = PdfDocument.Open(bytes);

        foreach (Page page in document.GetPages())
        {
            List<Annotation> annotationList = [];
            foreach (Annotation annotation in page.GetAnnotations())
            {
                annotationList.Add(annotation);
            }

            List<Annotation> stampAnnotationTypeList = [];
            if (type == AnnotationType.Stamp)
                stampAnnotationTypeList = annotationList.FindAll((e) => e.Type == AnnotationType.Stamp);
            else if (type == AnnotationType.FreeText || type == AnnotationType.Line)
                stampAnnotationTypeList = annotationList.FindAll((e) => e.Type == AnnotationType.FreeText || e.Type == AnnotationType.Line);

            pageBasedAnnotation.Add(page.Number, stampAnnotationTypeList);
        }

        return pageBasedAnnotation;
    }

    /// <summary>
    /// Get nearest word or object near the annotations
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="annotationType"></param>
    /// <returns></returns>
    public static Dictionary<int, Dictionary<string, List<string>>> GetNearestWords(byte[] bytes, AnnotationType annotationType)
    {
        Dictionary<int, List<Annotation>> pageBasedAnnotation = ReadAnnotations(bytes, annotationType);

        Dictionary<int, Dictionary<string, List<string>>> pageBasedNearestWord = [];
        using PdfDocument document = PdfDocument.Open(bytes);

        foreach (Page page in document.GetPages())
        {
            Dictionary<string, List<string>> annotationBasedNearestWords = [];
            var count = 0;
            foreach (var listAnnotationItem in pageBasedAnnotation[page.Number])
            {
                var words = page.GetWords().Select(word => new
                {
                    Text = word.Text,
                    BoundingBox = word.BoundingBox,
                    Distance = CalculateDistance(word.BoundingBox, listAnnotationItem.Rectangle.TopLeft.X, listAnnotationItem.Rectangle.TopLeft.Y)
                });

                //Refer first three nearest elements
                var nearestWords = words.OrderBy(w => w.Distance).Take(3).Select(e => e.Text).ToList();

                //Can include nearest words check if needed

                annotationBasedNearestWords.Add($"{page.Number}_{++count}", nearestWords);
            }

            pageBasedNearestWord.Add(page.Number, annotationBasedNearestWords);
        }

        return pageBasedNearestWord;
    }

    public static string DetermineAnnotationColorOfNearestWord(byte[] bytes, float scalingFactor, string expectedWord, AnnotationType annotationType)
    {
        Dictionary<int, List<Annotation>> pageBasedAnnotation = ReadAnnotations(bytes, annotationType);

        Dictionary<int, Dictionary<string, string>> pageBasedColorList = [];

        using PdfDocument document = PdfDocument.Open(bytes);

        document.AddSkiaPageFactory();

        for (int pageIndex = 1; pageIndex <= document.NumberOfPages; pageIndex++)
        {
            pageBasedColorList.Add(pageIndex, []);

            using MemoryStream stream = document.GetPageAsPng(pageIndex, scalingFactor);
            byte[] streamBytes = stream.ToArray();

            Mat img = new();
            CvInvoke.Imdecode(streamBytes, ImreadModes.Color, img);

            Page page = document.GetPage(pageIndex);

            foreach (var listAnnotation in pageBasedAnnotation[pageIndex])
            {
                var words = page.GetWords().Select(word => new
                {
                    Text = word.Text,
                    BoundingBox = word.BoundingBox,
                    Distance = CalculateDistance(word.BoundingBox, listAnnotation.Rectangle.TopLeft.X, listAnnotation.Rectangle.TopLeft.Y)
                });

                var nearestWords = words.OrderBy(w => w.Distance).Take(3).Select(w => w.Text).ToList();

                if (nearestWords.Contains(expectedWord))
                {
                    int adjustedX = (int)(listAnnotation.Rectangle.TopLeft.X * scalingFactor);
                    int adjustedY = (int)((img.Height - listAnnotation.Rectangle.TopLeft.Y) * scalingFactor);

                    Rectangle annotationRect = new(
                    adjustedX,
                    adjustedY,
                    (int)(listAnnotation.Rectangle.Width * scalingFactor),
                    (int)(listAnnotation.Rectangle.Height * scalingFactor)
                    );

                    //Extract the ROI
                    Mat roiImg = new(img, annotationRect);

                    Mat hsvRoiImg = new();
                    CvInvoke.CvtColor(roiImg, hsvRoiImg, ColorConversion.Bgr2Hsv);

                    MCvScalar avgColor = CvInvoke.Mean(hsvRoiImg);
                    return ""; //Determine the color using own scale
                }
            }
        }

        return "";
    }


    public static Dictionary<int, Dictionary<string, string>> DetermineAnnotationContent(byte[] bytes, AnnotationType annotationType)
    {

        Dictionary<int, Dictionary<string, string>> pageBasedAnnotationContentList = [];

        using PdfDocument document = PdfDocument.Open(bytes);

        foreach (Page page in document.GetPages())
        {
            pageBasedAnnotationContentList.Add(page.Number, []);
            var count = 0;
            foreach (Annotation annot in page.GetAnnotations())
            {
                count++;
                if (annot.Content != null)
                    pageBasedAnnotationContentList[page.Number].Add($"{page.Number}_{count}", annot.Content.Trim());
            }
        }
        return pageBasedAnnotationContentList;
    }

    private static double CalculateDistance(PdfRectangle rect, double x, double y)
    {
        //Calculate the centre of word's bounding box
        double centerX = (rect.Left + rect.Right) / 2;
        double centerY = (rect.Top + rect.Bottom) / 2;

        //Calculate Euclidean distance from the center of the bounding box to the target coordinates
        return Math.Sqrt(Math.Pow(centerX - x, 2) + Math.Pow(centerY - y, 2));
    }


}