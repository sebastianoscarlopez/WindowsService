using System;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JanoService.Service
{
    /// <summary>
    /// Make PDF signed
    /// </summary>
    public class ProcesarPDF
    {
        volatile internal string signedCoords;
        volatile internal string signed;
        volatile internal string pdfPath;
        volatile internal string pdfDestPath;
        volatile private object _lock = new object();
        /// <summary>
        /// This expression must match with data 6 on table AppDistribuidores_DatosAdicionalesTramitaciones
        /// </summary>
        Regex coordsPattern = new Regex(@"(?:[\|]{0,1}(\d+)\:(\d+)\;(\d+)\;(\d+)\;(\d+))+");
        /// <summary>
        /// Simple safe thread. Be carefull this aproach doesn't much too safe.
        /// </summary>
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
        /// <summary>
        /// Matematical relation between image size and max size would be used for signed
        /// </summary>
        /// <param name="height">Height of image</param>
        /// <param name="width">Width of image</param>
        /// <param name="maxW">Maximun width for signed</param>
        /// <param name="maxH">Maximun heigth for signed</param>
        /// <returns></returns>
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
        /// <summary>
        /// Signed
        /// </summary>
        /// <param name="document">PDF object</param>
        /// <param name="pageIdx">Page number</param>
        /// <param name="imgX">Left coordinate</param>
        /// <param name="imgY">Bottom coordinate</param>
        /// <param name="maxW">Maximun width for signed</param>
        /// <param name="maxH">Maximun heigth for signed</param>
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
