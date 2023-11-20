using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text.Json;
using System.IO;


namespace task
{
    class Program
    {
        static string ConvertToPostfix(string expression)
        {
            string postfix = "";
            Stack<char> stack = new Stack<char>();

            foreach (char c in expression)
            {
                if (char.IsDigit(c))
                {
                    postfix += c;
                }
                else if (IsOperator(c))
                {
                    while (stack.Count > 0 && IsOperator(stack.Peek()) && OperatorPriority(c) <= OperatorPriority(stack.Peek()))
                    {
                        postfix += stack.Pop();
                    }
                    stack.Push(c);
                }
                else if (c == '(')
                {
                    stack.Push(c);
                }
                else if (c == ')')
                {
                    while (stack.Count > 0 && stack.Peek() != '(')
                    {
                        postfix += stack.Pop();
                    }
                    stack.Pop();
                }
            }

            while (stack.Count > 0)
            {
                postfix += stack.Pop();
            }

            return postfix;
        }

        static double EvaluatePostfix(string postfixExpression)
        {
            Stack<double> stack = new Stack<double>();

            foreach (char c in postfixExpression)
            {
                if (char.IsDigit(c))
                {
                    stack.Push(double.Parse(c.ToString()));
                }
                else if (IsOperator(c))
                {
                    double operand2 = stack.Pop();
                    double operand1 = stack.Pop();
                    double result = PerformOperation(c, operand1, operand2);
                    stack.Push(result);
                }
            }

            return stack.Pop();
        }

        static bool IsOperator(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/';
        }

        static int OperatorPriority(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                    return 1;
                case '*':
                case '/':
                    return 2;
                default:
                    return 0;
            }
        }

        static double PerformOperation(char operation, double operand1, double operand2)
        {
            switch (operation)
            {
                case '+':
                    return operand1 + operand2;
                case '-':
                    return operand1 - operand2;
                case '*':
                    return operand1 * operand2;
                case '/':
                    return operand1 / operand2;
                default:
                    return 0;
            }
        }

