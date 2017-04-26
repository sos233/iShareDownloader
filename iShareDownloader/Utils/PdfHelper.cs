using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iShareDownloader.Utils
{
    /// <summary>
    /// 引用ITextSharp，项目地址 https://github.com/itext/itextsharp
    /// 只针对开源软件免费且DLL文件较大(3.68MB)
    /// </summary>
    public class PdfHelper
    {
        #region 将html转化为PDF的字节流
        /// <summary>
        /// 将html转化为PDF的字节流
        /// </summary>
        /// <param name="html">原始Html</param>
        /// <param name="message">转化信息</param>
        /// <returns>返回PDF的字节流</returns>
        public static byte[] FromHTMLtoPDF(string html, out string message)
        {
            Document document = new Document();
            MemoryStream m = new MemoryStream();
            MemoryStream stream = new MemoryStream();
            byte[] bytes = null;
            try
            {
                PdfWriter writer = PdfWriter.GetInstance(document, m);
                document.Open();
                PdfDestination pdfDest = new PdfDestination(PdfDestination.XYZ, 0, document.PageSize.Height, 1f);
                Encoding charset = Encoding.GetEncoding("gb2312");
                byte[] data = charset.GetBytes(html);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, stream, null, charset, new UnicodeFontFactory());
                PdfAction action = PdfAction.GotoLocalPage(1, pdfDest, writer);
                writer.SetOpenAction(action);
                document.Close();
                bytes = m.GetBuffer();
                message = "100|成功";
            }
            catch (Exception e)
            {
                string err = "Invalid nested tag body found";
                if (e.Message.Contains(err))
                {
                    message = "102|html元素没有闭合标签";
                }
                else
                {
                    message = "102|将html转换为pdf出错！";
                }
            }
            finally
            {
                stream.Close();
                m.Close();
            }
            return bytes;
        }
        #endregion

        #region PDF添加加图片水印
        /// <summary>
        /// PDF添加加图片水印
        /// </summary>
        /// <param name="pdfBytes">原始PDF流</param>
        /// <param name="watermarkeUrl">水印图片地址</param>
        /// <param name="positionX">图片中心与PDF中心重合，正数向上移，负数向下移</param>
        /// <param name="positionY">图片中心与PDF中心重合，正数向右移，负数向左移</param>
        /// <param name="rot">旋转角度，正数向左，负数向右</param>
        /// <returns>返回添加水印后的PDF流</returns>
        public static byte[] SetPDFWatermark(byte[] pdfBytes, string watermarkeUrl, float positionX = 0, float positionY = 0, float rot = 0)
        {
            PdfReader pdfReader = null;
            PdfStamper pdfStamper = null;
            MemoryStream outputPDFStream = new MemoryStream();
            byte[] images = pdfBytes;
            try
            {
                pdfReader = new PdfReader(pdfBytes);
                pdfStamper = new PdfStamper(pdfReader, outputPDFStream);

                Image image = Image.GetInstance(watermarkeUrl);
                //设置透明度，灰色填充
                image.GrayFill = 20;
                image.RotationDegrees = rot;

                //设置水印的位置
                Rectangle psize = pdfReader.GetPageSize(1);
                float width = psize.Width;
                float height = psize.Height;
                image.SetAbsolutePosition((width - image.Width) / 2 + positionX, (height - image.Height) / 2 + positionY);

                int numberOfPages = pdfReader.NumberOfPages;
                for (int i = 1; i <= numberOfPages; i++)
                {
                    PdfContentByte waterMarkContent = pdfStamper.GetUnderContent(i);//内容下层加水印
                    waterMarkContent.AddImage(image);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("PDF添加水印失败!" + ex.Message);
            }
            finally
            {
                if (pdfStamper != null)
                    pdfStamper.Close();
                if (pdfReader != null)
                    pdfReader.Close();
                images = outputPDFStream.GetBuffer();

                if (outputPDFStream != null)
                    outputPDFStream.Close();
            }
            return images;
        }
        #endregion

        #region PDF字体设置
        public class UnicodeFontFactory : FontFactoryImp
        {
            private static readonly string arialFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
              "arialuni.ttf");//arial unicode MS是完整的unicode字型。
            private static readonly string 标宋体Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
              "simsun.ttc,1");//标宋体Path simsun.ttc
            public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color,
              bool cached)
            {
                BaseFont baseFont = BaseFont.CreateFont(标宋体Path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                return new Font(baseFont, size, style, color);
            }
        }
        #endregion
    }
}
