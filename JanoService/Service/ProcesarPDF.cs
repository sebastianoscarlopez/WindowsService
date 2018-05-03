using System;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JanoService.Service
{
    public class ProcesarPDF
    {
        volatile internal string signedCoords;
        volatile internal string signed;
        volatile internal string pdfPath;
        volatile internal string pdfDestPath;
        volatile private object _lock = new object();
        Regex coordsPattern = new Regex(@"(?:[\|]{0,1}(\d+)\:\[(\d+)\;(\d+)\;(\d+)\;(\d+)\])+");
        internal void procesar()
        {
            lock (_lock)
            {
                var reader = new PdfReader(pdfPath);
                var writer = new PdfWriter(this.pdfDestPath);
                var pdfDest = new PdfDocument(reader, writer);
                var document = new Document(pdfDest);
                var matches = coordsPattern.Matches(signedCoords);
                var totalSigned = matches[0].Groups[1].Captures.Count;
                for(var idx = 0; idx < totalSigned; idx++)
                {
                    firmar(document,
                        int.Parse(matches[0].Groups[1].Captures[idx].Value), // Page
                        int.Parse(matches[0].Groups[2].Captures[idx].Value), // x
                        int.Parse(matches[0].Groups[3].Captures[idx].Value), // y
                        int.Parse(matches[0].Groups[4].Captures[idx].Value), // h
                        int.Parse(matches[0].Groups[5].Captures[idx].Value)  // w
                        );
                }
                document.Close();
            }
        }
        private float calcularAncho(int height, int width, int maxH, int maxW)
        {
            var relation = (float)maxH / maxW;
            var relationImg = (float)height / width;
            var scale = relationImg > relation
                ? (float)maxH / height
                : (float)maxW / width;
            var result = width * scale;
            return result;
        }
        void firmar(Document document, int pageIdx, int imgX, int imgY, int maxW, int maxH)
        {
            var image = new Image(ImageDataFactory.Create(signed));
            var imgFirma = System.Drawing.Image.FromFile(signed);

            image.SetFixedPosition(
                pageNumber: pageIdx, left: imgX, bottom: imgY, width:
                calcularAncho(imgFirma.Height, imgFirma.Width, maxW, maxH));
            document.Add(image);
        }
    }
}