        static void readArchive(string entryName, string archivePath, string entryName2)
        {
            //string archivePath = "archive.zip";
            //string entryName = "test2.txt";
            //string entryName2 = "output2.txt";
            int i = 1;

            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                ZipArchiveEntry entry2 = archive.GetEntry(entryName2);

                if (entry != null)
                {
                    
                    using (StreamWriter writer = new StreamWriter(entry2.Open()))
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            while (!reader.EndOfStream)
                            {
                                string expression = reader.ReadLine();
                                string postfixExpression = ConvertToPostfix(expression);
                                double result = EvaluatePostfix(postfixExpression);
                                writer.WriteLine($"{i}. {result}");
                                Console.WriteLine($"{i}. {result}\n");
                                i++;
                            }
                        }
                    }
                }
            }
        }

        static void unzip(string zipFilePath, string entryName)
        {
            int i = 1;
            string extractPath = "C:\\Users\\User\\source\\repos\\taskPP\\taskPP\\bin\\Debug\\net6.0\\extracted";

            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            string textFilePath = Path.Combine(extractPath, entryName);

            using (StreamReader f = new StreamReader(textFilePath))
            {
                while (!f.EndOfStream)
                {
                    string expression = f.ReadLine();
                    string postfixExpression = ConvertToPostfix(expression);
                    double result = EvaluatePostfix(postfixExpression);
                    Console.WriteLine($"{i}. {result}\n");
                    i++;
                }
            }
        }

        static void archive(string zipFile)
        {
            string extractPath = "C:\\Users\\User\\source\\repos\\taskPP\\taskPP\\bin\\Debug\\net6.0\\extracted";
            ZipFile.CreateFromDirectory(extractPath, zipFile);
        }

        static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        static void EncryptFile(string filePath, string password, string outputFilePath)
        {
            using (FileStream inputFileStream = File.OpenRead(filePath))
            using (FileStream outputFileStream = File.Create(outputFilePath))
            {
                byte[] salt = GenerateSalt();

                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Padding = PaddingMode.PKCS7;

                    var key = new Rfc2898DeriveBytes(password, salt, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);

                    outputFileStream.Write(salt, 0, salt.Length);

                    using (var cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[1024];
                        int read;
                        while ((read = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cryptoStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        static void DecryptFile(string filePath, string password, string outputFilePath)
        {
            using (FileStream inputFileStream = File.OpenRead(filePath))
            using (FileStream outputFileStream = File.Create(outputFilePath))
            {
                byte[] salt = new byte[16];
                inputFileStream.Read(salt, 0, salt.Length);

                using (RijndaelManaged aes = new RijndaelManaged())
                {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Padding = PaddingMode.PKCS7;

                    var key = new Rfc2898DeriveBytes(password, salt, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);

                    using (var cryptoStream = new CryptoStream(inputFileStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        byte[] buffer = new byte[1024];
                        int read;
                        while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputFileStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        static void xmlread (string xmlfileName, List<double> res)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xmlfileName);

            XmlElement? xRoot = xDoc.DocumentElement;
            if (xRoot != null)
            {
                foreach (XmlElement xnode in xRoot)
                {
                    XmlNode? attr = xnode.Attributes.GetNamedItem("exp");
                    string expression = attr.OuterXml;
                    string postfixExpression = ConvertToPostfix(expression);
                    double result = EvaluatePostfix(postfixExpression);
                    res.Add(result);
                }
            }
        }

        static void xmlwrite (List<double> res)
        {
            XmlWriter xmlWriter = XmlWriter.Create("result.xml");

            int i = 0;
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("result");
            while (i < res.Count)
            {
                xmlWriter.WriteStartElement("res");
                xmlWriter.WriteAttributeString("answer", res[i].ToString());
                xmlWriter.WriteEndElement();
                i++;
            }

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        static void jsonread(string xmlfileName, List<double> res)
        {
            string json = File.ReadAllText("file.json");
            JObject o = JObject.Parse(json);
            using (StreamReader sr = new StreamReader("file.json"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    dynamic d = JsonConvert.DeserializeObject(line);

                }
            }
        }






        static void Main(string[] args)
        {
            Console.WriteLine("is the json file? (true/false)");
            string tr3 = Console.ReadLine();

            if (tr3 == "true")
            {
                Console.WriteLine("enter the json file name");
                string archivePath = Console.ReadLine();

                List<double> res = new List<double>();

                jsonread("user.json", res);

                for (int j = 0; j < res.Count; j++)
                {
                    Console.WriteLine(res[j]);
                }

                //Console.WriteLine("write to an xml file? (true/false)");
                //string tru = Console.ReadLine();

                //if (tru == "true")
                //    xmlwrite(res);

            }


            Console.WriteLine("is the xml file? (true/false)");
            string tr = Console.ReadLine();

            if (tr == "true")
            {
                Console.WriteLine("enter the xml file name");
                string archivePath = Console.ReadLine();

                List<double> res = new List<double>();

                xmlread("expression.xml", res);

                Console.WriteLine("write to an xml file? (true/false)");
                string tru = Console.ReadLine();

                if (tru == "true")
                    xmlwrite(res);

            }

            StreamReader f = new StreamReader("test.txt");
            StreamWriter f1 = new StreamWriter("output.txt");
            int i = 1;
            while (!f.EndOfStream)
            {
                string expression = f.ReadLine();
                string postfixExpression = ConvertToPostfix(expression);
                double result = EvaluatePostfix(postfixExpression);
                f1.Write($"{i}. {result}\n");
                i++;
            }
            f1.Close();

            Console.WriteLine("is the file archived? (true/false)");
            string tr2 = Console.ReadLine();

            if (tr2 == "true")
            {
                Console.WriteLine("enter the file name");
                string archivePath = Console.ReadLine();
                Console.WriteLine("enter archive name");
                string entryName = Console.ReadLine();
                Console.WriteLine("enter the file you want to save to");
                string entryName2 = Console.ReadLine();
                readArchive(archivePath, entryName, entryName2);
            }

            Console.WriteLine("unzip? (true/false)");
            string tr1 = Console.ReadLine();

            if (tr1 == "true")
            {
                Console.WriteLine("enter archive name");
                string zipFilePath = Console.ReadLine();
                Console.WriteLine("enter the file name");
                string entryName = Console.ReadLine();
                unzip(zipFilePath, entryName);

                Console.WriteLine("archive the file? (yes/no)");
                string read = Console.ReadLine();

                if (read == "yes")
                {
                    string NameArc = Console.ReadLine();
                    Console.WriteLine("enter archive name");
                    archive(NameArc);
                }
            }

            Console.WriteLine("encrypt the file? (yes/no)");
            string encr = Console.ReadLine();

            if (encr == "yes")
            {
                Console.WriteLine("enter the name of the file that you want to encrypt");
                string filePath = Console.ReadLine();
                Console.WriteLine("enter password");
                string password = Console.ReadLine();
                Console.WriteLine("enter output file name");
                string outputFilePath = Console.ReadLine();
                EncryptFile(filePath, password, outputFilePath);
            }

            Console.WriteLine("decrypt the file? (yes/no)");
            string decr = Console.ReadLine();

            if (encr == "yes")
            {
                Console.WriteLine("enter the name of the file that you want to decrypt");
                string filePath = Console.ReadLine();
                Console.WriteLine("enter password");
                string password = Console.ReadLine();
                Console.WriteLine("enter output file name");
                string outputFilePath = Console.ReadLine();
                DecryptFile(filePath, password, outputFilePath);
            }
        }
    }
}

