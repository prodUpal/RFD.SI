using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace NPFGEO
{
  public static class DataContractHelper<T>
  {
    public static void Serialize(
      T obj,
      Stream stream,
      Encoding encoding,
      bool references,
      IEnumerable<Type> knownTypes = null)
    {
      DataContractSerializerSettings settings = new DataContractSerializerSettings();
      settings.PreserveObjectReferences = references;
      settings.MaxItemsInObjectGraph = int.MaxValue;
      if (knownTypes != null)
        settings.KnownTypes = knownTypes;
      DataContractSerializer contractSerializer = new DataContractSerializer(typeof (T), settings);
      XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings()
      {
        Indent = true,
        CheckCharacters = false,
        NewLineHandling = NewLineHandling.Entitize,
        Encoding = encoding
      });
      contractSerializer.WriteObject(writer, (object) obj);
      writer.Flush();
    }

    public static void Serialize(T obj, Stream stream, IEnumerable<Type> knownTypes = null)
    {
      DataContractHelper<T>.Serialize(obj, stream, Encoding.ASCII, true, knownTypes);
    }

    public static void Serialize2(T obj, Stream stream, IEnumerable<Type> knownTypes = null)
    {
      DataContractHelper<T>.Serialize(obj, stream, Encoding.UTF8, true, knownTypes);
    }

    public static void SerializeBinary(T obj, Stream stream)
    {
      using (XmlDictionaryWriter binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
        new DataContractSerializer(typeof (T)).WriteObject(binaryWriter, (object) obj);
    }

    public static T Deserialize(Stream stream, IEnumerable<Type> knownTypes = null)
    {
      /*DataContractSerializerSettings settings1 = new DataContractSerializerSettings();
      if (knownTypes != null)
        settings1.KnownTypes = knownTypes;
      DataContractSerializer contractSerializer = new DataContractSerializer(typeof (T), settings1);
      XmlReaderSettings settings2 = new XmlReaderSettings();
      settings2.CheckCharacters = false;
      T obj = default (T);
      using (XmlReader reader = XmlReader.Create(stream, settings2))
      {
        obj = (T) contractSerializer.ReadObject(reader);
        reader.Close();
      }
      stream.Close();
      return obj;*/
      /*if (stream == null || stream.Length == 0)
      {
        throw new ArgumentException("Stream is null or empty.");
      }

      try
      {
        // Настройка сериализатора
        DataContractSerializerSettings serializerSettings = new DataContractSerializerSettings
        {
          KnownTypes = knownTypes
        };
        DataContractSerializer contractSerializer = new DataContractSerializer(typeof(T), serializerSettings);

        // Настройка XmlReader
        XmlReaderSettings readerSettings = new XmlReaderSettings
        {
          CheckCharacters = false
        };

        // Десериализация
        using (XmlReader reader = XmlReader.Create(stream, readerSettings))
        {
          T obj = (T)contractSerializer.ReadObject(reader);
          return obj;
        }
      }
      catch (SerializationException ex)
      {
        Console.WriteLine("SerializationException: " + ex.Message);
        throw;
      }
      catch (XmlException ex)
      {
        Console.WriteLine("XmlException: " + ex.Message);
        throw;
      }
      catch (Exception ex)
      {
        Console.WriteLine("General exception: " + ex.Message);
        throw;
      }*/
      
      if (stream == null || stream.Length == 0)
      {
        throw new ArgumentException("Stream is null or empty.");
      }

      // Логирование содержимого потока
      stream.Position = 0;
      using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
      {
        string content = reader.ReadToEnd();
        Console.WriteLine("Stream content: " + content);

        // Проверка содержимого
        if (string.IsNullOrWhiteSpace(content))
        {
          throw new InvalidOperationException("Stream content is empty or whitespace.");
        }

        // Проверка на валидный XML
        if (!content.TrimStart().StartsWith("<"))
        {
          throw new InvalidOperationException("Stream does not contain valid XML.");
        }

        // Очищаем поток
        stream.Position = 0;
      }

      try
      {
        // Настройка DataContractSerializer
        DataContractSerializerSettings serializerSettings = new DataContractSerializerSettings
        {
          KnownTypes = knownTypes
        };
        DataContractSerializer contractSerializer = new DataContractSerializer(typeof(T), serializerSettings);

        // Настройка XmlReader
        XmlReaderSettings readerSettings = new XmlReaderSettings
        {
          CheckCharacters = true // Проверка символов
        };

        // Десериализация
        using (XmlReader xmlReader = XmlReader.Create(stream, readerSettings))
        {
          return (T)contractSerializer.ReadObject(xmlReader);
        }
      }
      catch (SerializationException ex)
      {
        Console.WriteLine("Serialization exception: " + ex.Message);
        throw;
      }
      catch (XmlException ex)
      {
        Console.WriteLine("XML exception: " + ex.Message);
        throw;
      }
      catch (Exception ex)
      {
        Console.WriteLine("General exception: " + ex.Message);
        throw;
      }
    }

    public static T DeserializeBinary(Stream stream)
    {
      DataContractSerializer contractSerializer = new DataContractSerializer(typeof (T));
      T obj = default (T);
      using (XmlDictionaryReader binaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
        obj = (T) contractSerializer.ReadObject(binaryReader);
      return obj;
    }
  }
}
