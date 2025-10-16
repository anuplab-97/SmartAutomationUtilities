
using System.Drawing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

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
    /// Get nearest word or object near the annotation
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

                annotationBasedNearestWords.Add($"{page.Number}_{++count}", nearestWords);
            }

            pageBasedNearestWord.Add(page.Number, annotationBasedNearestWords);
        }

        return pageBasedNearestWord;
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