﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using TF.Core.TranslationEntities;
using TF.IO;

namespace TFGame.PhoenixWrightTrilogy.Files.SingleLine
{
    public class File : EncryptedBinaryFile
    {
        public File(string path, string changesFolder, System.Text.Encoding encoding) : base(path, changesFolder, encoding)
        {
        }

        protected override IList<Subtitle> GetSubtitles()
        {
            var result = new List<Subtitle>();
            var encryptedData = System.IO.File.ReadAllBytes(Path);
            var data = DecryptData(encryptedData);

            using (var ms = new MemoryStream(data))
            using (var input = new ExtendedBinaryReader(ms, FileEncoding))
            {
                while (input.Position + 2 <= input.Length)
                {
                    var offset = input.Position;
                    var id = input.ReadUInt16();
                    var text = input.ReadString();
                    text = ToHalfWidthChars(text);
                    var subtitle = new Subtitle {Offset = offset, Text = text, Translation = text, Loaded = text};
                    subtitle.PropertyChanged += SubtitlePropertyChanged;
                    result.Add(subtitle);
                }
            }

            LoadChanges(result);

            return result;
        }

        public override void Rebuild(string outputFolder)
        {
            var outputPath = System.IO.Path.Combine(outputFolder, RelativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));

            var subtitles = GetSubtitles();

            var encryptedInputData = System.IO.File.ReadAllBytes(Path);
            var inputData = DecryptData(encryptedInputData);
            byte[] outputData;

            using (var msInput = new MemoryStream(inputData))
            using (var input = new ExtendedBinaryReader(msInput, FileEncoding))
            using (var msOutput = new MemoryStream())
            using (var output = new ExtendedBinaryWriter(msOutput, FileEncoding))
            {
                while (input.Position + 2 <= input.Length)
                {
                    var offset = input.Position;
                    var id = input.ReadUInt16();
                    var inputText = input.ReadString();

                    var subtitle = subtitles.First(x => x.Offset == offset);
                    var outputText = ToFullWidthChars(subtitle.Translation);
                    output.Write(id);
                    output.WriteString(outputText);
                }

                outputData = msOutput.ToArray();
            }

            var encryptedOutputData = EncryptData(outputData);
            System.IO.File.WriteAllBytes(outputPath, encryptedOutputData);
        }
    }
}
