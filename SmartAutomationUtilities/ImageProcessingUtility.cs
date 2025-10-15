using System.Data;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Tesseract;

namespace SmartAutomationUtilities;

/// <summary>
/// Concept and actual implementation of the image processing lib to generate text from images especially from canvas UI
/// It was also used to extract color and coordinates for the image elements to support interaction by any UI automation
/// library
/// </summary>
public class ImageProcessingUtil
{
    /// <summary>
    /// Method mainly deals with loading approximate words or sentences with their coordinates in a dataTable
    /// </summary>
    /// <param name="imageBytes">Image bytes is a two halves of screenshots of the whole canvas visible in the UI</param>
    /// <param name="threshold"></param>
    /// <param name="scaleFactor">Scale factor needs to be calculated using javascript</param>
    /// <param name="xL"></param>
    /// <param name="yT"></param>
    /// <returns></returns>
    public static DataTable LoadEntityInDataTableFormat(byte[][] imageBytes, int[] threshold)
    {
        //For Dimension of 1500 X 1080 (Set it to remove that section which is unwanted in the screenshot)
        //var yOffset = 545;

        //DataTable to store sentence and their trailing entities
        DataTable dataTable = new("canvasTableContext");
        dataTable.Columns.Add("ApproximateItem", typeof(string));
        dataTable.Columns.Add("Coordinates", typeof(Rectangle));

        //Get working directory and set the training data location according to requirement
        var workingDirectory = Directory.GetCurrentDirectory();
        var path = string.Concat(workingDirectory, "..\\tessdata");

        //Initialize Tesseract OCR engine
        using var engine = new TesseractEngine(path, "eng", EngineMode.Default);

        //Loop the segments of screenshot provided
        for (int i = 0; i < imageBytes.Length; i++)
        {
            //Raw colored image
            Mat rawImg = new();
            CvInvoke.Imdecode(imageBytes[i], ImreadModes.Color, rawImg);

            //Grey scale image
            Mat gray = new();
            CvInvoke.CvtColor(rawImg, gray, ColorConversion.Bgr2Gray);

            //Equalize the contrast of the image or balancing the contrast of the image provided
            CvInvoke.EqualizeHist(gray, gray);

            /*Adaptive thresholding algorithm application with 255 as the max value with best combination provided
            * i.e. Gaussian and Binary adaptive thresholding parameters
            * Setting the blocksize and cancelling parameter with threshold array values
            */
            Mat thresh = new();
            CvInvoke.AdaptiveThreshold(gray, thresh, 255, AdaptiveThresholdType.GaussianC,
            ThresholdType.Binary, threshold[0], threshold[1]);

            var page = engine.Process(PixConverter.ToPix(thresh.ToBitmap()), Tesseract.PageSegMode.Auto);

            var iterator = page.GetIterator();
            iterator.Begin();

            //Initialize the row in dataTable
            DataRow dataRow = dataTable.NewRow();

            do
            {
                var word = iterator.GetText(Tesseract.PageIteratorLevel.Word);

                //If confidence is less than 80%, then process the extracted word further
                if (word.Trim() != string.Empty && iterator.GetConfidence(Tesseract.PageIteratorLevel.Word) < 80f)
                {
                    iterator.TryGetBoundingBox(Tesseract.PageIteratorLevel.Word, out var rect);

                    Rectangle roi = new(rect.X1, rect.Y1, rect.Width, rect.Height);

                    //Adjustment with expansion
                    Rectangle expandedROI = new(Math.Max(0, rect.X1 - 2), Math.Max(0, rect.Y1 - 2),
                    rect.Width + 2, rect.Height + 2);

                    Mat roiImg = new(rawImg, expandedROI);

                    word = GetCorrectedText(roiImg);
                }

                //Post processing low confident words, follow the below block else ignore
                if (word.Trim() != string.Empty)
                {
                    iterator.TryGetBoundingBox(Tesseract.PageIteratorLevel.Word, out var vrect);

                    Rectangle roi = new(vrect.X1, vrect.Y1, vrect.Width, vrect.Height);

                    dataRow["ApproximateItem"] = word.Trim();
                    dataRow["Coordinates"] = roi;
                    dataTable.Rows.Add(dataRow);

                    dataRow = dataTable.NewRow(); //Create a new row for further entries
                }

            } while (iterator.Next(Tesseract.PageIteratorLevel.Word));
        }
        return dataTable;
    }

    public static Dictionary<string, List<dynamic>> GetWordBasedOnColorProcessing(byte[][] imageBytes, int[] threshold,
    double scaleFactor, float xL, float yT)
    {

    }

    private static string GetCorrectedText(Mat roiImg)
    {

        //Prepare Tesseract data path
        var workingDirectory = Directory.GetCurrentDirectory();
        var path = string.Concat(workingDirectory, "..\\tessdata");

        //Initialize Tesseract OCR engine
        var engine = new TesseractEngine(path, "Eng", EngineMode.LstmOnly);

        Mat grayImg = new();
        CvInvoke.CvtColor(roiImg, grayImg, ColorConversion.Bgr2Gray);

        CvInvoke.EqualizeHist(grayImg, grayImg);

        var page = engine.Process(PixConverter.ToPix(grayImg.ToBitmap()), PageSegMode.SingleWord);
        var iterator = page.GetIterator();

        var text = string.Empty;
        iterator.Begin();
        do
        {
            text = iterator.GetText(PageIteratorLevel.Word);
        } while (iterator.Next(PageIteratorLevel.Word));

        return !string.IsNullOrWhiteSpace(text) ? text : "";

    }
}
