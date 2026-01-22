using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

[Flags]
public enum ProductFlags : byte
{
    None = 0,
    Certificate = 1 << 0,
    Marked = 1 << 1,
    CDNCheck = 1 << 2,
    Fractional = 1 << 3,
    RefusalMarking = 1 << 4,
    RrNotControlOwner = 1 << 5
}

public class ProductData
{

    // Добавляем статическое свойство для пустого объекта
    public static ProductData Empty { get; } = new ProductData(0, "", 0, ProductFlags.None);


    public long Code { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public ProductFlags Flags { get; set; }
    public string Barcode { get; set; }

    public ProductData(long code, string name, decimal price, ProductFlags flags)
    {
        Code = code;
        Name = CompressString(name);
        Price = price;
        Flags = flags;
    }

    public bool isCertificate()
    {
        return (Flags & ProductFlags.Certificate) != 0;
    }

    public bool IsMarked()
    {
        return (Flags & ProductFlags.Marked) != 0;
    }

    public bool IsCDNCheck()
    {
        return (Flags & ProductFlags.CDNCheck) != 0;
    }

    public bool IsFractional()
    {
        return (Flags & ProductFlags.Fractional) != 0;
    }
    public bool IsRefusalMarking()
    {
        return (Flags & ProductFlags.RefusalMarking) != 0;
    }

    public bool RrNotControlOwner()
    {
        return (Flags & ProductFlags.RrNotControlOwner) != 0;
    }

    //    ProductData product = InventoryManager.GetItem(id);
    //if (product.IsEmpty())
    //{
    //// Обработка пустого значения
    //MessageBox.Show("ProductData is empty.");
    //}
    //else
    //{
    //// Обработка непустого значения
    //MessageBox.Show($"ProductData is not empty. Name: {product.GetName()}");
    //}

    //public bool IsEmpty()
    //{
    //    return Code == 0 && string.IsNullOrEmpty(Name) && Price == 0 && Flags == ProductFlags.None;
    //}

    public bool IsEmpty()
    {
        // Проверяем распакованное имя
        return Code == 0 && string.IsNullOrEmpty(GetName()) && Price == 0 && Flags == ProductFlags.None;
    }

    private static string CompressString(string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        using (var memoryStream = new MemoryStream())
        {
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }

    private static string DecompressString(string compressedText)
    {
        byte[] gZipBuffer = Convert.FromBase64String(compressedText);
        using (var memoryStream = new MemoryStream(gZipBuffer))
        {
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (var resultStream = new MemoryStream())
                {
                    gZipStream.CopyTo(resultStream);
                    return Encoding.UTF8.GetString(resultStream.ToArray());
                }
            }
        }
    }

    public string GetName()
    {
        return DecompressString(Name);
    }


}
